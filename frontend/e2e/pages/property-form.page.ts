import { type Page, type Locator } from '@playwright/test';
import { BasePage } from './base.page';
import { type TestProperty } from '../helpers/test-data.helper';

/**
 * PropertyFormPage - Page object for property creation form E2E tests
 *
 * Handles the property creation form at `/properties/new`.
 * Used in conjunction with test-setup.helper.ts for property creation.
 *
 * @example
 * ```typescript
 * test('should create property', async ({ propertyFormPage }) => {
 *   await propertyFormPage.goto();
 *   await propertyFormPage.fillForm({
 *     name: 'My Property',
 *     street: '123 Main St',
 *     city: 'Austin',
 *     state: 'Texas',
 *     zipCode: '78701',
 *   });
 *   await propertyFormPage.submit();
 * });
 * ```
 */
export class PropertyFormPage extends BasePage {
  /** Property name input field */
  readonly nameInput: Locator;

  /** Street address input field */
  readonly streetInput: Locator;

  /** City input field */
  readonly cityInput: Locator;

  /** State dropdown select */
  readonly stateSelect: Locator;

  /** ZIP code input field */
  readonly zipCodeInput: Locator;

  /** Form submit button */
  readonly saveButton: Locator;

  /** Cancel button to return to previous page */
  readonly cancelButton: Locator;

  constructor(page: Page) {
    super(page);
    this.nameInput = page.locator('input[formControlName="name"]');
    this.streetInput = page.locator('input[formControlName="street"]');
    this.cityInput = page.locator('input[formControlName="city"]');
    this.stateSelect = page.locator('mat-select[formControlName="state"]');
    this.zipCodeInput = page.locator('input[formControlName="zipCode"]');
    this.saveButton = page.locator('button[type="submit"]');
    this.cancelButton = page.locator('button', { hasText: 'Cancel' });
  }

  /**
   * Navigates directly to the new property form.
   */
  async goto(): Promise<void> {
    await this.page.goto('/properties/new');
  }

  /**
   * Fills the property form with the provided data.
   *
   * @param property - TestProperty data to fill into the form
   */
  async fillForm(property: TestProperty): Promise<void> {
    await this.nameInput.fill(property.name);
    await this.streetInput.fill(property.street);
    await this.cityInput.fill(property.city);

    // Open state dropdown and select option
    await this.stateSelect.click();
    await this.page.locator('mat-option').filter({ hasText: property.state }).click();

    await this.zipCodeInput.fill(property.zipCode);
  }

  /**
   * Submits the property form.
   *
   * After successful submission, the page redirects to the dashboard.
   */
  async submit(): Promise<void> {
    await this.saveButton.click();
  }

  /**
   * Creates a property by filling the form and submitting.
   *
   * @param property - TestProperty data to create
   */
  async createProperty(property: TestProperty): Promise<void> {
    await this.fillForm(property);
    await this.submit();
  }

  /**
   * Clicks the cancel button to return to the previous page.
   */
  async cancel(): Promise<void> {
    await this.cancelButton.click();
  }
}
