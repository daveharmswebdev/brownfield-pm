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
   * When used, automatically registers a new user via invitation,
   * and logs in before the test runs. The test user data is returned.
   *
   * Note: This fixture requires E2E_SEED_OWNER_EMAIL and E2E_SEED_OWNER_PASSWORD
   * environment variables to be set for invitation-based registration.
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
   * This fixture handles the full invitation-based authentication flow:
   * 1. Uses seed owner to send an invitation
   * 2. Registers the new user with the invitation token
   * 3. Logs in with the credentials
   *
   * Requires environment variables:
   * - E2E_SEED_OWNER_EMAIL: Email of pre-seeded owner user
   * - E2E_SEED_OWNER_PASSWORD: Password of pre-seeded owner user
   *
   * If seed owner credentials are not available, creates a test user
   * directly via API (requires test database seeding).
   */
  authenticatedUser: async ({ page, authHelper, mailhog }, use) => {
    const seedOwnerEmail = process.env.E2E_SEED_OWNER_EMAIL;
    const seedOwnerPassword = process.env.E2E_SEED_OWNER_PASSWORD;

    if (!seedOwnerEmail || !seedOwnerPassword) {
      throw new Error(
        'E2E tests require seed owner credentials. Set E2E_SEED_OWNER_EMAIL and E2E_SEED_OWNER_PASSWORD environment variables.'
      );
    }

    // Login as seed owner to get access token
    const accessToken = await authHelper.loginAndGetToken(seedOwnerEmail, seedOwnerPassword);

    // Generate test user data
    const testUser = {
      accountName: `Test Account ${Date.now()}`,
      email: `test-${Date.now()}@example.com`,
      password: 'TestPassword123!',
    };

    // Send invitation
    const invitationToken = await authHelper.sendInvitation(accessToken, testUser.email);

    // Register with invitation token
    await authHelper.registerWithInvitationToken(invitationToken, testUser.password);

    // Login as the new user
    await authHelper.login(testUser.email, testUser.password);

    await use(testUser);
  },
});

export { expect };
