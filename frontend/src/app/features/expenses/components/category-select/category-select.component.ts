import { Component, inject, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ExpenseStore } from '../../stores/expense.store';

/**
 * CategorySelectComponent (AC-3.1.4)
 *
 * Dropdown for selecting expense categories (15 IRS Schedule E categories).
 * Uses ExpenseStore for category data.
 */
@Component({
  selector: 'app-category-select',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatFormFieldModule,
    MatSelectModule,
    MatProgressSpinnerModule,
  ],
  template: `
    <mat-form-field appearance="outline" class="category-select">
      <mat-label>Category</mat-label>
      @if (store.isLoadingCategories()) {
        <mat-select disabled>
          <mat-option>Loading...</mat-option>
        </mat-select>
      } @else {
        <mat-select
          [ngModel]="value()"
          (ngModelChange)="onCategoryChange($event)"
          [disabled]="disabled()"
        >
          @for (category of store.sortedCategories(); track category.id) {
            <mat-option [value]="category.id">
              {{ category.name }}
              @if (category.scheduleELine) {
                <span class="schedule-e-line">({{ category.scheduleELine }})</span>
              }
            </mat-option>
          }
        </mat-select>
      }
      @if (error()) {
        <mat-error>{{ error() }}</mat-error>
      }
    </mat-form-field>
  `,
  styles: [`
    .category-select {
      width: 100%;
    }

    .schedule-e-line {
      color: var(--mat-sys-outline);
      font-size: 0.85em;
      margin-left: 4px;
    }
  `],
})
export class CategorySelectComponent {
  protected readonly store = inject(ExpenseStore);

  // Inputs
  value = input<string | null>(null);
  disabled = input(false);
  error = input<string | null>(null);

  // Outputs
  categoryChange = output<string>();

  protected onCategoryChange(categoryId: string): void {
    this.categoryChange.emit(categoryId);
  }
}
