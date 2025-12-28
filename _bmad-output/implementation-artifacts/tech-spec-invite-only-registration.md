# Tech-Spec: Invite-Only Registration

**Created:** 2025-12-28
**Status:** Ready for Development
**Approach:** TDD (Test-Driven Development)

---

## Overview

### Problem Statement

The application currently has public registration, allowing anyone to create an account. Before sharing with family and friends, access must be controlled so only invited users can register.

### Solution

Replace public registration with an invitation-based system where:
- Only existing account owners can send invitations
- Invited users receive an email with a registration link
- Each invited user creates their own independent account (separate data)
- Public registration is completely removed (dead code elimination)

### Scope

**In Scope:**
- Invitation entity and database table
- Send invitation endpoint (owner-only)
- Invitation email with secure token link
- Modified registration to require valid invitation token
- Invited users create their own Account (Owner role)
- Skip email verification for invited users (invitation = trust)
- 24-hour invitation expiry
- Remove all public registration code
- TDD: Tests written before implementation

**Out of Scope (Future):**
- Invitation management UI (view/resend/revoke)
- Bulk invitations
- Role selection when inviting
- Invitation to join existing account (shared data)
- Frontend role-based route guards

### User Mechanism for Sending Invitations

**Approach:** Bash script (no UI needed)

The account owner (Dave) will use a simple bash script to send invitations. This avoids frontend complexity with role-based route guards.

**Example Script:** `scripts/invite-user.sh`

```bash
#!/bin/bash

# invite-user.sh - Send an invitation to a new user
# Usage: ./scripts/invite-user.sh user@example.com

set -e

API_URL="${API_URL:-http://localhost:5292/api/v1}"
EMAIL="$1"

if [ -z "$EMAIL" ]; then
  echo "Usage: $0 <email>"
  echo "Example: $0 friend@example.com"
  exit 1
fi

# Prompt for credentials
read -p "Your email: " OWNER_EMAIL
read -s -p "Your password: " OWNER_PASSWORD
echo

# Step 1: Login to get access token
echo "Logging in..."
LOGIN_RESPONSE=$(curl -s -X POST "$API_URL/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\": \"$OWNER_EMAIL\", \"password\": \"$OWNER_PASSWORD\"}")

ACCESS_TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.accessToken')

if [ "$ACCESS_TOKEN" == "null" ] || [ -z "$ACCESS_TOKEN" ]; then
  echo "Login failed. Response:"
  echo "$LOGIN_RESPONSE"
  exit 1
fi

echo "Login successful."

# Step 2: Send invitation
echo "Sending invitation to $EMAIL..."
INVITE_RESPONSE=$(curl -s -X POST "$API_URL/auth/invite" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d "{\"email\": \"$EMAIL\"}")

echo "Response:"
echo "$INVITE_RESPONSE" | jq .

echo "Done! Check MailHog at http://localhost:8025 for the invitation email."
```

**Requirements:**
- `curl` and `jq` installed
- API running locally (or set `API_URL` environment variable)
- MailHog running for email capture in development

---

## Context for Development

### Codebase Patterns

| Pattern | Example | Apply To |
|---------|---------|----------|
| CQRS Commands | `Application/Auth/Register.cs` | `SendInvitation.cs` |
| Entity + Config | `Domain/Entities/RefreshToken.cs` + `Configurations/RefreshTokenConfiguration.cs` | Invitation entity |
| Token hashing | `RefreshToken.TokenHash` | Invitation token storage |
| Tenant isolation | `ITenantEntity` interface | Invitation entity |
| Email templates | `SmtpEmailService.SendVerificationEmailAsync` | Invitation email |
| FluentValidation | Validators in command files | SendInvitation validation |

### Files to Reference

**Patterns to follow:**
- `backend/src/PropertyManager.Application/Auth/Register.cs` - Command/Handler/Validator pattern
- `backend/src/PropertyManager.Domain/Entities/RefreshToken.cs` - Token entity pattern
- `backend/src/PropertyManager.Infrastructure/Email/SmtpEmailService.cs` - Email template pattern
- `backend/src/PropertyManager.Infrastructure/Identity/IdentityService.cs` - User creation pattern

**Test patterns to follow:**
- `backend/tests/PropertyManager.Application.Tests/` - Handler test patterns
- `backend/tests/PropertyManager.Api.Tests/` - Controller/integration test patterns

### Technical Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Token format | GUID, URL-safe base64 encoded | Simple, secure, no collisions |
| Token storage | SHA256 hash in database | Security: raw token never stored |
| Invitation expiry | 24 hours | User requirement |
| Email verification | Skipped for invitees | Invitation implies trusted email |
| Account name | Derived from email (prefix before @) | Simplify invitee experience |
| Role for invitees | "Owner" | Each user owns their own data |
| Authorization | Only "Owner" role can send invitations | Matches current role system |

