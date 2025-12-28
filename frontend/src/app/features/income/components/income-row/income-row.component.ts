import { Component, input, output, signal, effect, OnInit } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { IncomeDto, UpdateIncomeRequest } from '../../services/income.service';

/**
 * IncomeRowComponent (AC-4.1.6, AC-4.2.1, AC-4.2.2, AC-4.2.4, AC-4.2.5, AC-4.2.7)
 *
 * Displays a single income entry in the income list with:
 * - Date (formatted as "Jan 15, 2025")
 * - Amount (formatted as currency $1,500.00)
 * - Source (if present)
 * - Description (if present)
 * - Edit button (appears on hover) (AC-4.2.1)
 * - Delete button (appears on hover) (AC-4.2.1)
 * - Inline edit form (AC-4.2.2)
 * - Inline delete confirmation (AC-4.2.5)
 */
@Component({
  selector: 'app-income-row',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatInputModule,
    MatFormFieldModule,
    MatDatepickerModule,
    CurrencyPipe,
  ],
  template: `
    @if (isEditing()) {
      <!-- Edit Mode (AC-4.2.2) -->
      <div class="income-row income-row--editing">
        <form [formGroup]="editForm" (ngSubmit)="onSaveEdit()" class="edit-form">
          <div class="edit-form-row">
            <mat-form-field class="edit-field edit-field--amount">
              <mat-label>Amount</mat-label>
              <span matTextPrefix>$&nbsp;</span>
              <input
                matInput
                type="number"
                formControlName="amount"
                step="0.01"
                min="0.01"
              >
              @if (editForm.get('amount')?.hasError('required')) {
                <mat-error>Amount is required</mat-error>
              }
              @if (editForm.get('amount')?.hasError('min')) {
                <mat-error>Amount must be greater than $0</mat-error>
              }
            </mat-form-field>

            <mat-form-field class="edit-field edit-field--date">
              <mat-label>Date</mat-label>
              <input
                matInput
                [matDatepicker]="picker"
                formControlName="date"
              >
              <mat-datepicker-toggle matIconSuffix [for]="picker"></mat-datepicker-toggle>
              <mat-datepicker #picker></mat-datepicker>
              @if (editForm.get('date')?.hasError('required')) {
                <mat-error>Date is required</mat-error>
              }
            </mat-form-field>
          </div>

          <div class="edit-form-row">
            <mat-form-field class="edit-field edit-field--source">
              <mat-label>Source</mat-label>
              <input
                matInput
                type="text"
                formControlName="source"
                placeholder="e.g., John Smith - Rent"
              >
            </mat-form-field>

            <mat-form-field class="edit-field edit-field--description">
              <mat-label>Description</mat-label>
              <input
                matInput
                type="text"
                formControlName="description"
                placeholder="Optional notes"
              >
            </mat-form-field>
          </div>

          <div class="edit-actions">
            <button
              mat-button
              type="button"
              (click)="onCancelEdit()"
              class="cancel-button"
            >
              Cancel
            </button>
            <button
              mat-raised-button
              color="primary"
              type="submit"
              [disabled]="editForm.invalid || isSaving()"
            >
              @if (isSaving()) {
                Saving...
              } @else {
                Save
              }
            </button>
          </div>
        </form>
      </div>
    } @else if (isConfirmingDelete()) {
      <!-- Delete Confirmation Mode (AC-4.2.5) -->
      <div class="income-row income-row--confirming">
        <div class="confirm-message">
          Delete this income entry?
        </div>
        <div class="confirm-actions">
          <button
            mat-button
            (click)="onCancelDelete()"
            class="cancel-button"
          >
            Cancel
          </button>
          <button
            mat-raised-button
            color="warn"
            (click)="onConfirmDelete()"
            [disabled]="isDeleting()"
          >
            @if (isDeleting()) {
              Deleting...
            } @else {
              Delete
            }
          </button>
        </div>
      </div>
    } @else {
      <!-- Normal Display Mode -->
      <div class="income-row">
        <div class="income-date">
          {{ formatDate(income().date) }}
        </div>
        <div class="income-details">
          @if (income().source) {
            <div class="income-source">
              {{ income().source }}
            </div>
          }
          @if (income().description) {
            <div class="income-description">
              {{ income().description }}
            </div>
          }
          @if (!income().source && !income().description) {
            <div class="income-no-details">
              —
            </div>
          }
        </div>
        <div class="income-amount">
          {{ income().amount | currency }}
        </div>
        <!-- Edit and Delete Actions (AC-4.2.1) -->
        <div class="income-actions">
          <button
            mat-icon-button
            (click)="onEditClick()"
            matTooltip="Edit income"
            aria-label="Edit income entry"
            class="edit-button"
          >
            <mat-icon>edit</mat-icon>
          </button>
          <button
            mat-icon-button
            (click)="onDeleteClick()"
            matTooltip="Delete income"
            aria-label="Delete income entry"
            class="delete-button"
          >
            <mat-icon>delete</mat-icon>
          </button>
        </div>
      </div>
    }
  `,
  styles: [`
    .income-row {
      display: flex;
      align-items: center;
      padding: 12px 16px;
      border-bottom: 1px solid var(--mat-sys-outline-variant);
      gap: 16px;
      transition: background-color 0.2s ease;
    }

    .income-row:hover {
      background-color: var(--mat-sys-surface-container-low);
    }

    .income-row:last-child {
      border-bottom: none;
    }

    .income-row--editing,
    .income-row--confirming {
      flex-direction: column;
      align-items: stretch;
      background-color: var(--mat-sys-surface-container);
    }

    .income-date {
      min-width: 100px;
      font-size: 0.9em;
      color: var(--mat-sys-on-surface-variant);
    }

    .income-details {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .income-source {
      font-weight: 500;
    }

    .income-description {
      font-size: 0.9em;
      color: var(--mat-sys-on-surface-variant);
    }

    .income-no-details {
      color: var(--mat-sys-on-surface-variant);
    }

    .income-amount {
      font-weight: 600;
      font-size: 1.1em;
      min-width: 100px;
      text-align: right;
      color: var(--mat-sys-primary);
    }

    .income-actions {
      opacity: 0;
      transition: opacity 0.2s ease;
    }

    .income-row:hover .income-actions {
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

    /* Edit Form Styles (AC-4.2.2) */
    .edit-form {
      width: 100%;
    }

    .edit-form-row {
      display: flex;
      gap: 16px;
      margin-bottom: 8px;
    }

    .edit-field {
      flex: 1;
    }

    .edit-field--amount {
      max-width: 150px;
    }

    .edit-field--date {
      max-width: 180px;
    }

    .edit-actions {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
      margin-top: 8px;
    }

    .cancel-button {
      color: var(--mat-sys-on-surface-variant);
    }

    /* Delete Confirmation Styles (AC-4.2.5) */
    .confirm-message {
      font-size: 1em;
      color: var(--mat-sys-on-surface);
      margin-bottom: 16px;
    }

    .confirm-actions {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
    }

    @media (max-width: 600px) {
      .income-row {
        flex-wrap: wrap;
      }

      .income-date {
        min-width: auto;
        order: 1;
      }

      .income-actions {
        order: 2;
        opacity: 1; /* Always visible on mobile */
      }

      .income-amount {
        order: 3;
        min-width: auto;
      }

      .income-details {
        order: 4;
        width: 100%;
        margin-top: 8px;
      }

      .edit-form-row {
        flex-direction: column;
        gap: 8px;
      }

      .edit-field--amount,
      .edit-field--date {
        max-width: none;
      }
    }
  `],
})
export class IncomeRowComponent implements OnInit {
  income = input.required<IncomeDto>();

