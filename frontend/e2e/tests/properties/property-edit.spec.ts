import { test, expect } from '../../fixtures/test-fixtures';
import { createPropertyAndGetId } from '../../helpers/test-setup.helper';

test.describe('Property Edit/Delete E2E Tests (AC-TD.1.1)', () => {
  test('should edit property name and address, verify changes persist', async ({
    page,
    authenticatedUser,
    dashboardPage,
    propertyFormPage,
    propertyDetailPage,
  }) => {
    // Step 1: Create a property
    const { propertyId, propertyData } = await createPropertyAndGetId(
      dashboardPage,
      propertyFormPage,
      page
    );

    // Step 2: Navigate to edit page from detail
    await propertyDetailPage.clickEdit();
    await expect(page).toHaveURL(`/properties/${propertyId}/edit`);

    // Step 3: Modify fields
    const newName = `Updated Property ${Date.now()}`;
    const newStreet = `${Math.floor(Math.random() * 9999)} Updated Street`;
    const newCity = 'Dallas';

    await propertyDetailPage.fillEditForm({
      name: newName,
      street: newStreet,
      city: newCity,
    });

    // Step 4: Save changes
    await propertyDetailPage.submitEditForm();

    // Step 5: Verify redirect to detail page
    await expect(page).toHaveURL(`/properties/${propertyId}`);

    // Step 6: Verify changes persist
    await propertyDetailPage.expectPropertyName(newName);
    await propertyDetailPage.expectAddressContains(newStreet);
    await propertyDetailPage.expectAddressContains(newCity);
  });

  test('should delete property, verify removal from dashboard', async ({
    page,
    authenticatedUser,
    dashboardPage,
    propertyFormPage,
    propertyDetailPage,
  }) => {
    // Step 1: Create a property
    const { propertyId, propertyData } = await createPropertyAndGetId(
      dashboardPage,
      propertyFormPage,
      page
    );

    // Verify property appears in detail
    await propertyDetailPage.expectPropertyName(propertyData.name);

    // Step 2: Delete the property (click delete + confirm dialog)
    await propertyDetailPage.deleteProperty();

    // Step 3: Verify redirect to dashboard
    await expect(page).toHaveURL('/dashboard');

    // Step 6: Verify property is removed from dashboard list
    await expect(page.locator('app-property-row', { hasText: propertyData.name })).not.toBeVisible();
  });

  test('should cancel edit and return to detail without changes', async ({
    page,
    authenticatedUser,
    dashboardPage,
    propertyFormPage,
    propertyDetailPage,
  }) => {
    // Step 1: Create a property
    const { propertyId, propertyData } = await createPropertyAndGetId(
      dashboardPage,
      propertyFormPage,
      page
    );

    // Step 2: Navigate to edit page
    await propertyDetailPage.clickEdit();
    await expect(page).toHaveURL(`/properties/${propertyId}/edit`);

    // Step 3: Click cancel (no changes made)
    await propertyDetailPage.cancelEdit();

    // Step 4: Verify redirect back to detail
    await expect(page).toHaveURL(`/properties/${propertyId}`);

    // Step 5: Verify original data is unchanged
    await propertyDetailPage.expectPropertyName(propertyData.name);
    await propertyDetailPage.expectAddressContains(propertyData.city);
  });

  test('should cancel delete and keep property', async ({
    page,
    authenticatedUser,
    dashboardPage,
    propertyFormPage,
    propertyDetailPage,
  }) => {
    // Step 1: Create a property
    const { propertyId, propertyData } = await createPropertyAndGetId(
      dashboardPage,
      propertyFormPage,
      page
    );

    // Step 2: Click Delete button
    await propertyDetailPage.clickDelete();

    // Step 3: Confirm dialog appears
    await expect(propertyDetailPage.confirmDialog).toBeVisible();

    // Step 4: Click cancel
    await propertyDetailPage.cancelDelete();

    // Step 5: Verify dialog closes and property still exists
    await expect(propertyDetailPage.confirmDialog).not.toBeVisible();
    await propertyDetailPage.expectPropertyName(propertyData.name);

    // Step 6: Verify property still exists in dashboard
    await dashboardPage.goto();
    await expect(page.locator('app-property-row', { hasText: propertyData.name })).toBeVisible();
  });
});
