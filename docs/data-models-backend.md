# Data Models - Backend

## Overview

Entity Framework Core 10 with PostgreSQL 16. All entities support:
- **Multi-tenancy** via `ITenantEntity` (AccountId field)
- **Soft delete** via `ISoftDeletable` (DeletedAt field)
- **Audit fields** via `AuditableEntity` (CreatedAt, UpdatedAt)

## Entity Relationship Diagram

```
Account (1) ──────────────────────┬──── (*) User
    │                             │
    │ (1)                         │
    ├──── (*) Property            │
    │         │                   │
    │         ├──── (*) Expense ──┴──── User (CreatedBy)
    │         │         │
    │         │         └──── (1) ExpenseCategory
    │         │
    │         ├──── (*) Income ───────── User (CreatedBy)
    │         │
    │         └──── (*) Receipt ──────── Expense (optional)
    │
    └──── (*) RefreshToken ───────────── User
```

---

## Core Entities

### Account
Multi-tenant container for users and properties.

```csharp
public class Account : AuditableEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; }

    // Navigation
    public ICollection<User> Users { get; set; }
    public ICollection<Property> Properties { get; set; }
}
```

| Column | Type | Constraints |
|--------|------|-------------|
| Id | GUID | PK |
| Name | varchar(100) | NOT NULL |
| CreatedAt | timestamp | NOT NULL |
| UpdatedAt | timestamp | NOT NULL |

---

### User (ApplicationUser)
ASP.NET Core Identity user with account association.

```csharp
public class User : IdentityUser<Guid>
{
    public Guid AccountId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }

    // Navigation
    public Account Account { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; set; }
}
```

| Column | Type | Constraints |
|--------|------|-------------|
| Id | GUID | PK |
| AccountId | GUID | FK → Account, NOT NULL |
| Email | varchar(256) | UNIQUE, NOT NULL |
| FirstName | varchar(50) | NOT NULL |
| LastName | varchar(50) | NOT NULL |
| PasswordHash | varchar(max) | NOT NULL |
| (Identity fields) | various | Standard Identity columns |

---

### Property
Rental property with address details.

```csharp
public class Property : AuditableEntity, ITenantEntity, ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string Name { get; set; }
    public string Street { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public Account Account { get; set; }
    public ICollection<Expense> Expenses { get; set; }
    public ICollection<Income> Income { get; set; }
    public ICollection<Receipt> Receipts { get; set; }
}
```

| Column | Type | Constraints |
|--------|------|-------------|
| Id | GUID | PK |
| AccountId | GUID | FK → Account, NOT NULL |
| Name | varchar(100) | NOT NULL |
| Street | varchar(200) | NOT NULL |
| City | varchar(100) | NOT NULL |
| State | varchar(2) | NOT NULL |
| ZipCode | varchar(10) | NOT NULL |
| DeletedAt | timestamp | NULL (soft delete) |
| CreatedAt | timestamp | NOT NULL |
| UpdatedAt | timestamp | NOT NULL |

**Indexes:**
- `IX_Properties_AccountId` on AccountId
- `IX_Properties_AccountId_DeletedAt` (filtered: DeletedAt IS NULL)

---

### Expense
Expense record linked to property and category.

```csharp
public class Expense : AuditableEntity, ITenantEntity, ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid CategoryId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string? Description { get; set; }
    public Guid? ReceiptId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public Account Account { get; set; }
    public Property Property { get; set; }
    public ExpenseCategory Category { get; set; }
    public Receipt? Receipt { get; set; }
}
```

| Column | Type | Constraints |
|--------|------|-------------|
| Id | GUID | PK |
| AccountId | GUID | FK → Account, NOT NULL |
| PropertyId | GUID | FK → Property, NOT NULL |
| CategoryId | GUID | FK → ExpenseCategory, NOT NULL |
| Amount | decimal(18,2) | NOT NULL, > 0 |
| Date | date | NOT NULL |
| Description | varchar(500) | NULL |
| ReceiptId | GUID | FK → Receipt, NULL |
| CreatedByUserId | GUID | FK → User, NOT NULL |
| DeletedAt | timestamp | NULL (soft delete) |
| CreatedAt | timestamp | NOT NULL |
| UpdatedAt | timestamp | NOT NULL |

