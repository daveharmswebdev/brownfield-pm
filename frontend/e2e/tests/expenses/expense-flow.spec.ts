import { test, expect } from '../../fixtures/test-fixtures';
import { TestDataHelper } from '../../helpers/test-data.helper';
import { createPropertyAndGetId } from '../../helpers/test-setup.helper';

test.describe('Expense CRUD E2E Tests (AC-TD.1.2, AC-TD.1.4)', () => {
  test('should create expense from expense workspace', async ({
    page,
    authenticatedUser,
    dashboardPage,
    propertyFormPage,
    propertyDetailPage,
    expenseWorkspacePage,
  }) => {
    // Step 1: Create a property
    const { propertyId } = await createPropertyAndGetId(
      dashboardPage,
      propertyFormPage,
      page
    );

    // Step 2: Navigate to expense workspace
    await propertyDetailPage.clickAddExpense();
    await expect(page).toHaveURL(`/properties/${propertyId}/expenses`);

    // Step 3: Verify empty state initially
    await expenseWorkspacePage.expectEmptyState();

    // Step 4: Create expense
    const testExpense = TestDataHelper.generateExpense({
      amount: '150.00',
      category: 'Repairs',
    });
    await expenseWorkspacePage.createExpense(testExpense);

    // Step 5: Verify expense appears in list
    await expenseWorkspacePage.expectExpenseInList(testExpense.description!);

    // Step 6: Verify YTD total updates
    await expenseWorkspacePage.expectTotal('$150.00');
  });

  test('should edit existing expense and verify totals recalculate', async ({
    page,
    authenticatedUser,
    dashboardPage,
    propertyFormPage,
    propertyDetailPage,
    expenseWorkspacePage,
  }) => {
    // Step 1: Create a property
    const { propertyId } = await createPropertyAndGetId(
      dashboardPage,
      propertyFormPage,
      page
    );

    // Step 2: Navigate to expense workspace
    await propertyDetailPage.clickAddExpense();

    // Step 3: Create initial expense
    const testExpense = TestDataHelper.generateExpense({
      amount: '100.00',
      category: 'Repairs',
    });
    await expenseWorkspacePage.createExpense(testExpense);

    // Step 4: Edit the expense
    await expenseWorkspacePage.editExpense(testExpense.description!, {
      amount: '250.00',
      category: 'Supplies',
    });

    // Step 5: Verify changes persist
    await expenseWorkspacePage.expectExpenseInList(testExpense.description!);
    const amount = await expenseWorkspacePage.getExpenseAmount(testExpense.description!);
    expect(amount).toContain('$250.00');

    // Step 6: Verify YTD total updates
    await expenseWorkspacePage.expectTotal('$250.00');
  });

  test('should delete expense with confirmation', async ({
    page,
    authenticatedUser,
    dashboardPage,
    propertyFormPage,
    propertyDetailPage,
    expenseWorkspacePage,
  }) => {
    // Step 1: Create a property
    const { propertyId } = await createPropertyAndGetId(
      dashboardPage,
      propertyFormPage,
      page
    );

    // Step 2: Navigate to expense workspace and create expense
    await propertyDetailPage.clickAddExpense();
    const testExpense = TestDataHelper.generateExpense({
      amount: '200.00',
      category: 'Insurance',
    });
    await expenseWorkspacePage.createExpense(testExpense);

    // Verify it exists
    await expenseWorkspacePage.expectExpenseInList(testExpense.description!);
    await expenseWorkspacePage.expectTotal('$200.00');

    // Step 3: Delete the expense
    await expenseWorkspacePage.deleteExpense(testExpense.description!);

    // Step 4: Verify removal
    await expenseWorkspacePage.expectExpenseNotInList(testExpense.description!);

    // Step 5: Verify total recalculates (should show empty state or $0)
    await expenseWorkspacePage.expectEmptyState();
  });

  test('should create multiple expenses and verify cumulative totals', async ({
    page,
    authenticatedUser,
    dashboardPage,
    propertyFormPage,
    propertyDetailPage,
    expenseWorkspacePage,
  }) => {
    // Step 1: Create a property
    const { propertyId } = await createPropertyAndGetId(
      dashboardPage,
      propertyFormPage,
      page
    );

    // Step 2: Navigate to expense workspace
    await propertyDetailPage.clickAddExpense();

    // Step 3: Create first expense
    const expense1 = TestDataHelper.generateExpense({
      amount: '100.00',
      category: 'Repairs',
    });
    await expenseWorkspacePage.createExpense(expense1);
    await expenseWorkspacePage.expectTotal('$100.00');

    // Step 4: Create second expense
    const expense2 = TestDataHelper.generateExpense({
      amount: '50.00',
      category: 'Supplies',
    });
    await expenseWorkspacePage.createExpense(expense2);

    // Step 5: Verify cumulative total
    await expenseWorkspacePage.expectTotal('$150.00');

    // Step 6: Verify both expenses in list
    await expenseWorkspacePage.expectExpenseInList(expense1.description!);
    await expenseWorkspacePage.expectExpenseInList(expense2.description!);
    await expenseWorkspacePage.expectExpenseCount(2);
  });

  test('should verify dashboard totals update after expense changes', async ({
    page,
    authenticatedUser,
    dashboardPage,
    propertyFormPage,
    propertyDetailPage,
    expenseWorkspacePage,
  }) => {
    // Step 1: Create a property
    const { propertyId, propertyData } = await createPropertyAndGetId(
      dashboardPage,
      propertyFormPage,
      page
    );

    // Step 2: Navigate to expense workspace and create expense
    await propertyDetailPage.clickAddExpense();
    const testExpense = TestDataHelper.generateExpense({
      amount: '300.00',
      category: 'Utilities',
    });
    await expenseWorkspacePage.createExpense(testExpense);

    // Step 3: Go back to property detail
    await expenseWorkspacePage.goBack();

    // Step 4: Verify expense total on property detail
    await propertyDetailPage.expectExpenseTotal('$300.00');

    // Step 5: Go to dashboard and verify stats
    await dashboardPage.goto();
    // Dashboard shows property with expense total
    await expect(page.locator('app-property-row', { hasText: propertyData.name })).toContainText('$300.00');
  });

  test('should cancel edit and preserve original values', async ({
    page,
    authenticatedUser,
    dashboardPage,
    propertyFormPage,
    propertyDetailPage,
    expenseWorkspacePage,
  }) => {
    // Step 1: Create a property
    const { propertyId } = await createPropertyAndGetId(
      dashboardPage,
      propertyFormPage,
      page
    );

    // Step 2: Navigate to expense workspace and create expense
    await propertyDetailPage.clickAddExpense();
    const testExpense = TestDataHelper.generateExpense({
      amount: '175.00',
      category: 'Repairs',
    });
    await expenseWorkspacePage.createExpense(testExpense);

    // Step 3: Start editing
    await expenseWorkspacePage.clickEditExpense(testExpense.description!);
    await expect(expenseWorkspacePage.editFormContainer).toBeVisible();

    // Step 4: Modify fields but don't save
    await expenseWorkspacePage.editAmountInput.clear();
    await expenseWorkspacePage.editAmountInput.fill('999.00');

    // Step 5: Cancel edit
    await expenseWorkspacePage.cancelEdit();

    // Step 6: Verify original values preserved
    const amount = await expenseWorkspacePage.getExpenseAmount(testExpense.description!);
    expect(amount).toContain('$175.00');
    await expenseWorkspacePage.expectTotal('$175.00');
  });

  test('should filter expenses by category on expenses list page', async ({
    page,
    authenticatedUser,
    dashboardPage,
    propertyFormPage,
    propertyDetailPage,
    expenseWorkspacePage,
  }) => {
    // Step 1: Create a property with expenses in different categories
    const { propertyId } = await createPropertyAndGetId(
      dashboardPage,
      propertyFormPage,
      page
    );

    // Navigate to expense workspace
    await propertyDetailPage.clickAddExpense();

    // Create expense with Repairs category
    const repairsExpense = TestDataHelper.generateExpense({
      amount: '100.00',
      category: 'Repairs',
      description: `Repairs E2E ${Date.now()}`,
    });
    await expenseWorkspacePage.createExpense(repairsExpense);
    // Verify first expense was created
    await expenseWorkspacePage.expectExpenseInList(repairsExpense.description!);

    // Create expense with Utilities category
    const utilitiesExpense = TestDataHelper.generateExpense({
      amount: '75.00',
      category: 'Utilities',
      description: `Utilities E2E ${Date.now()}`,
    });
    await expenseWorkspacePage.createExpense(utilitiesExpense);
    // Verify second expense was created
    await expenseWorkspacePage.expectExpenseInList(utilitiesExpense.description!);

    // Verify we have 2 expenses
    await expenseWorkspacePage.expectExpenseCount(2);

    // Step 2: Navigate to /expenses list page
    await page.goto('/expenses');
    await page.waitForLoadState('networkidle');

    // Step 3: Verify both expenses are visible initially
    await expect(page.locator('app-expense-list-row', { hasText: repairsExpense.description })).toBeVisible({ timeout: 10000 });
    await expect(page.locator('app-expense-list-row', { hasText: utilitiesExpense.description })).toBeVisible({ timeout: 10000 });

    // Step 4: Apply category filter - select only "Repairs"
    // Find the mat-form-field containing "Categories" label, then click its mat-select
    const categoryFormField = page.locator('mat-form-field', { has: page.locator('mat-label', { hasText: 'Categories' }) });
    await categoryFormField.locator('mat-select').click();
    await page.locator('mat-option', { hasText: 'Repairs' }).click();
    // Click outside to close dropdown
    await page.keyboard.press('Escape');

    // Wait for filter to apply (API call + re-render)
    await page.waitForLoadState('networkidle');

    // Step 5: Verify only Repairs expense is visible
    await expect(page.locator('app-expense-list-row', { hasText: repairsExpense.description })).toBeVisible();
    await expect(page.locator('app-expense-list-row', { hasText: utilitiesExpense.description })).not.toBeVisible();

    // Step 6: Clear filter and verify both appear again
    await page.locator('button', { hasText: 'Clear all' }).click();
    await page.waitForLoadState('networkidle');

    await expect(page.locator('app-expense-list-row', { hasText: repairsExpense.description })).toBeVisible();
    await expect(page.locator('app-expense-list-row', { hasText: utilitiesExpense.description })).toBeVisible();
  });
});
