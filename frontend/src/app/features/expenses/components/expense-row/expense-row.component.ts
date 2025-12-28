import { Component, input, output } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ExpenseDto } from '../../services/expense.service';

/**
 * ExpenseRowComponent (AC-3.1.7, AC-3.2.1, AC-3.3.1)
 *
 * Displays a single expense in the expense list with:
 * - Date (formatted as "Nov 28, 2025")
 * - Description
 * - Category (as chip/tag)
 * - Amount (formatted as currency)
 * - Edit button (appears on hover) (AC-3.2.1)
 * - Delete button (appears on hover) (AC-3.3.1)
 */
@Component({
  selector: 'app-expense-row',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    CurrencyPipe,
  ],
  template: `
    <div class="expense-row">
      <div class="expense-date">
        {{ formatDate(expense().date) }}
      </div>
      <div class="expense-details">
        <div class="expense-description">
          {{ expense().description || 'No description' }}
        </div>
        <mat-chip-set class="expense-category">
          <mat-chip>{{ expense().categoryName }}</mat-chip>
        </mat-chip-set>
      </div>
      <div class="expense-amount">
        {{ expense().amount | currency }}
      </div>
      <!-- Edit and Delete Actions (AC-3.2.1, AC-3.3.1) -->
      <div class="expense-actions">
        <button
          mat-icon-button
          (click)="onEditClick()"
          matTooltip="Edit expense"
          class="edit-button"
        >
          <mat-icon>edit</mat-icon>
        </button>
        <button
          mat-icon-button
          (click)="onDeleteClick()"
          matTooltip="Delete expense"
          class="delete-button"
        >
          <mat-icon>delete</mat-icon>
        </button>
      </div>
    </div>
  `,
  styles: [`
    .expense-row {
      display: flex;
      align-items: center;
      padding: 12px 16px;
      border-bottom: 1px solid var(--mat-sys-outline-variant);
      gap: 16px;
      transition: background-color 0.2s ease;
    }

    .expense-row:hover {
      background-color: var(--mat-sys-surface-container-low);
    }

    .expense-row:last-child {
      border-bottom: none;
    }

    .expense-date {
      min-width: 100px;
      font-size: 0.9em;
      color: var(--mat-sys-on-surface-variant);
    }

    .expense-details {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .expense-description {
      font-weight: 500;
    }

    .expense-category {
      mat-chip {
        font-size: 0.8em;
        min-height: 24px;
        padding: 0 8px;
      }
    }

    .expense-amount {
      font-weight: 600;
      font-size: 1.1em;
      min-width: 100px;
      text-align: right;
    }

    .expense-actions {
      opacity: 0;
      transition: opacity 0.2s ease;
    }

    .expense-row:hover .expense-actions {
      opacity: 1;
    }

    .edit-button {
      color: var(--mat-sys-primary);
    }

    .delete-button {
      color: var(--mat-sys-on-surface-variant);
    }

    .delete-button:hover {
      color: var(--mat-sys-error);
    }

    @media (max-width: 600px) {
      .expense-row {
        flex-wrap: wrap;
      }

      .expense-date {
        min-width: auto;
        order: 1;
      }

      .expense-actions {
        order: 2;
        opacity: 1; /* Always visible on mobile */
      }

      .expense-amount {
        order: 3;
        min-width: auto;
      }

      .expense-details {
        order: 4;
        width: 100%;
        margin-top: 8px;
      }
    }
  `],
})
export class ExpenseRowComponent {
  expense = input.required<ExpenseDto>();

  // Output: Edit clicked (AC-3.2.1)
  edit = output<string>();

  // Output: Delete icon clicked (AC-3.3.1)
  delete = output<string>();

  /**
   * Format date as "Nov 28, 2025"
   */
  protected formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  }

  /**
   * Handle edit button click (AC-3.2.1)
   */
  protected onEditClick(): void {
    this.edit.emit(this.expense().id);
  }

  /**
   * Handle delete button click (AC-3.3.1)
   * Emits to show modal confirmation
   */
  protected onDeleteClick(): void {
    this.delete.emit(this.expense().id);
  }
}
