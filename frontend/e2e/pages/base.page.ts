import { type Page, type Locator, expect } from '@playwright/test';

/**
 * BasePage - Abstract base class for all page objects
 *
 * Provides common functionality shared across all page objects including:
 * - Loading state handling
 * - Snackbar notifications
 * - Confirmation dialogs
 * - Empty state assertions
 * - Navigation helpers
 *
 * All page objects should extend this class and implement the `goto()` method.
 *
 * @abstract
 * @example
 * ```typescript
 * export class MyPage extends BasePage {
 *   async goto(): Promise<void> {
 *     await this.page.goto('/my-page');
 *   }
 * }
 * ```
 */
export abstract class BasePage {
  readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  // ─────────────────────────────────────────────────────────────────────────────
  // Loading State
  // ─────────────────────────────────────────────────────────────────────────────

  /**
   * Locator for Angular Material spinner component
   */
  get loadingSpinner(): Locator {
    return this.page.locator('mat-spinner');
  }

  /**
   * Waits for any loading spinner to disappear.
   *
   * @param timeout - Maximum time to wait in milliseconds (default: 10000)
   */
  async waitForLoading(timeout = 10000): Promise<void> {
    await this.loadingSpinner.waitFor({ state: 'hidden', timeout });
  }

  // ─────────────────────────────────────────────────────────────────────────────
  // Snackbar Notifications
  // ─────────────────────────────────────────────────────────────────────────────

  /**
   * Locator for Angular Material snackbar component.
   *
   * Uses `[matsnackbarlabel]` attribute to avoid matching nested elements.
   */
  get snackBar(): Locator {
    return this.page.locator('[matsnackbarlabel]');
  }

  /**
   * Waits for a snackbar notification with specific text to appear.
   *
   * @param text - Expected text content of the snackbar
   * @param timeout - Maximum time to wait in milliseconds (default: 5000)
   */
  async waitForSnackBar(text: string, timeout = 5000): Promise<void> {
    const snackbar = this.snackBar.filter({ hasText: text });
    await snackbar.first().waitFor({ state: 'visible', timeout });
  }

  /**
   * Asserts that a snackbar with specific text is visible.
   *
   * Combines waiting and assertion in one method for cleaner test code.
   *
   * @param text - Expected text content of the snackbar
   * @param timeout - Maximum time to wait in milliseconds (default: 5000)
   *
   * @example
   * ```typescript
   * await page.expectSnackBar('Expense saved');
   * await page.expectSnackBar('Property deleted', 10000);
   * ```
   */
  async expectSnackBar(text: string, timeout = 5000): Promise<void> {
    const snackbar = this.snackBar.filter({ hasText: text });
    await expect(snackbar.first()).toBeVisible({ timeout });
  }

  // ─────────────────────────────────────────────────────────────────────────────
  // Confirmation Dialog
  // ─────────────────────────────────────────────────────────────────────────────

  /**
   * Locator for Angular Material dialog container.
   *
   * Dialogs render in an overlay, so this targets the root container.
   */
  get confirmDialog(): Locator {
    return this.page.locator('mat-dialog-container');
  }

  /**
   * Locator for the confirm/delete button within a dialog.
   *
   * Matches button with "Delete" text by default.
   */
  get confirmDialogConfirmButton(): Locator {
    return this.confirmDialog.locator('button', { hasText: 'Delete' });
  }

  /**
   * Locator for the cancel button within a dialog.
   */
  get confirmDialogCancelButton(): Locator {
    return this.confirmDialog.locator('button', { hasText: 'Cancel' });
  }

  /**
   * Waits for confirmation dialog to appear.
   *
   * @param timeout - Maximum time to wait in milliseconds (default: 5000)
   */
  async waitForConfirmDialog(timeout = 5000): Promise<void> {
    await expect(this.confirmDialog).toBeVisible({ timeout });
  }

  /**
   * Clicks the confirm button in a dialog and optionally waits for snackbar.
   *
   * @param snackBarText - If provided, waits for this snackbar message after confirmation
   *
   * @example
   * ```typescript
   * await page.confirmDialogAction('Expense deleted');
   * ```
   */
  async confirmDialogAction(snackBarText?: string): Promise<void> {
    await this.confirmDialogConfirmButton.click();
    if (snackBarText) {
      await this.waitForSnackBar(snackBarText);
    }
  }

  /**
   * Cancels a confirmation dialog.
   */
  async cancelDialogAction(): Promise<void> {
    await this.confirmDialogCancelButton.click();
    await expect(this.confirmDialog).not.toBeVisible();
  }

  // ─────────────────────────────────────────────────────────────────────────────
  // Empty State
  // ─────────────────────────────────────────────────────────────────────────────

  /**
   * Default locator for empty state component.
   *
   * Uses `.empty-state` CSS class as fallback. Override this getter in subclasses
   * if the page uses a different selector (e.g., `app-empty-state` component).
   *
   * @example
   * ```typescript
   * // In a subclass that uses component selector:
   * get emptyStateLocator(): Locator {
   *   return this.page.locator('app-empty-state');
   * }
   * ```
   */
  get emptyStateLocator(): Locator {
    return this.page.locator('.empty-state');
  }

  /**
   * Asserts that an empty state is visible.
   *
   * Uses the default `.empty-state` selector or a custom selector if provided.
   *
   * @param selector - Optional custom selector for empty state element
   *
   * @example
   * ```typescript
   * await page.expectEmptyState(); // Uses default .empty-state
   * await page.expectEmptyState('app-empty-state'); // Custom selector
   * ```
   */
  async expectEmptyState(selector?: string): Promise<void> {
    const locator = selector ? this.page.locator(selector) : this.emptyStateLocator;
    await expect(locator).toBeVisible();
  }

  // ─────────────────────────────────────────────────────────────────────────────
  // Navigation
  // ─────────────────────────────────────────────────────────────────────────────

  /**
   * Waits for navigation to a URL matching a pattern.
   *
   * @param pattern - URL pattern (string or RegExp) to wait for
   * @param timeout - Maximum time to wait in milliseconds (default: 10000)
   *
   * @example
   * ```typescript
   * await page.waitForNavigation('/dashboard');
   * await page.waitForNavigation(/\/properties\/[a-f0-9-]+$/);
   * ```
   */
  async waitForNavigation(pattern: string | RegExp, timeout = 10000): Promise<void> {
    await this.page.waitForURL(pattern, { timeout });
  }

  // ─────────────────────────────────────────────────────────────────────────────
  // Abstract Methods
  // ─────────────────────────────────────────────────────────────────────────────

  /**
   * Navigates to this page.
   *
   * Must be implemented by all page objects. For pages requiring parameters
   * (like property ID), this method should throw an error directing users
   * to use the parameterized navigation method.
   *
   * @abstract
   */
  abstract goto(): Promise<void>;
}
