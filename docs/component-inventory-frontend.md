# Component Inventory - Frontend

## Overview

Angular 20 application using standalone components, @ngrx/signals for state management, and Angular Material for UI.

## Architecture

```
src/app/
├── core/           # Singleton services, guards, shell
├── shared/         # Reusable components
└── features/       # Feature modules (lazy-loaded)
    ├── auth/
    ├── dashboard/
    ├── properties/
    ├── expenses/
    ├── income/
    ├── receipts/
    ├── reports/
    └── settings/
```

---

## Core Components

### Shell Component
`core/components/shell/shell.component.ts`

Main application shell with responsive navigation.
- Desktop: Sidebar navigation
- Mobile: Bottom navigation bar
- Handles auth state display

### Sidebar Navigation
`core/components/sidebar-nav/sidebar-nav.component.ts`

Desktop sidebar with:
- Logo/branding
- Navigation links (Dashboard, Properties, Expenses, Income, Reports)
- User info and logout

### Bottom Navigation
`core/components/bottom-nav/bottom-nav.component.ts`

Mobile bottom nav bar with icon-based navigation.

---

## Shared Components

### Confirm Dialog
`shared/components/confirm-dialog/confirm-dialog.component.ts`

Reusable confirmation dialog with:
- Configurable title/message
- Confirm/Cancel buttons
- Returns boolean result

**Usage:**
```typescript
const dialogRef = dialog.open(ConfirmDialogComponent, {
  data: { title: 'Delete?', message: 'Are you sure?' }
});
```

### Empty State
`shared/components/empty-state/empty-state.component.ts`

Display when lists are empty:
- Icon slot
- Title and description
- Optional action button

### Loading Spinner
`shared/components/loading-spinner/loading-spinner.component.ts`

Centered spinner for loading states.

### Year Selector
`shared/components/year-selector/year-selector.component.ts`

Dropdown to select tax year filter. Emits year change events.

### Property Row
`shared/components/property-row/property-row.component.ts`

Property list item displaying:
- Property name and address
- Expense/Income totals
- Net income calculation
- Navigation to property detail

### Stats Bar
`shared/components/stats-bar/stats-bar.component.ts`

Summary statistics display:
- Total properties count
- Total expenses
- Total income
- Net income

### Error Card
`shared/components/error-card/error-card.component.ts`

Error state display with retry action.

### Not Found
`shared/components/not-found/not-found.component.ts`

404 page component.

---

## Feature Components

### Auth Feature

#### Login Page
`features/auth/pages/login/login.page.ts`

- Email/password form
- Remember me option
- Link to register
- Error handling

#### Register Page
`features/auth/pages/register/register.page.ts`

- Registration form
- Password strength validation
- Email verification flow

---

### Properties Feature

#### Property Store
`features/properties/stores/property.store.ts`

Signal store with:
- Properties list state
- Selected property detail
- Loading/error states
- Computed totals (expenses, income, net)

**State:**
```typescript
interface PropertyState {
  properties: PropertySummaryDto[];
  isLoading: boolean;
  error: string | null;
  selectedYear: number | null;
  selectedProperty: PropertyDetailDto | null;
  isLoadingDetail: boolean;
  isUpdating: boolean;
  isDeleting: boolean;
}
```

**Computed:**
- `totalCount` - Property count
- `totalExpenses` - Sum of all expenses
- `totalIncome` - Sum of all income
- `netIncome` - Income minus expenses
- `isEmpty` - No properties check
- `selectedPropertyNetIncome` - Selected property net

---

### Expenses Feature

#### Expense Store
`features/expenses/stores/expense.store.ts`

Signal store for expense workspace (per-property view).

#### Expense List Store
`features/expenses/stores/expense-list.store.ts`

Signal store for cross-property expense list with filters.

#### Expense Form
`features/expenses/components/expense-form/expense-form.component.ts`

Create expense form:
- Amount input (currency formatted)
- Date picker
- Category dropdown
- Description textarea
- Duplicate detection on submit

#### Expense Row
`features/expenses/components/expense-row/expense-row.component.ts`

Expense list item with inline edit/delete.

#### Expense List Row
`features/expenses/components/expense-list-row/expense-list-row.component.ts`

Cross-property list item showing property name.

#### Category Select
`features/expenses/components/category-select/category-select.component.ts`

Category dropdown using mat-select.

#### Expense Filters
`features/expenses/components/expense-filters/expense-filters.component.ts`

Filter panel:
- Date range
- Category multi-select
- Search text
- Clear filters

#### Duplicate Warning Dialog
`features/expenses/components/duplicate-warning-dialog/duplicate-warning-dialog.component.ts`

Warning dialog when potential duplicate detected.

---

### Income Feature

#### Income Store
`features/income/stores/income.store.ts`

Signal store for income workspace.

#### Income List Store
`features/income/stores/income-list.store.ts`

Signal store for cross-property income list.

#### Income Form
`features/income/components/income-form/income-form.component.ts`

Create income form:
- Amount input
- Date picker
- Source input
- Description textarea

#### Income Row
`features/income/components/income-row/income-row.component.ts`

Income list item with inline edit/delete.

---

## Signal Store Pattern

All stores follow this pattern:

```typescript
export const FeatureStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed((store) => ({
    // Derived signals
  })),
  withMethods((store) => ({
    // Actions using rxMethod
    loadData: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { isLoading: true })),
        switchMap(() => service.getData().pipe(
          tap((data) => patchState(store, { data, isLoading: false })),
          catchError((error) => {
            patchState(store, { error: error.message, isLoading: false });
            return of(null);
          })
        ))
      )
    ),
  }))
);
```

---

## API Client

`core/api/api.service.ts`

NSwag-generated TypeScript client. Regenerate after backend changes:

```bash
npm run generate-api
```

Generates typed DTOs and service methods matching backend OpenAPI spec.

---

## Services

### Property Service
`features/properties/services/property.service.ts`

Wraps API client with additional logic.

### Auth Service
`core/auth/auth.service.ts`

Authentication handling:
- Login/logout
- Token storage
- Token refresh
- Current user state

### Token Interceptor
`core/auth/token.interceptor.ts`

HTTP interceptor adding Bearer token to requests.

---

## Guards

### Auth Guard
`core/guards/auth.guard.ts`

Route guard requiring authentication.

### Guest Guard
`core/guards/guest.guard.ts`

Route guard for unauthenticated users only (login/register pages).
