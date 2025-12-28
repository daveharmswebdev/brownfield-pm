import { test, expect } from '../../fixtures/test-fixtures';
import { TestDataHelper } from '../../helpers/test-data.helper';

test.describe('Property Critical Path', () => {
  test('create property, see in list, view detail', async ({
    page,
    authenticatedUser,
    dashboardPage,
    propertyFormPage,
  }) => {
    // authenticatedUser fixture ensures we're logged in

    // Step 1: Navigate to dashboard
    await dashboardPage.goto();
    await dashboardPage.expectWelcome();

    // Step 2: Click Add Property
    await dashboardPage.clickAddProperty();
    await expect(page).toHaveURL('/properties/new');

    // Step 3: Fill and submit property form
    const testProperty = TestDataHelper.generateProperty();
    await propertyFormPage.fillForm(testProperty);
    await propertyFormPage.submit();

    // Step 4: Verify redirect to dashboard
    await page.waitForURL('/dashboard', { timeout: 10000 });

    // Step 5: Verify property appears in list
    await expect(page.locator('app-property-row', { hasText: testProperty.name })).toBeVisible();

    // Step 6: Click on property to view detail
    await dashboardPage.clickProperty(testProperty.name);
    await expect(page).toHaveURL(/\/properties\/[a-f0-9-]+$/);

    // Step 7: Verify property detail page shows correct data
    await expect(page.getByText(testProperty.name)).toBeVisible();
    await expect(page.getByText(testProperty.city)).toBeVisible();
  });

  test('should validate required fields on property form', async ({
    page,
    authenticatedUser,
    propertyFormPage,
  }) => {
    await propertyFormPage.goto();

    // Try to submit empty form - click submit to trigger validation
    await propertyFormPage.saveButton.click();

    // Verify validation errors appear
    await expect(page.getByText('Property name is required')).toBeVisible();
  });

  test('should navigate from dashboard to add property and back', async ({
    page,
    authenticatedUser,
    dashboardPage,
    propertyFormPage,
  }) => {
    await dashboardPage.goto();
    await dashboardPage.clickAddProperty();
    await expect(page).toHaveURL('/properties/new');

    // Click cancel to go back
    await propertyFormPage.cancelButton.click();
    await expect(page).toHaveURL('/dashboard');
  });
});