---

## Implementation Plan

### Task Sequence (TDD Order)

#### Phase 1: Backend - Domain & Infrastructure

- [ ] **Task 1.1:** Create `Invitation` entity
  - File: `Domain/Entities/Invitation.cs`
  - Test: N/A (entity is data structure)
  - Fields: Id, AccountId, Email, TokenHash, InvitedByUserId, ExpiresAt, AcceptedAt, CreatedAt, UpdatedAt
  - Implement `ITenantEntity`

- [ ] **Task 1.2:** Create `InvitationConfiguration`
  - File: `Infrastructure/Persistence/Configurations/InvitationConfiguration.cs`
  - Indexes: Email, AccountId, TokenHash
  - Test: Migration applies successfully

- [ ] **Task 1.3:** Add DbSet and query filter
  - Files: `IAppDbContext.cs`, `AppDbContext.cs`
  - Add `DbSet<Invitation> Invitations`
  - Add tenant query filter

- [ ] **Task 1.4:** Create database migration
  - Command: `dotnet ef migrations add AddInvitationsTable`
  - Verify migration SQL

#### Phase 2: Backend - Send Invitation (TDD)

- [ ] **Task 2.1:** Write tests for `SendInvitation` command
  - File: `tests/PropertyManager.Application.Tests/Auth/SendInvitationTests.cs`
  - Test cases:
    - Valid invitation creates record and sends email
    - Validation fails for invalid email format
    - Validation fails for empty email
    - Duplicate pending invitation for same email returns error
    - Only Owner role can send invitations
    - Invitation token is hashed before storage

- [ ] **Task 2.2:** Add `SendInvitationEmailAsync` to email service
  - Files: `IEmailService.cs`, `SmtpEmailService.cs`
  - Test: Email content contains correct link with token

- [ ] **Task 2.3:** Implement `SendInvitation` command/handler
  - File: `Application/Auth/SendInvitation.cs`
  - Command: `SendInvitationCommand { Email }`
  - Handler: Create invitation, hash token, send email
  - Validator: Email format, not empty

- [ ] **Task 2.4:** Add `POST /api/v1/auth/invite` endpoint
  - File: `Api/Controllers/AuthController.cs`
  - Authorization: `[Authorize(Roles = "Owner")]`
  - Test: Returns 401 for unauthenticated, 403 for non-owner

#### Phase 3: Backend - Accept Invitation / Register (TDD)

- [ ] **Task 3.1:** Write tests for modified registration
  - File: `tests/PropertyManager.Application.Tests/Auth/RegisterWithInvitationTests.cs`
  - Test cases:
    - Valid token + password creates account and user
    - Expired invitation returns error
    - Already-accepted invitation returns error
    - Invalid/unknown token returns error
    - Missing token returns error (no public registration)
    - Account name derived from email
    - User role is "Owner"
    - Email verification is skipped (EmailConfirmed = true)
    - Invitation marked as accepted after registration

- [ ] **Task 3.2:** Modify `Register` command to require invitation
  - File: `Application/Auth/Register.cs`
  - Remove: AccountName from command
  - Add: InvitationToken to command
  - Validate invitation before creating user
  - Set EmailConfirmed = true
  - Mark invitation as accepted

- [ ] **Task 3.3:** Remove public registration
  - Delete or modify endpoint to require token
  - Remove AccountName from request DTO

#### Phase 4: Backend - Cleanup Dead Code

- [ ] **Task 4.1:** Remove public registration code paths
  - Remove AccountName from RegisterCommand
  - Remove any code paths that allow registration without token
  - Ensure tests fail if public registration attempted

- [ ] **Task 4.2:** Update API documentation/Swagger
  - Reflect new endpoint and changed register endpoint

#### Phase 5: Frontend (TDD with Vitest)

- [ ] **Task 5.1:** Write tests for invitation registration flow
  - File: `frontend/src/app/features/auth/register/register.component.spec.ts`
  - Test cases:
    - Extracts token from URL query param
    - Shows error if no token in URL
    - Shows only password fields (no email, no account name)
    - Displays email from invitation (read-only)
    - Submits with token and password
    - Redirects to login on success

- [ ] **Task 5.2:** Modify register component
  - File: `features/auth/register/register.component.ts`
  - Extract `token` from route query params
  - Fetch invitation details to get email (optional: or just accept password)
  - Remove account name field
  - Make email read-only (from invitation)
  - Call modified register endpoint with token

- [ ] **Task 5.3:** Update auth service
  - File: `core/services/auth.service.ts`
  - Modify `register()` to accept token instead of accountName
  - Add `getInvitationDetails(token)` method (optional)

- [ ] **Task 5.4:** Remove dead frontend code
  - Remove AccountName form field and validation
  - Remove any public registration messaging

