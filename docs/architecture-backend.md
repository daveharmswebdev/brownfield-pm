# Architecture - Backend

## Overview

.NET 10 Web API following Clean Architecture principles with CQRS pattern.

**Primary Technologies:**
- ASP.NET Core 10
- Entity Framework Core 10
- PostgreSQL 16
- MediatR (CQRS)
- FluentValidation
- JWT Authentication

---

## Architecture Pattern

### Clean Architecture Layers

```
┌─────────────────────────────────────────────────────────────┐
│                    PropertyManager.Api                       │
│              (Controllers, Middleware, Config)               │
└──────────────────────────┬──────────────────────────────────┘
                           │ depends on
┌──────────────────────────▼──────────────────────────────────┐
│              PropertyManager.Application                     │
│         (Commands, Queries, Handlers, Validators)           │
└──────────────────────────┬──────────────────────────────────┘
                           │ depends on
┌──────────────────────────▼──────────────────────────────────┐
│                PropertyManager.Domain                        │
│            (Entities, Interfaces, Exceptions)                │
└─────────────────────────────────────────────────────────────┘
                           ▲
                           │ implements
┌──────────────────────────┴──────────────────────────────────┐
│             PropertyManager.Infrastructure                   │
│        (EF Core, Repositories, Identity, Services)          │
└─────────────────────────────────────────────────────────────┘
```

### Dependency Flow
- Domain has no dependencies
- Application depends on Domain
- Infrastructure implements Domain interfaces
- Api depends on Application and Infrastructure (DI registration)

---

## CQRS Pattern

### Command/Query Structure

Each operation lives in a single file containing:
1. **Command/Query record** - The request
2. **Validator** - FluentValidation rules
3. **Handler** - MediatR handler

**Example: CreateProperty.cs**
```csharp
// Command
public record CreatePropertyCommand(
    string Name,
    string Street,
    string City,
    string State,
    string ZipCode
) : IRequest<Guid>;

// Validator
public class CreatePropertyCommandValidator : AbstractValidator<CreatePropertyCommand>
{
    public CreatePropertyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.State).NotEmpty().Length(2);
        // ...
    }
}

// Handler
public class CreatePropertyCommandHandler : IRequestHandler<CreatePropertyCommand, Guid>
{
    public async Task<Guid> Handle(CreatePropertyCommand request, CancellationToken cancellationToken)
    {
        // Business logic
    }
}
```

### Request Pipeline

```
Controller → MediatR.Send() → Validation Behavior → Handler → Response
```

---

## Multi-Tenancy

### ITenantEntity Pattern

All tenant-scoped entities implement:
```csharp
public interface ITenantEntity
{
    Guid AccountId { get; set; }
}
```

### Global Query Filter

EF Core automatically filters by current user's AccountId:
```csharp
modelBuilder.Entity<Property>()
    .HasQueryFilter(p => p.AccountId == _currentUserService.AccountId);
```

### Tenant Resolution

`CurrentUserService` extracts AccountId from JWT claims on each request.

---

## Soft Delete

### ISoftDeletable Pattern

```csharp
public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
}
```

### Global Query Filter

```csharp
modelBuilder.Entity<Property>()
    .HasQueryFilter(p => p.DeletedAt == null);
```

### Delete Implementation

Delete operations set `DeletedAt` instead of removing records:
```csharp
entity.DeletedAt = DateTime.UtcNow;
await _context.SaveChangesAsync();
```

---

## Authentication

### JWT Bearer Authentication

```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = config["Jwt:Issuer"],
            ValidAudience = config["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });
```

### Token Flow

1. User logs in → Server generates access + refresh tokens
2. Access token (short-lived) used for API calls
3. Refresh token (long-lived) used to get new access tokens
4. Tokens stored in `RefreshToken` entity

### Claims

JWT contains:
- `sub` - User ID
- `email` - User email
- `accountId` - Tenant ID
- `exp` - Expiration

---

## API Design

### RESTful Conventions

| Action | HTTP Method | Route Pattern |
|--------|-------------|---------------|
| List | GET | `/api/v1/resources` |
| Get | GET | `/api/v1/resources/{id}` |
| Create | POST | `/api/v1/resources` |
| Update | PUT | `/api/v1/resources/{id}` |
| Delete | DELETE | `/api/v1/resources/{id}` |

### Response Patterns

**Success:**
- 200 OK - Read operations
- 201 Created - Create operations (with Location header)
- 204 No Content - Update/Delete operations

**Errors:**
- 400 Bad Request - Validation errors (ValidationProblemDetails)
- 401 Unauthorized - Missing/invalid auth
- 404 Not Found - Resource not found (ProblemDetails)
- 500 Internal Server Error - Unhandled exceptions

### Error Format (RFC 7807)

```json
{
  "type": "https://propertymanager.app/errors/not-found",
  "title": "Resource not found",
  "status": 404,
  "detail": "Property 'guid' does not exist",
  "instance": "/api/v1/properties/guid",
  "traceId": "0HN7..."
}
```

---

## Validation

### FluentValidation

Validators registered via assembly scanning:
```csharp
services.AddValidatorsFromAssembly(typeof(CreatePropertyCommand).Assembly);
```

### Validation in Controllers

Controllers validate before sending to MediatR:
```csharp
var validationResult = await _validator.ValidateAsync(command);
if (!validationResult.IsValid)
{
    return BadRequest(CreateValidationProblemDetails(validationResult));
}
```

---

## Logging

### Serilog Configuration

```csharp
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
```

### Structured Logging

```csharp
_logger.LogInformation(
    "Property created: {PropertyId} at {Timestamp}",
    propertyId,
    DateTime.UtcNow);
```

---

## Database Access

### Entity Framework Core

- **Provider:** Npgsql.EntityFrameworkCore.PostgreSQL
- **DbContext:** ApplicationDbContext
- **Migrations:** Code-first with EF migrations

### Repository Pattern

Domain interfaces in Domain layer, implementations in Infrastructure:
```csharp
// Domain
public interface IPropertyRepository
{
    Task<Property?> GetByIdAsync(Guid id);
    Task<IEnumerable<Property>> GetAllAsync();
    Task AddAsync(Property property);
}

// Infrastructure
public class PropertyRepository : IPropertyRepository
{
    private readonly ApplicationDbContext _context;
    // Implementation
}
```

---

## Configuration

### appsettings.json Structure

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Database=propertymanager;..."
  },
  "Jwt": {
    "Secret": "...",
    "Issuer": "http://localhost:5292",
    "Audience": "http://localhost:4200",
    "ExpiryMinutes": 60
  },
  "Email": {
    "Provider": "Smtp",
    "SmtpHost": "localhost",
    "SmtpPort": 1025,
    "FromAddress": "noreply@localhost"
  }
}
```

### Environment Variables

Production uses environment variables:
- `ConnectionStrings__Default`
- `Jwt__Secret`
- `Jwt__Issuer`
- `Jwt__Audience`

---

## Health Checks

### Endpoints

- `GET /api/v1/health` - Basic liveness
- `GET /api/v1/health/ready` - Database connectivity

### Implementation

```csharp
services.AddHealthChecks()
    .AddNpgSql(connectionString);
```

---

## Testing Strategy

### Unit Tests
- Handler tests with mocked dependencies
- Validator tests
- Domain logic tests

### Integration Tests
- Controller tests with WebApplicationFactory
- Repository tests with test database

### Test Project Structure
```
tests/
├── PropertyManager.Api.Tests/
├── PropertyManager.Application.Tests/
└── PropertyManager.Infrastructure.Tests/
```
