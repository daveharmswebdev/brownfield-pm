import { Routes } from '@angular/router';
import { authGuard, guestGuard, publicGuard } from './core/auth/auth.guard';
import { unsavedChangesGuard } from './core/guards/unsaved-changes.guard';

export const routes: Routes = [
  // Public auth routes (only for guests) (AC7.6)
  {
    path: 'register',
    loadComponent: () =>
      import('./features/auth/register/register.component').then(
        (m) => m.RegisterComponent
      ),
    canActivate: [guestGuard],
  },
  {
    path: 'verify-email',
    loadComponent: () =>
      import('./features/auth/verify-email/verify-email.component').then(
        (m) => m.VerifyEmailComponent
      ),
    canActivate: [publicGuard],
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login/login.component').then(
        (m) => m.LoginComponent
      ),
    canActivate: [guestGuard],
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./features/auth/forgot-password/forgot-password.component').then(
        (m) => m.ForgotPasswordComponent
      ),
    canActivate: [guestGuard],
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./features/auth/reset-password/reset-password.component').then(
        (m) => m.ResetPasswordComponent
      ),
    canActivate: [publicGuard],
  },

  // Protected routes wrapped in Shell layout (AC7.6, AC7.7)
  {
    path: '',
    loadComponent: () =>
      import('./core/components/shell/shell.component').then(
        (m) => m.ShellComponent
      ),
    canActivate: [authGuard],
    children: [
      // Dashboard (AC7.7) - default route
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(
            (m) => m.DashboardComponent
          ),
      },
      // Properties (AC7.7, AC-2.1.1)
      {
        path: 'properties',
        loadComponent: () =>
          import('./features/properties/properties.component').then(
            (m) => m.PropertiesComponent
          ),
      },
      {
        path: 'properties/new',
        loadComponent: () =>
          import('./features/properties/property-form/property-form.component').then(
            (m) => m.PropertyFormComponent
          ),
      },
      // Property Detail (AC-2.3.1)
      {
        path: 'properties/:id',
        loadComponent: () =>
          import('./features/properties/property-detail/property-detail.component').then(
            (m) => m.PropertyDetailComponent
          ),
      },
      // Property Edit (AC-2.4.1, AC-2.4.3)
      {
        path: 'properties/:id/edit',
        loadComponent: () =>
          import('./features/properties/property-edit/property-edit.component').then(
            (m) => m.PropertyEditComponent
          ),
        canDeactivate: [unsavedChangesGuard],
      },
      // Property Expense Workspace (AC-3.1.1)
      {
        path: 'properties/:id/expenses',
        loadComponent: () =>
          import('./features/expenses/expense-workspace/expense-workspace.component').then(
            (m) => m.ExpenseWorkspaceComponent
          ),
      },
      // Property Income Workspace (AC-4.1.1)
      {
        path: 'properties/:id/income',
        loadComponent: () =>
          import('./features/income/income-workspace/income-workspace.component').then(
            (m) => m.IncomeWorkspaceComponent
          ),
      },
      // Expenses (AC7.7)
      {
        path: 'expenses',
        loadComponent: () =>
          import('./features/expenses/expenses.component').then(
            (m) => m.ExpensesComponent
          ),
      },
      // Income (AC7.7)
      {
        path: 'income',
        loadComponent: () =>
          import('./features/income/income.component').then(
            (m) => m.IncomeComponent
          ),
      },
      // Receipts (AC7.7)
      {
        path: 'receipts',
        loadComponent: () =>
          import('./features/receipts/receipts.component').then(
            (m) => m.ReceiptsComponent
          ),
      },
      // Reports (AC7.7)
      {
        path: 'reports',
        loadComponent: () =>
          import('./features/reports/reports.component').then(
            (m) => m.ReportsComponent
          ),
      },
      // Settings (AC7.7)
      {
        path: 'settings',
        loadComponent: () =>
          import('./features/settings/settings.component').then(
            (m) => m.SettingsComponent
          ),
      },
      // Default child redirect to dashboard
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full',
      },
    ],
  },

  // Catch-all redirect to dashboard
  {
    path: '**',
    redirectTo: '/dashboard',
  },
];
