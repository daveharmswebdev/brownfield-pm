# Source Tree Analysis

## Repository Structure

```
property-manager/
├── backend/                          # .NET 10 API (Clean Architecture)
│   ├── src/
│   │   ├── PropertyManager.Domain/        # Core domain layer
│   │   │   ├── Common/                    # Base classes, interfaces
│   │   │   │   ├── AuditableEntity.cs     # Base entity with timestamps
│   │   │   │   ├── ITenantEntity.cs       # Multi-tenancy interface
│   │   │   │   └── ISoftDeletable.cs      # Soft delete interface
│   │   │   ├── Entities/                  # Domain entities
│   │   │   │   ├── Account.cs             # Tenant container
│   │   │   │   ├── User.cs                # Identity user
│   │   │   │   ├── Property.cs            # Rental property
│   │   │   │   ├── Expense.cs             # Expense record
│   │   │   │   ├── Income.cs              # Income record
│   │   │   │   ├── ExpenseCategory.cs     # IRS categories
│   │   │   │   ├── Receipt.cs             # Receipt file
│   │   │   │   └── RefreshToken.cs        # JWT refresh token
│   │   │   ├── Exceptions/                # Domain exceptions
│   │   │   └── Interfaces/                # Repository interfaces
│   │   │
│   │   ├── PropertyManager.Application/   # CQRS handlers
│   │   │   ├── Common/                    # Shared DTOs, behaviors
│   │   │   │   ├── PagedResult.cs         # Pagination wrapper
│   │   │   │   └── Behaviors/             # MediatR pipeline behaviors
│   │   │   ├── Auth/                      # Auth commands/queries
│   │   │   │   ├── Register.cs            # Registration handler
│   │   │   │   ├── Login.cs               # Login handler
│   │   │   │   └── RefreshToken.cs        # Token refresh handler
│   │   │   ├── Properties/                # Property commands/queries
│   │   │   │   ├── CreateProperty.cs      # Create command + handler
│   │   │   │   ├── UpdateProperty.cs      # Update command + handler
│   │   │   │   ├── DeleteProperty.cs      # Delete command + handler
│   │   │   │   ├── GetAllProperties.cs    # List query + handler
│   │   │   │   └── GetPropertyById.cs     # Detail query + handler
│   │   │   ├── Expenses/                  # Expense commands/queries
│   │   │   │   ├── CreateExpense.cs
│   │   │   │   ├── UpdateExpense.cs
│   │   │   │   ├── DeleteExpense.cs
│   │   │   │   ├── GetAllExpenses.cs
│   │   │   │   ├── GetExpensesByProperty.cs
│   │   │   │   ├── GetExpenseTotals.cs
│   │   │   │   ├── GetExpenseCategories.cs
│   │   │   │   └── CheckDuplicateExpense.cs
│   │   │   ├── Income/                    # Income commands/queries
│   │   │   └── Dashboard/                 # Dashboard queries
│   │   │
│   │   ├── PropertyManager.Infrastructure/# Data access layer
│   │   │   ├── Data/
│   │   │   │   ├── ApplicationDbContext.cs # EF Core DbContext
│   │   │   │   └── Configurations/        # Entity configurations
│   │   │   ├── Identity/                  # ASP.NET Core Identity
│   │   │   ├── Migrations/                # EF Core migrations
│   │   │   ├── Repositories/              # Repository implementations
│   │   │   └── Services/                  # Infrastructure services
│   │   │       └── CurrentUserService.cs  # Get current user context
│   │   │
│   │   └── PropertyManager.Api/           # API layer
│   │       ├── Controllers/               # REST controllers
│   │       │   ├── AuthController.cs      # /api/v1/auth
│   │       │   ├── PropertiesController.cs# /api/v1/properties
│   │       │   ├── ExpensesController.cs  # /api/v1/expenses
│   │       │   ├── IncomeController.cs    # /api/v1/income
│   │       │   ├── DashboardController.cs # /api/v1/dashboard
│   │       │   └── HealthController.cs    # /api/v1/health
│   │       ├── Middleware/                # Custom middleware
│   │       │   └── ExceptionMiddleware.cs # Global exception handling
│   │       ├── Program.cs                 # App configuration
│   │       ├── appsettings.json           # Configuration
│   │       └── appsettings.Development.json
│   │
│   ├── tests/
│   │   ├── PropertyManager.Api.Tests/     # Controller tests
│   │   ├── PropertyManager.Application.Tests/ # Handler tests
│   │   └── PropertyManager.Infrastructure.Tests/ # Repository tests
│   │
│   ├── Dockerfile                         # Backend container
│   └── PropertyManager.sln                # Solution file
│
├── frontend/                              # Angular 20 SPA
│   ├── src/
│   │   ├── app/
│   │   │   ├── app.ts                     # Root component
│   │   │   ├── app.routes.ts              # Route configuration
│   │   │   ├── app.config.ts              # App providers
│   │   │   │
│   │   │   ├── core/                      # Singleton services
│   │   │   │   ├── api/                   # NSwag-generated client
│   │   │   │   │   └── api.service.ts     # API client (generated)
│   │   │   │   ├── auth/                  # Auth services
│   │   │   │   │   ├── auth.service.ts    # Auth state management
│   │   │   │   │   └── token.interceptor.ts
│   │   │   │   ├── components/            # Shell components
│   │   │   │   │   ├── shell/             # Main app shell
│   │   │   │   │   ├── sidebar-nav/       # Desktop navigation
│   │   │   │   │   └── bottom-nav/        # Mobile navigation
│   │   │   │   ├── guards/                # Route guards
│   │   │   │   │   ├── auth.guard.ts
│   │   │   │   │   └── guest.guard.ts
│   │   │   │   └── services/              # Core services
│   │   │   │
│   │   │   ├── shared/                    # Reusable components
│   │   │   │   └── components/
│   │   │   │       ├── confirm-dialog/
│   │   │   │       ├── empty-state/
│   │   │   │       ├── loading-spinner/
│   │   │   │       ├── year-selector/
│   │   │   │       ├── property-row/
│   │   │   │       ├── stats-bar/
│   │   │   │       └── error-card/
│   │   │   │
│   │   │   └── features/                  # Feature modules
│   │   │       ├── auth/                  # Login/Register
│   │   │       │   └── pages/
│   │   │       ├── dashboard/             # Dashboard view
│   │   │       │   └── pages/
│   │   │       ├── properties/            # Property management
│   │   │       │   ├── pages/
│   │   │       │   ├── stores/            # @ngrx/signals store
│   │   │       │   │   └── property.store.ts
│   │   │       │   └── services/
│   │   │       ├── expenses/              # Expense management
│   │   │       │   ├── pages/
│   │   │       │   ├── components/
│   │   │       │   │   ├── expense-form/
│   │   │       │   │   ├── expense-row/
│   │   │       │   │   ├── category-select/
│   │   │       │   │   ├── expense-filters/
│   │   │       │   │   └── duplicate-warning-dialog/
│   │   │       │   └── stores/
│   │   │       │       ├── expense.store.ts
│   │   │       │       └── expense-list.store.ts
│   │   │       ├── income/                # Income management
│   │   │       │   ├── pages/
│   │   │       │   ├── components/
│   │   │       │   └── stores/
│   │   │       ├── receipts/              # Receipt uploads (future)
│   │   │       ├── reports/               # Schedule E reports (future)
│   │   │       └── settings/              # User settings
│   │   │
│   │   ├── styles.scss                    # Global styles
│   │   ├── index.html                     # HTML entry
│   │   └── main.ts                        # Bootstrap
│   │
│   ├── e2e/                               # Playwright E2E tests
│   │   ├── fixtures/                      # Test fixtures
│   │   ├── helpers/                       # Test utilities
│   │   ├── pages/                         # Page Object Model
│   │   └── tests/                         # Test specs
│   │       ├── auth/
│   │       ├── properties/
│   │       ├── expenses/
│   │       └── income/
│   │
│   ├── angular.json                       # Angular CLI config
│   ├── package.json                       # NPM dependencies
│   ├── tsconfig.json                      # TypeScript config
│   ├── nswag.json                         # API client generator config
│   ├── playwright.config.ts               # E2E test config
│   └── vitest.config.ts                   # Unit test config
│
├── postman/                               # API testing
│   ├── PropertyManager.postman_collection.json
│   └── environments/
│
├── docker-compose.yml                     # Development stack
├── docker-compose.prod.yml                # Production overrides
├── render.yaml                            # Render deployment
├── README.md                              # Project documentation
├── CLAUDE.md                              # AI assistant instructions
└── .gitignore
```

---

## Critical Entry Points

### Backend
- **API Entry:** `backend/src/PropertyManager.Api/Program.cs`
- **DbContext:** `backend/src/PropertyManager.Infrastructure/Data/ApplicationDbContext.cs`

### Frontend
- **App Bootstrap:** `frontend/src/main.ts`
- **Root Component:** `frontend/src/app/app.ts`
- **Routes:** `frontend/src/app/app.routes.ts`
- **API Client:** `frontend/src/app/core/api/api.service.ts`

---

## Integration Points

### Frontend → Backend
- API calls via NSwag-generated TypeScript client
- Base URL configured in environment/proxy
- JWT token attached via HTTP interceptor

### Backend → Database
- EF Core with PostgreSQL
- Connection string in appsettings.json
- Migrations auto-run on startup (production)

---

## Key Configuration Files

| File | Purpose |
|------|---------|
| `backend/src/PropertyManager.Api/appsettings.json` | API configuration |
| `frontend/angular.json` | Angular CLI configuration |
| `frontend/proxy.conf.json` | Dev server API proxy |
| `docker-compose.yml` | Local development stack |
| `render.yaml` | Production deployment |
| `.env.example` | Environment variables template |
