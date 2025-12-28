/**
 * Currency Helper Utilities for E2E Tests
 *
 * Provides consistent currency formatting and parsing functions
 * for assertions and data handling in E2E tests.
 *
 * @module e2e/helpers/currency.helper
 */

/**
 * Formats a number as US currency string.
 *
 * Uses standard US locale formatting with $ prefix and comma separators.
 *
 * @param amount - The numeric amount to format
 * @returns Formatted currency string (e.g., "$1,500.00")
 *
 * @example
 * ```typescript
 * formatCurrency(1500); // "$1,500.00"
 * formatCurrency(99.5); // "$99.50"
 * formatCurrency(0);    // "$0.00"
 * ```
 */
export function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
  }).format(amount);
}

/**
 * Parses a currency string to a number.
 *
 * Handles common currency formats including $ prefix, commas, and decimals.
 *
 * @param currencyString - The currency string to parse (e.g., "$1,500.00")
 * @returns The numeric value
 *
 * @example
 * ```typescript
 * parseCurrency("$1,500.00"); // 1500
 * parseCurrency("$99.50");    // 99.5
 * parseCurrency("$0.00");     // 0
 * ```
 */
export function parseCurrency(currencyString: string): number {
  // Remove currency symbol, commas, and whitespace
  const cleaned = currencyString.replace(/[$,\s]/g, '');
  return parseFloat(cleaned) || 0;
}

/**
 * Formats a string amount (from test data) to expected display format.
 *
 * Test data generators use string amounts like "150.00".
 * This converts them to the expected UI display format "$150.00".
 *
 * @param amount - String amount from test data (e.g., "150.00")
 * @returns Formatted currency string for assertions
 *
 * @example
 * ```typescript
 * formatTestAmount("150.00");    // "$150.00"
 * formatTestAmount("1500.00");   // "$1,500.00"
 * formatTestAmount("99.5");      // "$99.50"
 * ```
 */
export function formatTestAmount(amount: string): string {
  return formatCurrency(parseFloat(amount));
}
