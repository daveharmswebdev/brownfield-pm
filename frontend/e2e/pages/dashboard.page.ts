import { type Page, type Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';

/**
 * DashboardPage - Page object for the main dashboard E2E tests
 *
 * The dashboard is the main landing page after login, showing:
 * - Welcome header with user greeting
 * - Stats bar with portfolio totals
 * - Property list with individual property cards
 * - "Add Property" action button
 *
 * @example
 * ```typescript
 * test('should display dashboard', async ({ dashboardPage }) => {
 *   await dashboardPage.goto();
 *   await dashboardPage.expectWelcome();
 *   await dashboardPage.expectPropertyCount(1);
 * });
 * ```
 */
export class DashboardPage extends BasePage {
  /** Welcome header containing user greeting */
  readonly welcomeHeader: Locator;

  /** Button to navigate to property creation form */
  readonly addPropertyButton: Locator;

  /** Container for the properties list card */
  readonly propertyList: Locator;

  /** Individual property row components */
  readonly propertyRows: Locator;

  /** Empty state component shown when no properties exist */
  readonly emptyState: Locator;

  /** Stats bar showing portfolio totals */
  readonly statsBar: Locator;

  constructor(page: Page) {
    super(page);
    this.welcomeHeader = page.locator('.dashboard-header h1');
    this.addPropertyButton = page.locator('button', { hasText: 'Add Property' });
    this.propertyList = page.locator('.properties-list-card');
    this.propertyRows = page.locator('app-property-row');
    this.emptyState = page.locator('app-empty-state');
    this.statsBar = page.locator('app-stats-bar');
  }

  /**
   * Navigates to the dashboard page.
   */
  async goto(): Promise<void> {
    await this.page.goto('/dashboard');
  }

  /**
   * Asserts that the welcome header is visible and contains expected text.
   */
  async expectWelcome(): Promise<void> {
    await expect(this.welcomeHeader).toContainText('Welcome back');
  }

  /**
   * Asserts that the empty state component is visible.
   *
   * Use this when testing a new user with no properties.
   */
  async expectNoProperties(): Promise<void> {
    await expect(this.emptyState).toBeVisible();
  }

  /**
   * Asserts the number of properties displayed in the list.
   *
   * @param count - Expected number of property rows
   */
  async expectPropertyCount(count: number): Promise<void> {
    await expect(this.propertyRows).toHaveCount(count);
  }

  /**
   * Clicks the "Add Property" button to navigate to property creation.
   */
  async clickAddProperty(): Promise<void> {
    await this.addPropertyButton.first().click();
  }

  /**
   * Clicks on a property row by name to navigate to its detail page.
   *
   * @param name - The property name to click on
   */
  async clickProperty(name: string): Promise<void> {
    await this.page.locator('app-property-row', { hasText: name }).click();
  }

  /**
   * Gets a property row locator by name.
   *
   * @param name - The property name to find
   * @returns Locator for the property row
   */
  getPropertyRow(name: string): Locator {
    return this.page.locator('app-property-row', { hasText: name });
  }

  /**
   * Asserts that a property with the given name exists in the list.
   *
   * @param name - The property name to check for
   */
  async expectPropertyInList(name: string): Promise<void> {
    await expect(this.getPropertyRow(name)).toBeVisible();
  }
}
