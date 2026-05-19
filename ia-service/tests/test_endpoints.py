"""Integration-style tests for the FastAPI endpoints using a mocked analyzer."""

from unittest.mock import patch

from fastapi.testclient import TestClient

from app.main import app

client = TestClient(app)

_FAKE_RESULT = {
    "components":       "API Gateway → Application Service → PostgreSQL database",
    "risks":            "Single point of failure on the gateway layer",
    "recommendations":  "Add a load balancer and circuit breaker for resilience",
    "feedback":         "Solid architecture with minor gaps",
    "model_used":       "test-model",
    "processing_time_ms": 100,
}

# ─── /health ──────────────────────────────────────────────────────────────────

def test_health_returns_ok():
    response = client.get("/health")
    assert response.status_code == 200
    data = response.json()
    assert data["status"] == "ok"
    assert "model_loaded" in data
    assert "model_id" in data

# ─── /analyze ─────────────────────────────────────────────────────────────────

def test_analyze_valid_image_returns_200():
    with patch("app.main.analyzer.analyze", return_value=_FAKE_RESULT):
        response = client.post(
            "/analyze",
            files={"file": ("diagram.png", b"fake-image-bytes", "image/png")},
        )
    assert response.status_code == 200
    data = response.json()
    assert data["components"] == _FAKE_RESULT["components"]
    assert data["model_used"] == "test-model"

def test_analyze_rejects_unsupported_content_type():
    response = client.post(
        "/analyze",
        files={"file": ("data.csv", b"a,b,c", "text/csv")},
    )
    assert response.status_code == 400
    assert "Unsupported" in response.json()["detail"]

def test_analyze_returns_500_when_analyzer_raises():
    with patch("app.main.analyzer.analyze", side_effect=RuntimeError("model crashed")):
        response = client.post(
            "/analyze",
            files={"file": ("diagram.png", b"fake-image-bytes", "image/png")},
        )
    assert response.status_code == 500
