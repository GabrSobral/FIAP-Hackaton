from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", env_file_encoding="utf-8", extra="ignore")

    # Which Hugging Face model to load
    model_id: str = "Qwen/Qwen2-VL-2B-Instruct"

    # "auto" lets accelerate pick GPU when available, falls back to CPU
    device: str = "auto"

    # Maximum tokens the model may generate
    max_new_tokens: int = 1024

    # 4-bit quantization (requires bitsandbytes); reduces VRAM by ~50%
    load_in_4bit: bool = False


settings = Settings()
