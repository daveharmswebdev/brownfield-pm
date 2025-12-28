import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

/**
 * Reusable empty state component
 * Displays an icon, title, message, and optional action button
 */
@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [CommonModule, RouterLink, MatCardModule, MatIconModule, MatButtonModule],
  template: `
    <mat-card class="empty-state-card">
      <mat-icon class="placeholder-icon">{{ icon() }}</mat-icon>
      <h2>{{ title() }}</h2>
      <p>{{ message() }}</p>
      @if (actionLabel() && actionRoute()) {
        <button mat-raised-button color="primary" [routerLink]="actionRoute()">
          @if (actionIcon()) {
            <mat-icon>{{ actionIcon() }}</mat-icon>
          }
          {{ actionLabel() }}
        </button>
      }
    </mat-card>
  `,
  styles: [`
    .empty-state-card {
      text-align: center;
      padding: 48px;
      max-width: 400px;
      width: 100%;

      .placeholder-icon {
        font-size: 64px;
        width: 64px;
        height: 64px;
        color: var(--pm-primary);
        margin-bottom: 16px;
      }

      h2 {
        color: var(--pm-text-primary);
        font-size: 24px;
        font-weight: 500;
        margin: 0 0 12px 0;
      }

      p {
        color: var(--pm-text-secondary);
        font-size: 16px;
        margin: 0 0 24px 0;
        line-height: 1.5;
      }

      button mat-icon {
        margin-right: 8px;
      }
    }

    @media (max-width: 767px) {
      .empty-state-card {
        padding: 32px 24px;
      }
    }
  `]
})
export class EmptyStateComponent {
  readonly icon = input<string>('home_work');
  readonly title = input<string>('No items yet');
  readonly message = input<string>('Add your first item to get started.');
  readonly actionLabel = input<string | null>(null);
  readonly actionRoute = input<string | null>(null);
  readonly actionIcon = input<string | null>(null);
}
