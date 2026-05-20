# 🚀 Architecture Diagram Analyzer (Hackathon - FIAP SOAT + IADT)

## 🧠 Problem Statement

Companies that operate distributed systems maintain many architecture diagrams (images or PDFs) used in:

- Architecture reviews
- Security audits
- Scalability analysis
- Technical discussions

Challenges:

- Manual analysis
- Time-consuming
- Requires experts
- Not scalable

---

## 🎯 Objective

Build a backend system capable of:

- Receiving architecture diagrams (image or PDF)
- Processing diagrams
- Applying AI for automated analysis
- Generating structured technical reports
- Operating with scalable architecture

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
- Create analysis process
- Track status:
  - RECEIVED
  - PROCESSING
  - PROCESSED
  - ERROR
- Generate report with:
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

### Services (minimum)

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
- Prompt engineering
- JSON structured output

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

- Structured logs
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
