import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { ExpenseListStore, FilterChip, DateRangePreset } from './stores/expense-list.store';
import { ExpenseFiltersComponent } from './components/expense-filters/expense-filters.component';
import { ExpenseListRowComponent } from './components/expense-list-row/expense-list-row.component';

/**
 * ExpensesComponent (AC-3.4.1, AC-3.4.7, AC-3.4.8)
 *
 * Main expense list page displaying all expenses across all properties with:
 * - Filters (date range, categories, search)
 * - Paginated expense list
 * - Empty states (filtered vs truly empty)
 */
@Component({
  selector: 'app-expenses',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatPaginatorModule,
    ExpenseFiltersComponent,
    ExpenseListRowComponent,
  ],
  template: `
    <div class="expenses-container">
      <!-- Page Header -->
      <div class="page-header">
        <h1>Expenses</h1>
        <p class="subtitle">View and filter all expenses across your properties</p>
      </div>

      <!-- Filters (AC-3.4.3, AC-3.4.4, AC-3.4.5, AC-3.4.6) -->
      <app-expense-filters
        [categories]="store.categories()"
        [dateRangePreset]="store.dateRangePreset()"
        [selectedCategoryIds]="store.selectedCategoryIds()"
        [searchText]="store.searchText()"
        [filterChips]="store.filterChips()"
        (dateRangePresetChange)="onDateRangePresetChange($event)"
        (customDateRangeChange)="onCustomDateRangeChange($event)"
        (categoryChange)="onCategoryChange($event)"
        (searchChange)="onSearchChange($event)"
        (chipRemove)="onChipRemove($event)"
        (clearAll)="onClearAllFilters()"
      />

      <!-- Loading State -->
      @if (store.isLoading()) {
        <div class="loading-container">
          <mat-spinner diameter="48"></mat-spinner>
          <p>Loading expenses...</p>
        </div>
      }

      <!-- Error State -->
      @if (store.error()) {
        <mat-card class="error-card">
          <mat-icon color="warn">error_outline</mat-icon>
          <p>{{ store.error() }}</p>
          <button mat-stroked-button color="primary" (click)="store.initialize()">
            Try Again
          </button>
        </mat-card>
      }

      <!-- Expense List -->
      @if (!store.isLoading() && !store.error()) {
        <!-- Truly Empty State (AC-3.4.7) -->
        @if (store.isTrulyEmpty()) {
          <mat-card class="empty-state-card">
            <mat-icon class="empty-icon">receipt_long</mat-icon>
            <h2>No expenses recorded yet</h2>
            <p>Start by adding expenses to your properties.</p>
            <p class="hint">Go to a property's expense workspace to add expenses.</p>
          </mat-card>
        }

        <!-- Filtered Empty State (AC-3.4.7) -->
        @if (store.isFilteredEmpty()) {
          <mat-card class="empty-state-card">
            <mat-icon class="empty-icon">search_off</mat-icon>
            <h2>No expenses match your filters</h2>
            <p>Try adjusting your search criteria or clearing filters.</p>
            <button mat-stroked-button color="primary" (click)="onClearAllFilters()">
              Clear filters
            </button>
          </mat-card>
        }

        <!-- Expense List with Pagination -->
        @if (store.hasExpenses()) {
          <mat-card class="expense-list-card">
            <!-- List Header -->
            <div class="list-header">
              <div class="header-date">Date</div>
              <div class="header-property">Property</div>
              <div class="header-description">Description</div>
              <div class="header-category">Category</div>
              <div class="header-receipt"></div>
              <div class="header-amount">Amount</div>
            </div>

            <!-- Expense Rows -->
            @for (expense of store.expenses(); track expense.id) {
              <app-expense-list-row [expense]="expense" />
            }

            <!-- Pagination (AC-3.4.8) -->
            <div class="pagination-container">
              <span class="pagination-info">{{ store.totalDisplay() }}</span>
              <mat-paginator
                [length]="store.totalCount()"
                [pageSize]="store.pageSize()"
                [pageIndex]="store.page() - 1"
                [pageSizeOptions]="[25, 50, 100]"
                (page)="onPageChange($event)"
                showFirstLastButtons
              >
              </mat-paginator>
            </div>
          </mat-card>
        }
      }
    </div>
  `,
  styles: [`
    .expenses-container {
      max-width: 1200px;
      margin: 0 auto;
      padding: 24px;
    }

    .page-header {
      margin-bottom: 24px;

      h1 {
        margin: 0 0 8px 0;
        color: var(--mat-sys-on-surface);
      }

      .subtitle {
        margin: 0;
        color: var(--mat-sys-on-surface-variant);
      }
    }

    .loading-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 64px 0;
      gap: 16px;

      p {
        color: var(--mat-sys-on-surface-variant);
      }
    }

    .error-card {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 32px;
      text-align: center;
      gap: 16px;

      mat-icon {
        font-size: 48px;
        width: 48px;
        height: 48px;
      }

      p {
        margin: 0;
        color: var(--mat-sys-on-surface-variant);
      }
    }

    .empty-state-card {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 48px;
      text-align: center;

      .empty-icon {
        font-size: 64px;
        width: 64px;
        height: 64px;
        color: var(--mat-sys-primary);
        margin-bottom: 16px;
      }

      h2 {
        margin: 0 0 8px 0;
        color: var(--mat-sys-on-surface);
      }

      p {
        margin: 0;
        color: var(--mat-sys-on-surface-variant);
      }

      .hint {
        font-size: 0.9em;
        margin-top: 8px;
        opacity: 0.8;
      }

      button {
        margin-top: 24px;
      }
    }

    .expense-list-card {
      overflow: hidden;
    }

    .list-header {
      display: grid;
      grid-template-columns: 100px 150px 1fr auto 40px 100px;
      gap: 16px;
      padding: 12px 16px;
      background: var(--mat-sys-surface-container);
      border-bottom: 2px solid var(--mat-sys-outline-variant);
      font-weight: 500;
      font-size: 0.85em;
      color: var(--mat-sys-on-surface-variant);
    }

    .header-amount {
      text-align: right;
    }

    .pagination-container {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 8px 16px;
      border-top: 1px solid var(--mat-sys-outline-variant);
      background: var(--mat-sys-surface-container);
    }

    .pagination-info {
      font-size: 0.9em;
      color: var(--mat-sys-on-surface-variant);
    }

    @media (max-width: 768px) {
      .expenses-container {
        padding: 16px;
      }

      .list-header {
        display: none;
      }

      .pagination-container {
        flex-direction: column;
        gap: 8px;
      }
    }
  `],
})
export class ExpensesComponent implements OnInit {
  readonly store = inject(ExpenseListStore);

  ngOnInit(): void {
    // Initialize store - load categories and expenses
    this.store.initialize();
  }

  onDateRangePresetChange(preset: DateRangePreset): void {
    this.store.setDateRangePreset(preset);
  }

  onCustomDateRangeChange(range: { dateFrom: string; dateTo: string }): void {
    this.store.setCustomDateRange(range.dateFrom, range.dateTo);
  }

  onCategoryChange(categoryIds: string[]): void {
    this.store.setCategories(categoryIds);
  }

  onSearchChange(search: string): void {
    this.store.setSearch(search);
  }

  onChipRemove(chip: FilterChip): void {
    this.store.removeFilterChip(chip);
  }

  onClearAllFilters(): void {
    this.store.clearFilters();
  }

  onPageChange(event: PageEvent): void {
    if (event.pageSize !== this.store.pageSize()) {
      this.store.setPageSize(event.pageSize);
    } else {
      // PageEvent uses 0-based index, our API uses 1-based
      this.store.goToPage(event.pageIndex + 1);
    }
  }
}
