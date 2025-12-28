import { Injectable, signal, computed } from '@angular/core';

/**
 * Global year selector state management service (AC-3.5.3, AC-3.5.5).
 * Provides reactive year selection that persists during navigation session.
 * Year selection resets to current year on new session/login.
 */
@Injectable({ providedIn: 'root' })
export class YearSelectorService {
  // Private state
  private readonly _selectedYear = signal<number>(new Date().getFullYear());

  // Public readonly signals
  readonly selectedYear = this._selectedYear.asReadonly();

  /**
   * Computed signal for available years (AC-3.5.1).
   * Returns current year + 5 previous years in descending order.
   */
  readonly availableYears = computed(() => {
    const currentYear = new Date().getFullYear();
    return Array.from({ length: 6 }, (_, i) => currentYear - i);
  });

  /**
   * Set the selected year (AC-3.5.5).
   * @param year The year to select
   */
  setYear(year: number): void {
    this._selectedYear.set(year);
  }

  /**
   * Reset to current year (called on new session/login).
   */
  reset(): void {
    this._selectedYear.set(new Date().getFullYear());
  }
}
