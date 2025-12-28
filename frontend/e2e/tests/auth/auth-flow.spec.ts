import { test, expect } from '../../fixtures/test-fixtures';

test.describe('Auth Critical Path', () => {
  // Note: Don't delete all messages in beforeEach - causes race conditions with parallel tests
  // Each test uses unique email addresses (timestamp-based), so filtering works correctly

  test('complete registration, email verification, and login flow', async ({
    page,
    registerPage,
    loginPage,
    dashboardPage,
    mailhog,
  }) => {
    const timestamp = Date.now();
    const testUser = {
      accountName: `E2E Test Account ${timestamp}`,
      email: `e2e-test-${timestamp}@example.com`,
      password: 'SecurePassword123!',
    };

    // Step 1: Navigate to register page
    await registerPage.goto();
    await expect(page).toHaveURL('/register');

    // Step 2: Register new user
    await registerPage.register(testUser.accountName, testUser.email, testUser.password);

    // Step 3: Verify registration success message
    await registerPage.expectSuccess();

    // Step 4: Get verification token from MailHog
    const verificationEmail = await mailhog.waitForEmail(testUser.email, 'Verify', 30000);
    expect(verificationEmail).toBeDefined();

    const token = mailhog.extractVerificationToken(verificationEmail);
    expect(token).toBeTruthy();

    // Step 5: Verify email by navigating to verification URL
    await page.goto(`/verify-email?token=${token}`);

    // Step 6: Wait for verification success, then redirect (component has 3s delay)
    await page.locator('.success-icon, mat-icon:has-text("check_circle")').waitFor({ state: 'visible', timeout: 10000 });
    await page.waitForURL('/login', { timeout: 10000 });

    // Step 7: Login with verified account
    await loginPage.login(testUser.email, testUser.password);

    // Step 8: Verify successful login - redirected to dashboard
    await page.waitForURL('/dashboard', { timeout: 10000 });
    await dashboardPage.expectWelcome();
  });

  test('should show error for invalid credentials', async ({ page, loginPage }) => {
    await loginPage.goto();
    await loginPage.login('nonexistent@example.com', 'WrongPassword123!');
    await loginPage.expectError('Invalid email or password');
  });

  test('should show error for unverified email', async ({
    page,
    registerPage,
    loginPage,
    mailhog,
  }) => {
    const timestamp = Date.now();
    const testUser = {
      accountName: `Unverified Account ${timestamp}`,
      email: `unverified-${timestamp}@example.com`,
      password: 'SecurePassword123!',
    };

    // Register without verifying
    await registerPage.goto();
    await registerPage.register(testUser.accountName, testUser.email, testUser.password);
    await registerPage.expectSuccess();

    // Try to login without verification
    await loginPage.goto();
    await loginPage.login(testUser.email, testUser.password);

    // Should show error about unverified email
    await loginPage.expectError('verify');
  });
});
