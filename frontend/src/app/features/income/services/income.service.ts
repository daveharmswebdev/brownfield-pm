import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

/**
 * Request model for creating an income entry (AC-4.1.3)
 */
export interface CreateIncomeRequest {
  propertyId: string;
  amount: number;
  date: string; // ISO date string (YYYY-MM-DD)
  source?: string;
  description?: string;
}

/**
 * Response model for income creation
 */
export interface CreateIncomeResponse {
  id: string;
}

/**
 * Income DTO (AC-4.1.6)
 */
export interface IncomeDto {
  id: string;
  propertyId: string;
  propertyName: string;
  amount: number;
  date: string;
  source?: string;
  description?: string;
  createdAt: string;
}

/**
 * Response model for income by property
 */
export interface IncomeListResponse {
  items: IncomeDto[];
  totalCount: number;
  ytdTotal: number;
}

/**
 * Response model for income total
 */
export interface IncomeTotalResponse {
  total: number;
  year: number;
}

/**
 * Filter parameters for getting all income (AC-4.3.3, AC-4.3.4)
 */
export interface IncomeFilterParams {
  dateFrom?: string;
  dateTo?: string;
  propertyId?: string;
  year?: number;
}

/**
 * Response model for all income (AC-4.3.1, AC-4.3.6)
 */
export interface AllIncomeResponse {
  items: IncomeDto[];
  totalCount: number;
  totalAmount: number;
}

/**
 * Request model for updating an income entry (AC-4.2.2)
 */
export interface UpdateIncomeRequest {
  amount: number;
  date: string; // ISO date string (YYYY-MM-DD)
  source?: string;
  description?: string;
}

/**
 * IncomeService (AC-4.1.3, AC-4.1.4, AC-4.2.2, AC-4.2.3, AC-4.2.6)
 *
 * Provides API methods for income management.
 */
@Injectable({ providedIn: 'root' })
export class IncomeService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1';

  /**
   * Get all income across all properties with optional filters (AC-4.3.1, AC-4.3.3, AC-4.3.4)
   * @param params Filter parameters (dateFrom, dateTo, propertyId, year)
   * @returns Observable with income list, total count, and total amount
   */
  getAllIncome(params?: IncomeFilterParams): Observable<AllIncomeResponse> {
    const queryParams: Record<string, string> = {};

    if (params?.dateFrom) {
      queryParams['dateFrom'] = params.dateFrom;
    }
    if (params?.dateTo) {
      queryParams['dateTo'] = params.dateTo;
    }
    if (params?.propertyId) {
      queryParams['propertyId'] = params.propertyId;
    }
    if (params?.year) {
      queryParams['year'] = params.year.toString();
    }

    return this.http.get<AllIncomeResponse>(`${this.baseUrl}/income`, {
      params: Object.keys(queryParams).length > 0 ? queryParams : undefined,
    });
  }

  /**
   * Create a new income entry (AC-4.1.3)
   * @param request Income details
   * @returns Observable with new income ID
   */
  createIncome(request: CreateIncomeRequest): Observable<CreateIncomeResponse> {
    return this.http.post<CreateIncomeResponse>(`${this.baseUrl}/income`, request);
  }

  /**
   * Get income for a property (AC-4.1.2, AC-4.1.6)
   * @param propertyId Property GUID
   * @param year Optional tax year filter
   * @returns Observable with income list and YTD total
   */
  getIncomeByProperty(propertyId: string, year?: number): Observable<IncomeListResponse> {
    const params = year ? { year: year.toString() } : undefined;
    return this.http.get<IncomeListResponse>(
      `${this.baseUrl}/properties/${propertyId}/income`,
      { params }
    );
  }

  /**
   * Get income total for a property (AC-4.1.4)
   * @param propertyId Property GUID
   * @param year Tax year
   * @returns Observable with total income amount
   */
  getIncomeTotalByProperty(propertyId: string, year: number): Observable<IncomeTotalResponse> {
    return this.http.get<IncomeTotalResponse>(
      `${this.baseUrl}/properties/${propertyId}/income/total`,
      { params: { year: year.toString() } }
    );
  }

  /**
   * Get a single income entry by ID (AC-4.2.2)
   * @param id Income GUID
   * @returns Observable with income entry
   */
  getIncomeById(id: string): Observable<IncomeDto> {
    return this.http.get<IncomeDto>(`${this.baseUrl}/income/${id}`);
  }

  /**
   * Update an existing income entry (AC-4.2.2, AC-4.2.3)
   * @param id Income GUID
   * @param request Updated income details
   * @returns Observable<void> on success
   */
  updateIncome(id: string, request: UpdateIncomeRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/income/${id}`, request);
  }

  /**
   * Delete an income entry (soft delete) (AC-4.2.5, AC-4.2.6)
   * @param id Income GUID
   * @returns Observable<void> on success
   */
  deleteIncome(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/income/${id}`);
  }
}
