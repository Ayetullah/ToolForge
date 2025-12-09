# UtilityTools - Production-Ready SaaS Utility Platform

A comprehensive, production-ready .NET 8 SaaS platform providing web utility tools (PDF merge/split, image compression, document conversion, Excel cleaning, AI text summarization, JSON formatting, regex generation, background removal, video compression) with subscription-based billing via Stripe.

## 🏗️ Architecture

This project follows **Clean Architecture** principles with clear separation of concerns:

```
src/
├── UtilityTools.Domain/          # Domain entities, value objects, interfaces
├── UtilityTools.Application/      # CQRS with MediatR, DTOs, validators
├── UtilityTools.Infrastructure/   # EF Core, file storage, external services
├── UtilityTools.Api/              # ASP.NET Core 8 Minimal API
├── UtilityTools.Workers/          # Background job processors
└── UtilityTools.Shared/           # Common DTOs, exceptions, extensions

tests/
├── UtilityTools.Tests.Unit/       # Unit tests
└── UtilityTools.Tests.Integration/ # Integration tests
```

## 🚀 Quick Start

### Prerequisites

- .NET 8 SDK
- Docker & Docker Compose
- PostgreSQL 16+ (or use Docker)
- MinIO (or use Docker)

### Local Development Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd ToolForge
   ```

2. **Copy environment file**
   ```bash
   cp .env.example .env
   # Edit .env with your configuration
   ```

3. **Start infrastructure services**
   ```bash
   docker-compose up -d postgres minio
   ```

4. **Run database migrations**
   ```bash
   cd src/UtilityTools.Api
   dotnet ef database update --project ../UtilityTools.Infrastructure
   ```

5. **Run the API**
   ```bash
   dotnet run --project src/UtilityTools.Api
   ```

   The API will be available at `http://localhost:5000`

6. **Access Swagger UI**
   Navigate to `http://localhost:5000/swagger`

### Docker Compose (Full Stack)

Run the entire stack with Docker Compose:

```bash
docker-compose up --build
```

This will start:
- PostgreSQL (port 5432)
- MinIO (ports 9000, 9001)
- API (port 8080)

**Note:** The application uses in-memory caching instead of Redis for simplicity. For production environments requiring distributed caching, consider implementing Redis or another distributed cache solution.

## 📋 API Endpoints

### Authentication

- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login and get JWT tokens
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/logout` - Logout and invalidate refresh token

### PDF Tools

- `POST /api/tools/pdf/merge` - Merge multiple PDFs
- `POST /api/tools/pdf/split` - Split PDF by pages
- `GET /api/tools/pdf/status/{jobId}` - Get job status

### Image Tools

- `POST /api/tools/image/compress` - Compress images
- `POST /api/tools/image/remove-background` - Remove background (Premium)
- `GET /api/tools/image/options` - Get supported formats

### Document Conversion

- `POST /api/tools/convert/doc-to-pdf` - Convert documents to PDF

### Excel Tools

- `POST /api/tools/excel/clean` - Clean Excel files

### AI Tools

- `POST /api/tools/ai/summarize` - Summarize text or URL

### JSON Tools

- `POST /api/tools/json/format` - Format JSON

### Regex Tools

- `POST /api/tools/regex/generate` - Generate regex pattern

### Video Tools

- `POST /api/tools/video/compress` - Compress video (background job)

## 🧪 Testing

### Run Unit Tests
```bash
dotnet test tests/UtilityTools.Tests.Unit
```

### Run Integration Tests
```bash
dotnet test tests/UtilityTools.Tests.Integration
```

### Run All Tests
```bash
dotnet test
```

## 🔧 Configuration

Key configuration options in `.env`:

- **Database**: PostgreSQL connection string
- **JWT**: Secret key, issuer, audience, expiration
- **File Storage**: Local, S3, or MinIO configuration
- **Stripe**: API keys for payment processing
- **AI Services**: OpenAI or Gemini API keys
- **Rate Limiting**: Request limits per tier

## 📦 Technology Stack

- **.NET 8** - Latest .NET framework
- **ASP.NET Core Minimal API** - Lightweight API framework
- **Entity Framework Core** - ORM with PostgreSQL
- **MediatR** - CQRS pattern implementation
- **FluentValidation** - Input validation
- **AutoMapper** - Object mapping
- **Hangfire** - Background job processing
- **Serilog** - Structured logging
- **OpenTelemetry** - Observability and metrics
- **Stripe.net** - Payment processing
- **xUnit** - Testing framework
- **Docker** - Containerization

## 🏭 Production Deployment

### Build Docker Image
```bash
docker build -t utilitytools-api:latest .
```

### Deploy to Kubernetes
```bash
kubectl apply -f k8s/
```

### Deploy to DigitalOcean App Platform
See `docs/deployment/digitalocean.md`

### Deploy to AWS ECS
See `docs/deployment/aws-ecs.md`

## 📊 Monitoring & Observability

- **Health Checks**: `/health/live`, `/health/ready`
- **Metrics**: `/metrics` (Prometheus format)
- **Logging**: Structured logs with Serilog
- **Tracing**: OpenTelemetry integration

## 🔒 Security

- JWT authentication with refresh tokens
- Rate limiting per subscription tier
- Input validation with FluentValidation
- File type restrictions
- CORS, CSP, HSTS headers
- OWASP best practices

## 📝 License

[Your License Here]

## 🤝 Contributing

[Contributing Guidelines]

## 📧 Support

[Support Information]

---

**Note**: This is a production-ready scaffold. Some features may require additional implementation based on your specific requirements.

