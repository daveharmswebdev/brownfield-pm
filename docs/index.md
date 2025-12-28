# Property Manager - Project Documentation Index

> AI-optimized reference documentation for brownfield development

## Project Overview

| Attribute | Value |
|-----------|-------|
| **Project Name** | Property Manager |
| **Purpose** | Rental property expense tracking with Schedule E reports |
| **Repository Type** | Multi-part (Frontend + Backend) |
| **Primary Language** | TypeScript (Frontend), C# (Backend) |

---

## Quick Reference

### Frontend (Angular)

| Aspect | Details |
|--------|---------|
| **Framework** | Angular 20.3 |
| **State Management** | @ngrx/signals |
| **UI Library** | Angular Material 20 |
| **Testing** | Vitest (unit), Playwright (E2E) |
| **Entry Point** | `frontend/src/main.ts` |
| **Root Path** | `frontend/` |

### Backend (.NET)

| Aspect | Details |
|--------|---------|
| **Framework** | ASP.NET Core 10 |
| **Architecture** | Clean Architecture + CQRS |
| **ORM** | Entity Framework Core 10 |
| **Database** | PostgreSQL 16 |
| **Entry Point** | `backend/src/PropertyManager.Api/Program.cs` |
| **Root Path** | `backend/` |

---

## Generated Documentation

### Architecture

- [Architecture - Backend](./architecture-backend.md) - Clean Architecture, CQRS, multi-tenancy patterns
- [Architecture - Frontend](./architecture-frontend.md) - Angular structure, signal stores, component patterns
- [Integration Architecture](./integration-architecture.md) - Frontend-backend communication, auth flow

### API & Data

- [API Contracts - Backend](./api-contracts-backend.md) - REST endpoints, request/response schemas
- [Data Models - Backend](./data-models-backend.md) - Entity relationships, database schema

### Components & Structure

- [Component Inventory - Frontend](./component-inventory-frontend.md) - UI components, stores, services
- [Source Tree Analysis](./source-tree-analysis.md) - Directory structure, entry points

---

## Existing Documentation

| Document | Location | Description |
|----------|----------|-------------|
| [Project README](../README.md) | Root | Quick start, commands, deployment |
| [E2E Test Guide](../frontend/e2e/README.md) | Frontend | Playwright patterns, fixtures |
| [Claude Instructions](../CLAUDE.md) | Root | AI assistant configuration |

---

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js 22 LTS
- Docker Desktop
- Angular CLI

### Quick Start

```bash
# 1. Start infrastructure
docker compose up -d db mailhog

# 2. Run backend (terminal 1)
cd backend
dotnet restore
dotnet ef database update --project src/PropertyManager.Infrastructure --startup-project src/PropertyManager.Api
dotnet run --project src/PropertyManager.Api

# 3. Run frontend (terminal 2)
cd frontend
npm install
npm start
```

### Service URLs

| Service | URL |
|---------|-----|
| Frontend | http://localhost:4200 |
| Backend API | http://localhost:5292 |
| Swagger UI | http://localhost:5292/swagger |
| MailHog | http://localhost:8025 |
| PostgreSQL | localhost:5432 |

---

## Development Commands

### Backend

```bash
cd backend
dotnet build                    # Build
dotnet test                     # Run tests
dotnet run --project src/PropertyManager.Api  # Start API

# Migrations
dotnet ef migrations add <Name> --project src/PropertyManager.Infrastructure --startup-project src/PropertyManager.Api
dotnet ef database update --project src/PropertyManager.Infrastructure --startup-project src/PropertyManager.Api
```

### Frontend

```bash
cd frontend
npm start                       # Dev server
npm test                        # Unit tests
npm run test:e2e                # E2E tests
npm run generate-api            # Regenerate API client
```

---

## Key Patterns

### Backend

- **CQRS**: Commands/Queries with MediatR handlers
- **Multi-tenancy**: ITenantEntity + global query filters
- **Soft delete**: ISoftDeletable + DeletedAt timestamp
- **Validation**: FluentValidation in handlers

### Frontend

- **Signal stores**: @ngrx/signals for state management
- **Standalone components**: No NgModules
- **Feature modules**: Lazy-loaded routes
- **Page Object Model**: E2E test organization

---

## Document Generation Info

| Field | Value |
|-------|-------|
| Generated | 2025-12-28 |
| Scan Level | Deep |
| Workflow | document-project v1.2.0 |
| Mode | initial_scan |
