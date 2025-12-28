import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';

import { YearSelectorService } from '../../../core/services/year-selector.service';

/**
 * Year Selector Component (AC-3.5.1)
 *
 * Compact dropdown for selecting tax year.
 * Designed for placement in toolbar/header areas.
 * Binds to YearSelectorService for global state.
 */
@Component({
  selector: 'app-year-selector',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatSelectModule,
    MatFormFieldModule,
    MatIconModule,
  ],
  template: `
    <div class="year-selector-wrapper">
      <mat-icon class="calendar-icon">calendar_today</mat-icon>
      <mat-form-field appearance="outline" class="year-selector">
        <mat-select
          [value]="yearService.selectedYear()"
          (selectionChange)="onYearChange($event.value)"
          aria-label="Select tax year"
          data-testid="year-selector"
          panelClass="year-selector-panel"
        >
          @for (year of yearService.availableYears(); track year) {
            <mat-option [value]="year" [attr.data-testid]="'year-option-' + year">
              {{ year }}
            </mat-option>
          }
        </mat-select>
      </mat-form-field>
    </div>
  `,
  styles: [`
    .year-selector-wrapper {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .calendar-icon {
      color: rgba(255, 255, 255, 0.8);
      font-size: 24px;
      width: 24px;
      height: 24px;
    }

    .year-selector {
      width: 90px;

      ::ng-deep {
        .mat-mdc-form-field-subscript-wrapper {
          display: none;
        }

        .mat-mdc-text-field-wrapper {
          padding: 0 8px;
          background: rgba(255, 255, 255, 0.1);
          border-radius: 8px;
        }

        .mat-mdc-form-field-flex {
          height: 36px;
          align-items: center;
        }

        .mat-mdc-select-value {
          font-size: 14px;
          font-weight: 500;
          color: white !important;
        }

        .mat-mdc-select-value-text {
          color: white !important;
        }

        .mdc-notched-outline__leading,
        .mdc-notched-outline__notch,
        .mdc-notched-outline__trailing {
          border-color: rgba(255, 255, 255, 0.3) !important;
        }

        .mat-mdc-select-arrow {
          color: rgba(255, 255, 255, 0.7);
        }
      }
    }

    :host-context(.light-theme) {
      .calendar-icon {
        color: rgba(0, 0, 0, 0.54);
      }

      .year-selector {
        ::ng-deep {
          .mat-mdc-text-field-wrapper {
            background: rgba(0, 0, 0, 0.05);
          }

          .mat-mdc-select-value,
          .mat-mdc-select-value-text {
            color: rgba(0, 0, 0, 0.87) !important;
          }

          .mdc-notched-outline__leading,
          .mdc-notched-outline__notch,
          .mdc-notched-outline__trailing {
            border-color: rgba(0, 0, 0, 0.2) !important;
          }

          .mat-mdc-select-arrow {
            color: rgba(0, 0, 0, 0.54);
          }
        }
      }
    }
  `]
})
export class YearSelectorComponent {
  readonly yearService = inject(YearSelectorService);

  onYearChange(year: number): void {
    this.yearService.setYear(year);
  }
}
