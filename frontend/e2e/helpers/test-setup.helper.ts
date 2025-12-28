import { type Page } from '@playwright/test';
import { DashboardPage } from '../pages/dashboard.page';
import { PropertyFormPage } from '../pages/property-form.page';
import { TestDataHelper, type TestProperty } from './test-data.helper';

/**
 * Result of creating a property for test setup
 */
export interface PropertySetupResult {
  /** The property ID extracted from the URL after creation */
  propertyId: string;
  /** The test data used to create the property */
  propertyData: TestProperty;
}

/**
 * Creates a property through the UI and returns its ID and data.
 *
 * This is the primary setup helper for tests that need an existing property.
 * It performs the full user workflow:
 * 1. Navigate to dashboard
 * 2. Click "Add Property"
 * 3. Fill and submit the property form
 * 4. Navigate to the created property to extract its ID
 *
 * @param dashboardPage - Dashboard page object instance
 * @param propertyFormPage - Property form page object instance
 * @param page - Playwright page instance
 * @returns PropertySetupResult with propertyId and propertyData
 *
 * @example
 * ```typescript
 * test('should add expense to property', async ({
 *   page,
 *   authenticatedUser,
 *   dashboardPage,
 *   propertyFormPage,
 *   expenseWorkspacePage,
 * }) => {
 *   // Create property first
 *   const { propertyId, propertyData } = await createPropertyAndGetId(
 *     dashboardPage,
 *     propertyFormPage,
 *     page
 *   );
 *
 *   // Now use propertyId to navigate to expense workspace
 *   await expenseWorkspacePage.gotoWithPropertyId(propertyId);
 * });
 * ```
 *
 * @throws Error if property creation fails or ID cannot be extracted
 */
export async function createPropertyAndGetId(
  dashboardPage: DashboardPage,
  propertyFormPage: PropertyFormPage,
  page: Page
): Promise<PropertySetupResult> {
  const testProperty = TestDataHelper.generateProperty();

  // Navigate to dashboard and start property creation
  await dashboardPage.goto();
  await dashboardPage.clickAddProperty();

  // Fill and submit the property form
  await propertyFormPage.fillForm(testProperty);
  await propertyFormPage.submit();

  // Wait for redirect to dashboard after creation
  await page.waitForURL('/dashboard', { timeout: 10000 });

  // Click on the newly created property to get to detail page
  await dashboardPage.clickProperty(testProperty.name);

  // Wait for navigation to property detail page and extract ID from URL
  await page.waitForURL(/\/properties\/[a-f0-9-]+$/);
  const url = page.url();
  const propertyId = url.split('/properties/')[1];

  if (!propertyId) {
    throw new Error(`Failed to extract property ID from URL: ${url}`);
  }

  return { propertyId, propertyData: testProperty };
}

/**
 * Creates a property with custom data (overrides defaults).
 *
 * Use this when you need a property with specific attributes for your test.
 *
 * @param dashboardPage - Dashboard page object instance
 * @param propertyFormPage - Property form page object instance
 * @param page - Playwright page instance
 * @param overrides - Partial property data to override defaults
 * @returns PropertySetupResult with propertyId and propertyData
 *
 * @example
 * ```typescript
 * const { propertyId } = await createPropertyWithData(
 *   dashboardPage,
 *   propertyFormPage,
 *   page,
 *   { name: 'My Custom Property', city: 'Houston' }
 * );
 * ```
 */
export async function createPropertyWithData(
  dashboardPage: DashboardPage,
  propertyFormPage: PropertyFormPage,
  page: Page,
  overrides: Partial<TestProperty>
): Promise<PropertySetupResult> {
  const testProperty = {
    ...TestDataHelper.generateProperty(),
    ...overrides,
  };

  // Navigate to dashboard and start property creation
  await dashboardPage.goto();
  await dashboardPage.clickAddProperty();

  // Fill and submit the property form
  await propertyFormPage.fillForm(testProperty);
  await propertyFormPage.submit();

  // Wait for redirect to dashboard after creation
  await page.waitForURL('/dashboard', { timeout: 10000 });

  // Click on the newly created property to get to detail page
  await dashboardPage.clickProperty(testProperty.name);

  // Wait for navigation to property detail page and extract ID from URL
  await page.waitForURL(/\/properties\/[a-f0-9-]+$/);
  const url = page.url();
  const propertyId = url.split('/properties/')[1];

  if (!propertyId) {
    throw new Error(`Failed to extract property ID from URL: ${url}`);
  }

  return { propertyId, propertyData: testProperty };
}
