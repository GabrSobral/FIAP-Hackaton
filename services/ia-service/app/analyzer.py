"""
Diagram analyzer backed by a locally-loaded Qwen2-VL vision-language model.

Supported models (set MODEL_ID env var):
  Qwen/Qwen2-VL-2B-Instruct  – recommended default (~4 GB, CPU/GPU)
  Qwen/Qwen2-VL-7B-Instruct  – higher quality  (~14 GB, GPU recommended)
"""

import json
import logging
import re
import time
from io import BytesIO
from threading import Lock

import torch
from PIL import Image
from transformers import GenerationConfig, Qwen2VLForConditionalGeneration, Qwen2VLProcessor

from .config import settings

logger = logging.getLogger(__name__)

# ─── Prompt ───────────────────────────────────────────────────────────────────

_PROMPT = """\
You are a senior software architect reviewing an architecture diagram.
Analyse the diagram carefully and return ONLY a valid JSON object with \
exactly these four keys:

{
  "components": "<identify every component, service, database, queue, API \
gateway, load balancer, cache, and their relationships>",
  "risks": "<list architectural risks: single points of failure, missing \
resilience patterns, security concerns, scalability bottlenecks, \
tight coupling>",
  "recommendations": "<concrete, actionable improvements: patterns to adopt, \
technologies to add, refactors to consider, best practices>",
  "feedback": "<overall expert assessment: architecture maturity level, \
main strengths, and top concern in 2-4 sentences>"
}

Return ONLY the JSON object. No markdown fences, no preamble, no extra text.\
"""


# ─── Analyzer ─────────────────────────────────────────────────────────────────

class DiagramAnalyzer:
    def __init__(self) -> None:
        self._model: Qwen2VLForConditionalGeneration | None = None
        self._processor: AutoProcessor | None = None
        self._lock = Lock()
        self._loaded = False

    @property
    def model_id(self) -> str:
        return settings.model_id

    @property
    def is_loaded(self) -> bool:
        return self._loaded

    # ── Loading ───────────────────────────────────────────────────────────────

    def ensure_loaded(self) -> None:
        """Load the model on first use (thread-safe, idempotent)."""
        if self._loaded:
            return
        with self._lock:
            if self._loaded:
                return
            self._load()

    def _load(self) -> None:
        logger.info("Loading model %s …", settings.model_id)
        t0 = time.time()

        kwargs: dict = {
            # device_map="auto" lets accelerate distribute layers across GPU/CPU/disk
            # automatically, which is essential on machines without enough free RAM to
            # hold all weights at once.
            "device_map": settings.device,
            # float16 halves the memory footprint vs float32 (~4 GB for 2B params)
            # and is safe for inference on both CPU and CUDA.
            "dtype": torch.float16,
            # Load weights one shard at a time instead of all at once, reducing the
            # peak RAM spike during model initialisation.
            "low_cpu_mem_usage": True,
        }

        if settings.load_in_4bit:
            from transformers import BitsAndBytesConfig
            kwargs["quantization_config"] = BitsAndBytesConfig(load_in_4bit=True)
            kwargs.pop("dtype", None)  # 4-bit config sets its own dtype

        self._model = Qwen2VLForConditionalGeneration.from_pretrained(
            settings.model_id, **kwargs
        )
        # use_fast=False keeps the stable slow processor and silences the
        # "breaking change" warning introduced when fast processors became default.
        self._processor = Qwen2VLProcessor.from_pretrained(
            settings.model_id, use_fast=False
        )
        self._loaded = True
        logger.info("Model loaded in %.1f s.", time.time() - t0)

    # ── Inference ─────────────────────────────────────────────────────────────

    def analyze(self, image_data: bytes) -> dict:
        """
        Run the VLM over the image bytes and return a dict with keys:
        components, risks, recommendations, feedback, model_used, processing_time_ms
        """
        self.ensure_loaded()
        t0 = time.time()

        image = Image.open(BytesIO(image_data)).convert("RGB")

        messages = [
            {
                "role": "user",
                "content": [
                    {"type": "image", "image": image},
                    {"type": "text", "text": _PROMPT},
                ],
            }
        ]

        # process_vision_info handles PIL Images, URLs and file paths
        try:
            from qwen_vl_utils import process_vision_info
            image_inputs, video_inputs = process_vision_info(messages)
        except ImportError:
            image_inputs = [image]
            video_inputs = None

        text = self._processor.apply_chat_template(  # type: ignore[union-attr]
            messages, tokenize=False, add_generation_prompt=True
        )

        proc_kwargs: dict = dict(
            text=[text],
            images=image_inputs,
            padding=True,
            return_tensors="pt",
        )
        if video_inputs:
            proc_kwargs["videos"] = video_inputs

        inputs = self._processor(**proc_kwargs).to(self._model.device)  # type: ignore[union-attr]

        # Build a clean GenerationConfig so the model's stored generation_config.json
        # (which may contain temperature/top_p/top_k) is fully overridden and no
        # "invalid generation flags" warnings are emitted.
        gen_cfg = GenerationConfig(
            max_new_tokens=settings.max_new_tokens,
            do_sample=False,
        )

        with torch.no_grad():
            output_ids = self._model.generate(  # type: ignore[union-attr]
                **inputs,
                generation_config=gen_cfg,
            )

        trimmed = [
            out[len(inp):]
            for inp, out in zip(inputs.input_ids, output_ids)
        ]
        raw = self._processor.batch_decode(  # type: ignore[union-attr]
            trimmed, skip_special_tokens=True
        )[0].strip()

        elapsed_ms = int((time.time() - t0) * 1000)
        logger.info("Inference finished in %d ms.", elapsed_ms)

        result = _parse(raw)
        result["model_used"] = settings.model_id
        result["processing_time_ms"] = elapsed_ms
        return result


