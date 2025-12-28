import { computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import {
  patchState,
  signalStore,
  withComputed,
  withMethods,
  withState,
} from '@ngrx/signals';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { pipe, switchMap, tap, catchError, of } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  PropertyService,
  PropertySummaryDto,
  PropertyDetailDto,
  CreatePropertyRequest,
} from '../services/property.service';

/**
 * Property Store State Interface
 */
interface PropertyState {
  properties: PropertySummaryDto[];
  isLoading: boolean;
  error: string | null;
  selectedYear: number | null;
  // Property detail state (AC-2.3.2)
  selectedProperty: PropertyDetailDto | null;
  isLoadingDetail: boolean;
  detailError: string | null;
  // Property update state (AC-2.4.2)
  isUpdating: boolean;
  updateError: string | null;
  // Property delete state (AC-2.5.2)
  isDeleting: boolean;
  deleteError: string | null;
}

/**
 * Initial state for property store
 */
const initialState: PropertyState = {
  properties: [],
  isLoading: false,
  error: null,
  selectedYear: null,
  // Property detail initial state
  selectedProperty: null,
  isLoadingDetail: false,
  detailError: null,
  // Property update initial state
  isUpdating: false,
  updateError: null,
  // Property delete initial state
  isDeleting: false,
  deleteError: null,
};

/**
 * PropertyStore (AC-2.2.2, AC-2.2.4)
 *
 * State management for properties using @ngrx/signals.
 * Provides:
 * - Properties list with loading/error states
 * - Computed signals for totals (expense, income, net)
 * - Method to load properties from API
 */
