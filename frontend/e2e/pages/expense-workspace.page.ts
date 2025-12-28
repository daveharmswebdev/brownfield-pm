import { type Page, type Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';
import { type TestExpense } from '../helpers/test-data.helper';
import { formatDateForInput } from '../helpers/date.helper';

/**
 * ExpenseWorkspacePage - Page object for expense workspace E2E tests
 *
 * Covers: AC-TD.1.2 - Expense CRUD E2E tests
 */
export class ExpenseWorkspacePage extends BasePage {
  // Header elements
  readonly header: Locator;
  readonly backButton: Locator;

  // Form elements
  readonly formCard: Locator;
  readonly amountInput: Locator;
  readonly dateInput: Locator;
  readonly categorySelect: Locator;
  readonly descriptionInput: Locator;
  readonly saveButton: Locator;

  // Edit form elements
  readonly editFormContainer: Locator;
  readonly editAmountInput: Locator;
  readonly editDateInput: Locator;
  readonly editCategorySelect: Locator;
  readonly editDescriptionInput: Locator;
  readonly editSaveButton: Locator;
  readonly editCancelButton: Locator;

  // List elements
  readonly expensesList: Locator;
  readonly expenseRows: Locator;
  readonly emptyState: Locator;
  readonly ytdTotal: Locator;

  // Note: Confirmation dialog locators inherited from BasePage
  // - confirmDialog, confirmDialogConfirmButton, confirmDialogCancelButton

  constructor(page: Page) {
    super(page);

    // Header
    this.header = page.locator('.workspace-header');
    this.backButton = page.locator('.back-button');

    // New Expense Form (app-expense-form)
    this.formCard = page.locator('app-expense-form .expense-form-card');
    this.amountInput = page.locator('app-expense-form input[formControlName="amount"]');
    this.dateInput = page.locator('app-expense-form input[formControlName="date"]');
    this.categorySelect = page.locator('app-expense-form app-category-select mat-select');
    this.descriptionInput = page.locator('app-expense-form textarea[formControlName="description"]');
    this.saveButton = page.locator('app-expense-form button[type="submit"]');

    // Edit Form (app-expense-edit-form)
    this.editFormContainer = page.locator('app-expense-edit-form');
    this.editAmountInput = page.locator('app-expense-edit-form input[formControlName="amount"]');
    this.editDateInput = page.locator('app-expense-edit-form input[formControlName="date"]');
    this.editCategorySelect = page.locator('app-expense-edit-form app-category-select mat-select');
    this.editDescriptionInput = page.locator('app-expense-edit-form textarea[formControlName="description"]');
    this.editSaveButton = page.locator('app-expense-edit-form button[type="submit"]');
    this.editCancelButton = page.locator('app-expense-edit-form button', { hasText: 'Cancel' });

    // Expense List
    this.expensesList = page.locator('.expenses-list');
    this.expenseRows = page.locator('app-expense-row');
    this.emptyState = page.locator('.empty-state');
    this.ytdTotal = page.locator('.ytd-amount');

    // Confirmation Dialog - uses inherited locators from BasePage
  }

  async goto(): Promise<void> {
    // This page requires a property ID, so it can't navigate directly
    // Use gotoWithPropertyId instead
    throw new Error('Use gotoWithPropertyId(propertyId) to navigate to expense workspace');
  }

  async gotoWithPropertyId(propertyId: string): Promise<void> {
    await this.page.goto(`/properties/${propertyId}/expenses`);
    await this.waitForLoading();
  }

  /**
   * Fill the new expense form with provided data
   */
  async fillForm(expense: TestExpense): Promise<void> {
    // Clear and fill amount
    await this.amountInput.clear();
    await this.amountInput.fill(expense.amount);

    // Fill date if provided (otherwise default is today)
    if (expense.date) {
      await this.dateInput.clear();
      await this.dateInput.fill(formatDateForInput(expense.date));
    }

    // Select category
    await this.categorySelect.click();
    await this.page.locator('mat-option').filter({ hasText: expense.category }).click();

    // Fill description if provided
    if (expense.description) {
      await this.descriptionInput.fill(expense.description);
    }
  }

  /**
   * Submit the new expense form
   */
  async submit(): Promise<void> {
    await this.saveButton.click();
  }

  /**
   * Fill form and submit in one action
   */
  async createExpense(expense: TestExpense): Promise<void> {
    await this.fillForm(expense);
    await this.submit();
    // Wait for snackbar confirmation
    await this.waitForSnackBar('Expense saved');
  }

  /**
   * Click edit button on a specific expense row
   */
  async clickEditExpense(description: string): Promise<void> {
    const row = this.expenseRows.filter({ hasText: description });
    // Hover to make buttons visible
    await row.hover();
    await row.locator('.edit-button').click();
  }

  /**
   * Edit an expense with new data
   */
  async editExpense(originalDescription: string, newData: Partial<TestExpense>): Promise<void> {
    await this.clickEditExpense(originalDescription);

    // Wait for edit form to appear
    await expect(this.editFormContainer).toBeVisible();

    // Fill edit form
    if (newData.amount !== undefined) {
      await this.editAmountInput.clear();
      await this.editAmountInput.fill(newData.amount);
    }

    if (newData.date) {
      await this.editDateInput.clear();
      await this.editDateInput.fill(formatDateForInput(newData.date));
    }

    if (newData.category) {
      await this.editCategorySelect.click();
      await this.page.locator('mat-option').filter({ hasText: newData.category }).click();
    }

    if (newData.description !== undefined) {
      await this.editDescriptionInput.clear();
      await this.editDescriptionInput.fill(newData.description);
    }

    // Save changes
    await this.editSaveButton.click();
    await this.waitForSnackBar('Expense updated');
  }

  /**
   * Cancel editing an expense
   */
  async cancelEdit(): Promise<void> {
    await this.editCancelButton.click();
    await expect(this.editFormContainer).not.toBeVisible();
  }

  /**
   * Click delete button on a specific expense row
   */
  async clickDeleteExpense(description: string): Promise<void> {
    const row = this.expenseRows.filter({ hasText: description });
    // Hover to make buttons visible
    await row.hover();
    await row.locator('.delete-button').click();
  }

  /**
   * Delete an expense (click delete + confirm)
   */
  async deleteExpense(description: string): Promise<void> {
    await this.clickDeleteExpense(description);

    // Wait for and click confirm in dialog, then wait for snackbar
    await this.waitForConfirmDialog();
    await this.confirmDialogAction('Expense deleted');
  }

  /**
   * Cancel delete operation
   */
  async cancelDelete(): Promise<void> {
    await this.waitForConfirmDialog();
    await this.cancelDialogAction();
  }

  /**
   * Assert expense appears in list
   */
  async expectExpenseInList(description: string): Promise<void> {
    await expect(this.expenseRows.filter({ hasText: description })).toBeVisible();
  }

  /**
   * Assert expense does NOT appear in list
   */
  async expectExpenseNotInList(description: string): Promise<void> {
    await expect(this.expenseRows.filter({ hasText: description })).not.toBeVisible();
  }

  /**
   * Get expense row by description
   */
  getExpenseRow(description: string): Locator {
    return this.expenseRows.filter({ hasText: description });
  }

  /**
   * Assert expense count in list
   */
  async expectExpenseCount(count: number): Promise<void> {
    await expect(this.expenseRows).toHaveCount(count);
  }

  /**
   * Assert YTD total displays expected value
   */
  async expectTotal(expectedTotal: string): Promise<void> {
    await expect(this.ytdTotal).toContainText(expectedTotal);
  }

  // Note: expectEmptyState() is inherited from BasePage
  // Override emptyStateLocator getter if custom selector needed

  /**
   * Get expense amount from a row
   */
  async getExpenseAmount(description: string): Promise<string> {
    const row = this.expenseRows.filter({ hasText: description });
    const amount = row.locator('.expense-amount');
    return (await amount.textContent()) || '';
  }

  /**
   * Get expense category from a row
   */
  async getExpenseCategory(description: string): Promise<string> {
    const row = this.expenseRows.filter({ hasText: description });
    const category = row.locator('mat-chip');
    return (await category.textContent()) || '';
  }

  /**
   * Navigate back to property detail
   */
  async goBack(): Promise<void> {
    await this.backButton.click();
  }
}
