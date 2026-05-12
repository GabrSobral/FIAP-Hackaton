# Architecture Diagram

```mermaid
flowchart TD
    User(["User - Browser"])

    subgraph Frontend["Frontend - Vanilla JS + Web Components"]
        UI["upload-dropzone / status-tracker / report-panel / history-panel"]
    end

    subgraph API["ASP.NET Core API - port 8080"]
        EP["Presentation Layer - Minimal API - /api/v1/analyses"]
        SSE["GET /stream - Server-Sent Events"]
        SVC["Application Services - CreateAnalysis / GetAnalysis / GetAnalysisStatus / GetAnalysisReport"]
        FS["LocalFileStorage - /app/uploads"]
        PUB["RabbitMqEventPublisher"]
        CON["DiagramAnalysisConsumer - BackgroundService"]

        EP --> SVC
        SVC --> FS
        SVC --> PUB
        CON --> SVC
    end

    subgraph Infra["Infrastructure - Docker Compose"]
        MQ["RabbitMQ - port 5672 - queue: diagram-analysis"]
        DB[("PostgreSQL - port 5432 - fiap_hackaton")]
        VOL[("uploads volume")]
    end

    subgraph AILayer["AI Layer - priority-based selection"]
        LOCAL["1st - ia-service port 8001 - FastAPI + Qwen2-VL"]
        GEMINI["2nd - Gemini API - gemini-2.0-flash"]
        CLAUDE["3rd - Claude API"]
        OPENAI["4th - OpenAI API"]
        STUB["fallback - StubAiAnalysisService"]
    end

    User -->|"drop diagram - JPEG/PNG/GIF/WEBP/PDF"| UI
    UI -->|"POST /api/v1/analyses"| EP
    UI -->|"SSE /stream every 2s"| SSE
    SSE --> SVC

    SVC -->|"save file"| VOL
    PUB -->|"publish DiagramUploadedEvent"| MQ
    MQ -->|"consume message"| CON

    CON -->|"AnalyzeAsync"| LOCAL
    CON -.->|"if no local AI"| GEMINI
    CON -.->|"if no Gemini key"| CLAUDE
    CON -.->|"if no Claude key"| OPENAI
    CON -.->|"no AI configured"| STUB

    CON -->|"save Analysis + Report + AnalysisLog"| DB
    SVC -->|"read Analysis / Report"| DB

    UI -->|"GET /api/v1/analyses/id/report"| EP
```

## Flow description

1. **Upload** — the user drops an image or PDF onto the frontend. The browser calls `POST /api/v1/analyses`, which validates the file (≤ 10 MB, allowed MIME types), saves it to the shared `uploads` volume, persists an `Analysis` record (status `Received`), and publishes a `DiagramUploadedEvent` to RabbitMQ.

2. **Async processing** — the `DiagramAnalysisConsumer` background service dequeues the event, sets status → `Processing`, and calls the configured AI provider (priority: Local → Gemini → Claude → OpenAI → Stub). Progress is written step-by-step to `AnalysisLog`.

3. **AI analysis** — the selected provider receives the diagram, runs inference, and returns structured JSON: `components`, `risks`, `recommendations`, `feedback`. The result is stored as a `Report` entity and status is set to `Processed` (or `Error` on failure).

4. **Result delivery** — the frontend polls status via the SSE stream (`/api/v1/analyses/stream`, every 2 s). Once `Processed`, it fetches the full report from `GET /api/v1/analyses/{id}/report` and renders it.

## Services & ports

| Service | Image / Runtime | Port |
|---|---|---|
| `api` | ASP.NET Core (.NET 10) | 8080 |
| `ia-service` | FastAPI + Qwen2-VL (Python) | 8001 → 8000 |
| `postgres` | postgres:16-alpine | 5432 |
| `rabbitmq` | rabbitmq:3.13-management-alpine | 5672, 15672 |
