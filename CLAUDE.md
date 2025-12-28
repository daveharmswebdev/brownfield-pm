# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Property Manager is a web application for rental property expense tracking, generating tax-ready Schedule E reports. Full-stack app with Angular frontend and .NET backend using Clean Architecture.

## Common Commands

### Infrastructure
```bash
# Start database and email server
docker compose up -d db mailhog
```

### Backend (from /backend)
```bash
dotnet restore
dotnet build
dotnet test                                    # Run all backend tests
dotnet test --filter "FullyQualifiedName~TestName"  # Run single test
dotnet run --project src/PropertyManager.Api   # Start API on http://localhost:5292

# Database migrations
dotnet ef database update --project src/PropertyManager.Infrastructure --startup-project src/PropertyManager.Api
dotnet ef migrations add <Name> --project src/PropertyManager.Infrastructure --startup-project src/PropertyManager.Api
```

### Frontend (from /frontend)
```bash
npm install
npm start                    # Start dev server on http://localhost:4200
npm test                     # Run unit tests (Vitest)
npm run test:watch           # Run tests in watch mode
npm run test:e2e             # Run Playwright E2E tests (requires full stack)
npm run test:e2e:ui          # E2E with visual debugger
npm run generate-api         # Regenerate TypeScript API client from Swagger (requires API running)
```

## Architecture

### Backend (.NET 10 Clean Architecture)

```
backend/src/
├── PropertyManager.Domain/        # Entities, interfaces (no dependencies)
├── PropertyManager.Application/   # CQRS commands/queries, MediatR handlers, FluentValidation
├── PropertyManager.Infrastructure/# EF Core, PostgreSQL, Identity
└── PropertyManager.Api/           # Controllers, middleware
```

**Key patterns:**
- CQRS with MediatR: Each operation is a Command (write) or Query (read) with dedicated Handler
- Command/Query files contain the record, validator, and handler in one file (e.g., `CreateProperty.cs`)
- Multi-tenant via `ITenantEntity` - entities include `AccountId` for tenant isolation
- Soft delete via `ISoftDeletable` - entities have `DeletedAt` property
- FluentValidation for request validation

### Frontend (Angular 21)

```
frontend/src/app/
├── core/           # Auth guards, interceptors, API client, shell/nav components
├── shared/         # Reusable components (confirm-dialog, empty-state, loading-spinner)
└── features/       # Feature modules (properties, expenses, income, dashboard, auth)
```

**Key patterns:**
- State management: `@ngrx/signals` stores in `features/*/stores/`
- API client: NSwag-generated TypeScript client at `core/api/api.service.ts` - regenerate with `npm run generate-api` when backend API changes
- UI: Angular Material components
- Unit tests: Vitest (not Karma)
- E2E: Playwright with Page Object Model in `e2e/pages/`

### API Client Generation

The frontend API client is auto-generated from the backend Swagger spec:
1. Start the backend API (`dotnet run --project src/PropertyManager.Api`)
2. Run `npm run generate-api` from frontend directory
3. Client generated to `src/app/core/api/api.service.ts`

## Testing

### E2E Test Structure
```
frontend/e2e/
├── fixtures/      # Playwright fixtures (authenticatedUser, page objects)
├── helpers/       # MailHog, auth, test data generators
├── pages/         # Page Object Model classes extending BasePage
└── tests/         # Test specs organized by feature
```

E2E tests require full stack running (database, API, frontend). Use `authenticatedUser` fixture for logged-in state. Test data uses timestamp-based unique identifiers.

## Key URLs

| Service    | URL                            |
|------------|--------------------------------|
| Frontend   | http://localhost:4200          |
| API        | http://localhost:5292          |
| Swagger    | http://localhost:5292/swagger  |
| MailHog    | http://localhost:8025          |
| PostgreSQL | localhost:5432                 |
