import { Component, inject } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import {
  MatDialogModule,
  MatDialogRef,
  MAT_DIALOG_DATA,
} from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

/**
 * Data interface for DuplicateWarningDialogComponent (AC-3.6.2)
 */
export interface DuplicateWarningDialogData {
  existingExpense: {
    id: string;
    date: string;
    amount: number;
    description?: string;
  };
}

/**
 * DuplicateWarningDialogComponent (AC-3.6.2, AC-3.6.3, AC-3.6.4)
 *
 * Warning dialog displayed when a potential duplicate expense is detected.
 *
 * Features:
 * - Shows existing expense details (date, amount, description)
 * - Cancel button returns false (keep form data, don't save)
 * - Save Anyway button returns true (proceed with save)
 * - Styled with Forest Green accent (not warn color since this isn't destructive)
 */
@Component({
  selector: 'app-duplicate-warning-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    CurrencyPipe,
  ],
  template: `
    <div class="duplicate-warning-dialog">
      <div class="dialog-header">
        <mat-icon class="header-icon">warning_amber</mat-icon>
        <h2 mat-dialog-title>Possible Duplicate</h2>
      </div>
      <mat-dialog-content>
        <p class="primary-message">
          You entered a similar expense on {{ formatDate(data.existingExpense.date) }}
          for {{ data.existingExpense.amount | currency }}.
        </p>
        <p class="secondary-message">Save anyway?</p>
        @if (data.existingExpense.description) {
          <div class="existing-expense-details">
            <span class="label">Existing expense:</span>
            <span class="description">{{ data.existingExpense.description }}</span>
          </div>
        }
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button (click)="onCancel()">Cancel</button>
        <button mat-raised-button color="primary" (click)="onSaveAnyway()">
          <mat-icon>check</mat-icon>
          Save Anyway
        </button>
      </mat-dialog-actions>
    </div>
  `,
  styles: [
    `
      .duplicate-warning-dialog {
        min-width: 350px;
        max-width: 450px;
        padding: 24px;
        overflow: hidden;
      }

      .dialog-header {
        display: flex;
        align-items: flex-end;
        gap: 12px;
        margin-bottom: 16px;

        .header-icon {
          font-size: 28px;
          width: 28px;
          height: 28px;
          margin-bottom: 2px;
          color: var(--pm-accent, #ffa726);
        }

        h2 {
          margin: 0;
          padding: 0;
          color: var(--pm-text-primary, #333);
          font-size: 20px;
          font-weight: 500;
        }
      }

      mat-dialog-content {
        padding: 0;
        margin: 0;
        overflow: visible;

        .primary-message {
          margin: 0 0 8px 0;
          color: var(--pm-text-primary, #333);
          font-size: 14px;
          line-height: 1.5;
        }

        .secondary-message {
          margin: 0 0 12px 0;
          color: var(--pm-text-primary, #333);
          font-size: 14px;
          font-weight: 500;
        }

        .existing-expense-details {
          padding: 12px;
          background-color: #f5f5f5;
          border-radius: 4px;
          border-left: 3px solid var(--pm-accent, #ffa726);

          .label {
            display: block;
            font-size: 12px;
            color: var(--pm-text-secondary, #666);
            margin-bottom: 4px;
          }

          .description {
            font-size: 14px;
            color: var(--pm-text-primary, #333);
          }
        }
      }

      mat-dialog-actions {
        padding: 24px 0 0 0;
        margin: 0;
        gap: 8px;

        button {
          min-width: 80px;

          mat-icon {
            margin-right: 4px;
            font-size: 18px;
            width: 18px;
            height: 18px;
          }
        }
      }
    `,
  ],
})
export class DuplicateWarningDialogComponent {
  private readonly dialogRef = inject(
    MatDialogRef<DuplicateWarningDialogComponent>
  );
  protected readonly data: DuplicateWarningDialogData = inject(MAT_DIALOG_DATA);

  /**
   * Format date as "Nov 28, 2025"
   */
  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
  }

  /**
   * Cancel - return false to indicate user wants to go back to form (AC-3.6.3)
   */
  onCancel(): void {
    this.dialogRef.close(false);
  }

  /**
   * Save Anyway - return true to proceed with saving (AC-3.6.4)
   */
  onSaveAnyway(): void {
    this.dialogRef.close(true);
  }
}
