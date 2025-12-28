/**
 * Test Data Helper for E2E Tests
 *
 * Provides interfaces and factory methods for generating test data.
 * All generated data uses timestamps for uniqueness, allowing tests
 * to run independently and in parallel.
 *
 * @module e2e/helpers/test-data.helper
 */

/**
 * Test user data for registration and authentication tests
 */
export interface TestUser {
  /** Display name for the account */
  accountName: string;
  /** Email address (unique per test run) */
  email: string;
  /** Password meeting complexity requirements */
  password: string;
}

/**
 * Test property data matching the property form fields
 */
export interface TestProperty {
  /** Property display name */
  name: string;
  /** Street address */
  street: string;
  /** City name */
  city: string;
  /** Full state name (e.g., "Texas" not "TX") */
  state: string;
  /** 5-digit ZIP code */
  zipCode: string;
}

/**
 * Test expense data matching the expense form fields
 */
export interface TestExpense {
  /** Amount as string (e.g., "150.00") */
  amount: string;
  /** Date of expense (defaults to today) */
  date?: Date;
  /** IRS Schedule E category */
  category: string;
  /** Optional description */
  description?: string;
}

/**
 * Test income data matching the income form fields
 */
export interface TestIncome {
  /** Amount as string (e.g., "1500.00") */
  amount: string;
  /** Date of income (defaults to today) */
  date?: Date;
  /** Income source (e.g., tenant name) */
  source?: string;
  /** Optional description */
  description?: string;
}

/**
 * IRS Schedule E expense categories
 *
 * These match the categories available in the expense category dropdown.
 */
const EXPENSE_CATEGORIES = [
  'Advertising',
  'Auto and Travel',
  'Cleaning and Maintenance',
  'Commissions',
  'Insurance',
  'Legal and Professional Fees',
  'Management Fees',
  'Mortgage Interest',
  'Other Interest',
  'Repairs',
  'Supplies',
  'Taxes',
  'Utilities',
  'Depreciation',
  'Other',
] as const;

/**
 * Type for valid expense categories
 */
export type ExpenseCategory = (typeof EXPENSE_CATEGORIES)[number];

/**
 * Factory class for generating test data with unique identifiers.
 *
 * All methods use timestamps to ensure test data is unique across
 * parallel test runs.
 *
 * @example
 * ```typescript
 * const user = TestDataHelper.generateTestUser();
 * const property = TestDataHelper.generateProperty();
 * const expense = TestDataHelper.generateExpense({ amount: '200.00' });
 * ```
 */
export class TestDataHelper {
  /**
   * Generates unique test user data.
   *
   * @returns TestUser with timestamp-based unique email
   */
  static generateTestUser(): TestUser {
    const timestamp = Date.now();
    return {
      accountName: `Test Account ${timestamp}`,
      email: `test-${timestamp}@example.com`,
      password: 'TestPassword123!',
    };
  }

  /**
   * Generates unique test property data.
   *
   * Uses timestamp for unique property name and random street number.
   *
   * @returns TestProperty with unique name
   */
  static generateProperty(): TestProperty {
    const timestamp = Date.now();
    return {
      name: `Test Property ${timestamp}`,
      street: `${Math.floor(Math.random() * 9999)} Test Street`,
      city: 'Austin',
      state: 'Texas', // Use full state name to match dropdown options
      zipCode: '78701',
    };
  }

  /**
   * Generates test expense data with unique timestamp-based description.
   *
   * @param options - Optional overrides for expense fields
   * @returns TestExpense with unique description
   *
   * @example
   * ```typescript
   * // Random expense
   * const expense = TestDataHelper.generateExpense();
   *
   * // Expense with specific amount and category
   * const repair = TestDataHelper.generateExpense({
   *   amount: '150.00',
   *   category: 'Repairs',
   * });
   * ```
   */
  static generateExpense(options?: Partial<TestExpense>): TestExpense {
    const timestamp = Date.now();
    const randomAmount = (Math.random() * 500 + 50).toFixed(2);
    const randomCategory = EXPENSE_CATEGORIES[Math.floor(Math.random() * EXPENSE_CATEGORIES.length)];

    return {
      amount: options?.amount ?? randomAmount,
      date: options?.date ?? new Date(),
      category: options?.category ?? randomCategory,
      description: options?.description ?? `E2E Test Expense ${timestamp}`,
    };
  }

  /**
   * Generates test income data with unique timestamp-based source.
   *
   * @param options - Optional overrides for income fields
   * @returns TestIncome with unique source
   *
   * @example
   * ```typescript
   * // Random income
   * const income = TestDataHelper.generateIncome();
   *
   * // Income with specific amount and source
   * const rent = TestDataHelper.generateIncome({
   *   amount: '1500.00',
   *   source: 'Tenant A',
   * });
   * ```
   */
  static generateIncome(options?: Partial<TestIncome>): TestIncome {
    const timestamp = Date.now();
    const randomAmount = (Math.random() * 2000 + 500).toFixed(2);

    return {
      amount: options?.amount ?? randomAmount,
      date: options?.date ?? new Date(),
      source: options?.source ?? `Tenant ${timestamp}`,
      description: options?.description ?? `E2E Test Income ${timestamp}`,
    };
  }

  /**
   * Creates a Date object for a specific number of days in the past.
   *
   * @deprecated Use `createPastDate()` from `date.helper.ts` instead.
   * This method will be removed in a future version.
   *
   * @param daysAgo - Number of days before today
   * @returns Date object
   *
   * @example
   * ```typescript
   * // Preferred:
   * import { createPastDate } from './date.helper';
   * const lastWeek = createPastDate(7);
   *
   * // Deprecated:
   * const lastWeek = TestDataHelper.generatePastDate(7);
   * ```
   */
  static generatePastDate(daysAgo: number): Date {
    // Delegate to shared utility for consistency
    const date = new Date();
    date.setDate(date.getDate() - daysAgo);
    return date;
  }

  /**
   * Gets an expense category by index (wraps around).
   *
   * @param index - Index into category array
   * @returns Category string
   */
  static getExpenseCategory(index: number): ExpenseCategory {
    return EXPENSE_CATEGORIES[index % EXPENSE_CATEGORIES.length];
  }

  /**
   * Returns all available expense categories.
   *
   * @returns Array of category strings
   */
  static getAllExpenseCategories(): readonly ExpenseCategory[] {
    return EXPENSE_CATEGORIES;
  }
}
