import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

/**
 * Reusable error card component
 * Displays an error message with optional retry action
 */
@Component({
  selector: 'app-error-card',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, MatButtonModule],
  template: `
    <mat-card class="error-card">
      <mat-icon>error_outline</mat-icon>
      <p>{{ message() }}</p>
      @if (showRetry()) {
        <button mat-button color="primary" (click)="retry.emit()">
          {{ retryLabel() }}
        </button>
      }
    </mat-card>
  `,
  styles: [`
    .error-card {
      text-align: center;
      padding: 24px;
      max-width: 400px;
      margin: 0 auto;

      mat-icon {
        font-size: 48px;
        width: 48px;
        height: 48px;
        color: var(--pm-error, #c62828);
      }

      p {
        margin: 16px 0;
        color: var(--pm-text-secondary);
      }
    }
  `]
})
export class ErrorCardComponent {
  readonly message = input<string>('An error occurred. Please try again.');
  readonly showRetry = input<boolean>(true);
  readonly retryLabel = input<string>('Try Again');
  readonly retry = output<void>();
}
