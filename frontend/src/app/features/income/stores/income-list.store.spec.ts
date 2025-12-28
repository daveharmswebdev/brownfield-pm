import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { of, throwError } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { IncomeListStore } from './income-list.store';
import { IncomeService, AllIncomeResponse, IncomeDto } from '../services/income.service';

describe('IncomeListStore (AC-4.3.1, AC-4.3.3, AC-4.3.4, AC-4.3.5, AC-4.3.6)', () => {
  let store: InstanceType<typeof IncomeListStore>;
  let incomeServiceMock: {
    getAllIncome: ReturnType<typeof vi.fn>;
  };
  let snackBarMock: {
    open: ReturnType<typeof vi.fn>;
  };

  const mockIncomeData: IncomeDto[] = [
    {
      id: '1',
      propertyId: 'prop-1',
      propertyName: 'Property One',
      amount: 1500.00,
      date: '2025-01-15',
      source: 'Tenant 1',
      description: 'January rent',
      createdAt: '2025-01-01T00:00:00Z',
    },
    {
      id: '2',
      propertyId: 'prop-2',
      propertyName: 'Property Two',
      amount: 1600.00,
      date: '2025-01-10',
      source: 'Tenant 2',
      description: 'January rent',
      createdAt: '2025-01-01T00:00:00Z',
    },
  ];

  const mockResponse: AllIncomeResponse = {
    items: mockIncomeData,
    totalCount: 2,
    totalAmount: 3100.00,
  };

  beforeEach(() => {
    incomeServiceMock = {
      getAllIncome: vi.fn().mockReturnValue(of(mockResponse)),
    };
    snackBarMock = {
      open: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        IncomeListStore,
        { provide: IncomeService, useValue: incomeServiceMock },
        { provide: MatSnackBar, useValue: snackBarMock },
      ],
    });

    store = TestBed.inject(IncomeListStore);
  });

  describe('initial state', () => {
    it('should have empty income entries', () => {
      expect(store.incomeEntries()).toEqual([]);
    });

    it('should have zero total amount', () => {
      expect(store.totalAmount()).toBe(0);
    });

    it('should not be loading initially', () => {
      expect(store.isLoading()).toBe(false);
    });

    it('should have no active filters', () => {
      expect(store.hasActiveFilters()).toBe(false);
    });
  });

  describe('loadIncome (AC-4.3.1)', () => {
    it('should load income entries', () => {
      // Act
      store.initialize();

      // Assert
      expect(incomeServiceMock.getAllIncome).toHaveBeenCalled();
    });

    it('should update state with loaded income', () => {
      // Act
      store.initialize();

      // Assert
      expect(store.incomeEntries()).toEqual(mockIncomeData);
      expect(store.totalCount()).toBe(2);
      expect(store.totalAmount()).toBe(3100.00);
    });

    it('should set hasEverLoaded after loading', () => {
      // Act
      store.initialize();

      // Assert
      expect(store.hasIncome()).toBe(true);
    });

    it('should handle load error', () => {
      // Arrange
      incomeServiceMock.getAllIncome.mockReturnValue(throwError(() => new Error('Network error')));

      // Act
      store.initialize();

      // Assert
      expect(store.error()).toBe('Failed to load income. Please try again.');
      expect(store.incomeEntries()).toEqual([]);
    });
  });

  describe('setDateRange (AC-4.3.3)', () => {
    it('should update date range filters', () => {
      // Act
      store.setDateRange('2025-01-01', '2025-01-31');

      // Assert
      expect(incomeServiceMock.getAllIncome).toHaveBeenCalledWith(
        expect.objectContaining({
          dateFrom: '2025-01-01',
          dateTo: '2025-01-31',
        })
      );
    });

    it('should mark hasActiveFilters when date range set', () => {
      // Act
      store.setDateRange('2025-01-01', null);

      // Assert
      expect(store.hasActiveFilters()).toBe(true);
    });
  });

  describe('setPropertyFilter (AC-4.3.4)', () => {
    it('should filter by property', () => {
      // Act
      store.setPropertyFilter('prop-1');

      // Assert
      expect(incomeServiceMock.getAllIncome).toHaveBeenCalledWith(
        expect.objectContaining({
          propertyId: 'prop-1',
        })
      );
    });

    it('should mark hasActiveFilters when property set', () => {
      // Act
      store.setPropertyFilter('prop-1');

      // Assert
      expect(store.hasActiveFilters()).toBe(true);
    });
  });

  describe('setYear', () => {
    it('should filter by year', () => {
      // Act
      store.setYear(2025);

      // Assert
      expect(incomeServiceMock.getAllIncome).toHaveBeenCalledWith(
        expect.objectContaining({
          year: 2025,
        })
      );
    });
  });

  describe('clearFilters (AC-4.3.5)', () => {
    it('should clear all filters', () => {
      // Arrange
      store.setDateRange('2025-01-01', '2025-01-31');
      store.setPropertyFilter('prop-1');

      // Act
      store.clearFilters();

      // Assert
      expect(store.hasActiveFilters()).toBe(false);
    });

    it('should reload income after clearing filters', () => {
      // Act
      store.clearFilters();

      // Assert
      expect(incomeServiceMock.getAllIncome).toHaveBeenCalled();
    });
  });

  describe('computed signals', () => {
    it('formattedTotalAmount should format as currency (AC-4.3.6)', () => {
      // Arrange
      store.initialize();

      // Assert
      expect(store.formattedTotalAmount()).toMatch(/\$3,100\.00/);
    });

    it('isTrulyEmpty should be true when no income exists', () => {
      // Arrange
      incomeServiceMock.getAllIncome.mockReturnValue(of({
        items: [],
        totalCount: 0,
        totalAmount: 0,
      }));

      // Act
      store.initialize();

      // Assert
      expect(store.isTrulyEmpty()).toBe(true);
    });

    it('isFilteredEmpty should be true when filters return no results (AC-4.3.5)', () => {
      // Arrange - First load some data
      store.initialize();

      // Then set filter that returns empty
      incomeServiceMock.getAllIncome.mockReturnValue(of({
        items: [],
        totalCount: 0,
        totalAmount: 0,
      }));
      store.setPropertyFilter('non-existent');

      // Assert
      expect(store.isFilteredEmpty()).toBe(true);
    });
  });

  describe('reset', () => {
    it('should reset to initial state', () => {
      // Arrange
      store.initialize();

      // Act
      store.reset();

      // Assert
      expect(store.incomeEntries()).toEqual([]);
      expect(store.totalAmount()).toBe(0);
      expect(store.hasActiveFilters()).toBe(false);
    });
  });
});
