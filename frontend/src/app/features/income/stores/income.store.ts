import { computed, inject } from '@angular/core';
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
  IncomeService,
  IncomeDto,
  CreateIncomeRequest,
  UpdateIncomeRequest,
} from '../services/income.service';

/**
 * Income Store State Interface (AC-4.1.2, AC-4.1.4, AC-4.1.6, AC-4.2.3, AC-4.2.6)
 */
interface IncomeState {
  incomeEntries: IncomeDto[];
  currentPropertyId: string | null;
  currentPropertyName: string | null;
  ytdTotal: number;
  isLoading: boolean;
  isSaving: boolean;
  isUpdating: boolean;
  isDeleting: boolean;
  editingIncomeId: string | null;
  error: string | null;
}

/**
 * Initial state for income store
 */
const initialState: IncomeState = {
  incomeEntries: [],
  currentPropertyId: null,
  currentPropertyName: null,
  ytdTotal: 0,
  isLoading: false,
  isSaving: false,
  isUpdating: false,
  isDeleting: false,
  editingIncomeId: null,
  error: null,
};

/**
 * IncomeStore (AC-4.1.2, AC-4.1.4, AC-4.1.6, AC-4.2.3, AC-4.2.6)
 *
 * State management for income using @ngrx/signals.
 * Provides:
 * - Income list with loading/error states
 * - Methods for create, update, delete and load operations
 * - Optimistic updates on create/delete
 */