export const PropertyStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed((store) => ({
    /**
     * Total count of properties
     */
    totalCount: computed(() => store.properties().length),

    /**
     * Sum of all property expense totals
     */
    totalExpenses: computed(() =>
      store.properties().reduce((sum, p) => sum + p.expenseTotal, 0)
    ),

    /**
     * Sum of all property income totals
     */
    totalIncome: computed(() =>
      store.properties().reduce((sum, p) => sum + p.incomeTotal, 0)
    ),

    /**
     * Net income (total income - total expenses)
     */
    netIncome: computed(() => {
      const income = store
        .properties()
        .reduce((sum, p) => sum + p.incomeTotal, 0);
      const expenses = store
        .properties()
        .reduce((sum, p) => sum + p.expenseTotal, 0);
      return income - expenses;
    }),

    /**
     * Whether the property list is empty
     */
    isEmpty: computed(() => store.properties().length === 0),

    /**
     * Whether we have properties loaded (not loading and not empty)
     */
    hasProperties: computed(
      () => !store.isLoading() && store.properties().length > 0
    ),

    /**
     * Net income for selected property (income - expenses) (AC-2.3.2)
     */
    selectedPropertyNetIncome: computed(() => {
      const property = store.selectedProperty();
      if (!property) return 0;
      return property.incomeTotal - property.expenseTotal;
    }),

    /**
     * Full formatted address for selected property
     */
    selectedPropertyFullAddress: computed(() => {
      const property = store.selectedProperty();
      if (!property) return '';
      return `${property.street}, ${property.city}, ${property.state} ${property.zipCode}`;
    }),
  })),
  withMethods((store, propertyService = inject(PropertyService), router = inject(Router), snackBar = inject(MatSnackBar)) => ({
    /**
     * Load properties from API
     * @param year Optional tax year filter
     */
    loadProperties: rxMethod<number | undefined>(
      pipe(
        tap(() =>
          patchState(store, {
            isLoading: true,
            error: null,
          })
        ),
        switchMap((year) =>
          propertyService.getProperties(year).pipe(
            tap((response) =>
              patchState(store, {
                properties: response.items,
                isLoading: false,
                selectedYear: year ?? null,
              })
            ),
            catchError((error) => {
              patchState(store, {
                isLoading: false,
                error: 'Failed to load properties. Please try again.',
              });
              console.error('Error loading properties:', error);
              return of(null);
            })
          )
        )
      )
    ),

    /**
     * Clear the error state
     */
    clearError(): void {
      patchState(store, { error: null });
    },

    /**
     * Reset store to initial state
     */
    reset(): void {
      patchState(store, initialState);
    },

    /**
     * Set selected year filter
     */
    setSelectedYear(year: number | null): void {
      patchState(store, { selectedYear: year });
    },

    /**
     * Load a single property by ID (AC-2.3.2, AC-2.3.5, AC-3.5.6)
     * @param params Object containing id and optional year filter
     */
    loadPropertyById: rxMethod<{ id: string; year?: number }>(
      pipe(
        tap(() =>
          patchState(store, {
            isLoadingDetail: true,
            detailError: null,
            selectedProperty: null,
          })
        ),
        switchMap(({ id, year }) =>
          propertyService.getPropertyById(id, year).pipe(
            tap((property) =>
              patchState(store, {
                selectedProperty: property,
                isLoadingDetail: false,
              })
            ),
            catchError((error) => {
              const errorMessage = error.status === 404
                ? 'Property not found'
                : 'Failed to load property. Please try again.';
              patchState(store, {
                isLoadingDetail: false,
                detailError: errorMessage,
              });
              console.error('Error loading property:', error);
              return of(null);
            })
          )
        )
      )
    ),

    /**
     * Clear selected property
     */
    clearSelectedProperty(): void {
      patchState(store, {
        selectedProperty: null,
        detailError: null,
      });
    },

    /**
     * Clear the detail error state
     */
    clearDetailError(): void {
      patchState(store, { detailError: null });
    },

    /**
     * Update an existing property (AC-2.4.2)
     * @param params Object containing id and request with updated property data
     */
    updateProperty: rxMethod<{ id: string; request: CreatePropertyRequest }>(
      pipe(
        tap(() =>
          patchState(store, {
            isUpdating: true,
            updateError: null,
          })
        ),
        switchMap(({ id, request }) =>
          propertyService.updateProperty(id, request).pipe(
            tap(() => {
              // Update succeeded - refresh the selected property to get updated data
              patchState(store, {
                isUpdating: false,
              });
            }),
            catchError((error) => {
              const errorMessage = error.status === 404
                ? 'Property not found'
                : error.status === 400
                ? 'Invalid property data'
                : 'Failed to update property. Please try again.';
              patchState(store, {
                isUpdating: false,
                updateError: errorMessage,
              });
              console.error('Error updating property:', error);
              return of(null);
            })
          )
        )
      )
    ),

    /**
     * Clear the update error state
     */
    clearUpdateError(): void {
      patchState(store, { updateError: null });
    },

    /**
     * Delete a property (soft delete) (AC-2.5.2, AC-2.5.4)
     * @param id Property GUID
     */
    deleteProperty: rxMethod<string>(
      pipe(
        tap(() =>
          patchState(store, {
            isDeleting: true,
            deleteError: null,
          })
        ),
        switchMap((id) =>
          propertyService.deleteProperty(id).pipe(
            tap(() => {
              // Remove property from local state
              const currentProperties = store.properties();
              patchState(store, {
                properties: currentProperties.filter(p => p.id !== id),
                isDeleting: false,
                selectedProperty: null,
              });

              // Show success snackbar (AC-2.5.4)
              snackBar.open('Property deleted', 'Close', {
                duration: 3000,
                horizontalPosition: 'center',
                verticalPosition: 'bottom',
              });

              // Navigate to dashboard (AC-2.5.4)
              router.navigate(['/dashboard']);
            }),
            catchError((error) => {
              const errorMessage = error.status === 404
                ? 'Property not found'
                : 'Failed to delete property. Please try again.';
              patchState(store, {
                isDeleting: false,
                deleteError: errorMessage,
              });

              // Show error snackbar
              snackBar.open(errorMessage, 'Close', {
                duration: 5000,
                horizontalPosition: 'center',
                verticalPosition: 'bottom',
              });

              console.error('Error deleting property:', error);
              return of(null);
            })
          )
        )
      )
    ),

    /**
     * Clear the delete error state
     */
    clearDeleteError(): void {
      patchState(store, { deleteError: null });
    },
  }))
);
