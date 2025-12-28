# API Contracts - Backend

## Overview

REST API built with ASP.NET Core 10, following CQRS pattern with MediatR. All endpoints require JWT authentication except health checks.

**Base URL:** `http://localhost:5292/api/v1`

## Authentication

All protected endpoints require Bearer token authentication:
```
Authorization: Bearer <jwt_token>
```

---

## Auth Controller

**Base Route:** `/api/v1/auth`

### POST /auth/register
Create a new user account.

**Request:**
```json
{
  "email": "string",
  "password": "string",
  "firstName": "string",
  "lastName": "string"
}
```

**Response:** `201 Created`
```json
{
  "userId": "guid",
  "email": "string"
}
```

### POST /auth/login
Authenticate user and receive tokens.

**Request:**
```json
{
  "email": "string",
  "password": "string"
}
```

**Response:** `200 OK`
```json
{
  "accessToken": "string",
  "refreshToken": "string",
  "expiresIn": 3600
}
```

### POST /auth/refresh
Refresh access token using refresh token.

### POST /auth/logout
Invalidate refresh token.

---

## Properties Controller

**Base Route:** `/api/v1/properties`

### GET /properties
Get all properties for current user.

**Query Parameters:**
- `year` (optional): Tax year filter for expense/income totals

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "name": "string",
      "street": "string",
      "city": "string",
      "state": "string",
      "zipCode": "string",
      "expenseTotal": 0.00,
      "incomeTotal": 0.00
    }
  ],
  "totalCount": 0
}
```

### GET /properties/{id}
Get property details by ID.

**Query Parameters:**
- `year` (optional): Tax year filter

**Response:** `200 OK`
```json
{
  "id": "guid",
  "name": "string",
  "street": "string",
  "city": "string",
  "state": "string",
  "zipCode": "string",
  "expenseTotal": 0.00,
  "incomeTotal": 0.00,
  "createdAt": "datetime",
  "updatedAt": "datetime"
}
```

### POST /properties
Create a new property.

**Request:**
```json
{
  "name": "string",
  "street": "string",
  "city": "string",
  "state": "string",
  "zipCode": "string"
}
```

**Response:** `201 Created`
```json
{
  "id": "guid"
}
```

### PUT /properties/{id}
Update an existing property.

**Request:** Same as POST

**Response:** `204 No Content`

### DELETE /properties/{id}
Soft delete a property.

**Response:** `204 No Content`

---

## Expenses Controller

**Base Route:** `/api/v1`

### GET /expenses
Get all expenses with filtering and pagination.

**Query Parameters:**
- `dateFrom` (optional): Filter start date (DateOnly)
- `dateTo` (optional): Filter end date (DateOnly)
- `categoryIds` (optional): Filter by category IDs (multi-select)
- `search` (optional): Search description text
- `year` (optional): Tax year filter
- `page` (default: 1): Page number
- `pageSize` (default: 50, max: 100): Items per page

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "propertyId": "guid",
      "propertyName": "string",
      "amount": 0.00,
      "date": "2025-01-01",
      "categoryId": "guid",
      "categoryName": "string",
      "description": "string"
    }
  ],
  "totalCount": 0,
  "page": 1,
  "pageSize": 50,
  "totalPages": 1
}
```

### GET /expenses/{id}
Get expense by ID.

### POST /expenses
Create a new expense.

**Request:**
```json
{
  "propertyId": "guid",
  "amount": 0.00,
  "date": "2025-01-01",
  "categoryId": "guid",
  "description": "string (optional)"
}
```

**Response:** `201 Created`
```json
{
  "id": "guid"
}
```

### PUT /expenses/{id}
Update an expense. Note: PropertyId cannot be changed.

**Request:**
```json
{
  "amount": 0.00,
  "date": "2025-01-01",
  "categoryId": "guid",
  "description": "string (optional)"
}
```

**Response:** `204 No Content`

### DELETE /expenses/{id}
Soft delete an expense.

**Response:** `204 No Content`

### GET /expenses/check-duplicate
Check for potential duplicate expenses.

**Query Parameters:**
- `propertyId` (required): Property GUID
- `amount` (required): Expense amount
- `date` (required): Expense date

**Response:** `200 OK`
```json
{
  "isDuplicate": false,
  "existingExpense": null
}
```

### GET /expenses/totals
Get expense totals for a year with per-property breakdown.

**Query Parameters:**
- `year` (optional): Tax year (defaults to current)

**Response:** `200 OK`
```json
{
  "totalExpenses": 0.00,
  "year": 2025,
  "byProperty": [
    {
      "propertyId": "guid",
      "propertyName": "string",
      "total": 0.00
    }
  ]
}
```

### GET /expense-categories
Get all expense categories (15 IRS Schedule E categories).

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "guid",
      "name": "string",
      "scheduleELine": 3
    }
  ],
  "totalCount": 15
}
```

### GET /properties/{id}/expenses
Get expenses for a specific property.

**Query Parameters:**
- `year` (optional): Tax year filter

---

## Income Controller

**Base Route:** `/api/v1`

### GET /income
Get all income with filtering and pagination.

### GET /income/{id}
Get income by ID.

### POST /income
Create income record.

**Request:**
```json
{
  "propertyId": "guid",
  "amount": 0.00,
  "date": "2025-01-01",
  "source": "string (optional)",
  "description": "string (optional)"
}
```

### PUT /income/{id}
Update income record.

### DELETE /income/{id}
Soft delete income record.

### GET /income/totals
Get income totals for a year.

### GET /properties/{id}/income
Get income for a specific property.

---

## Dashboard Controller

**Base Route:** `/api/v1/dashboard`

### GET /dashboard/summary
Get dashboard summary data.

**Query Parameters:**
- `year` (optional): Tax year filter

---

## Health Controller

**Base Route:** `/api/v1/health`

### GET /health
Basic health check. Returns 200 if API is running.

**Response:** `200 OK`
```json
{
  "status": "Healthy"
}
```

### GET /health/ready
Readiness check. Returns 200 if database is connected.

---

## Error Responses

All errors follow RFC 7807 Problem Details format:

```json
{
  "type": "https://propertymanager.app/errors/not-found",
  "title": "Resource not found",
  "status": 404,
  "detail": "Property 'guid' does not exist",
  "instance": "/api/v1/properties/guid",
  "traceId": "string"
}
```

### Validation Errors (400)
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["Name is required"],
    "Amount": ["Amount must be greater than 0"]
  }
}
```

---

## Authentication Flow

1. User registers via `/auth/register`
2. User logs in via `/auth/login` → receives accessToken + refreshToken
3. Access token used in Authorization header for all protected endpoints
4. When access token expires, use `/auth/refresh` with refreshToken
5. On logout, call `/auth/logout` to invalidate refresh token
