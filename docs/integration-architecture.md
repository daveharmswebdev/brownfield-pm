# Integration Architecture

## Overview

Property Manager is a multi-part application with separate frontend and backend components communicating via REST API.

```
┌─────────────────┐         ┌─────────────────┐         ┌─────────────────┐
│                 │  HTTP   │                 │  TCP    │                 │
│    Frontend     │◄───────►│     Backend     │◄───────►│   PostgreSQL    │
│   (Angular)     │  :4200  │   (.NET API)    │  :5432  │   Database      │
│                 │         │     :5292       │         │                 │
└─────────────────┘         └─────────────────┘         └─────────────────┘
                                    │
                                    │ SMTP :1025
                                    ▼
                            ┌─────────────────┐
                            │    MailHog      │
                            │  (Dev Email)    │
                            └─────────────────┘
```

---

## Communication Patterns

### Frontend → Backend

**Protocol:** HTTP/HTTPS REST
**Format:** JSON
**Authentication:** JWT Bearer tokens

**API Base URL:**
- Development: `http://localhost:5292/api/v1`
- Production: `https://api.propertymanager.app/api/v1`

**Request Flow:**
```
Angular Component
    │
    ▼
Signal Store (rxMethod)
    │
    ▼
NSwag API Client
    │
    ▼
HTTP Interceptor (adds JWT)
    │
    ▼
Backend Controller
    │
    ▼
MediatR Handler
    │
    ▼
Repository → Database
```

### API Contract Sync

The frontend TypeScript client is generated from the backend OpenAPI spec:

1. Backend exposes `/swagger/v1/swagger.json`
2. NSwag generates TypeScript client
3. Frontend imports generated types and services

**Regeneration Command:**
```bash
cd frontend && npm run generate-api
```

---

## Authentication Flow

### Login Sequence

```
┌──────────┐     ┌──────────┐     ┌──────────┐
│ Frontend │     │ Backend  │     │ Database │
└────┬─────┘     └────┬─────┘     └────┬─────┘
     │                │                │
     │ POST /auth/login               │
     │ {email, password}              │
     │───────────────►│                │
     │                │ Verify user    │
     │                │───────────────►│
     │                │◄───────────────│
     │                │ Generate JWT   │
     │◄───────────────│                │
     │ {accessToken,  │                │
     │  refreshToken} │                │
     │                │                │
     │ Store tokens   │                │
     │                │                │
```

### Token Refresh

```
┌──────────┐     ┌──────────┐     ┌──────────┐
│ Frontend │     │ Backend  │     │ Database │
└────┬─────┘     └────┬─────┘     └────┬─────┘
     │                │                │
     │ API Request    │                │
     │ (expired token)│                │
     │───────────────►│                │
     │◄───────────────│                │
     │ 401 Unauthorized               │
     │                │                │
     │ POST /auth/refresh             │
     │ {refreshToken} │                │
     │───────────────►│                │
     │                │ Validate token │
     │                │───────────────►│
     │◄───────────────│                │
     │ {newAccessToken}               │
     │                │                │
     │ Retry original │                │
     │ request        │                │
     │───────────────►│                │
     │◄───────────────│                │
```

---

## Data Flow Patterns

### Property List Loading

```
1. User navigates to Dashboard
2. DashboardComponent calls PropertyStore.loadProperties()
3. PropertyStore dispatches rxMethod
4. PropertyService.getProperties() called
5. NSwag client sends GET /api/v1/properties
6. Backend returns PropertySummaryDto[]
7. Store updates state with properties
8. Component reactively displays data
```

### Expense Creation

```
1. User fills expense form
2. ExpenseFormComponent emits submit event
3. ExpenseStore.createExpense() called
4. Optional: Check for duplicates first
5. POST /api/v1/expenses sent
6. Backend validates, creates expense
7. 201 Created returned with new ID
8. Store refreshes expense list
9. Snackbar shows success message
```

---

## Development Environment

### Docker Compose Stack

```yaml
services:
  api:        # Backend API (:5292)
  web:        # Frontend (:4200)
  db:         # PostgreSQL (:5432)
  mailhog:    # Email testing (:8025)
```

### Service URLs

| Service | Development URL | Purpose |
|---------|-----------------|---------|
| Frontend | http://localhost:4200 | Angular SPA |
| Backend API | http://localhost:5292 | REST API |
| Swagger UI | http://localhost:5292/swagger | API documentation |
| PostgreSQL | localhost:5432 | Database |
| MailHog UI | http://localhost:8025 | Email testing |

### Proxy Configuration

Frontend dev server proxies API calls:

```json
// proxy.conf.json
{
  "/api": {
    "target": "http://localhost:5292",
    "secure": false,
    "changeOrigin": true
  }
}
```

---

## Production Architecture

### Render Deployment

```
┌─────────────────────────────────────────────────────┐
│                    Render Platform                   │
├─────────────────────────────────────────────────────┤
│                                                     │
│  ┌─────────────┐  ┌─────────────┐  ┌────────────┐  │
│  │   Static    │  │ Web Service │  │ PostgreSQL │  │
│  │   Site      │  │   (API)     │  │  Database  │  │
│  │  (Frontend) │  │             │  │            │  │
│  └──────┬──────┘  └──────┬──────┘  └─────┬──────┘  │
│         │                │               │         │
│         │    HTTPS       │    Internal   │         │
│         └───────────────►│◄──────────────┘         │
│                          │                         │
└─────────────────────────────────────────────────────┘
```

### Environment Configuration

**Backend (Web Service):**
```
ConnectionStrings__Default=<render_internal_url>
Jwt__Secret=<production_secret>
Jwt__Issuer=https://api.propertymanager.app
Jwt__Audience=https://propertymanager.app
Email__Provider=SendGrid
```

**Frontend (Static Site):**
- Built with production configuration
- API URL baked into build

---

## Error Handling

### Frontend Error Handling

```typescript
catchError((error) => {
  if (error.status === 401) {
    // Trigger token refresh or logout
  } else if (error.status === 404) {
    // Resource not found
  } else {
    // Generic error handling
  }
  return EMPTY;
})
```

### Backend Error Responses

All errors follow RFC 7807 Problem Details:

```json
{
  "type": "https://propertymanager.app/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "errors": { "Amount": ["Must be greater than 0"] }
}
```

---

## Health Monitoring

### Health Check Endpoints

| Endpoint | Purpose | Checks |
|----------|---------|--------|
| `/api/v1/health` | Liveness | API running |
| `/api/v1/health/ready` | Readiness | Database connected |

### Render Health Checks

```yaml
# render.yaml
services:
  - type: web
    healthCheckPath: /api/v1/health
```

---

## Shared Concepts

### Multi-Tenancy

Both parts understand tenant isolation:
- Backend: AccountId in JWT claims, global query filters
- Frontend: User context from auth service, all API calls scoped

### Soft Delete

- Backend: DeletedAt timestamp, filtered in queries
- Frontend: Delete confirmation, success feedback

### Date Handling

- Backend: DateOnly for dates, DateTime for timestamps
- Frontend: Date objects, formatted via helpers
- API: ISO 8601 string format
