import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

/**
 * Settings placeholder component (AC7.7)
 * Will be implemented in a future epic
 */
@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule],
  template: `
    <div class="placeholder-container">
      <mat-card class="placeholder-card">
        <mat-icon class="placeholder-icon">settings</mat-icon>
        <h2>Settings</h2>
        <p>User settings coming soon.</p>
        <p class="hint">This feature will be implemented in a future release.</p>
      </mat-card>
    </div>
  `,
  styles: [`
    .placeholder-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: calc(100vh - 100px);
    }
    .placeholder-card {
      text-align: center;
      padding: 48px;
      max-width: 400px;
    }
    .placeholder-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      color: var(--pm-primary);
      margin-bottom: 16px;
    }
    h2 {
      color: var(--pm-text-primary);
      margin: 0 0 8px 0;
    }
    p {
      color: var(--pm-text-secondary);
      margin: 0;
    }
    .hint {
      font-size: 12px;
      opacity: 0.7;
      margin-top: 16px;
    }
  `]
})
export class SettingsComponent {}
