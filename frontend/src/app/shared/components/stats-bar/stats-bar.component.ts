import { Component, input, computed } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

/**
 * StatsBarComponent (AC-2.2.1, AC-4.4.1, AC-4.4.2, AC-4.4.3, AC-4.4.4)
 *
 * Displays a horizontal bar with three financial summary cards:
 * - Total Expenses YTD
 * - Total Income YTD
 * - Net Income YTD (calculated: income - expenses)
 *
 * Uses Forest Green theme styling.
 * Negative net income displays in accounting format with parentheses: ($1,234.00)
 */
@Component({
  selector: 'app-stats-bar',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule, CurrencyPipe],
  template: `
    <div class="stats-bar">
      <mat-card class="stat-card expense-card">
        <mat-icon class="stat-icon">trending_down</mat-icon>
        <div class="stat-content">
          <span class="stat-label">Total Expenses YTD</span>
          <span class="stat-value expense">{{ expenseTotal() | currency:'USD':'symbol':'1.2-2' }}</span>
        </div>
      </mat-card>

      <mat-card class="stat-card income-card">
        <mat-icon class="stat-icon">trending_up</mat-icon>
        <div class="stat-content">
          <span class="stat-label">Total Income YTD</span>
          <span class="stat-value income">{{ incomeTotal() | currency:'USD':'symbol':'1.2-2' }}</span>
        </div>
      </mat-card>

      <mat-card class="stat-card net-card" [class.positive]="netIncome() > 0" [class.negative]="netIncome() < 0" [class.zero]="netIncome() === 0">
        <mat-icon class="stat-icon">{{ netIncome() >= 0 ? 'account_balance' : 'warning' }}</mat-icon>
        <div class="stat-content">
          <span class="stat-label">Net Income YTD</span>
          <span class="stat-value net">{{ formattedNetIncome() }}</span>
        </div>
      </mat-card>
    </div>
  `,
  styles: [`
    .stats-bar {
      display: flex;
      gap: 16px;
      margin-bottom: 24px;
      flex-wrap: wrap;
    }

    .stat-card {
      flex: 1;
      min-width: 200px;
      display: flex;
      align-items: center;
      padding: 16px;
      gap: 12px;
      border-radius: 12px;
      box-shadow: var(--pm-shadow-1);
      transition: box-shadow 0.2s ease;

      &:hover {
        box-shadow: var(--pm-shadow-2);
      }
    }

    .stat-icon {
      font-size: 32px;
      width: 32px;
      height: 32px;
      opacity: 0.7;
    }

    .expense-card .stat-icon {
      color: #e57373;
    }

    .income-card .stat-icon {
      color: var(--pm-primary);
    }

    .net-card.positive .stat-icon {
      color: var(--pm-primary);
    }

    .net-card.negative .stat-icon {
      color: #e57373;
    }

    .stat-content {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .stat-label {
      font-size: 12px;
      font-weight: 500;
      color: var(--pm-text-secondary);
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .stat-value {
      font-size: 24px;
      font-weight: 600;
      color: var(--pm-text-primary);
    }

    .stat-value.expense {
      color: #c62828;
    }

    .stat-value.income {
      color: var(--pm-primary-dark);
    }

    .stat-value.net {
      color: var(--pm-text-primary);
    }

    .net-card.positive .stat-value.net {
      color: var(--pm-primary-dark);
    }

    .net-card.negative .stat-value.net {
      color: #c62828;
    }

    .net-card.zero .stat-value.net {
      color: var(--pm-text-primary);
    }

    @media (max-width: 767px) {
      .stats-bar {
        flex-direction: column;
      }

      .stat-card {
        min-width: 100%;
      }

      .stat-value {
        font-size: 20px;
      }
    }
  `]
})
export class StatsBarComponent {
  /**
   * Total expenses for the year-to-date period.
   * Default: 0
   */
  readonly expenseTotal = input<number>(0);

  /**
   * Total income for the year-to-date period.
   * Default: 0
   */
  readonly incomeTotal = input<number>(0);

  /**
   * Computed net income: income - expenses
   */
  readonly netIncome = computed(() => this.incomeTotal() - this.expenseTotal());

  /**
   * Formatted net income with accounting format for negative values (AC-4.4.4)
   * Positive: $1,234.00
   * Negative: ($1,234.00) - accounting format with parentheses
   * Zero: $0.00
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
      // Accounting format: wrap in parentheses
      return `(${formatted})`;
    }
    return formatted;
  });
}
