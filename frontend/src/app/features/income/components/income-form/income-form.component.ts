import { Component, inject, input, output, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  FormGroupDirective,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { IncomeStore } from '../../stores/income.store';
import { CurrencyInputDirective } from '../../../../shared/directives/currency-input.directive';

/**
 * IncomeFormComponent (AC-4.1.2, AC-4.1.3, AC-4.1.5)
 *
 * Form for creating income entries with:
 * - Amount (currency, required, > $0)
 * - Date (datepicker, defaults to today)
 * - Source (optional)
 * - Description (optional)
 * - Form clears after successful save
 */
@Component({
  selector: 'app-income-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
    CurrencyInputDirective,
  ],
  template: `
    <mat-card class="income-form-card">
      <mat-card-header>
        <mat-card-title>New Income</mat-card-title>
      </mat-card-header>
      <mat-card-content>
        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="income-form">
          <div class="form-row">
            <!-- Amount Field (AC-4.1.5) -->
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

            <!-- Date Field (AC-4.1.2) -->
            <mat-form-field appearance="outline" class="date-field">
              <mat-label>Date</mat-label>
              <input
                matInput
                [matDatepicker]="picker"
                formControlName="date"
              />
              <mat-datepicker-toggle matIconSuffix [for]="picker" />
              <mat-datepicker #picker />
              @if (form.get('date')?.hasError('required') && form.get('date')?.touched) {
                <mat-error>Date is required</mat-error>
              }
            </mat-form-field>
          </div>

          <!-- Source Field (AC-4.1.2) -->
          <mat-form-field appearance="outline" class="source-field">
            <mat-label>Source (optional)</mat-label>
            <input
              matInput
              formControlName="source"
              placeholder="e.g., John Smith - Rent"
              maxlength="255"
            />
            <mat-hint align="end">{{ form.get('source')?.value?.length || 0 }} / 255</mat-hint>
            @if (form.get('source')?.hasError('maxlength') && form.get('source')?.touched) {
              <mat-error>Source must be 255 characters or less</mat-error>
            }
          </mat-form-field>

          <!-- Description Field (AC-4.1.2) -->
          <mat-form-field appearance="outline" class="description-field">
            <mat-label>Description (optional)</mat-label>
            <textarea
              matInput
              formControlName="description"
              placeholder="e.g., January rent payment"
              rows="2"
              maxlength="500"
            ></textarea>
            <mat-hint align="end">{{ form.get('description')?.value?.length || 0 }} / 500</mat-hint>
            @if (form.get('description')?.hasError('maxlength') && form.get('description')?.touched) {
              <mat-error>Description must be 500 characters or less</mat-error>
            }
          </mat-form-field>

          <!-- Submit Button -->
          <div class="form-actions">
            <button
              mat-raised-button
              color="primary"
              type="submit"
              [disabled]="!form.valid || store.isSaving()"
            >
              @if (store.isSaving()) {
                <mat-spinner diameter="20" />
              } @else {
                <ng-container>
                  <mat-icon>add</mat-icon>
                  Save
                </ng-container>
              }
            </button>
          </div>
        </form>
      </mat-card-content>
    </mat-card>
  `,
  styles: [`
    .income-form-card {
      margin-bottom: 24px;
    }

    .income-form {
      display: flex;
      flex-direction: column;
      gap: 16px;
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

    .source-field,
    .description-field {
      width: 100%;
    }

    .form-actions {
      display: flex;
      justify-content: flex-end;
      padding-top: 8px;
    }

    .form-actions button {
      min-width: 100px;

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
    }
  `],
})
export class IncomeFormComponent {
  protected readonly store = inject(IncomeStore);
  private readonly fb = inject(FormBuilder);

  // Input: Property ID
  propertyId = input.required<string>();

  // Output: Income created event
  incomeCreated = output<void>();

  protected readonly today = new Date();

  protected form: FormGroup = this.fb.group({
    amount: [null, [Validators.required, Validators.min(0.01), Validators.max(9999999.99)]],
    date: [this.today, [Validators.required]],
    source: ['', [Validators.maxLength(255)]],
    description: ['', [Validators.maxLength(500)]],
  });

  // ViewChild to access FormGroupDirective for resetting submitted state
  @ViewChild(FormGroupDirective) private formDirective!: FormGroupDirective;

  /**
   * Handle form submission (AC-4.1.3, AC-4.1.5)
   */
  protected onSubmit(): void {
    if (!this.form.valid) {
      this.form.markAllAsTouched();
      return;
    }

    const { amount, date, source, description } = this.form.value;
    const formattedDate = this.formatDate(date);

    this.store.createIncome({
      propertyId: this.propertyId(),
      amount,
      date: formattedDate,
      source: source?.trim() || undefined,
      description: description?.trim() || undefined,
    });

    this.resetForm();
    this.incomeCreated.emit();
  }

  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private resetForm(): void {
    // Use formDirective.resetForm() to reset both form values AND the submitted state
    // This is critical because ErrorStateMatcher shows errors when form.submitted is true
    this.formDirective.resetForm({
      amount: null,
      date: this.today,
      source: '',
      description: '',
    });
  }
}
