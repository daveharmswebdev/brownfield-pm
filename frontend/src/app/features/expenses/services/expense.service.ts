import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

/**
 * Request model for creating an expense (AC-3.1.1)
 */
export interface CreateExpenseRequest {
  propertyId: string;
  amount: number;
  date: string; // ISO date string (YYYY-MM-DD)
  categoryId: string;
  description?: string;
}

/**
 * Response model for expense creation
 */
export interface CreateExpenseResponse {
  id: string;
}

/**
 * Expense category DTO (AC-3.1.4)
 */
export interface ExpenseCategoryDto {
  id: string;
  name: string;
  scheduleELine?: string;
  sortOrder: number;
}

/**
 * Response model for expense categories
 */
export interface ExpenseCategoriesResponse {
  items: ExpenseCategoryDto[];
  totalCount: number;
}

/**
 * Expense DTO (AC-3.1.7)
 */
export interface ExpenseDto {
  id: string;
  propertyId: string;
  propertyName: string;
  categoryId: string;
  categoryName: string;
  scheduleELine?: string;
  amount: number;
  date: string;
  description?: string;
  receiptId?: string;
  createdAt: string;
}

/**
 * Response model for expenses by property
 */
export interface ExpenseListResponse {
  items: ExpenseDto[];
  totalCount: number;
  ytdTotal: number;
}

/**
 * Filter parameters for expense list (AC-3.4.3, AC-3.4.4, AC-3.4.5)
 */
export interface ExpenseFilters {
  dateFrom?: string; // ISO date string (YYYY-MM-DD)
  dateTo?: string; // ISO date string (YYYY-MM-DD)
  categoryIds?: string[];
  search?: string;
  year?: number;
  page: number;
  pageSize: number;
}

/**
 * Paginated result response (AC-3.4.8)
 */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

/**
 * Expense list item DTO for all-expenses list (AC-3.4.2)
 */
export interface ExpenseListItemDto {
  id: string;
  propertyId: string;
  propertyName: string;
  categoryId: string;
  categoryName: string;
  scheduleELine?: string;
  amount: number;
  date: string;
  description?: string;
  receiptId?: string;
  createdAt: string;
}

/**
 * Request model for updating an expense (AC-3.2.1)
 * Note: PropertyId is not included - expenses cannot be moved between properties
 */
export interface UpdateExpenseRequest {
  amount: number;
  date: string; // ISO date string (YYYY-MM-DD)
  categoryId: string;
  description?: string;
}

/**
 * Duplicate check result (AC-3.6.1, AC-3.6.5)
 */
export interface DuplicateCheckResult {
  isDuplicate: boolean;
  existingExpense?: DuplicateExpenseDto;
}

/**
 * Existing expense info for duplicate warning (AC-3.6.2)
 */
export interface DuplicateExpenseDto {
  id: string;
  date: string;
  amount: number;
  description?: string;
}

/**
 * ExpenseService (AC-3.1.1, AC-3.1.4, AC-3.1.6, AC-3.1.7, AC-3.6.1)
 *
 * Provides API methods for expense management.
 */
@Injectable({ providedIn: 'root' })
export class ExpenseService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1';

  /**
   * Create a new expense (AC-3.1.1, AC-3.1.6)
   * @param request Expense details
   * @returns Observable with new expense ID
   */
  createExpense(request: CreateExpenseRequest): Observable<CreateExpenseResponse> {
    return this.http.post<CreateExpenseResponse>(`${this.baseUrl}/expenses`, request);
  }

  /**
   * Get all expense categories (AC-3.1.4)
   * @returns Observable with list of 15 IRS Schedule E categories
   */
  getCategories(): Observable<ExpenseCategoriesResponse> {
    return this.http.get<ExpenseCategoriesResponse>(`${this.baseUrl}/expense-categories`);
  }

  /**
   * Get expenses for a property (AC-3.1.7)
   * @param propertyId Property GUID
   * @param year Optional tax year filter
   * @returns Observable with expenses list and YTD total
   */
  getExpensesByProperty(propertyId: string, year?: number): Observable<ExpenseListResponse> {
    const params = year ? { year: year.toString() } : undefined;
    return this.http.get<ExpenseListResponse>(
      `${this.baseUrl}/properties/${propertyId}/expenses`,
      { params }
    );
  }

  /**
   * Get a single expense by ID (AC-3.2.1, AC-3.2.2)
   * @param expenseId Expense GUID
   * @returns Observable with expense details
   */
  getExpense(expenseId: string): Observable<ExpenseDto> {
    return this.http.get<ExpenseDto>(`${this.baseUrl}/expenses/${expenseId}`);
  }

  /**
   * Update an existing expense (AC-3.2.1, AC-3.2.3, AC-3.2.4)
   * @param expenseId Expense GUID
   * @param request Updated expense details
   * @returns Observable that completes on success
   */
  updateExpense(expenseId: string, request: UpdateExpenseRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/expenses/${expenseId}`, request);
  }

  /**
   * Delete an expense (soft delete) (AC-3.3.1, AC-3.3.3)
   * @param expenseId Expense GUID
   * @returns Observable that completes on success
   */
  deleteExpense(expenseId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/expenses/${expenseId}`);
  }

  /**
   * Check for potential duplicate expenses (AC-3.6.1, AC-3.6.5)
   * @param propertyId Property GUID
   * @param amount Expense amount
   * @param date Expense date (ISO string YYYY-MM-DD)
   * @returns Observable with duplicate check result
   */
  checkDuplicateExpense(propertyId: string, amount: number, date: string): Observable<DuplicateCheckResult> {
    return this.http.get<DuplicateCheckResult>(`${this.baseUrl}/expenses/check-duplicate`, {
      params: {
        propertyId,
        amount: amount.toString(),
        date,
      },
    });
  }

  /**
   * Get all expenses across all properties with filtering and pagination (AC-3.4.1, AC-3.4.3, AC-3.4.4, AC-3.4.5, AC-3.4.8)
   * @param filters Filter and pagination parameters
   * @returns Observable with paginated expense list
   */
  getExpenses(filters: ExpenseFilters): Observable<PagedResult<ExpenseListItemDto>> {
    let params: Record<string, string | string[]> = {
      page: filters.page.toString(),
      pageSize: filters.pageSize.toString(),
    };

    if (filters.dateFrom) {
      params['dateFrom'] = filters.dateFrom;
    }
    if (filters.dateTo) {
      params['dateTo'] = filters.dateTo;
    }
    if (filters.categoryIds && filters.categoryIds.length > 0) {
      params['categoryIds'] = filters.categoryIds;
    }
    if (filters.search && filters.search.trim()) {
      params['search'] = filters.search.trim();
    }
    if (filters.year) {
      params['year'] = filters.year.toString();
    }

    return this.http.get<PagedResult<ExpenseListItemDto>>(`${this.baseUrl}/expenses`, { params });
  }
}