  // Input: Whether this row is currently being edited (controlled by parent)
  isEditing = input<boolean>(false);

  // Input: Whether this row is currently being saved
  isSaving = input<boolean>(false);

  // Input: Whether this row is currently being deleted
  isDeleting = input<boolean>(false);

  // Output: Edit clicked (AC-4.2.1)
  edit = output<string>();

  // Output: Save edit with updated data (AC-4.2.3)
  save = output<{ incomeId: string; request: UpdateIncomeRequest }>();

  // Output: Cancel edit (AC-4.2.7)
  cancelEdit = output<void>();

  // Output: Delete confirmed (AC-4.2.6)
  delete = output<string>();

  // Internal state for delete confirmation (AC-4.2.5)
  isConfirmingDelete = signal(false);

  // Edit form
  editForm!: FormGroup;

  constructor(private fb: FormBuilder) {}

  ngOnInit(): void {
    this.initEditForm();
  }

  /**
   * Initialize the edit form with current income values
   */
  private initEditForm(): void {
    const income = this.income();
    this.editForm = this.fb.group({
      amount: [income.amount, [Validators.required, Validators.min(0.01)]],
      date: [new Date(income.date), Validators.required],
      source: [income.source || ''],
      description: [income.description || ''],
    });
  }

  /**
   * Reset form when editing starts (called via effect or from parent)
   */
  private resetFormToCurrentValues(): void {
    const income = this.income();
    this.editForm.patchValue({
      amount: income.amount,
      date: new Date(income.date),
      source: income.source || '',
      description: income.description || '',
    });
  }

  /**
   * Format date as "Jan 15, 2025" (AC-4.1.6)
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
   * Handle edit button click (AC-4.2.1)
   */
  protected onEditClick(): void {
    this.resetFormToCurrentValues();
    this.edit.emit(this.income().id);
  }

  /**
   * Handle save edit (AC-4.2.3)
   */
  protected onSaveEdit(): void {
    if (this.editForm.invalid) return;

    const formValue = this.editForm.value;
    const dateValue = formValue.date instanceof Date
      ? formValue.date.toISOString().split('T')[0]
      : formValue.date;

    const request: UpdateIncomeRequest = {
      amount: formValue.amount,
      date: dateValue,
      source: formValue.source?.trim() || undefined,
      description: formValue.description?.trim() || undefined,
    };

    this.save.emit({ incomeId: this.income().id, request });
  }

  /**
   * Handle cancel edit (AC-4.2.7)
   */
  protected onCancelEdit(): void {
    this.cancelEdit.emit();
  }

  /**
   * Handle delete button click - show confirmation (AC-4.2.5)
   */
  protected onDeleteClick(): void {
    this.isConfirmingDelete.set(true);
  }

  /**
   * Handle cancel delete - hide confirmation (AC-4.2.7)
   */
  protected onCancelDelete(): void {
    this.isConfirmingDelete.set(false);
  }

  /**
   * Handle confirm delete (AC-4.2.6)
   */
  protected onConfirmDelete(): void {
    this.delete.emit(this.income().id);
  }
}
