# E2E Test Suite

End-to-end tests for Property Manager using Playwright.

## Quick Start

```bash
# Run all E2E tests
npm run test:e2e

# Run with UI for debugging
npm run test:e2e:ui

# Run specific test file
npx playwright test e2e/tests/expenses/expense-flow.spec.ts

# View test report
npm run test:e2e:report
```

## Test Structure

```
e2e/
├── fixtures/
│   └── test-fixtures.ts       # Playwright fixture definitions
├── helpers/
│   ├── auth.helper.ts         # Authentication utilities
│   ├── currency.helper.ts     # Currency formatting/parsing
│   ├── date.helper.ts         # Date formatting utilities
│   ├── mailhog.helper.ts      # Email verification utilities
│   ├── test-data.helper.ts    # Test data generators
│   └── test-setup.helper.ts   # Shared setup (createPropertyAndGetId)
├── pages/
│   ├── base.page.ts           # Abstract base page object
│   ├── dashboard.page.ts      # Dashboard interactions
│   ├── expense-workspace.page.ts  # Expense CRUD operations
│   ├── income-workspace.page.ts   # Income CRUD operations
│   ├── login.page.ts              # Login page
│   ├── property-detail.page.ts    # Property detail/edit
│   ├── property-form.page.ts      # Property creation form
│   └── register.page.ts           # Registration page
└── tests/
    ├── auth/
    │   └── auth-flow.spec.ts      # Authentication tests (3 tests)
    ├── expenses/
    │   └── expense-flow.spec.ts   # Expense CRUD tests (7 tests)
    ├── income/
    │   └── income-flow.spec.ts    # Income CRUD tests (6 tests)
    └── properties/
        ├── property-edit.spec.ts  # Property edit/delete tests (4 tests)
        └── property-flow.spec.ts  # Property creation tests (3 tests)
```

## Test Patterns

### Page Object Pattern

All page interactions are encapsulated in page objects that extend `BasePage`:

```typescript
import { BasePage } from './base.page';

export class MyPage extends BasePage {
  readonly myButton: Locator;

  constructor(page: Page) {
    super(page);
    this.myButton = page.locator('[data-testid="my-button"]');
  }

  async goto(): Promise<void> {
    await this.page.goto('/my-page');
  }

  async clickMyButton(): Promise<void> {
    await this.myButton.click();
    await this.expectSnackBar('Action completed');
  }
}
```

### Fixture Pattern

Page objects are injected via Playwright fixtures:

```typescript
import { test, expect } from '../../fixtures/test-fixtures';

test('my test', async ({
  authenticatedUser,  // Auto-registers and logs in
  dashboardPage,      // Dashboard page object
  propertyFormPage,   // Property form page object
}) => {
  // authenticatedUser fixture ensures user is logged in
  await dashboardPage.goto();
  // ...
});
```

### Test Data Generation

Use `TestDataHelper` for consistent test data with unique timestamps:

```typescript
import { TestDataHelper } from '../../helpers/test-data.helper';

const property = TestDataHelper.generateProperty();
// { name: "Test Property 1703547891234", ... }

const expense = TestDataHelper.generateExpense({
  amount: '150.00',
  category: 'Repairs',
});
```

### Property Setup Helper

For tests that need an existing property:

```typescript
import { createPropertyAndGetId } from '../../helpers/test-setup.helper';

test('should add expense', async ({
  page,
  authenticatedUser,
  dashboardPage,
  propertyFormPage,
  expenseWorkspacePage,
}) => {
  // Create property and get its ID
  const { propertyId, propertyData } = await createPropertyAndGetId(
    dashboardPage,
    propertyFormPage,
    page
  );

  // Navigate using the property ID
  await expenseWorkspacePage.gotoWithPropertyId(propertyId);
});
```

## Selector Strategy

Use selectors in this priority order for resilience:

1. **`getByRole()`** - Most resilient for accessibility
2. **`getByLabel()`** - For form fields
3. **`getByText()`** - For visible text content
4. **`locator('[data-testid="..."]')`** - For non-semantic elements
5. **Angular component selectors** (`app-expense-row`) - For component boundaries

### Angular Material Considerations

- Snackbars: Use `[matsnackbarlabel]` for text (not parent container)
- Dialogs: Use `mat-dialog-container` as root (renders in overlay)
- Selects: Options render in `mat-option` within overlay panel
- Avoid brittle CSS class selectors (`.mdc-*`) - these can change

## Test Independence

Tests must be independent and runnable in any order:

1. Each test creates its own data using timestamp-based unique identifiers
2. Tests should not depend on data from other tests
3. Use `authenticatedUser` fixture for consistent auth state

## Shared Helpers

### Date Formatting

```typescript
import { formatDateForInput, createPastDate } from '../helpers/date.helper';

// Format date for input fields (MM/DD/YYYY)
const dateStr = formatDateForInput(new Date()); // "12/26/2025"

// Create a date in the past
const lastWeek = createPastDate(7);
```

### Currency Formatting

```typescript
import { formatCurrency, parseCurrency } from '../helpers/currency.helper';

formatCurrency(1500);        // "$1,500.00"
parseCurrency("$1,500.00");  // 1500
```

### BasePage Methods

All page objects inherit these methods from `BasePage`:

```typescript
// Snackbar assertions
await page.expectSnackBar('Success message');
await page.waitForSnackBar('Success message', 10000);

// Confirmation dialogs
await page.waitForConfirmDialog();
await page.confirmDialogAction('Item deleted');
await page.cancelDialogAction();

// Empty state
await page.expectEmptyState();

// Navigation
await page.waitForNavigation('/dashboard');
await page.waitForNavigation(/\/properties\/[a-f0-9-]+$/);
```

## Best Practices

### DO

- ✅ Use page objects for all interactions
- ✅ Generate unique test data with timestamps
- ✅ Wait for network idle before assertions on filtered data
- ✅ Use semantic selectors (roles, labels, text)
- ✅ Keep tests focused on single user flows

### DON'T

- ❌ Share mutable state between tests
- ❌ Use arbitrary sleep/delays (use proper waits)
- ❌ Hard-code IDs or test data
- ❌ Use brittle CSS class selectors
- ❌ Mock API calls (E2E should use real backend)

## Test Count

Current test suite: **23 tests**

| Category | File | Tests |
|----------|------|-------|
| Auth | auth-flow.spec.ts | 3 |
| Properties | property-flow.spec.ts | 3 |
| Properties | property-edit.spec.ts | 4 |
| Expenses | expense-flow.spec.ts | 7 |
| Income | income-flow.spec.ts | 6 |

## Debugging

### Visual Debugging

```bash
# Run with Playwright UI
npm run test:e2e:ui

# Run with browser visible
npx playwright test --headed
```

### Trace Viewer

Failed tests generate traces. View with:

```bash
npx playwright show-trace path/to/trace.zip
```

### Console Logs

Add `console.log` statements in tests and view in terminal output.

## Environment Requirements

- PostgreSQL database running (via Docker)
- MailHog for email testing
- Backend API running at http://localhost:5292
- Frontend running at http://localhost:4200

Start all services:

```bash
docker compose up -d db mailhog
cd backend && dotnet run --project src/PropertyManager.Api
cd frontend && npm start
```
