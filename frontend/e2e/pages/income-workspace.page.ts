import { type Page, type Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';
import { type TestIncome } from '../helpers/test-data.helper';
import { formatDateForInput } from '../helpers/date.helper';

/**
 * IncomeWorkspacePage - Page object for income workspace E2E tests
 *
 * Covers: AC-TD.1.3 - Income CRUD E2E tests
 */
export class IncomeWorkspacePage extends BasePage {
  // Header elements
  readonly header: Locator;
  readonly backButton: Locator;

  // Form elements
  readonly formCard: Locator;
  readonly amountInput: Locator;
  readonly dateInput: Locator;
  readonly sourceInput: Locator;
  readonly descriptionInput: Locator;
  readonly saveButton: Locator;

  // List elements
  readonly incomeList: Locator;
  readonly incomeRows: Locator;
  readonly emptyState: Locator;
  readonly ytdTotal: Locator;

  constructor(page: Page) {
    super(page);

    // Header
    this.header = page.locator('.workspace-header');
    this.backButton = page.locator('.back-button');

    // New Income Form (app-income-form)
    this.formCard = page.locator('app-income-form .income-form-card');
    this.amountInput = page.locator('app-income-form input[formControlName="amount"]');
    this.dateInput = page.locator('app-income-form input[formControlName="date"]');
    this.sourceInput = page.locator('app-income-form input[formControlName="source"]');
    this.descriptionInput = page.locator('app-income-form textarea[formControlName="description"]');
    this.saveButton = page.locator('app-income-form button[type="submit"]');

    // Income List
    this.incomeList = page.locator('.income-list');
    this.incomeRows = page.locator('app-income-row');
    this.emptyState = page.locator('.empty-state');
    this.ytdTotal = page.locator('.ytd-amount');
  }

  async goto(): Promise<void> {
    // This page requires a property ID, so it can't navigate directly
    throw new Error('Use gotoWithPropertyId(propertyId) to navigate to income workspace');
  }

  async gotoWithPropertyId(propertyId: string): Promise<void> {
    await this.page.goto(`/properties/${propertyId}/income`);
    await this.waitForLoading();
  }

  /**
   * Fill the new income form with provided data
   */
  async fillForm(income: TestIncome): Promise<void> {
    // Clear and fill amount
    await this.amountInput.clear();
    await this.amountInput.fill(income.amount);

    // Fill date if provided (otherwise default is today)
    if (income.date) {
      await this.dateInput.clear();
      await this.dateInput.fill(formatDateForInput(income.date));
    }

    // Fill source if provided
    if (income.source) {
      await this.sourceInput.fill(income.source);
    }

    // Fill description if provided
    if (income.description) {
      await this.descriptionInput.fill(income.description);
    }
  }

  /**
   * Submit the new income form
   */
  async submit(): Promise<void> {
    await this.saveButton.click();
  }

  /**
   * Fill form and submit in one action
   */
  async createIncome(income: TestIncome): Promise<void> {
    await this.fillForm(income);
    await this.submit();
    // Wait for snackbar confirmation - uses "Income recorded" not "Income saved"
    await this.waitForSnackBar('Income recorded');
  }

  /**
   * Click edit button on a specific income row
   * Income uses inline editing - the edit button is in the row itself
   */
  async clickEditIncome(sourceOrAmount: string): Promise<void> {
    const row = this.incomeRows.filter({ hasText: sourceOrAmount });
    // Hover to make buttons visible
    await row.hover();
    await row.locator('.edit-button').click();
  }

  /**
   * Edit an income entry with new data
   * Income uses inline editing within the row
   */
  async editIncome(originalIdentifier: string, newData: Partial<TestIncome>): Promise<void> {
    await this.clickEditIncome(originalIdentifier);

    // Wait for inline edit form to appear (class changes to income-row--editing)
    const editingRow = this.page.locator('.income-row--editing');
    await expect(editingRow).toBeVisible();

    // Fill edit form fields
    if (newData.amount !== undefined) {
      const amountInput = editingRow.locator('input[formControlName="amount"]');
      await amountInput.clear();
      await amountInput.fill(newData.amount);
    }

    if (newData.date) {
      const dateInput = editingRow.locator('input[formControlName="date"]');
      await dateInput.clear();
      await dateInput.fill(formatDateForInput(newData.date));
    }

    if (newData.source !== undefined) {
      const sourceInput = editingRow.locator('input[formControlName="source"]');
      await sourceInput.clear();
      await sourceInput.fill(newData.source);
    }

    if (newData.description !== undefined) {
      const descriptionInput = editingRow.locator('input[formControlName="description"]');
      await descriptionInput.clear();
      await descriptionInput.fill(newData.description);
    }

    // Save changes
    await editingRow.locator('button[type="submit"]').click();
    await this.waitForSnackBar('Income updated');
  }

  /**
   * Cancel editing an income entry
   */
  async cancelEdit(): Promise<void> {
    const editingRow = this.page.locator('.income-row--editing');
    await editingRow.locator('button', { hasText: 'Cancel' }).click();
    await expect(editingRow).not.toBeVisible();
  }

  /**
   * Click delete button on a specific income row
   * Income uses inline delete confirmation
   */
  async clickDeleteIncome(sourceOrAmount: string): Promise<void> {
    const row = this.incomeRows.filter({ hasText: sourceOrAmount });
    // Hover to make buttons visible
    await row.hover();
    await row.locator('.delete-button').click();
  }

  /**
   * Delete an income entry (click delete + confirm)
   * Income uses inline confirmation instead of modal
   */
  async deleteIncome(sourceOrAmount: string): Promise<void> {
    await this.clickDeleteIncome(sourceOrAmount);

    // Wait for inline confirmation to appear (class changes to income-row--confirming)
    const confirmingRow = this.page.locator('.income-row--confirming');
    await expect(confirmingRow).toBeVisible();

    // Click confirm delete button
    await confirmingRow.locator('button', { hasText: 'Delete' }).click();

    // Wait for snackbar confirmation
    await this.waitForSnackBar('Income deleted');
  }

  /**
   * Cancel delete operation
   */
  async cancelDelete(): Promise<void> {
    const confirmingRow = this.page.locator('.income-row--confirming');
    await confirmingRow.locator('button', { hasText: 'Cancel' }).click();
    await expect(confirmingRow).not.toBeVisible();
  }

  /**
   * Assert income appears in list (by source or amount)
   */
  async expectIncomeInList(sourceOrAmount: string): Promise<void> {
    await expect(this.incomeRows.filter({ hasText: sourceOrAmount })).toBeVisible();
  }

  /**
   * Assert income does NOT appear in list
   */
  async expectIncomeNotInList(sourceOrAmount: string): Promise<void> {
    await expect(this.incomeRows.filter({ hasText: sourceOrAmount })).not.toBeVisible();
  }

  /**
   * Get income row by source or amount
   */
  getIncomeRow(sourceOrAmount: string): Locator {
    return this.incomeRows.filter({ hasText: sourceOrAmount });
  }

  /**
   * Assert income count in list
   */
  async expectIncomeCount(count: number): Promise<void> {
    await expect(this.incomeRows).toHaveCount(count);
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
   * Get income amount from a row
   */
  async getIncomeAmount(sourceOrIdentifier: string): Promise<string> {
    const row = this.incomeRows.filter({ hasText: sourceOrIdentifier });
    const amount = row.locator('.income-amount');
    return (await amount.textContent()) || '';
  }

  /**
   * Get income source from a row
   */
  async getIncomeSource(identifier: string): Promise<string> {
    const row = this.incomeRows.filter({ hasText: identifier });
    const source = row.locator('.income-source');
    return (await source.textContent()) || '';
  }

  /**
   * Navigate back to property detail
   */
  async goBack(): Promise<void> {
    await this.backButton.click();
  }
}
