import { Component, input, output, computed } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatRippleModule } from '@angular/material/core';

/**
 * PropertyRowComponent (AC-2.2.2, AC-2.2.5, AC-4.4.5, AC-4.4.6)
 *
 * Displays a single property row in a scannable list format:
 * - Property name
 * - Address (city, state format)
 * - YTD expense total
 * - YTD net income with color coding
 * - Disabled [+] button for quick-add expense (Epic 3)
 *
 * Emits click event for navigation to property detail.
 */
@Component({
  selector: 'app-property-row',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatButtonModule,
    MatTooltipModule,
    MatRippleModule,
    CurrencyPipe,
  ],
  template: `
    <div class="property-row" matRipple (click)="onRowClick()" (keydown.enter)="onRowClick()" tabindex="0" role="button">
      <div class="property-icon">
        <mat-icon>home</mat-icon>
      </div>

      <div class="property-info">
        <span class="property-name">{{ name() }}</span>
        <span class="property-address">{{ city() }}, {{ state() }}</span>
      </div>

      <div class="property-expense">
        <span class="expense-label">YTD Expenses</span>
        <span class="expense-value">{{ expenseTotal() | currency:'USD':'symbol':'1.2-2' }}</span>
      </div>

      <div class="property-net" [class.positive]="netIncome() > 0" [class.negative]="netIncome() < 0" [class.zero]="netIncome() === 0">
        <span class="net-label">Net</span>
        <span class="net-value">{{ formattedNetIncome() }}</span>
      </div>

      <button
        mat-icon-button
        class="add-expense-btn"
        [disabled]="true"
        matTooltip="Add expense (coming soon)"
        (click)="onAddExpenseClick($event)"
        aria-label="Add expense">
        <mat-icon>add</mat-icon>
      </button>
    </div>
  `,
  styles: [`
    .property-row {
      display: flex;
      align-items: center;
      padding: 12px 16px;
      gap: 16px;
      border-radius: 8px;
      cursor: pointer;
      transition: background-color 0.2s ease;
      border-bottom: 1px solid rgba(0, 0, 0, 0.06);

      &:hover {
        background-color: rgba(0, 0, 0, 0.04);
      }

      &:focus {
        outline: 2px solid var(--pm-primary);
        outline-offset: -2px;
      }

      &:last-child {
        border-bottom: none;
      }
    }

    .property-icon {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 40px;
      height: 40px;
      border-radius: 8px;
      background-color: var(--pm-primary-light);

      mat-icon {
        color: var(--pm-primary-dark);
      }
    }

    .property-info {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 2px;
      min-width: 0;
    }

    .property-name {
      font-size: 16px;
      font-weight: 500;
      color: var(--pm-text-primary);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .property-address {
      font-size: 14px;
      color: var(--pm-text-secondary);
    }

    .property-expense {
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      gap: 2px;
      min-width: 100px;
    }

    .expense-label {
      font-size: 11px;
      font-weight: 500;
      color: var(--pm-text-secondary);
      text-transform: uppercase;
      letter-spacing: 0.3px;
    }

    .expense-value {
      font-size: 16px;
      font-weight: 600;
      color: var(--pm-text-primary);
    }

    .property-net {
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      gap: 2px;
      min-width: 90px;
    }

    .net-label {
      font-size: 11px;
      font-weight: 500;
      color: var(--pm-text-secondary);
      text-transform: uppercase;
      letter-spacing: 0.3px;
    }

    .net-value {
      font-size: 16px;
      font-weight: 600;
      color: var(--pm-text-primary);
    }

    .property-net.positive .net-value {
      color: var(--pm-primary-dark);
    }

    .property-net.negative .net-value {
      color: #c62828;
    }

    .property-net.zero .net-value {
      color: var(--pm-text-primary);
    }

    .add-expense-btn {
      opacity: 0.5;
      flex-shrink: 0;

      &:disabled {
        cursor: not-allowed;
      }
    }

    @media (max-width: 767px) {
      .property-row {
        padding: 12px;
        gap: 12px;
      }

      .property-icon {
        width: 36px;
        height: 36px;

        mat-icon {
          font-size: 20px;
          width: 20px;
          height: 20px;
        }
      }

      .property-name {
        font-size: 14px;
      }

      .property-address {
        font-size: 12px;
      }

      .property-expense {
        min-width: 80px;
      }

      .expense-label {
        font-size: 10px;
      }

      .expense-value {
        font-size: 14px;
      }

      .property-net {
        min-width: 70px;
      }

      .net-value {
        font-size: 14px;
      }
    }
  `]
})
export class PropertyRowComponent {
  /**
   * Property ID for navigation
   */
  readonly id = input.required<string>();

  /**
   * Property name
   */
  readonly name = input.required<string>();

  /**
   * Property city
   */
  readonly city = input.required<string>();

  /**
   * Property state (2-letter code)
   */
  readonly state = input.required<string>();

  /**
   * Year-to-date expense total
   * Default: 0
   */
  readonly expenseTotal = input<number>(0);

  /**
   * Year-to-date income total (AC-4.4.5)
   * Default: 0
   */
  readonly incomeTotal = input<number>(0);

  /**
   * Computed net income: income - expenses (AC-4.4.5)
   */
  readonly netIncome = computed(() => this.incomeTotal() - this.expenseTotal());

  /**
   * Formatted net income with accounting format for negative values (AC-4.4.4)
   */
  readonly formattedNetIncome = computed(() => {
    const value = this.netIncome();
    const absValue = Math.abs(value);
    const formatted = absValue.toLocaleString('en-US', {
      style: 'currency',
      currency: 'USD',
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });

    if (value < 0) {
      return `(${formatted})`;
    }
    return formatted;
  });

  /**
   * Emitted when the row is clicked for navigation
   */
  readonly rowClick = output<string>();

  /**
   * Emitted when the add expense button is clicked (for future use)
   */
  readonly addExpenseClick = output<string>();

  onRowClick(): void {
    this.rowClick.emit(this.id());
  }

  onAddExpenseClick(event: Event): void {
    event.stopPropagation();
    this.addExpenseClick.emit(this.id());
  }
}