export const IncomeStore = signalStore(
  { providedIn: 'root' },
  withState(initialState),
  withComputed((store) => ({
    /**
     * Income for current property, sorted by date (newest first)
     */
    incomeForCurrentProperty: computed(() =>
      store.incomeEntries().filter((i) => i.propertyId === store.currentPropertyId())
    ),

    /**
     * Total income count for current property
     */
    incomeCount: computed(() => store.incomeEntries().length),

    /**
     * Whether we have income loaded
     */
    hasIncome: computed(() => !store.isLoading() && store.incomeEntries().length > 0),

    /**
     * Whether the income list is empty
     */
    isEmpty: computed(() => !store.isLoading() && store.incomeEntries().length === 0),

    /**
     * Whether we are currently editing an income entry (AC-4.2.2)
     */
    isEditing: computed(() => store.editingIncomeId() !== null),
  })),
  withMethods((store, incomeService = inject(IncomeService), snackBar = inject(MatSnackBar)) => ({
    /**
     * Load income for a property (AC-4.1.2, AC-4.1.6)
     * @param params Object with propertyId, propertyName, and optional year
     */
    loadIncomeByProperty: rxMethod<{
      propertyId: string;
      propertyName: string;
      year?: number;
    }>(
      pipe(
        tap(({ propertyId, propertyName }) =>
          patchState(store, {
            isLoading: true,
            error: null,
            currentPropertyId: propertyId,
            currentPropertyName: propertyName,
          })
        ),
        switchMap(({ propertyId, year }) =>
          incomeService.getIncomeByProperty(propertyId, year).pipe(
            tap((response) =>
              patchState(store, {
                incomeEntries: response.items,
                ytdTotal: response.ytdTotal,
                isLoading: false,
              })
            ),
            catchError((error) => {
              const errorMessage =
                error.status === 404
                  ? 'Property not found'
                  : 'Failed to load income. Please try again.';
              patchState(store, {
                isLoading: false,
                error: errorMessage,
              });
              console.error('Error loading income:', error);
              return of(null);
            })
          )
        )
      )
    ),

    /**
     * Create a new income entry (AC-4.1.3, AC-4.1.4)
     * On success:
     * - Prepends income to list
     * - Updates YTD total
     * - Shows snackbar confirmation
     * @param request Income details
     */
    createIncome: rxMethod<CreateIncomeRequest>(
      pipe(
        tap(() =>
          patchState(store, {
            isSaving: true,
            error: null,
          })
        ),
        switchMap((request) =>
          incomeService.createIncome(request).pipe(
            tap((response) => {
              // Create a new income DTO for local state (optimistic update)
              const newIncome: IncomeDto = {
                id: response.id,
                propertyId: request.propertyId,
                propertyName: store.currentPropertyName() || '',
                amount: request.amount,
                date: request.date,
                source: request.source,
                description: request.description,
                createdAt: new Date().toISOString(),
              };

              // Prepend to income list and update YTD total (AC-4.1.4)
              const currentIncome = store.incomeEntries();
              const currentYtdTotal = store.ytdTotal();
              patchState(store, {
                incomeEntries: [newIncome, ...currentIncome],
                ytdTotal: currentYtdTotal + request.amount,
                isSaving: false,
              });

              // Show success snackbar (AC-4.1.3)
              snackBar.open('Income recorded \u2713', 'Close', {
                duration: 3000,
                horizontalPosition: 'center',
                verticalPosition: 'bottom',
              });
            }),
            catchError((error) => {
              let errorMessage = 'Failed to save income. Please try again.';
              if (error.status === 400) {
                errorMessage = 'Invalid income data. Please check your input.';
              } else if (error.status === 404) {
                errorMessage = 'Property not found.';
              }

              patchState(store, {
                isSaving: false,
                error: errorMessage,
              });

              // Show error snackbar
              snackBar.open(errorMessage, 'Close', {
                duration: 5000,
                horizontalPosition: 'center',
                verticalPosition: 'bottom',
              });

              console.error('Error creating income:', error);
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
     * Set current property context
     */
    setCurrentProperty(propertyId: string, propertyName: string): void {
      patchState(store, {
        currentPropertyId: propertyId,
        currentPropertyName: propertyName,
      });
    },

    /**
     * Set income to editing mode (AC-4.2.2)
     */
    setEditingIncome(incomeId: string): void {
      patchState(store, { editingIncomeId: incomeId });
    },

    /**
     * Cancel editing (AC-4.2.7)
     */
    cancelEditing(): void {
      patchState(store, { editingIncomeId: null });
    },

    /**
     * Update an existing income entry (AC-4.2.2, AC-4.2.3, AC-4.2.4)
     * On success:
     * - Updates income in list
     * - Updates YTD total
     * - Shows snackbar confirmation
     */
    updateIncome: rxMethod<{ incomeId: string; request: UpdateIncomeRequest }>(
      pipe(
        tap(() =>
          patchState(store, {
            isUpdating: true,
            error: null,
          })
        ),
        switchMap(({ incomeId, request }) =>
          incomeService.updateIncome(incomeId, request).pipe(
            tap(() => {
              // Get the original income to calculate YTD difference
              const originalIncome = store.incomeEntries().find((i) => i.id === incomeId);
              const amountDifference = originalIncome
                ? request.amount - originalIncome.amount
                : 0;

              // Update the income in the list (AC-4.2.3)
              const updatedIncome = store.incomeEntries().map((income) => {
                if (income.id !== incomeId) return income;
                return {
                  ...income,
                  amount: request.amount,
                  date: request.date,
                  source: request.source,
                  description: request.description,
                };
              });

              patchState(store, {
                incomeEntries: updatedIncome,
                ytdTotal: store.ytdTotal() + amountDifference,
                isUpdating: false,
                editingIncomeId: null,
              });

              // Show success snackbar (AC-4.2.3)
              snackBar.open('Income updated ✓', 'Close', {
                duration: 3000,
                horizontalPosition: 'center',
                verticalPosition: 'bottom',
              });
            }),
            catchError((error) => {
              let errorMessage = 'Failed to update income. Please try again.';
              if (error.status === 400) {
                errorMessage = 'Invalid income data. Please check your input.';
              } else if (error.status === 404) {
                errorMessage = 'Income not found.';
              }

              patchState(store, {
                isUpdating: false,
                error: errorMessage,
              });

              // Show error snackbar
              snackBar.open(errorMessage, 'Close', {
                duration: 5000,
                horizontalPosition: 'center',
                verticalPosition: 'bottom',
              });

              console.error('Error updating income:', error);
              return of(null);
            })
          )
        )
      )
    ),

    /**
     * Delete an income entry (AC-4.2.5, AC-4.2.6)
     * On success:
     * - Removes income from list
     * - Updates YTD total
     * - Shows snackbar confirmation
     */
    deleteIncome: rxMethod<string>(
      pipe(
        tap(() =>
          patchState(store, {
            isDeleting: true,
            error: null,
          })
        ),
        switchMap((incomeId) => {
          // Get the income before deleting to know the amount to subtract
          const incomeToDelete = store.incomeEntries().find((i) => i.id === incomeId);

          return incomeService.deleteIncome(incomeId).pipe(
            tap(() => {
              // Remove income from list (AC-4.2.6)
              const updatedIncome = store.incomeEntries().filter((i) => i.id !== incomeId);

              // Calculate new YTD total (AC-4.2.6)
              const deletedAmount = incomeToDelete?.amount || 0;
              const newYtdTotal = store.ytdTotal() - deletedAmount;

              patchState(store, {
                incomeEntries: updatedIncome,
                ytdTotal: newYtdTotal,
                isDeleting: false,
              });

              // Show success snackbar (AC-4.2.6)
              snackBar.open('Income deleted', 'Close', {
                duration: 3000,
                horizontalPosition: 'center',
                verticalPosition: 'bottom',
              });
            }),
            catchError((error) => {
              let errorMessage = 'Failed to delete income. Please try again.';
              if (error.status === 404) {
                errorMessage = 'Income not found.';
              }

              patchState(store, {
                isDeleting: false,
                error: errorMessage,
              });

              // Show error snackbar
              snackBar.open(errorMessage, 'Close', {
                duration: 5000,
                horizontalPosition: 'center',
                verticalPosition: 'bottom',
              });

              console.error('Error deleting income:', error);
              return of(null);
            })
          );
        })
      )
    ),
  }))
);
