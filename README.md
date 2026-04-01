# FinRecon — Financial Reconciliation Dashboard

![CI](https://github.com/guicamarotto/finrecon/actions/workflows/ci.yml/badge.svg)
![Coverage](https://codecov.io/gh/guicamarotto/finrecon/badge.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![React](https://img.shields.io/badge/React-18-61DAFB?logo=react)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-4169E1?logo=postgresql)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-3.x-FF6600?logo=rabbitmq)
![Docker](https://img.shields.io/badge/Docker-Compose-2496ED?logo=docker)
![Kubernetes](https://img.shields.io/badge/Kubernetes-EKS-326CE5?logo=kubernetes)
![License](https://img.shields.io/badge/license-MIT-green)

> A production-grade full-stack application that processes financial product snapshots,
> detects discrepancies through async reconciliation, and surfaces actionable reports via a React dashboard.
> Demonstrates full ownership across cloud infrastructure, backend services, and frontend — targeting real-world fintech scenarios.

---

## Architecture

```mermaid
graph TB
    subgraph Browser["Browser"]
        UI["React Dashboard\n(Vite · Ant Design · Recharts)"]
    end

    subgraph API["FinRecon.API · ASP.NET Core 8"]
        EP["REST Endpoints\n+ JWT Auth"]
        VAL["FluentValidation"]
        PUB["MassTransit Publisher"]
    end

    subgraph Worker["FinRecon.Worker · .NET Worker Service"]
        CON["MassTransit Consumer"]
        ENG["Reconciliation Engine"]
        PAR["CSV / JSON Parsers"]
    end

    subgraph Storage["Storage Layer"]
        PG[("PostgreSQL 16")]
        MQ["RabbitMQ"]
        S3["MinIO / S3"]
    end

    UI -->|"HTTP + JWT"| EP
    EP --> VAL
    EP --> PUB
    PUB -->|"ReconciliationJobCreated"| MQ
    MQ --> CON
    CON --> PAR
    PAR --> ENG
    EP -->|"EF Core"| PG
    ENG -->|"EF Core"| PG
    EP -->|"Upload"| S3
    CON -->|"Download"| S3
```

> **Key async flow:** File upload returns `201 Created` immediately. The API publishes a message to RabbitMQ; the Worker consumes it independently — decoupling upload throughput from processing time.

---

## Reconciliation Algorithm

```mermaid
flowchart TD
    A([File received]) --> B{Duplicate hash\nfor same date?}
    B -->|Yes| C([409 Conflict])
    B -->|No| D[Store to S3/MinIO]
    D --> E[Create Job: status=pending]
    E --> F[Publish ReconciliationJobCreated]
    F --> G[Worker: parse file]
    G --> H{Parse error?}
    H -->|Yes| I([Job: status=failed])
    H -->|No| J[Load previous day records]
    J --> K[For each incoming record]
    K --> L{Match by\nclientId + productType?}
    L -->|No| M[status = new]
    L -->|Yes| N{delta <= 0.01?}
    N -->|Yes| O[status = matched]
    N -->|No| P[status = discrepant]
    M & O & P --> Q[Previous records not in current → missing]
    Q --> R[Generate Report]
    R --> S([Job: status=completed])
```

---

## CI/CD Pipeline

```mermaid
flowchart LR
    PR["Pull Request"] --> CI["CI Workflow"]
    CI --> BT["Backend Tests\nxUnit + Testcontainers"]
    CI --> FT["Frontend Tests\nJest + build check"]
    CI --> DC["Docker Build\nVerification"]
    BT --> COV["Codecov\nCoverage Report"]

    MERGE["Merge to main"] --> CD["CD Workflow"]
    CD --> PUSH["Build & Push\nDocker Images\nghcr.io"]
    PUSH --> DEPLOY["helm upgrade\n--atomic --timeout 5m"]
    DEPLOY -->|"auto-rollback on failure"| K8S["Kubernetes\nEKS"]
```

---

## Cloud Infrastructure (Phase 2)

```mermaid
graph TB
    subgraph AWS["AWS us-east-1"]
        subgraph VPC["VPC 10.0.0.0/16"]
            subgraph Public["Public Subnets"]
                ALB["Application\nLoad Balancer"]
                NAT["NAT Gateway"]
            end
            subgraph Private["Private Subnets"]
                subgraph EKS["EKS Cluster"]
                    API_POD["API Pods\nHPA 2–10"]
                    WORKER_POD["Worker Pods\n1–3"]
                    FE_POD["Frontend Pods\n2–5"]
                end
                RDS["RDS PostgreSQL 16\nMulti-AZ prod"]
                MQ["Amazon MQ\nRabbitMQ"]
            end
        end
        S3["S3 Bucket\nversioned + SSE"]
        ECR["GHCR\nContainer Images"]
    end

    Internet --> ALB
    ALB --> API_POD & FE_POD
    API_POD --> RDS & MQ & S3
    WORKER_POD --> MQ & RDS & S3
    EKS --> ECR
```

---

## Terraform Modules

```mermaid
graph TB
    subgraph TF["Terraform — infra/terraform/"]
        direction TB

        VPC["<b>vpc/</b>\nVPC 10.0.0.0/16\n2 public subnets + Internet GW\n2 private subnets + NAT GW"]
        EKS["<b>eks/</b>\nEKS Cluster 1.29\nManaged node group t3.medium × 2–4\nOIDC Provider for IRSA"]
        RDS["<b>rds/</b>\nPostgreSQL 16\ndev: db.t3.micro single-AZ\nprod: db.t3.small Multi-AZ\nEncryption + 7-day backups"]
        S3["<b>s3/</b>\nFile storage bucket\nVersioning + AES256\nIRSA role → Worker only"]
        MQ["<b>amazon-mq/</b>\nRabbitMQ 3.13 broker\nAMQPS port 5671\nAccess: EKS nodes only"]
    end

    VPC -->|"subnet_ids\nvpc_id"| EKS
    VPC -->|"private_subnet_ids\nvpc_id"| RDS
    VPC -->|"private_subnet_ids\nvpc_id"| MQ
    EKS -->|"node_security_group_id"| RDS
    EKS -->|"node_security_group_id"| MQ
    EKS -->|"oidc_provider_arn\noidc_provider_url"| S3
```

> Remote state stored in S3 (`finrecon-terraform-state`). Same `.tf` files for dev and prod — environment differences controlled via `terraform.tfvars`.

---

## Kubernetes / Helm

```mermaid
graph TB
    Internet["🌐 Internet"] --> ING

    subgraph Cluster["EKS Cluster — infra/k8s/finrecon/"]
        ING["Ingress\nnginx class\nfinrecon.example.com\n<i>/api → api-svc\n/ → frontend-svc</i>"]

        subgraph Services["Services (ClusterIP)"]
            APISVC["finrecon-api\n:80 → pod :8080"]
            FESVC["finrecon-frontend\n:80 → pod :80"]
        end

        subgraph Workloads["Deployments"]
            API["API Pods\nASP.NET Core 8\nreplicas: 2–10"]
            WORKER["Worker Pod\n.NET Worker Service\nreplicas: 1"]
            FE["Frontend Pods\nNginx + React build\nreplicas: 2"]
        end

        HPA["HPA\nCPU ≥ 70% → scale up\nmin 2 / max 10"] --> API

        subgraph Config["ConfigMap + Secret"]
            CM["finrecon-config\nASPNETCORE_ENVIRONMENT\nJwt__Issuer / Audience"]
            SEC["finrecon-secrets\nDB connection string\nRabbitMQ / MinIO / JWT"]
        end
    end

    subgraph AWS["AWS Services"]
        RDS[("RDS PostgreSQL")]
        AMQP["Amazon MQ\nRabbitMQ"]
        S3["S3 Bucket"]
    end

    ING --> APISVC --> API
    ING --> FESVC --> FE
    CM --> API
    SEC --> API
    SEC --> WORKER
    API --> RDS & AMQP
    WORKER --> AMQP & RDS
    WORKER -->|"IRSA — no credentials\nin environment"| S3
```

---

## Tech Stack

| Layer | Technology |
|---|---|
| **Frontend** | React 18, TypeScript, Vite, Ant Design, Recharts, React Query, Axios |
| **Backend API** | .NET 8, ASP.NET Core, FluentValidation, JWT (HS256) |
| **Backend Worker** | .NET 8 Worker Service, MassTransit, CsvHelper |
| **Domain** | Clean Architecture, Result\<T\> pattern, Domain state machine |
| **Database** | PostgreSQL 16 via EF Core 8 + Npgsql |
| **Messaging** | RabbitMQ 3.x + MassTransit (retry policy, dead-letter queue) |
| **Storage** | MinIO (local) / AWS S3 (cloud) |
| **Infrastructure** | Terraform, Kubernetes, Helm, Docker Compose |
| **CI/CD** | GitHub Actions, GHCR, ArgoCD-ready |
| **Testing** | xUnit, FluentAssertions, Testcontainers, Jest |

---

## Key Design Decisions

**1. Clean Architecture with enforced dependency rules.**
`Core` has zero external dependencies — only BCL and logging abstractions. The reconciliation engine is independently testable with zero mocks. Infrastructure implements Core interfaces and is the only layer that knows about Postgres or RabbitMQ.

**2. Async reconciliation via RabbitMQ instead of synchronous processing.**
The API returns `201 Created` immediately; the Worker runs independently. This decouples upload throughput from processing time and allows horizontal Worker scaling without touching the API.

**3. SHA-256 duplicate detection at both application and database level.**
The domain service checks the hash before creating a job. The database enforces a unique index on `(file_hash, reference_date)` as a backstop — defense-in-depth appropriate for financial data.

**4. `decimal` arithmetic with explicit tolerance constant.**
`ReconciliationConstants.MatchTolerance = 0.01m` — using `decimal` throughout prevents floating-point rounding errors unacceptable in financial calculations. The tolerance constant is centrally defined and easily testable.

**5. Testcontainers for integration tests instead of in-memory database.**
SQLite in-memory tests miss Postgres-specific behaviors (enum storage as string, constraint enforcement, unique indexes). Testcontainers starts a real Postgres 16 container, catching real integration failures.

**6. `Result<T>` pattern for business errors.**
Domain methods never throw for expected errors. Callers handle errors explicitly — prevents silent exception swallowing and makes API response mapping explicit and auditable.

---

## Getting Started

### Prerequisites
- Docker Desktop (or Docker Engine + Compose)
- `.NET 8 SDK` (for local dev without Docker)
- `Node 20+` (for frontend local dev)

### Local Development (Docker Compose)

```bash
# 1. Clone and copy env file
git clone https://github.com/guicamarotto/finrecon.git
cd finrecon
cp .env.example .env

# 2. Start everything (postgres, rabbitmq, minio, api, worker, frontend)
docker compose up

# 3. Open the dashboard
open http://localhost:3000

# Default API: http://localhost:5000
# RabbitMQ UI: http://localhost:15672  (finrecon / finrecon_dev_password)
# MinIO console: http://localhost:9001  (minioadmin / minioadmin_dev_password)
# Swagger UI:  http://localhost:5000/swagger
```

**First login:** Register a user at `POST /api/auth/register` or via the login page.

### Running Tests

```bash
# Backend (unit + integration — requires Docker for Testcontainers)
cd src && dotnet test

# Frontend
cd frontend && npm test

# Health check
curl http://localhost:5000/api/health
```

---

## API Reference

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/api/auth/register` | — | Register a new user |
| `POST` | `/api/auth/login` | — | Login, returns JWT |
| `POST` | `/api/reconciliations` | JWT | Upload file for reconciliation |
| `GET` | `/api/reconciliations` | JWT | List jobs (paginated, filterable) |
| `GET` | `/api/reconciliations/{id}` | JWT | Job detail with report |
| `GET` | `/api/reconciliations/{id}/records` | JWT | Records with filters |
| `GET` | `/api/health` | — | Liveness + readiness |

Full Swagger docs available at `http://localhost:5000/swagger` in development.

---

## File Format Spec

**CSV**
```csv
client_id,product_type,value,date
C001,Equity,1500.00,2025-01-15
C002,Crypto,320.50,2025-01-15
```

**JSON**
```json
{
  "referenceDate": "2025-01-15",
  "records": [
    { "clientId": "C001", "productType": "Equity", "value": 1500.00 },
    { "clientId": "C002", "productType": "Crypto", "value": 320.50 }
  ]
}
```

Valid product types: `Equity`, `Fund`, `Crypto`, `Bond`

---

## Project Structure

```
finrecon/
├── src/
│   ├── FinRecon.Core/          # Domain models, interfaces, engine (zero external deps)
│   ├── FinRecon.Infrastructure/ # EF Core, MinIO adapter, MassTransit setup
│   ├── FinRecon.API/           # ASP.NET Core Web API + JWT auth
│   ├── FinRecon.Worker/        # RabbitMQ consumer + CSV/JSON parsers
│   └── FinRecon.Tests/         # xUnit unit + Testcontainers integration tests
├── frontend/                   # React 18 + Vite SPA
├── infra/
│   ├── terraform/              # AWS VPC, EKS, RDS, S3, Amazon MQ modules
│   └── k8s/finrecon/           # Helm chart with HPA and Ingress
├── .github/workflows/          # CI (build+test), CD (build+push+deploy), Security scan
├── docker-compose.yml          # Full local stack — one command startup
├── .env.example                # Environment variable template
├── PRD.md                      # Product requirements
└── CLAUDE.md                   # Developer conventions and domain rules
```

---

## Cloud Deployment (Phase 2)

```bash
# Deploy AWS infrastructure
cd infra/terraform
cp environments/dev/terraform.tfvars.example terraform.tfvars
# Fill in db_password and rabbitmq_password
terraform init && terraform apply

# Deploy to Kubernetes via Helm
helm upgrade --install finrecon ./infra/k8s/finrecon \
  -f ./infra/k8s/finrecon/values.dev.yaml \
  --set api.image.tag=<sha>
```

---

## Roadmap

- **Phase 2 (Infrastructure):** Terraform + EKS deployment, ArgoCD GitOps, GitHub Actions full CD pipeline
- **Phase 3 (Features):** Real-time job status via SignalR WebSockets, PDF/Excel export, RabbitMQ metrics panel
- **Phase 4 (ML):** Anomaly detection on deltas using ML.NET or Python microservice

---

## License

MIT
