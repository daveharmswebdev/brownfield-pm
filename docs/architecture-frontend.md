# Architecture - Frontend

## Overview

Angular 20 Single Page Application with signal-based state management.

**Primary Technologies:**
- Angular 20.3
- TypeScript 5.9
- @ngrx/signals
- Angular Material 20
- RxJS 7.8
- Vitest + Playwright

---

## Architecture Pattern

### Feature-Based Module Structure

```
src/app/
├── core/           # Singleton services (provided in root)
├── shared/         # Reusable, stateless components
└── features/       # Feature modules (lazy-loaded)
```

### Module Responsibilities

**Core Module:**
- Authentication services
- HTTP interceptors
- Route guards
- Shell/navigation components
- API client (generated)

**Shared Module:**
- Presentational components
- Pipes and directives
- No services or state

**Feature Modules:**
- Business logic components
- Signal stores
- Feature-specific services
- Lazy-loaded routes

---

## State Management

### @ngrx/signals Pattern

Signal stores provide reactive state without NgRx boilerplate:

```typescript
export const FeatureStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed((store) => ({
    // Derived signals
    total: computed(() => store.items().reduce((sum, i) => sum + i.amount, 0)),
  })),
  withMethods((store, service = inject(Service)) => ({
    // Async actions with rxMethod
    loadData: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { loading: true })),
        switchMap(() => service.getData()),
        tap((data) => patchState(store, { items: data, loading: false })),
        catchError((error) => {
          patchState(store, { error: error.message, loading: false });
          return EMPTY;
        })
      )
    ),
  }))
);
```

### Store Patterns

**State Interface:**
```typescript
interface FeatureState {
  items: Item[];
  isLoading: boolean;
  error: string | null;
}
```

**Initial State:**
```typescript
const initialState: FeatureState = {
  items: [],
  isLoading: false,
  error: null,
};
```

### Available Stores

| Store | Purpose | Location |
|-------|---------|----------|
| PropertyStore | Property CRUD, selected property | `features/properties/stores/` |
| ExpenseStore | Per-property expense workspace | `features/expenses/stores/` |
| ExpenseListStore | Cross-property expense list | `features/expenses/stores/` |
| IncomeStore | Per-property income workspace | `features/income/stores/` |
| IncomeListStore | Cross-property income list | `features/income/stores/` |

---

## Component Architecture

### Standalone Components

All components are standalone (no NgModules):

```typescript
@Component({
  selector: 'app-feature',
  standalone: true,
  imports: [CommonModule, MatButtonModule, SharedComponent],
  template: `...`,
})
export class FeatureComponent {}
```

### Component Types

**Smart Components (Containers):**
- Inject stores
- Handle business logic
- Located in `features/*/pages/`

**Presentational Components:**
- Input/Output only
- No injected services
- Located in `features/*/components/` or `shared/components/`

### Component Communication

```
┌─────────────────────────────────────────┐
│           Smart Component               │
│  ┌─────────────────────────────────┐    │
│  │        Signal Store             │    │
│  └─────────────────────────────────┘    │
│              │                          │
│     @Input() │ @Output()                │
│              ▼                          │
│  ┌─────────────────────────────────┐    │
│  │   Presentational Component      │    │
│  └─────────────────────────────────┘    │
└─────────────────────────────────────────┘
```

---

## Routing

### Route Configuration

```typescript
export const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/...') },
      { path: 'properties', loadComponent: () => import('./features/properties/...') },
      { path: 'properties/:id', loadComponent: () => import('./features/properties/...') },
      { path: 'expenses', loadComponent: () => import('./features/expenses/...') },
      { path: 'income', loadComponent: () => import('./features/income/...') },
    ],
  },
  { path: 'login', loadComponent: () => import('./features/auth/...'), canActivate: [guestGuard] },
  { path: 'register', loadComponent: () => import('./features/auth/...'), canActivate: [guestGuard] },
];
```

### Guards

**authGuard:** Requires authenticated user
**guestGuard:** Requires unauthenticated user (login/register only)

---

## API Integration

### NSwag Client Generation

TypeScript API client generated from OpenAPI spec:

```bash
npm run generate-api
```

Generates:
- Service classes with typed methods
- Request/Response DTOs
- Proper error handling

### API Service Location

`src/app/core/api/api.service.ts` (generated)

### HTTP Interceptors

**Token Interceptor:**
```typescript
export const tokenInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.getToken();

  if (token) {
    req = req.clone({
      setHeaders: { Authorization: `Bearer ${token}` }
    });
  }

  return next(req);
};
```

---

## Authentication

### Auth Service

```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
  private currentUser = signal<User | null>(null);

  readonly isAuthenticated = computed(() => !!this.currentUser());
  readonly user = this.currentUser.asReadonly();

  login(credentials: LoginRequest): Observable<void> { }
  logout(): void { }
  refreshToken(): Observable<void> { }
}
```

### Token Storage

- Access token: Memory (signal)
- Refresh token: localStorage (with secure flag consideration)

### Auth Flow

1. User submits login form
2. AuthService calls API
3. Tokens stored, user redirected to dashboard
4. Token interceptor adds Bearer header to requests
5. On 401, attempt token refresh
6. On refresh failure, redirect to login

---

## UI Framework

### Angular Material

Material Design components with custom theming:

```scss
// styles.scss
@use '@angular/material' as mat;

$primary: mat.m2-define-palette(mat.$m2-indigo-palette);
$accent: mat.m2-define-palette(mat.$m2-pink-palette);

@include mat.all-component-themes((
  color: (primary: $primary, accent: $accent),
  typography: mat.m2-define-typography-config(),
  density: 0,
));
```

### Responsive Design

- Desktop: Sidebar navigation
- Mobile: Bottom navigation bar
- Breakpoint: 768px

---

## Forms

### Reactive Forms

```typescript
form = new FormGroup({
  name: new FormControl('', [Validators.required, Validators.maxLength(100)]),
  amount: new FormControl<number | null>(null, [Validators.required, Validators.min(0.01)]),
  date: new FormControl<Date | null>(null, Validators.required),
});
```

### Form Validation Display

```html
<mat-form-field>
  <input matInput formControlName="name">
  @if (form.controls.name.hasError('required')) {
    <mat-error>Name is required</mat-error>
  }
</mat-form-field>
```

---

## Testing

### Unit Tests (Vitest)

```typescript
describe('PropertyStore', () => {
  let store: InstanceType<typeof PropertyStore>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PropertyStore]
    });
    store = TestBed.inject(PropertyStore);
  });

  it('should load properties', () => {
    // Test implementation
  });
});
```

### E2E Tests (Playwright)

Page Object Model pattern:
```typescript
export class DashboardPage extends BasePage {
  async goto() {
    await this.page.goto('/dashboard');
  }

  async getPropertyCount() {
    return this.page.locator('[data-testid="property-count"]').textContent();
  }
}
```

---

## Build & Bundle

### Angular Build

```bash
npm run build  # Production build
```

### Bundle Analysis

```bash
npm run analyze  # source-map-explorer
```

### Budget Configuration

```json
{
  "budgets": [
    { "type": "initial", "maximumWarning": "550kB", "maximumError": "1MB" },
    { "type": "anyComponentStyle", "maximumWarning": "6kB" }
  ]
}
```

---

## Development Workflow

### Dev Server with Proxy

```bash
npm start  # Starts with proxy to backend
```

`proxy.conf.json`:
```json
{
  "/api": {
    "target": "http://localhost:5292",
    "secure": false
  }
}
```

### API Client Regeneration

After backend API changes:
```bash
npm run generate-api
```
