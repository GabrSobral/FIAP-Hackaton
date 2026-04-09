import logging
from contextlib import asynccontextmanager
from io import BytesIO

from fastapi import FastAPI, File, HTTPException, UploadFile

from .analyzer import analyzer
from .schemas import AnalysisResponse, HealthResponse

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
)
logger = logging.getLogger(__name__)

_SUPPORTED_IMAGES = {"image/jpeg", "image/png", "image/gif", "image/webp"}


@asynccontextmanager
async def lifespan(app: FastAPI):
    # Model loads lazily on first request — startup stays fast
    logger.info("ia-service ready. Model will load on the first /analyze call.")
    yield


app = FastAPI(
    title="IA Analysis Service",
    description=(
        "Local vision-language model service for architecture diagram analysis. "
        "Powered by Qwen2-VL running on HuggingFace Transformers."
    ),
    version="1.0.0",
    lifespan=lifespan,
)


# ─── Routes ───────────────────────────────────────────────────────────────────

@app.get("/health", response_model=HealthResponse, tags=["ops"])
def health() -> HealthResponse:
    return HealthResponse(
        status="ok",
        model_loaded=analyzer.is_loaded,
        model_id=analyzer.model_id,
    )


@app.post("/analyze", response_model=AnalysisResponse, tags=["analysis"])
async def analyze(file: UploadFile = File(...)) -> AnalysisResponse:
    content_type = (file.content_type or "").lower()

    if content_type not in _SUPPORTED_IMAGES and content_type != "application/pdf":
        raise HTTPException(
            status_code=400,
            detail=f"Unsupported file type '{content_type}'. "
                   f"Accepted: JPEG, PNG, GIF, WEBP, PDF.",
        )

    data = await file.read()

    if content_type == "application/pdf":
        data = _pdf_to_image(data)

    try:
        result = analyzer.analyze(data)
    except Exception as exc:
        logger.exception("Analysis failed for file '%s'", file.filename)
        raise HTTPException(status_code=500, detail=str(exc))

    return AnalysisResponse(**result)


# ─── Helpers ──────────────────────────────────────────────────────────────────

def _pdf_to_image(data: bytes) -> bytes:
    """Convert the first page of a PDF to PNG bytes."""
    try:
        from pdf2image import convert_from_bytes
        pages = convert_from_bytes(data, first_page=1, last_page=1, dpi=150)
        buf = BytesIO()
        pages[0].save(buf, format="PNG")
        return buf.getvalue()
    except Exception as exc:
        raise HTTPException(
            status_code=400,
            detail=f"PDF conversion failed: {exc}",
        )
