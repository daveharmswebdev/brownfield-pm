import { test, expect } from '../../fixtures/test-fixtures';

test.describe('Auth Critical Path', () => {
  // Note: With invite-only registration, we test:
  // 1. Registration without token shows error
  // 2. Invalid credentials show error
  // 3. Invalid/expired token shows error
  //
  // The full invitation flow requires a seed owner user which should be
  // set up via database seeding in the E2E environment.

  test('should show error when accessing register without token', async ({
    page,
    registerPage,
  }) => {
    await registerPage.goto();
    await registerPage.expectTokenError();
  });

  test('should show error for invalid credentials', async ({ page, loginPage }) => {
    await loginPage.goto();
    await loginPage.login('nonexistent@example.com', 'WrongPassword123!');
    await loginPage.expectError('Invalid email or password');
  });

  test('should show error for invalid/expired invitation token', async ({
    page,
    registerPage,
  }) => {
    // Use a clearly invalid token
    await registerPage.gotoWithToken('invalid-token-12345');

    // Fill in password and try to submit
    await registerPage.register('SecurePassword123!');

    // Should show an error (either specific validation error or general error)
    // The key is that submission with invalid token should not succeed
    await expect(page.locator('.server-error')).toBeVisible();
  });

  test('should show password requirements on register page with valid token format', async ({
    page,
  }) => {
    // Navigate to register with a token (even if invalid, form should render)
    await page.goto('/register?token=some-test-token-value');

    // The form should show password requirements
    await expect(page.locator('.password-requirements')).toBeVisible();
    await expect(page.locator('text=At least 8 characters')).toBeVisible();
    await expect(page.locator('text=One uppercase letter')).toBeVisible();
    await expect(page.locator('text=One lowercase letter')).toBeVisible();
    await expect(page.locator('text=One number')).toBeVisible();
    await expect(page.locator('text=One special character')).toBeVisible();
  });

  test('should validate password requirements before submission', async ({
    page,
    registerPage,
  }) => {
    // Navigate to register with a token
    await page.goto('/register?token=test-token-for-validation');

    // Fill in a weak password
    const passwordInput = page.locator('input[formControlName="password"]');
    await passwordInput.fill('weak');
    await passwordInput.blur();

    // Check that requirements show as not met (error color)
    const minLengthItem = page.locator('.password-requirements li:has-text("At least 8 characters")');
    await expect(minLengthItem).not.toHaveClass(/met/);
  });

  test('login form should have required fields', async ({ page, loginPage }) => {
    await loginPage.goto();

    // Check email and password inputs exist
    await expect(page.locator('input[formControlName="email"]')).toBeVisible();
    await expect(page.locator('input[formControlName="password"]')).toBeVisible();
    await expect(page.locator('button[type="submit"]')).toBeVisible();
  });

  test('register page should have link to login', async ({ page }) => {
    await page.goto('/register?token=test');

    // Should have a link to sign in
    const loginLink = page.locator('a[routerLink="/login"]');
    await expect(loginLink).toBeVisible();
    await expect(loginLink).toContainText('Sign in');
  });

  test('login page should have link to contact for account', async ({ page, loginPage }) => {
    await loginPage.goto();

    // With invite-only, there should be text about contacting admin
    // or a link explaining how to get an account
    const registerLink = page.locator('a[routerLink="/register"]');

    // The register link might exist but lead to the token-required page
    if (await registerLink.isVisible()) {
      await registerLink.click();
      // Should show the token error since we don't have a token
      await expect(page.locator('.error-content')).toBeVisible();
    }
  });
});

// Integration test that requires seed data - only run if E2E_FULL_FLOW is set
test.describe('Full Invitation Flow', () => {
  // Skip these tests unless we have proper E2E seed data
  test.skip(
    () => !process.env.E2E_SEED_OWNER_EMAIL,
    'Skipping full flow tests - E2E_SEED_OWNER_EMAIL not set'
  );

  test('complete invitation registration and login flow', async ({
    page,
    registerPage,
    loginPage,
    dashboardPage,
    mailhog,
  }) => {
    // This test requires environment variables:
    // E2E_SEED_OWNER_EMAIL - email of pre-seeded owner
    // E2E_SEED_OWNER_PASSWORD - password of pre-seeded owner
    const ownerEmail = process.env.E2E_SEED_OWNER_EMAIL!;
    const ownerPassword = process.env.E2E_SEED_OWNER_PASSWORD!;

    const timestamp = Date.now();
    const inviteeEmail = `invited-${timestamp}@example.com`;
    const inviteePassword = 'SecurePassword123!';

    // Step 1: Login as owner to get access token
    const loginResponse = await page.request.post('http://localhost:5292/api/v1/auth/login', {
      data: { email: ownerEmail, password: ownerPassword },
    });

    expect(loginResponse.ok()).toBeTruthy();
    const { accessToken } = await loginResponse.json();

    // Step 2: Send invitation via API
    const inviteResponse = await page.request.post('http://localhost:5292/api/v1/auth/invite', {
      headers: {
        Authorization: `Bearer ${accessToken}`,
        'Content-Type': 'application/json',
      },
      data: { email: inviteeEmail },
    });

    expect(inviteResponse.ok()).toBeTruthy();

    // Step 3: Get invitation token from MailHog
    const invitationToken = await mailhog.getInvitationToken(inviteeEmail);
    expect(invitationToken).toBeTruthy();

    // Step 4: Clear auth state (page.request shares cookies with browser)
    await page.context().clearCookies();
    await page.goto('/login');
    await page.evaluate(() => localStorage.clear());

    // Step 5: Navigate to register page with token
    await page.goto(`/register?token=${invitationToken}`);

    // Step 6: Complete registration
    await registerPage.register(inviteePassword);

    // Step 7: Verify registration success
    await registerPage.expectSuccess();

    // Step 8: Navigate to login and log in
    await loginPage.goto();
    await loginPage.login(inviteeEmail, inviteePassword);

    // Step 9: Verify successful login - redirected to dashboard
    await page.waitForURL('/dashboard', { timeout: 10000 });
    await dashboardPage.expectWelcome();
  });
});
