import { Component, inject, input, output, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { CategorySelectComponent } from '../category-select/category-select.component';
import { ExpenseDto, UpdateExpenseRequest } from '../../services/expense.service';
import { ExpenseStore } from '../../stores/expense.store';
import { CurrencyInputDirective } from '../../../../shared/directives/currency-input.directive';

/**
 * ExpenseEditFormComponent (AC-3.2.1, AC-3.2.2, AC-3.2.3, AC-3.2.5)
 *
 * Inline edit form for expenses with:
 * - Pre-populated fields from existing expense
 * - Same validation as create form
 * - Save and Cancel buttons
 * - Replaces expense row when editing
 */
@Component({
  selector: 'app-expense-edit-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
    CategorySelectComponent,
    CurrencyInputDirective,
  ],
  template: `
    <div class="expense-edit-form">
      <form [formGroup]="form" (ngSubmit)="onSubmit()" class="edit-form">
        <div class="form-row">
          <!-- Amount Field (AC-3.2.1) -->
          <mat-form-field appearance="outline" class="amount-field">
            <mat-label>Amount</mat-label>
            <span matPrefix>$ </span>
            <input
              matInput
              appCurrencyInput
              formControlName="amount"
              placeholder="0.00"
            />
            @if (form.get('amount')?.hasError('required') && form.get('amount')?.touched) {
              <mat-error>Amount is required</mat-error>
            }
            @if (form.get('amount')?.hasError('min') && form.get('amount')?.touched) {
              <mat-error>Amount must be greater than $0</mat-error>
            }
            @if (form.get('amount')?.hasError('max') && form.get('amount')?.touched) {
              <mat-error>Amount exceeds maximum</mat-error>
            }
          </mat-form-field>

          <!-- Date Field (AC-3.2.1) -->
          <mat-form-field appearance="outline" class="date-field">
            <mat-label>Date</mat-label>
            <input
              matInput
              [matDatepicker]="picker"
              formControlName="date"
              [max]="today"
            />
            <mat-datepicker-toggle matIconSuffix [for]="picker" />
            <mat-datepicker #picker />
            @if (form.get('date')?.hasError('required') && form.get('date')?.touched) {
              <mat-error>Date is required</mat-error>
            }
          </mat-form-field>
        </div>

        <!-- Category Field (AC-3.2.1) -->
        <app-category-select
          [value]="form.get('categoryId')?.value"
          (categoryChange)="onCategoryChange($event)"
          [error]="getCategoryError()"
        />

        <!-- Description Field (AC-3.2.1) -->
        <mat-form-field appearance="outline" class="description-field">
          <mat-label>Description (optional)</mat-label>
          <textarea
            matInput
            formControlName="description"
            placeholder="e.g., Home Depot - Faucet repair"
            rows="2"
            maxlength="500"
          ></textarea>
          <mat-hint align="end">{{ form.get('description')?.value?.length || 0 }} / 500</mat-hint>
          @if (form.get('description')?.hasError('maxlength') && form.get('description')?.touched) {
            <mat-error>Description must be 500 characters or less</mat-error>
          }
        </mat-form-field>

        <!-- Action Buttons (AC-3.2.3, AC-3.2.5) -->
        <div class="form-actions">
          <button
            mat-button
            type="button"
            (click)="onCancel()"
            [disabled]="store.isUpdating()"
          >
            Cancel
          </button>
          <button
            mat-raised-button
            color="primary"
            type="submit"
            [disabled]="!form.valid || store.isUpdating()"
          >
            @if (store.isUpdating()) {
              <mat-spinner diameter="20" />
            } @else {
              <ng-container>
                <mat-icon>save</mat-icon>
                Save Changes
              </ng-container>
            }
          </button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .expense-edit-form {
      padding: 16px;
      background-color: var(--mat-sys-surface-container-low);
      border-radius: 8px;
      margin: 8px 0;
    }

    .edit-form {
      display: flex;
      flex-direction: column;
      gap: 12px;
    }

    .form-row {
      display: flex;
      gap: 16px;
    }

    .amount-field {
      flex: 1;

      ::ng-deep .mat-mdc-form-field-infix {
        padding-left: 0;
      }

      ::ng-deep [matPrefix] {
        padding-left: 12px;
      }
    }

    .date-field {
      flex: 1;
    }

    .description-field {
      width: 100%;
    }

    .form-actions {
      display: flex;
      justify-content: flex-end;
      gap: 8px;
      padding-top: 8px;
    }

    .form-actions button {
      min-width: 120px;

      mat-icon {
        vertical-align: middle;
        margin-right: 4px;
      }
    }

    mat-spinner {
      display: inline-block;
    }

    @media (max-width: 600px) {
      .form-row {
        flex-direction: column;
        gap: 0;
      }

      .amount-field,
      .date-field {
        width: 100%;
      }

      .form-actions {
        flex-direction: column-reverse;
      }

      .form-actions button {
        width: 100%;
      }
    }
  `],
})
export class ExpenseEditFormComponent implements OnInit, OnChanges {
  protected readonly store = inject(ExpenseStore);
  private readonly fb = inject(FormBuilder);

  // Input: Expense to edit (AC-3.2.2)
  expense = input.required<ExpenseDto>();

  // Output: Edit cancelled
  cancelled = output<void>();

  // Output: Edit saved
  saved = output<void>();

  protected readonly today = new Date();

  protected form: FormGroup = this.fb.group({
    amount: [null, [Validators.required, Validators.min(0.01), Validators.max(9999999.99)]],
    date: [null, [Validators.required]],
    categoryId: ['', [Validators.required]],
    description: ['', [Validators.maxLength(500)]],
  });

  ngOnInit(): void {
    this.populateForm();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['expense'] && !changes['expense'].firstChange) {
      this.populateForm();
    }
  }

  /**
   * Populate form with expense data (AC-3.2.2)
   */
  private populateForm(): void {
    const exp = this.expense();
    if (!exp) return;

    // Parse the date string to Date object
    const dateValue = this.parseDate(exp.date);

    this.form.patchValue({
      amount: exp.amount,
      date: dateValue,
      categoryId: exp.categoryId,
      description: exp.description || '',
    });
  }

  private parseDate(dateString: string): Date {
    // Handle ISO date string (YYYY-MM-DD)
    const parts = dateString.split('T')[0].split('-');
    return new Date(parseInt(parts[0]), parseInt(parts[1]) - 1, parseInt(parts[2]));
  }

  protected onCategoryChange(categoryId: string): void {
    this.form.patchValue({ categoryId });
    this.form.get('categoryId')?.markAsTouched();
  }

  protected getCategoryError(): string | null {
    const control = this.form.get('categoryId');
    if (control?.hasError('required') && control?.touched) {
      return 'Category is required';
    }
    return null;
  }

  protected onCancel(): void {
    this.store.cancelEditing();
    this.cancelled.emit();
  }

  protected onSubmit(): void {
    if (!this.form.valid) {
      this.form.markAllAsTouched();
      return;
    }

    const { amount, date, categoryId, description } = this.form.value;

    // Format date as ISO string (YYYY-MM-DD)
    const formattedDate = this.formatDate(date);

    const request: UpdateExpenseRequest = {
      amount,
      date: formattedDate,
      categoryId,
      description: description?.trim() || undefined,
    };

    // Update expense via store (AC-3.2.3)
    this.store.updateExpense({ expenseId: this.expense().id, request });
    this.saved.emit();
  }

  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
