/**
 * Playwright Test Fixtures for E2E Tests
 *
 * Extends Playwright's base test with page object fixtures and authentication.
 * Import `test` and `expect` from this file instead of `@playwright/test`.
 *
 * @module e2e/fixtures/test-fixtures
 *
 * @example
 * ```typescript
 * import { test, expect } from '../../fixtures/test-fixtures';
 *
 * test('my test', async ({
 *   authenticatedUser,  // Auto-handles registration and login
 *   dashboardPage,      // Dashboard page object
 *   expenseWorkspacePage,
 * }) => {
 *   // Test code here
 * });
 * ```
 */

import { test as base, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { RegisterPage } from '../pages/register.page';
import { DashboardPage } from '../pages/dashboard.page';
import { PropertyFormPage } from '../pages/property-form.page';
import { PropertyDetailPage } from '../pages/property-detail.page';
import { ExpenseWorkspacePage } from '../pages/expense-workspace.page';
import { IncomeWorkspacePage } from '../pages/income-workspace.page';
import { AuthHelper } from '../helpers/auth.helper';
import { MailHogHelper } from '../helpers/mailhog.helper';
import { type TestUser } from '../helpers/test-data.helper';

/**
 * Custom fixture types for E2E tests
 */
type Fixtures = {
  /** Login page object */
  loginPage: LoginPage;
  /** Registration page object */
  registerPage: RegisterPage;
  /** Dashboard page object */
  dashboardPage: DashboardPage;
  /** Property creation form page object */
  propertyFormPage: PropertyFormPage;
  /** Property detail/edit page object */
  propertyDetailPage: PropertyDetailPage;
  /** Expense workspace page object */
  expenseWorkspacePage: ExpenseWorkspacePage;
  /** Income workspace page object */
  incomeWorkspacePage: IncomeWorkspacePage;
  /** Authentication helper for registration/login flows */
  authHelper: AuthHelper;
  /** MailHog helper for email verification */
  mailhog: MailHogHelper;
  /**
   * Authenticated user fixture.
   *
   * When used, automatically registers a new user, verifies email,
   * and logs in before the test runs. The test user data is returned.
   */
  authenticatedUser: TestUser;
};

/**
 * Extended Playwright test with custom fixtures.
 *
 * Use this instead of importing `test` from `@playwright/test`.
 */
export const test = base.extend<Fixtures>({
  loginPage: async ({ page }, use) => {
    await use(new LoginPage(page));
  },

  registerPage: async ({ page }, use) => {
    await use(new RegisterPage(page));
  },

  dashboardPage: async ({ page }, use) => {
    await use(new DashboardPage(page));
  },

  propertyFormPage: async ({ page }, use) => {
    await use(new PropertyFormPage(page));
  },

  propertyDetailPage: async ({ page }, use) => {
    await use(new PropertyDetailPage(page));
  },

  expenseWorkspacePage: async ({ page }, use) => {
    await use(new ExpenseWorkspacePage(page));
  },

  incomeWorkspacePage: async ({ page }, use) => {
    await use(new IncomeWorkspacePage(page));
  },

  authHelper: async ({ page }, use) => {
    const mailhogUrl = process.env.MAILHOG_URL || 'http://localhost:8025';
    await use(new AuthHelper(page, mailhogUrl));
  },

  mailhog: async ({}, use) => {
    const mailhogUrl = process.env.MAILHOG_URL || 'http://localhost:8025';
    await use(new MailHogHelper(mailhogUrl));
  },

  /**
   * Authenticated user fixture.
   *
   * This fixture handles the full authentication flow:
   * 1. Generates unique test user data
   * 2. Registers the user via the registration form
   * 3. Retrieves email verification token from MailHog
   * 4. Verifies the email address
   * 5. Logs in with the verified credentials
   *
   * The resulting user is logged in and ready for test interactions.
   */
  authenticatedUser: async ({ page, authHelper }, use) => {
    const user = await authHelper.registerAndLogin();
    await use(user);
  },
});

export { expect };
