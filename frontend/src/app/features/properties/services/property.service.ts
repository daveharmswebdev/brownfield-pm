import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface CreatePropertyRequest {
  name: string;
  street: string;
  city: string;
  state: string;
  zipCode: string;
}

export interface CreatePropertyResponse {
  id: string;
}

export interface PropertySummaryDto {
  id: string;
  name: string;
  street: string;
  city: string;
  state: string;
  zipCode: string;
  expenseTotal: number;
  incomeTotal: number;
}

export interface GetAllPropertiesResponse {
  items: PropertySummaryDto[];
  totalCount: number;
}

export interface ExpenseSummaryDto {
  id: string;
  description: string;
  amount: number;
  date: string;
}

export interface IncomeSummaryDto {
  id: string;
  description: string;
  amount: number;
  date: string;
}

export interface PropertyDetailDto {
  id: string;
  name: string;
  street: string;
  city: string;
  state: string;
  zipCode: string;
  expenseTotal: number;
  incomeTotal: number;
  createdAt: string;
  updatedAt: string;
  recentExpenses: ExpenseSummaryDto[];
  recentIncome: IncomeSummaryDto[];
}

@Injectable({ providedIn: 'root' })
export class PropertyService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1/properties';

  createProperty(request: CreatePropertyRequest): Observable<CreatePropertyResponse> {
    return this.http.post<CreatePropertyResponse>(this.baseUrl, request);
  }

  /**
   * Get all properties for the current user.
   * @param year Optional tax year filter for expense/income totals
   * @returns Observable with properties list and total count
   */
  getProperties(year?: number): Observable<GetAllPropertiesResponse> {
    const params = year ? { year: year.toString() } : undefined;
    return this.http.get<GetAllPropertiesResponse>(this.baseUrl, { params });
  }

  /**
   * Get a single property by ID (AC-2.3.5, AC-3.5.6).
   * @param id Property GUID
   * @param year Optional tax year filter for expense/income totals
   * @returns Observable with property detail or 404 error
   */
  getPropertyById(id: string, year?: number): Observable<PropertyDetailDto> {
    const params = year ? { year: year.toString() } : undefined;
    return this.http.get<PropertyDetailDto>(`${this.baseUrl}/${id}`, { params });
  }

  /**
   * Update an existing property (AC-2.4.2, AC-2.4.5).
   * @param id Property GUID
   * @param request Updated property details
   * @returns Observable<void> (204 No Content response)
   */
  updateProperty(id: string, request: CreatePropertyRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, request);
  }

  /**
   * Delete a property (soft delete) (AC-2.5.2).
   * @param id Property GUID
   * @returns Observable<void> (204 No Content response)
   */
  deleteProperty(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