#### Phase 6: Scripts & E2E Tests

- [ ] **Task 6.1:** Create invitation bash script
  - File: `scripts/invite-user.sh`
  - Functionality: Login, get token, call invite endpoint
  - Make executable: `chmod +x scripts/invite-user.sh`

- [ ] **Task 6.2:** Write Playwright E2E tests
  - File: `frontend/e2e/tests/auth/invitation.spec.ts`
  - Test full flow: send invite → receive email → click link → register → login

---

## Acceptance Criteria

### Send Invitation

- [ ] **AC1:** Given I am logged in as an Owner, when I submit an email address to `/api/v1/auth/invite`, then an invitation record is created with a hashed token and expiry of 24 hours
- [ ] **AC2:** Given I send an invitation, when the email is sent, then it contains a link to `/register?token={token}` with the raw (unhashed) token
- [ ] **AC3:** Given I am not logged in, when I call `/api/v1/auth/invite`, then I receive 401 Unauthorized
- [ ] **AC4:** Given I am logged in as a Contributor, when I call `/api/v1/auth/invite`, then I receive 403 Forbidden
- [ ] **AC5:** Given an invitation already exists for an email (pending), when I try to invite the same email, then I receive an error

### Accept Invitation (Register)

- [ ] **AC6:** Given I have a valid invitation token, when I submit a password to `/api/v1/auth/register`, then a new Account and User (Owner role) are created
- [ ] **AC7:** Given I register via invitation, when my user is created, then EmailConfirmed is set to true (no verification needed)
- [ ] **AC8:** Given I register via invitation, when my Account is created, then the Account name is derived from my email (prefix before @)
- [ ] **AC9:** Given my invitation token is expired (>24 hours), when I try to register, then I receive an error
- [ ] **AC10:** Given my invitation token was already used, when I try to register, then I receive an error
- [ ] **AC11:** Given no invitation token is provided, when I call `/api/v1/auth/register`, then I receive an error (public registration disabled)

### Frontend

- [ ] **AC12:** Given I navigate to `/register` without a token, when the page loads, then I see an error message (no public registration)
- [ ] **AC13:** Given I navigate to `/register?token=xxx`, when the page loads, then I see only password fields (no email input, no account name)
- [ ] **AC14:** Given I complete registration via invitation, when successful, then I am redirected to login and can sign in immediately

### Dead Code Removal

- [ ] **AC15:** Given the codebase, when I search for public registration code paths, then none exist
- [ ] **AC16:** Given the RegisterCommand, when I inspect it, then AccountName is not a property

---

## Additional Context

### Dependencies

- No new external packages required
- Uses existing: FluentValidation, MediatR, ASP.NET Core Identity, EF Core

### Testing Strategy

| Layer | Framework | Focus |
|-------|-----------|-------|
| Unit (Handlers) | xUnit + Moq | Business logic, validation |
| Integration (API) | xUnit + WebApplicationFactory | HTTP endpoints, auth |
| Unit (Frontend) | Vitest | Component behavior |
| E2E | Playwright | Full user journey |

### Database Migration Notes

```sql
-- Expected migration creates:
CREATE TABLE "Invitations" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "AccountId" uuid NOT NULL REFERENCES "Accounts"("Id") ON DELETE CASCADE,
    "Email" varchar(256) NOT NULL,
    "TokenHash" varchar(500) NOT NULL,
    "InvitedByUserId" uuid NOT NULL,
    "ExpiresAt" timestamp NOT NULL,
    "AcceptedAt" timestamp NULL,
    "CreatedAt" timestamp NOT NULL,
    "UpdatedAt" timestamp NOT NULL
);

CREATE INDEX "IX_Invitations_Email" ON "Invitations" ("Email");
CREATE INDEX "IX_Invitations_AccountId" ON "Invitations" ("AccountId");
CREATE INDEX "IX_Invitations_TokenHash" ON "Invitations" ("TokenHash");
```

### Email Template Content

**Subject:** You're invited to Property Manager

**Body:**
```
Hi,

You've been invited to join Property Manager - a simple tool for tracking rental property expenses and generating Schedule E tax reports.

Click the link below to create your account:
{BaseUrl}/register?token={token}

This invitation expires in 24 hours.

If you didn't expect this invitation, you can safely ignore this email.

- Property Manager
```

### Security Considerations

- Token is cryptographically random (GUID or secure random bytes)
- Only token hash stored in database (raw token sent in email only)
- Invitation expires after 24 hours
- Invitation can only be used once (marked as accepted)
- No user enumeration: same error for invalid/expired/used tokens

### Notes

- Account name derived from email: `dave@example.com` → Account name: `dave`
- If email prefix contains special characters, sanitize or use full email
- Consider: rate limiting on invitation endpoint (future)
- Consider: invitation audit log (future)
