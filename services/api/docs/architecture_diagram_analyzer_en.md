# 🚀 Architecture Diagram Analyzer (Hackathon - FIAP SOAT + IADT)

## 🧠 Problem Statement

Companies operating distributed systems maintain numerous architecture diagrams (images or PDFs) used for:

- Architecture reviews
- Security audits
- Scalability analysis
- Technical discussions

Challenges:

- Manual analysis
- Time-consuming
- Requires specialists
- Not scalable

---

## 🎯 Objective

Build a backend system capable of:

- Receiving architecture diagrams (image or PDF)
- Processing diagrams
- Applying AI for automated analysis
- Generating structured technical reports
- Operating with a scalable architecture

---

## 🔄 MVP Scope

End-to-end flow:

1. Upload diagram
2. Process diagram
3. Perform AI analysis
4. Generate report
5. Check processing status

---

## 🧩 Functional Requirements

- Upload diagram (image or PDF)
- Create an analysis process
- Track status:
  - RECEIVED
  - PROCESSING
  - PROCESSED
  - ERROR
- Generate report including:
  - Components
  - Risks
  - Recommendations

---

## ⚙️ Technical Requirements

### Architecture

- Microservices-based
- Communication:
  - REST
  - Async messaging (queue)
- Clean Architecture or Hexagonal Architecture

### Minimum Services

- API Gateway / BFF
- Upload & Orchestrator Service
- AI Processing Service
- Report Service

---

## 🌐 API Design

### Base Route

/api/v1

### Routes

POST   /analyses  
GET    /analyses/{id}  
GET    /analyses/{id}/status  
GET    /analyses/{id}/report  

---

## 🔄 Async Communication

Queue: diagram-analysis

---

## 🤖 AI Strategy

- Use LLM (OpenAI / Azure OpenAI)
- Prompt engineering approach
- Structured JSON output

---

## ⚙️ .NET Stack

- .NET 10
- ASP.NET Minimal APIs
- Entity Framework Core
- PostgreSQL
- RabbitMQ
- Docker

---

## 📊 Observability

- Structured logging
- Correlation ID

---

## 🔐 Security

- Input validation
- File size limits
- Secure communication

---

## 🚀 Key Principles

- Keep it simple
- Focus on end-to-end flow
- Avoid overengineering
