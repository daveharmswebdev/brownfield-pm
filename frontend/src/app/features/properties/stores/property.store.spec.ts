import { TestBed } from '@angular/core/testing';
import { PropertyStore } from './property.store';
import { PropertyService, GetAllPropertiesResponse, PropertyDetailDto } from '../services/property.service';
import { of, throwError, delay } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

describe('PropertyStore', () => {
  let store: InstanceType<typeof PropertyStore>;
  let propertyServiceSpy: {
    getProperties: ReturnType<typeof vi.fn>;
    getPropertyById: ReturnType<typeof vi.fn>;
  };

  const mockPropertiesResponse: GetAllPropertiesResponse = {
    items: [
      {
        id: '1',
        name: 'Oak Street Duplex',
        street: '123 Oak St',
        city: 'Austin',
        state: 'TX',
        zipCode: '78701',
        expenseTotal: 1500,
        incomeTotal: 3000,
      },
      {
        id: '2',
        name: 'Maple Ave Condo',
        street: '456 Maple Ave',
        city: 'Dallas',
        state: 'TX',
        zipCode: '75201',
        expenseTotal: 800,
        incomeTotal: 2000,
      },
    ],
    totalCount: 2,
  };

  const mockPropertyDetail: PropertyDetailDto = {
    id: '1',
    name: 'Oak Street Duplex',
    street: '123 Oak St',
    city: 'Austin',
    state: 'TX',
    zipCode: '78701',
    expenseTotal: 1500,
    incomeTotal: 3000,
    createdAt: '2025-01-15T10:30:00Z',
    updatedAt: '2025-01-20T14:45:00Z',
    recentExpenses: [],
    recentIncome: [],
  };

  beforeEach(() => {
    propertyServiceSpy = {
      getProperties: vi.fn().mockReturnValue(of({ items: [], totalCount: 0 })),
      getPropertyById: vi.fn().mockReturnValue(of(mockPropertyDetail)),
    };

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        PropertyStore,
        { provide: PropertyService, useValue: propertyServiceSpy },
      ],
    });

    store = TestBed.inject(PropertyStore);
  });

  describe('initial state', () => {
    it('should have empty properties array', () => {
      expect(store.properties()).toEqual([]);
    });

    it('should not be loading initially', () => {
      expect(store.isLoading()).toBe(false);
    });

    it('should have no error initially', () => {
      expect(store.error()).toBeNull();
    });

    it('should have null selected year initially', () => {
      expect(store.selectedYear()).toBeNull();
    });
  });

  describe('computed signals', () => {
    it('should compute totalCount correctly', async () => {
      propertyServiceSpy.getProperties.mockReturnValue(of(mockPropertiesResponse));

      store.loadProperties(undefined);
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(store.totalCount()).toBe(2);
    });

    it('should compute totalExpenses correctly', async () => {
      propertyServiceSpy.getProperties.mockReturnValue(of(mockPropertiesResponse));

      store.loadProperties(undefined);
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(store.totalExpenses()).toBe(2300); // 1500 + 800
    });

    it('should compute totalIncome correctly', async () => {
      propertyServiceSpy.getProperties.mockReturnValue(of(mockPropertiesResponse));

      store.loadProperties(undefined);
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(store.totalIncome()).toBe(5000); // 3000 + 2000
    });

    it('should compute netIncome correctly', async () => {
      propertyServiceSpy.getProperties.mockReturnValue(of(mockPropertiesResponse));

      store.loadProperties(undefined);
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(store.netIncome()).toBe(2700); // 5000 - 2300
    });

    it('should compute isEmpty correctly when empty', () => {
      expect(store.isEmpty()).toBe(true);
    });

    it('should compute isEmpty correctly when has properties', async () => {
      propertyServiceSpy.getProperties.mockReturnValue(of(mockPropertiesResponse));

      store.loadProperties(undefined);
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(store.isEmpty()).toBe(false);
    });

    it('should compute hasProperties correctly', async () => {
      propertyServiceSpy.getProperties.mockReturnValue(of(mockPropertiesResponse));

      expect(store.hasProperties()).toBe(false);

      store.loadProperties(undefined);
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(store.hasProperties()).toBe(true);
    });
  });

  describe('loadProperties', () => {
    it('should load properties successfully', async () => {
      propertyServiceSpy.getProperties.mockReturnValue(of(mockPropertiesResponse));

      store.loadProperties(undefined);
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(store.properties()).toEqual(mockPropertiesResponse.items);
      expect(store.error()).toBeNull();
      expect(store.isLoading()).toBe(false);
    });

    it('should call service without year parameter', async () => {
      store.loadProperties(undefined);
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(propertyServiceSpy.getProperties).toHaveBeenCalledWith(undefined);
    });

    it('should call service with year parameter', async () => {
      store.loadProperties(2024);
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(propertyServiceSpy.getProperties).toHaveBeenCalledWith(2024);
    });

    it('should set selectedYear when loading with year', async () => {
      store.loadProperties(2024);
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(store.selectedYear()).toBe(2024);
    });

    it('should handle error gracefully', async () => {
      propertyServiceSpy.getProperties.mockReturnValue(
        throwError(() => new Error('Network error'))
      );

      store.loadProperties(undefined);
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(store.error()).toBe('Failed to load properties. Please try again.');
      expect(store.isLoading()).toBe(false);
      expect(store.properties()).toEqual([]);
    });

    it('should clear error before loading', async () => {
      // First, trigger an error
      propertyServiceSpy.getProperties.mockReturnValue(
        throwError(() => new Error('Network error'))
      );

      store.loadProperties(undefined);
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(store.error()).not.toBeNull();

      // Now load successfully
      propertyServiceSpy.getProperties.mockReturnValue(of(mockPropertiesResponse));

      store.loadProperties(undefined);
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(store.error()).toBeNull();
    });
  });

  describe('clearError', () => {
    it('should clear the error state', async () => {
      propertyServiceSpy.getProperties.mockReturnValue(
        throwError(() => new Error('Network error'))
      );

      store.loadProperties(undefined);
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(store.error()).not.toBeNull();

      store.clearError();

      expect(store.error()).toBeNull();
    });
  });

  describe('reset', () => {
    it('should reset store to initial state', async () => {
      propertyServiceSpy.getProperties.mockReturnValue(of(mockPropertiesResponse));

      store.loadProperties(2024);
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(store.properties().length).toBeGreaterThan(0);

      store.reset();

      expect(store.properties()).toEqual([]);
      expect(store.isLoading()).toBe(false);
      expect(store.error()).toBeNull();
      expect(store.selectedYear()).toBeNull();
    });
  });

  describe('setSelectedYear', () => {
    it('should set selected year', () => {
      store.setSelectedYear(2024);
      expect(store.selectedYear()).toBe(2024);
    });

    it('should allow setting null year', () => {
      store.setSelectedYear(2024);
      store.setSelectedYear(null);
      expect(store.selectedYear()).toBeNull();
    });
  });

  // Tests for property detail functionality (AC-2.3.2, AC-2.3.5, AC-2.3.6)
  describe('initial detail state', () => {
    it('should have null selectedProperty initially', () => {
      expect(store.selectedProperty()).toBeNull();
    });

    it('should not be loading detail initially', () => {
      expect(store.isLoadingDetail()).toBe(false);
    });

    it('should have no detail error initially', () => {
      expect(store.detailError()).toBeNull();
    });
  });

  describe('loadPropertyById', () => {
    it('should load property successfully', async () => {
      store.loadPropertyById({ id: '1' });
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(store.selectedProperty()).toEqual(mockPropertyDetail);
      expect(store.detailError()).toBeNull();
      expect(store.isLoadingDetail()).toBe(false);
    });

    it('should call service with property ID', async () => {
      store.loadPropertyById({ id: 'test-id' });
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(propertyServiceSpy.getPropertyById).toHaveBeenCalledWith('test-id', undefined);
    });

    it('should call service with property ID and year', async () => {
      store.loadPropertyById({ id: 'test-id', year: 2024 });
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(propertyServiceSpy.getPropertyById).toHaveBeenCalledWith('test-id', 2024);
    });

    it('should handle 404 error with specific message', async () => {
      const error404 = new HttpErrorResponse({ status: 404, statusText: 'Not Found' });
      propertyServiceSpy.getPropertyById.mockReturnValue(throwError(() => error404));

      store.loadPropertyById({ id: 'non-existent-id' });
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(store.detailError()).toBe('Property not found');
      expect(store.isLoadingDetail()).toBe(false);
      expect(store.selectedProperty()).toBeNull();
    });

    it('should handle other errors with generic message', async () => {
      const error500 = new HttpErrorResponse({ status: 500, statusText: 'Server Error' });
      propertyServiceSpy.getPropertyById.mockReturnValue(throwError(() => error500));

      store.loadPropertyById({ id: '1' });
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(store.detailError()).toBe('Failed to load property. Please try again.');
      expect(store.isLoadingDetail()).toBe(false);
    });

    it('should set loading state while fetching', () => {
      propertyServiceSpy.getPropertyById.mockReturnValue(of(mockPropertyDetail).pipe(delay(100)));

      store.loadPropertyById({ id: '1' });

      expect(store.isLoadingDetail()).toBe(true);
    });

    it('should clear previous property and error before loading', async () => {
      // First load a property
      store.loadPropertyById({ id: '1' });
      await new Promise(resolve => setTimeout(resolve, 0));
      expect(store.selectedProperty()).not.toBeNull();

      // Now trigger a load with a delayed response to verify clearing behavior
      propertyServiceSpy.getPropertyById.mockReturnValue(of({
        ...mockPropertyDetail,
        id: '2',
        name: 'New Property',
      }).pipe(delay(50)));

      store.loadPropertyById({ id: '2' });
      // During loading (before response), selectedProperty should be cleared
      expect(store.selectedProperty()).toBeNull();
      expect(store.isLoadingDetail()).toBe(true);

      // Wait for response
      await new Promise(resolve => setTimeout(resolve, 100));
      expect(store.selectedProperty()?.id).toBe('2');
    });
  });

  describe('selectedPropertyNetIncome', () => {
    it('should return 0 when no property selected', () => {
      expect(store.selectedPropertyNetIncome()).toBe(0);
    });

    it('should calculate net income correctly', async () => {
      store.loadPropertyById({ id: '1' });
      await new Promise(resolve => setTimeout(resolve, 0));

      // incomeTotal (3000) - expenseTotal (1500) = 1500
      expect(store.selectedPropertyNetIncome()).toBe(1500);
    });
  });

  describe('selectedPropertyFullAddress', () => {
    it('should return empty string when no property selected', () => {
      expect(store.selectedPropertyFullAddress()).toBe('');
    });

    it('should format address correctly', async () => {
      store.loadPropertyById({ id: '1' });
      await new Promise(resolve => setTimeout(resolve, 0));

      expect(store.selectedPropertyFullAddress()).toBe('123 Oak St, Austin, TX 78701');
    });
  });

  describe('clearSelectedProperty', () => {
    it('should clear selected property and error', async () => {
      store.loadPropertyById({ id: '1' });
      await new Promise(resolve => setTimeout(resolve, 0));
      expect(store.selectedProperty()).not.toBeNull();

      store.clearSelectedProperty();

      expect(store.selectedProperty()).toBeNull();
      expect(store.detailError()).toBeNull();
    });
  });

  describe('clearDetailError', () => {
    it('should clear detail error', async () => {
      const error404 = new HttpErrorResponse({ status: 404, statusText: 'Not Found' });
      propertyServiceSpy.getPropertyById.mockReturnValue(throwError(() => error404));

      store.loadPropertyById({ id: 'non-existent-id' });
      await new Promise(resolve => setTimeout(resolve, 0));
      expect(store.detailError()).not.toBeNull();

      store.clearDetailError();

      expect(store.detailError()).toBeNull();
    });
  });
});
