import { type Page, type APIRequestContext } from '@playwright/test';
import { MailHogHelper } from './mailhog.helper';
import { TestDataHelper, type TestUser } from './test-data.helper';

/**
 * AuthHelper - Authentication utilities for E2E tests
 *
 * Provides methods for handling the full authentication flow including
 * invitation-based registration and login.
 *
 * @example
 * ```typescript
 * const authHelper = new AuthHelper(page, 'http://localhost:8025');
 *
 * // Register via invitation and login
 * const user = await authHelper.registerWithInvitation(accessToken, 'owner@example.com');
 * ```
 */
export class AuthHelper {
  private readonly page: Page;
  private readonly mailhog: MailHogHelper;
  private readonly apiBaseUrl: string;

  /**
   * Creates a new AuthHelper instance.
   *
   * @param page - Playwright page instance
   * @param mailhogUrl - URL of MailHog service (default: http://localhost:8025)
   * @param apiBaseUrl - URL of API service (default: http://localhost:5292)
   */
  constructor(page: Page, mailhogUrl?: string, apiBaseUrl?: string) {
    this.page = page;
    this.mailhog = new MailHogHelper(mailhogUrl);
    this.apiBaseUrl = apiBaseUrl || 'http://localhost:5292';
  }

  /**
   * Creates an owner user directly via API for E2E test bootstrapping.
   * This is used to create the initial user who can send invitations.
   *
   * Note: This bypasses normal registration flow and is only for test setup.
   *
   * @returns The created owner user credentials
   */
  async createSeedOwnerUser(): Promise<{ email: string; password: string; accessToken: string }> {
    const email = `seed-owner-${Date.now()}@example.com`;
    const password = 'SeedOwner123!';

    // First, register the seed user (this will be the first user, auto-promoted to owner)
    // Actually, with invite-only, we need a different approach.
    // For E2E tests, we'll create a test endpoint or use the first-user bootstrap mechanism.

    // For now, try to register directly - this may fail if invite-only is enforced
    // The backend should have a seed user or allow first user to register
    const registerResponse = await this.page.request.post(`${this.apiBaseUrl}/api/v1/auth/register`, {
      data: { password, token: 'SEED_USER_BOOTSTRAP' },
      failOnStatusCode: false,
    });

    // If that didn't work, the database should have a seed user
    // Try to login with default seed credentials
    const loginResponse = await this.page.request.post(`${this.apiBaseUrl}/api/v1/auth/login`, {
      data: { email, password },
      failOnStatusCode: false,
    });

    if (loginResponse.ok()) {
      const loginData = await loginResponse.json();
      return { email, password, accessToken: loginData.accessToken };
    }

    throw new Error(
      'Could not create or login seed owner user. Ensure the test database has a seed user or update the E2E test configuration.'
    );
  }

  /**
   * Sends an invitation to a user via API.
   *
   * @param accessToken - JWT access token of an authenticated Owner user
   * @param inviteeEmail - Email address to send invitation to
   * @returns The invitation token from the email
   */
  async sendInvitation(accessToken: string, inviteeEmail: string): Promise<string> {
    const response = await this.page.request.post(`${this.apiBaseUrl}/api/v1/auth/invite`, {
      headers: {
        Authorization: `Bearer ${accessToken}`,
        'Content-Type': 'application/json',
      },
      data: { email: inviteeEmail },
    });

    if (!response.ok()) {
      throw new Error(`Failed to send invitation: ${response.status()} ${await response.text()}`);
    }

    // Get the invitation token from MailHog
    const token = await this.mailhog.getInvitationToken(inviteeEmail);
    return token;
  }

  /**
   * Registers a new user via invitation token and logs them in.
   *
   * @param invitationToken - The invitation token from the invitation email
   * @param password - Password for the new user
   * @returns The test user data
   */
  async registerWithInvitationToken(invitationToken: string, password: string): Promise<void> {
    // Clear browser auth state before navigating to register page
    // This is needed because page.request shares cookies/auth state with the browser
    await this.page.context().clearCookies();

    // Navigate to a page first to be able to clear localStorage
    await this.page.goto('/login');
    await this.page.evaluate(() => localStorage.clear());

    // Navigate to register page with token
    await this.page.goto(`/register?token=${invitationToken}`);

    // Fill password fields
    await this.page.locator('input[formControlName="password"]').fill(password);
    await this.page.locator('input[formControlName="confirmPassword"]').fill(password);
    await this.page.locator('button[type="submit"]').click();

    // Wait for success message
    await this.page.locator('.success-content').waitFor({ state: 'visible', timeout: 10000 });
  }

  /**
   * Performs the full invitation-based authentication flow.
   * Requires an authenticated Owner user to send the invitation.
   *
   * @param ownerAccessToken - JWT access token of an authenticated Owner user
   * @returns The TestUser that was registered and logged in
   */
  async registerAndLogin(ownerAccessToken?: string): Promise<TestUser> {
    const testUser = TestDataHelper.generateTestUser();

    // If no owner token provided, try to create/login a seed owner
    let accessToken = ownerAccessToken;
    if (!accessToken) {
      const seedOwner = await this.createSeedOwnerUser();
      accessToken = seedOwner.accessToken;
    }

    // Send invitation
    const invitationToken = await this.sendInvitation(accessToken, testUser.email);

    // Register with invitation
    await this.registerWithInvitationToken(invitationToken, testUser.password);

    // Login
    await this.login(testUser.email, testUser.password);

    return testUser;
  }

  /**
   * Logs in an existing user.
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
   * Logs in and returns the access token from the API response.
   *
   * @param email - User email address
   * @param password - User password
   * @returns The JWT access token
   */
  async loginAndGetToken(email: string, password: string): Promise<string> {
    const response = await this.page.request.post(`${this.apiBaseUrl}/api/v1/auth/login`, {
      data: { email, password },
    });

    if (!response.ok()) {
      throw new Error(`Login failed: ${response.status()} ${await response.text()}`);
    }

    const data = await response.json();
    return data.accessToken;
  }

  /**
   * Logs out the current user.
   */
  async logout(): Promise<void> {
    await this.page.locator('button', { hasText: 'Logout' }).click();
    await this.page.waitForURL('/login', { timeout: 10000 });
  }
}
