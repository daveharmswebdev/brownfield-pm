# Property Manager

A web application that transforms paper-based rental property expense tracking into organized, tax-ready Schedule E reports.

## Technology Stack

- **Frontend:** Angular 21 + @ngrx/signals + Angular Material
- **Backend:** .NET 10 + ASP.NET Core + Clean Architecture + CQRS/MediatR
- **Database:** PostgreSQL 16 + EF Core 10
- **API Client Generation:** NSwag

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22 LTS](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Angular CLI](https://angular.io/cli) (`npm install -g @angular/cli`)

## Quick Start

### 1. Start Infrastructure

```bash
# Start PostgreSQL and MailHog
docker compose up -d db mailhog
```

### 2. Run Backend

```bash
cd backend
dotnet restore
dotnet ef database update --project src/PropertyManager.Infrastructure --startup-project src/PropertyManager.Api
dotnet run --project src/PropertyManager.Api
```

API will be available at: http://localhost:5000
Swagger UI: http://localhost:5000/swagger

### 3. Run Frontend

```bash
cd frontend
npm install
ng serve
```

Web app will be available at: http://localhost:4200

### 4. Access Services

| Service | URL |
|---------|-----|
| Web App | http://localhost:4200 |
| API | http://localhost:5000 |
| Swagger | http://localhost:5000/swagger |
| MailHog | http://localhost:8025 |
| PostgreSQL | localhost:5432 |

## Project Structure

```
property-manager/
├── backend/
│   ├── src/
│   │   ├── PropertyManager.Domain/        # Entities, value objects, interfaces
│   │   ├── PropertyManager.Application/   # Commands, queries, handlers
│   │   ├── PropertyManager.Infrastructure/# EF Core, persistence, identity
│   │   └── PropertyManager.Api/           # Controllers, middleware
│   ├── tests/
│   │   ├── PropertyManager.Domain.Tests/
│   │   ├── PropertyManager.Application.Tests/
│   │   └── PropertyManager.Api.Tests/
│   ├── Dockerfile
│   └── PropertyManager.sln
├── frontend/
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/          # Auth, API services
│   │   │   ├── shared/        # Reusable components
│   │   │   └── features/      # Feature modules
│   │   ├── styles/
│   │   └── assets/
│   ├── e2e/
│   ├── Dockerfile
│   └── angular.json
├── postman/
│   ├── PropertyManager.postman_collection.json
│   └── environments/
├── docker-compose.yml
└── README.md
```

## Development

### Generate TypeScript API Client

```bash
cd frontend
npm run generate-api
```

### Run Tests

```bash
# Backend tests
cd backend
dotnet test

# Frontend unit tests
cd frontend
npm test

# Frontend E2E tests (requires full stack running)
npm run test:e2e
```

### E2E Testing

E2E tests use Playwright and require the full stack running (API, database, MailHog).

```bash
# Start infrastructure
docker compose up -d db mailhog

# Start backend (in another terminal)
cd backend
dotnet run --project src/PropertyManager.Api

# Run E2E tests (in another terminal)
cd frontend
npm run test:e2e

# Run with UI mode (for debugging)
npm run test:e2e:ui

# View HTML report after test run
npm run test:e2e:report
```

#### E2E Test Structure

```
frontend/e2e/
  fixtures/     # Custom Playwright fixtures
  helpers/      # MailHog API, auth helpers
  pages/        # Page Object Model classes
  tests/        # Test specifications
```

E2E tests run automatically in CI on pull requests after unit tests pass. Failed tests produce HTML reports and trace files uploaded as GitHub artifacts.

### Database Migrations

```bash
cd backend

# Create migration
dotnet ef migrations add <MigrationName> --project src/PropertyManager.Infrastructure --startup-project src/PropertyManager.Api

# Apply migrations
dotnet ef database update --project src/PropertyManager.Infrastructure --startup-project src/PropertyManager.Api
```

## Deployment

### CI/CD Pipeline

This project uses GitHub Actions for continuous integration and deployment:

- **CI Pipeline** (`.github/workflows/ci.yml`): Triggered on pull requests to `main`
  - Builds and tests backend (.NET 10)
  - Builds and tests frontend (Angular with Vitest)
  - Verifies Docker image builds

- **CD Pipeline** (`.github/workflows/cd.yml`): Triggered on push to `main`
  - Runs CI checks first
  - Builds and pushes Docker images to GitHub Container Registry
  - Deploys to Render via webhook

### Deploy to Render

1. **Using Render Blueprint** (Recommended):
   - Click "New" → "Blueprint" in Render dashboard
   - Connect your GitHub repository
   - Render will automatically detect `render.yaml` and create services

2. **Manual Setup**:
   - Create a Web Service for the API (Docker, `./backend`)
   - Create a Static Site for the frontend (`./frontend`)
   - Create a PostgreSQL database
   - Configure environment variables (see below)

### Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `ConnectionStrings__Default` | PostgreSQL connection string | Yes |
| `Jwt__Secret` | 256-bit secret for JWT signing | Yes |
| `Jwt__Issuer` | JWT issuer URL | Yes |
| `Jwt__Audience` | JWT audience URL | Yes |
| `Jwt__ExpiryMinutes` | Token expiry (default: 60) | No |
| `Email__Provider` | `Smtp` or `SendGrid` | Yes |
| `Email__SmtpHost` | SMTP server host | If using SMTP |
| `Email__SmtpPort` | SMTP server port | If using SMTP |
| `Email__FromAddress` | Sender email address | Yes |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Yes |

### Health Checks

- **Basic Health**: `GET /api/v1/health` - Returns 200 if API is running
- **Readiness**: `GET /api/v1/health/ready` - Returns 200 if database is connected

### Database Migrations

In production, EF Core migrations run automatically on application startup. If migrations fail, the application will not start (fail-safe behavior).

### Rollback Procedure

1. **Via Render Dashboard**:
   - Navigate to your service
   - Click "Rollback" and select a previous deploy

2. **Via Git**:
   - Revert the commit causing issues
   - Push to `main` branch
   - CD pipeline will deploy the reverted version

### Local Production Testing

```bash
# Test with production-like settings
docker compose -f docker-compose.yml -f docker-compose.prod.yml up --build
```

## Troubleshooting

### Common Issues

1. **Database connection failed**: Check `ConnectionStrings__Default` format
2. **JWT validation failed**: Ensure `Jwt__Secret` is at least 32 characters
3. **CORS errors**: Verify `Jwt__Audience` matches your frontend URL
4. **Email not sending**: Check SMTP credentials and port settings

### Viewing Logs

- **Render**: Navigate to your service → "Logs" tab
- **Local Docker**: `docker compose logs -f api`

## License

Private - All rights reserved.
