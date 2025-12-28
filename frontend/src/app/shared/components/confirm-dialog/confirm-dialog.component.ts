import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  MatDialogModule,
  MatDialogRef,
  MAT_DIALOG_DATA,
} from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

/**
 * Data interface for ConfirmDialogComponent
 *
 * Supports both simple confirmations and more complex dialogs with icons and secondary messages.
 */
export interface ConfirmDialogData {
  title: string;
  message: string;
  confirmText: string;
  cancelText: string;
  /** Optional icon name (Material Icons) to display next to the title */
  icon?: string;
  /** Optional icon color: 'warn' (red), 'primary' (green), 'accent' (orange) */
  iconColor?: 'warn' | 'primary' | 'accent';
  /** Optional secondary message displayed in a highlighted box */
  secondaryMessage?: string;
  /** Optional icon for the confirm button */
  confirmIcon?: string;
}

/**
 * Reusable Confirmation Dialog Component
 *
 * A generic confirmation dialog for actions requiring user confirmation.
 * Used for unsaved changes warnings, delete confirmations, etc.
 *
 * Features:
 * - Optional warning/info icon next to title
 * - Optional secondary message with highlight styling
 * - Optional icon on confirm button
 * - Consistent padding and no scrollbar issues
 *
 * Returns true when confirm button is clicked, false when cancel is clicked.
 */
@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatIconModule],
  template: `
    <div class="confirm-dialog">
      <div class="dialog-header">
        @if (data.icon) {
          <mat-icon
            class="header-icon"
            [class.icon-warn]="data.iconColor === 'warn'"
            [class.icon-primary]="data.iconColor === 'primary'"
            [class.icon-accent]="data.iconColor === 'accent'"
          >{{ data.icon }}</mat-icon>
        }
        <h2 mat-dialog-title>{{ data.title }}</h2>
      </div>
      <mat-dialog-content>
        <p class="primary-message">{{ data.message }}</p>
        @if (data.secondaryMessage) {
          <p class="secondary-message">{{ data.secondaryMessage }}</p>
        }
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button (click)="onCancel()">{{ data.cancelText }}</button>
        <button mat-raised-button color="warn" (click)="onConfirm()">
          @if (data.confirmIcon) {
            <mat-icon>{{ data.confirmIcon }}</mat-icon>
          }
          {{ data.confirmText }}
        </button>
      </mat-dialog-actions>
    </div>
  `,
  styles: [
    `
      .confirm-dialog {
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

          &.icon-warn {
            color: #f44336;
          }

          &.icon-primary {
            color: var(--pm-primary, #66BB6A);
          }

          &.icon-accent {
            color: var(--pm-accent, #FFA726);
          }
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
          margin: 0 0 12px 0;
          color: var(--pm-text-primary, #333);
          font-size: 14px;
          line-height: 1.5;
        }

        .secondary-message {
          margin: 0;
          color: var(--pm-text-secondary, #666);
          font-size: 13px;
          line-height: 1.5;
          padding: 12px;
          background-color: #f5f5f5;
          border-radius: 4px;
          border-left: 3px solid #1976d2;
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
export class ConfirmDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<ConfirmDialogComponent>);
  protected readonly data: ConfirmDialogData = inject(MAT_DIALOG_DATA);

  onConfirm(): void {
    this.dialogRef.close(true);
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }
}
