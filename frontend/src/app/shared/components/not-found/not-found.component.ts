import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

/**
 * Not Found Component (AC-2.3.6)
 *
 * Generic 404 page for displaying "not found" errors.
 * Used when:
 * - Property doesn't exist
 * - Property belongs to different account (security - show same message)
 */
@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
  ],
  template: `
    <div class="not-found-container">
      <mat-card class="not-found-card">
        <mat-icon class="not-found-icon">search_off</mat-icon>
        <h1>{{ title }}</h1>
        <p>{{ message }}</p>
        <button mat-raised-button color="primary" [routerLink]="returnLink">
          <mat-icon>arrow_back</mat-icon>
          {{ returnLinkText }}
        </button>
      </mat-card>
    </div>
  `,
  styles: [`
    .not-found-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: calc(100vh - 200px);
      padding: 24px;
    }

    .not-found-card {
      text-align: center;
      padding: 48px;
      max-width: 400px;
      width: 100%;

      .not-found-icon {
        font-size: 80px;
        width: 80px;
        height: 80px;
        color: var(--pm-text-secondary);
        opacity: 0.5;
        margin-bottom: 24px;
      }

      h1 {
        color: var(--pm-text-primary);
        font-size: 28px;
        font-weight: 600;
        margin: 0 0 16px 0;
      }

      p {
        color: var(--pm-text-secondary);
        font-size: 16px;
        margin: 0 0 32px 0;
        line-height: 1.5;
      }

      button mat-icon {
        margin-right: 8px;
      }
    }

    @media (max-width: 767px) {
      .not-found-card {
        padding: 32px 24px;

        h1 {
          font-size: 24px;
        }
      }
    }
  `]
})
export class NotFoundComponent {
  @Input() title = 'Property not found';
  @Input() message = 'The property you\'re looking for doesn\'t exist or you don\'t have access to it.';
  @Input() returnLink = '/dashboard';
  @Input() returnLinkText = 'Back to Dashboard';
}