**Indexes:**
- `IX_Expenses_PropertyId` on PropertyId
- `IX_Expenses_CategoryId` on CategoryId
- `IX_Expenses_AccountId_Date` on (AccountId, Date)
- `IX_Expenses_PropertyId_Amount_Date` for duplicate detection

---

### ExpenseCategory
IRS Schedule E expense categories (seeded data).

```csharp
public class ExpenseCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public int ScheduleELine { get; set; }
    public int SortOrder { get; set; }
}
```

| Column | Type | Constraints |
|--------|------|-------------|
| Id | GUID | PK |
| Name | varchar(50) | NOT NULL, UNIQUE |
| ScheduleELine | int | NOT NULL |
| SortOrder | int | NOT NULL |

**Seeded Categories (15 IRS Schedule E lines):**
1. Advertising
2. Auto and Travel
3. Cleaning and Maintenance
4. Commissions
5. Insurance
6. Legal and Professional Fees
7. Management Fees
8. Mortgage Interest
9. Other Interest
10. Repairs
11. Supplies
12. Taxes
13. Utilities
14. Depreciation
15. Other

---

### Income
Income record linked to property.

```csharp
public class Income : AuditableEntity, ITenantEntity, ISoftDeletable
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid PropertyId { get; set; }
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public string? Source { get; set; }
    public string? Description { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime? DeletedAt { get; set; }

    // Navigation
    public Account Account { get; set; }
    public Property Property { get; set; }
}
```

| Column | Type | Constraints |
|--------|------|-------------|
| Id | GUID | PK |
| AccountId | GUID | FK → Account, NOT NULL |
| PropertyId | GUID | FK → Property, NOT NULL |
| Amount | decimal(18,2) | NOT NULL, > 0 |
| Date | date | NOT NULL |
| Source | varchar(100) | NULL |
| Description | varchar(500) | NULL |
| CreatedByUserId | GUID | FK → User, NOT NULL |
| DeletedAt | timestamp | NULL (soft delete) |
| CreatedAt | timestamp | NOT NULL |
| UpdatedAt | timestamp | NOT NULL |

---

### Receipt
Receipt file storage (future feature).

```csharp
public class Receipt : AuditableEntity, ITenantEntity
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid PropertyId { get; set; }
    public string FilePath { get; set; }
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long FileSize { get; set; }

    // Navigation
    public Account Account { get; set; }
    public Property Property { get; set; }
    public Expense? Expense { get; set; }
}
```

---

### RefreshToken
JWT refresh token storage.

```csharp
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRevoked { get; set; }

    // Navigation
    public User User { get; set; }
}
```

---

## Common Interfaces

### ITenantEntity
```csharp
public interface ITenantEntity
{
    Guid AccountId { get; set; }
}
```
All tenant-scoped entities implement this. EF Core global query filter automatically filters by current user's AccountId.

### ISoftDeletable
```csharp
public interface ISoftDeletable
{
    DateTime? DeletedAt { get; set; }
}
```
Soft delete support. EF Core global query filter excludes records where DeletedAt is not null.

### AuditableEntity
```csharp
public abstract class AuditableEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```
Auto-populated on SaveChanges via EF Core interceptor.

---

## Database Configuration

**Connection String Pattern:**
```
Host=localhost;Database=propertymanager;Username=postgres;Password=xxx
```

**EF Core Configuration:**
- PostgreSQL provider: `Npgsql.EntityFrameworkCore.PostgreSQL`
- Migrations location: `PropertyManager.Infrastructure/Migrations`
- DbContext: `ApplicationDbContext`

**Migration Commands:**
```bash
# Create migration
dotnet ef migrations add <Name> \
  --project src/PropertyManager.Infrastructure \
  --startup-project src/PropertyManager.Api

# Apply migrations
dotnet ef database update \
  --project src/PropertyManager.Infrastructure \
  --startup-project src/PropertyManager.Api
```
