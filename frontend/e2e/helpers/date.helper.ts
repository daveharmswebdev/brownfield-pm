/**
 * Date Helper Utilities for E2E Tests
 *
 * Provides consistent date formatting functions used across page objects
 * and test files.
 *
 * @module e2e/helpers/date.helper
 */

/**
 * Formats a Date object to MM/DD/YYYY string for input fields.
 *
 * Angular Material date inputs expect dates in locale format.
 * This function ensures consistent formatting across all tests.
 *
 * @param date - The Date object to format
 * @returns Formatted date string in MM/DD/YYYY format
 *
 * @example
 * ```typescript
 * const date = new Date(2025, 0, 15); // Jan 15, 2025
 * formatDateForInput(date); // "01/15/2025"
 * ```
 */
export function formatDateForInput(date: Date): string {
  return date.toLocaleDateString('en-US', {
    month: '2-digit',
    day: '2-digit',
    year: 'numeric',
  });
}

/**
 * Creates a Date object for a specific number of days in the past.
 *
 * Useful for testing date range filters or creating historical data.
 *
 * @param daysAgo - Number of days before today
 * @returns Date object representing that many days ago
 *
 * @example
 * ```typescript
 * const lastWeek = createPastDate(7);
 * const lastMonth = createPastDate(30);
 * ```
 */
export function createPastDate(daysAgo: number): Date {
  const date = new Date();
  date.setDate(date.getDate() - daysAgo);
  return date;
}

/**
 * Creates a Date object for a specific number of days in the future.
 *
 * Useful for testing future-dated entries or scheduling features.
 *
 * @param daysFromNow - Number of days after today
 * @returns Date object representing that many days from now
 *
 * @example
 * ```typescript
 * const nextWeek = createFutureDate(7);
 * ```
 */
export function createFutureDate(daysFromNow: number): Date {
  const date = new Date();
  date.setDate(date.getDate() + daysFromNow);
  return date;
}
