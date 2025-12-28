import { type Page } from '@playwright/test';
import { MailHogHelper } from './mailhog.helper';
import { TestDataHelper, type TestUser } from './test-data.helper';

/**
 * AuthHelper - Authentication utilities for E2E tests
 *
 * Provides methods for handling the full authentication flow including
 * registration, email verification, and login.
 *
 * @example
 * ```typescript
 * const authHelper = new AuthHelper(page, 'http://localhost:8025');
 *
 * // Register and login in one call
 * const user = await authHelper.registerAndLogin();
 *
 * // Or with custom user data
 * const customUser = await authHelper.registerAndLogin({
 *   accountName: 'My Account',
 *   email: `custom-${Date.now()}@example.com`,
 *   password: 'MyPassword123!',
 * });
 * ```
 */
export class AuthHelper {
  private readonly page: Page;
  private readonly mailhog: MailHogHelper;

  /**
   * Creates a new AuthHelper instance.
   *
   * @param page - Playwright page instance
   * @param mailhogUrl - URL of MailHog service (default: http://localhost:8025)
   */
  constructor(page: Page, mailhogUrl?: string) {
    this.page = page;
    this.mailhog = new MailHogHelper(mailhogUrl);
  }

  /**
   * Performs the full authentication flow: register, verify email, and login.
   *
   * This method:
   * 1. Generates unique test user data (or uses provided data)
   * 2. Navigates to registration page and fills the form
   * 3. Waits for success message
   * 4. Retrieves verification token from MailHog
   * 5. Navigates to email verification URL
   * 6. Waits for verification success and redirect to login
   * 7. Logs in with the verified credentials
   * 8. Waits for redirect to dashboard
   *
   * @param user - Optional TestUser data. If not provided, generates unique user data.
   * @returns The TestUser that was registered and logged in
   *
   * @example
   * ```typescript
   * // Generate unique user automatically
   * const user = await authHelper.registerAndLogin();
   *
   * // Use specific user data
   * const specificUser = await authHelper.registerAndLogin({
   *   accountName: 'Test Account',
   *   email: `test-${Date.now()}@example.com`,
   *   password: 'SecurePassword123!',
   * });
   * ```
   */
  async registerAndLogin(user?: TestUser): Promise<TestUser> {
    const testUser = user || TestDataHelper.generateTestUser();

    // Note: Don't delete all messages here - causes race conditions with parallel tests
    // Each test uses unique email addresses, so we can filter by email

    // Step 1: Register the user
    await this.page.goto('/register');
    await this.page.locator('input[formControlName="accountName"]').fill(testUser.accountName);
    await this.page.locator('input[formControlName="email"]').fill(testUser.email);
    await this.page.locator('input[formControlName="password"]').fill(testUser.password);
    await this.page.locator('input[formControlName="confirmPassword"]').fill(testUser.password);
    await this.page.locator('button[type="submit"]').click();

    // Wait for success message indicating registration complete
    await this.page.locator('.success-content').waitFor({ state: 'visible' });

    // Step 2: Get verification token from MailHog
    const token = await this.mailhog.getVerificationToken(testUser.email);

    // Step 3: Verify email
    // Component has a 3s delay after verification before redirecting
    await this.page.goto(`/verify-email?token=${token}`);
    await this.page
      .locator('.success-icon, mat-icon:has-text("check_circle")')
      .waitFor({ state: 'visible', timeout: 10000 });
    await this.page.waitForURL('/login', { timeout: 10000 });

    // Step 4: Login with verified credentials
    await this.page.locator('input[formControlName="email"]').fill(testUser.email);
    await this.page.locator('input[formControlName="password"]').fill(testUser.password);
    await this.page.locator('button[type="submit"]').click();

    // Wait for redirect to dashboard
    await this.page.waitForURL('/dashboard', { timeout: 10000 });

    return testUser;
  }

  /**
   * Logs in an existing user.
   *
   * Use this when you have already verified credentials and just need to log in.
   *
   * @param email - User email address
   * @param password - User password
   */
  async login(email: string, password: string): Promise<void> {
    await this.page.goto('/login');
    await this.page.locator('input[formControlName="email"]').fill(email);
    await this.page.locator('input[formControlName="password"]').fill(password);
    await this.page.locator('button[type="submit"]').click();
    await this.page.waitForURL('/dashboard', { timeout: 10000 });
  }

  /**
   * Logs out the current user.
   *
   * Clicks the logout button in the navigation and waits for redirect to login.
   */
  async logout(): Promise<void> {
    await this.page.locator('button', { hasText: 'Logout' }).click();
    await this.page.waitForURL('/login', { timeout: 10000 });
  }
}