# ─── Parsing helpers ──────────────────────────────────────────────────────────

_FIELDS = ("components", "risks", "recommendations", "feedback")
_REQUIRED_FIELDS = ("components", "risks", "recommendations")
_MIN_FIELD_LENGTH = 20

_PLACEHOLDER_VALUES: frozenset[str] = frozenset({
    "n/a", "não disponível", "not available", "not applicable",
    "no information", "sem informação", "nenhum", "none",
    "não identificado", "não há riscos", "no risks identified",
})


def _sanitize_field(value: str) -> str:
    """Strip control characters and collapse excessive blank lines."""
    # Remove C0/C1 control chars except \t (0x09) and \n (0x0A)
    clean = re.sub(r"[\x00-\x08\x0b\x0c\x0e-\x1f\x7f]", "", value)
    clean = re.sub(r"\n{3,}", "\n\n", clean.strip())
    return clean


def _validate_result(result: dict) -> None:
    """Raise ValueError if any required field is empty, too short, or a placeholder."""
    for field in _REQUIRED_FIELDS:
        value = result.get(field, "")
        stripped = value.strip()

        if not stripped or len(stripped) < _MIN_FIELD_LENGTH:
            raise ValueError(
                f"Guardrail: field '{field}' is empty or too short ({len(stripped)} chars)."
            )

        first_line = stripped.split("\n")[0].strip().lower()
        if stripped.lower() in _PLACEHOLDER_VALUES or first_line in _PLACEHOLDER_VALUES:
            raise ValueError(
                f"Guardrail: field '{field}' contains a placeholder value: {value!r}"
            )


def _parse(text: str) -> dict:
    """Extract the four fields from the model's raw output, then sanitise and validate."""
    clean = re.sub(r"^```(?:json)?\s*", "", text.strip())
    clean = re.sub(r"\s*```$", "", clean.strip())

    try:
        data = json.loads(clean)
        result = {k: _sanitize_field(str(data.get(k, ""))) for k in _FIELDS}
    except json.JSONDecodeError:
        logger.warning("Model output is not valid JSON — falling back to regex extraction.")
        result = {k: _sanitize_field(_extract_field(text, k)) for k in _FIELDS}

    _validate_result(result)
    return result


def _extract_field(text: str, field: str) -> str:
    """Best-effort extraction when the model didn't return clean JSON."""
    pattern = rf'"{field}"\s*:\s*"((?:[^"\\]|\\.)*)"'
    m = re.search(pattern, text, re.IGNORECASE | re.DOTALL)
    if m:
        return m.group(1).encode("raw_unicode_escape").decode("unicode_escape")
    # Last resort: return truncated raw text for feedback
    if field == "feedback":
        return text[:400]
    return ""


# ─── Singleton ────────────────────────────────────────────────────────────────

analyzer = DiagramAnalyzer()
