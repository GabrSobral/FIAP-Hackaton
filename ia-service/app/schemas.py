from pydantic import BaseModel


class AnalysisResponse(BaseModel):
    components: str
    risks: str
    recommendations: str
    feedback: str
    model_used: str
    processing_time_ms: int


class HealthResponse(BaseModel):
    status: str
    model_loaded: bool
    model_id: str
