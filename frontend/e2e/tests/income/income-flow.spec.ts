import { test, expect } from '../../fixtures/test-fixtures';
import { TestDataHelper } from '../../helpers/test-data.helper';
import { createPropertyAndGetId } from '../../helpers/test-setup.helper';

test.describe('Income CRUD E2E Tests (AC-TD.1.3, AC-TD.1.4)', () => {
  test('should create income entry from income workspace', async ({
    page,
    authenticatedUser,
    dashboardPage,
    propertyFormPage,
    propertyDetailPage,
    incomeWorkspacePage,
  }) => {
    // Step 1: Create a property
    const { propertyId } = await createPropertyAndGetId(
      dashboardPage,
      propertyFormPage,
      page
    );

    // Step 2: Navigate to income workspace
    await propertyDetailPage.clickAddIncome();
    await expect(page).toHaveURL(`/properties/${propertyId}/income`);

    // Step 3: Verify empty state initially
    await incomeWorkspacePage.expectEmptyState();

    // Step 4: Create income
    const testIncome = TestDataHelper.generateIncome({
      amount: '1500.00',
      source: 'Test Tenant',
    });
    await incomeWorkspacePage.createIncome(testIncome);

    // Step 5: Verify income appears in list
    await incomeWorkspacePage.expectIncomeInList(testIncome.source!);

    // Step 6: Verify YTD total updates
    await incomeWorkspacePage.expectTotal('$1,500.00');
  });

  test('should edit existing income entry', async ({
    page,
    authenticatedUser,
    dashboardPage,
    propertyFormPage,
    propertyDetailPage,
    incomeWorkspacePage,
  }) => {
    // Step 1: Create a property
    const { propertyId } = await createPropertyAndGetId(
      dashboardPage,
      propertyFormPage,
      page
    );

    // Step 2: Navigate to income workspace
    await propertyDetailPage.clickAddIncome();

    // Step 3: Create initial income
    const testIncome = TestDataHelper.generateIncome({
      amount: '1000.00',
      source: 'Original Tenant',
    });
    await incomeWorkspacePage.createIncome(testIncome);

    // Step 4: Edit the income
    await incomeWorkspacePage.editIncome(testIncome.source!, {
      amount: '1200.00',
      source: 'Updated Tenant',
    });

    // Step 5: Verify changes persist
    await incomeWorkspacePage.expectIncomeInList('Updated Tenant');
    const amount = await incomeWorkspacePage.getIncomeAmount('Updated Tenant');
    expect(amount).toContain('$1,200.00');

    // Step 6: Verify YTD total updates
    await incomeWorkspacePage.expectTotal('$1,200.00');
  });

  test('should delete income with confirmation', async ({
    page,
    authenticatedUser,
    dashboardPage,
    propertyFormPage,
    propertyDetailPage,
    incomeWorkspacePage,
  }) => {
    // Step 1: Create a property
    const { propertyId } = await createPropertyAndGetId(
      dashboardPage,
      propertyFormPage,
      page
    );

    // Step 2: Navigate to income workspace and create income
    await propertyDetailPage.clickAddIncome();
    const testIncome = TestDataHelper.generateIncome({
      amount: '2000.00',
      source: 'Tenant To Delete',
    });
    await incomeWorkspacePage.createIncome(testIncome);

    // Verify it exists
    await incomeWorkspacePage.expectIncomeInList(testIncome.source!);
    await incomeWorkspacePage.expectTotal('$2,000.00');

    // Step 3: Delete the income
    await incomeWorkspacePage.deleteIncome(testIncome.source!);

    // Step 4: Verify removal
    await incomeWorkspacePage.expectIncomeNotInList(testIncome.source!);

    // Step 5: Verify empty state returns
    await incomeWorkspacePage.expectEmptyState();
  });

  test('should create multiple income entries and verify cumulative totals', async ({
    page,
    authenticatedUser,
    dashboardPage,
    propertyFormPage,
    propertyDetailPage,
    incomeWorkspacePage,
  }) => {
    // Step 1: Create a property
    const { propertyId } = await createPropertyAndGetId(
      dashboardPage,
      propertyFormPage,
      page
    );

    // Step 2: Navigate to income workspace
    await propertyDetailPage.clickAddIncome();

    // Step 3: Create first income
    const income1 = TestDataHelper.generateIncome({
      amount: '1000.00',
      source: 'Tenant A',
    });
    await incomeWorkspacePage.createIncome(income1);
    await incomeWorkspacePage.expectTotal('$1,000.00');

    // Step 4: Create second income
    const income2 = TestDataHelper.generateIncome({
      amount: '500.00',
      source: 'Tenant B',
    });
    await incomeWorkspacePage.createIncome(income2);

    // Step 5: Verify cumulative total
    await incomeWorkspacePage.expectTotal('$1,500.00');

    // Step 6: Verify both income entries in list
    await incomeWorkspacePage.expectIncomeInList(income1.source!);
    await incomeWorkspacePage.expectIncomeInList(income2.source!);
    await incomeWorkspacePage.expectIncomeCount(2);
  });

  test('should verify dashboard income total updates after income changes', async ({
    page,
    authenticatedUser,
    dashboardPage,
    propertyFormPage,
    propertyDetailPage,
    incomeWorkspacePage,
  }) => {
    // Step 1: Create a property
    const { propertyId, propertyData } = await createPropertyAndGetId(
      dashboardPage,
      propertyFormPage,
      page
    );

    // Step 2: Navigate to income workspace and create income
    await propertyDetailPage.clickAddIncome();
    const testIncome = TestDataHelper.generateIncome({
      amount: '2500.00',
      source: 'Monthly Rent',
    });
    await incomeWorkspacePage.createIncome(testIncome);

    // Step 3: Go back to property detail
    await incomeWorkspacePage.goBack();

    // Step 4: Verify income total on property detail
    await propertyDetailPage.expectIncomeTotal('$2,500.00');

    // Step 5: Go to dashboard and verify stats
    await dashboardPage.goto();
    // Dashboard shows property with income total
    await expect(page.locator('app-property-row', { hasText: propertyData.name })).toContainText('$2,500.00');
  });

  test('should cancel delete and keep income entry', async ({
    page,
    authenticatedUser,
    dashboardPage,
    propertyFormPage,
    propertyDetailPage,
    incomeWorkspacePage,
  }) => {
    // Step 1: Create a property
    const { propertyId } = await createPropertyAndGetId(
      dashboardPage,
      propertyFormPage,
      page
    );

    // Step 2: Navigate to income workspace and create income
    await propertyDetailPage.clickAddIncome();
    const testIncome = TestDataHelper.generateIncome({
      amount: '1800.00',
      source: 'Tenant To Keep',
    });
    await incomeWorkspacePage.createIncome(testIncome);

    // Step 3: Start delete flow
    await incomeWorkspacePage.clickDeleteIncome(testIncome.source!);

    // Step 4: Verify confirmation appears
    const confirmingRow = page.locator('.income-row--confirming');
    await expect(confirmingRow).toBeVisible();

    // Step 5: Cancel delete
    await incomeWorkspacePage.cancelDelete();

    // Step 6: Verify income still exists
    await incomeWorkspacePage.expectIncomeInList(testIncome.source!);
    await incomeWorkspacePage.expectTotal('$1,800.00');
  });
});
