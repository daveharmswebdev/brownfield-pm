import { Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

/**
 * Reusable loading spinner component
 * Displays a centered spinner with configurable size
 */
@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule],
  template: `
    <div class="loading-container">
      <mat-spinner [diameter]="diameter()"></mat-spinner>
    </div>
  `,
  styles: [`
    .loading-container {
      display: flex;
      justify-content: center;
      padding: 48px;
    }
  `]
})
export class LoadingSpinnerComponent {
  readonly diameter = input<number>(40);
}
