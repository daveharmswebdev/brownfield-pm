import { Component, inject, OnInit, OnDestroy, effect } from '@angular/core';
import { CommonModule, CurrencyPipe, Location } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { PropertyStore } from '../stores/property.store';
import { YearSelectorService } from '../../../core/services/year-selector.service';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../../shared/components/confirm-dialog/confirm-dialog.component';

/**
 * Property Detail Component (AC-2.3.1, AC-2.3.2, AC-2.3.3, AC-2.3.4, AC-2.3.6)
 *
 * Displays:
 * - Property name as page title
 * - Full address (street, city, state, ZIP)
 * - YTD stats (expenses, income, net)
 * - Recent Expenses section with empty state
 * - Recent Income section with empty state
 * - Action buttons (Add Expense, Add Income, Edit, Delete)
 */
@Component({
  selector: 'app-property-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatDialogModule,
    CurrencyPipe,
  ],
  template: `
    <div class="property-detail-container">
      <!-- Loading State -->
      @if (propertyStore.isLoadingDetail()) {
        <div class="loading-container">
          <mat-spinner diameter="40"></mat-spinner>
        </div>
      }

      <!-- Error State / 404 (AC-2.3.6) -->
      @if (propertyStore.detailError()) {
        <mat-card class="error-card">
          <mat-icon>error_outline</mat-icon>
          <h2>Property not found</h2>
          <p>{{ propertyStore.detailError() }}</p>
          <button mat-raised-button color="primary" (click)="goBack()">
            <mat-icon>arrow_back</mat-icon>
            Go Back
          </button>
        </mat-card>
      }

      <!-- Property Detail Content -->
      @if (!propertyStore.isLoadingDetail() && !propertyStore.detailError() && propertyStore.selectedProperty()) {
        <!-- Header with Property Name (AC-2.3.2) -->
        <header class="property-header">
          <div class="header-content">
            <button mat-icon-button (click)="goBack()" aria-label="Go back" class="back-button">
              <mat-icon>arrow_back</mat-icon>
            </button>
            <div class="title-section">
              <h1>{{ propertyStore.selectedProperty()!.name }}</h1>
              <p class="address">{{ propertyStore.selectedPropertyFullAddress() }}</p>
            </div>
          </div>
          <!-- Action Buttons (AC-2.3.4, AC-3.1.1) -->
          <div class="action-buttons">
            <button mat-stroked-button
                    color="primary"
                    [routerLink]="['/properties', propertyStore.selectedProperty()!.id, 'expenses']">
              <mat-icon>add</mat-icon>
              Add Expense
            </button>
            <button mat-stroked-button
                    color="primary"
                    [routerLink]="['/properties', propertyStore.selectedProperty()!.id, 'income']">
              <mat-icon>add</mat-icon>
              Add Income
            </button>
            <button mat-stroked-button color="primary" [routerLink]="['/properties', propertyStore.selectedProperty()!.id, 'edit']">
              <mat-icon>edit</mat-icon>
              Edit
            </button>
            <button mat-stroked-button
                    color="warn"
                    (click)="onDeleteClick()"
                    [disabled]="propertyStore.isDeleting()">
              @if (propertyStore.isDeleting()) {
                <mat-spinner diameter="18" class="button-spinner"></mat-spinner>
              } @else {
                <mat-icon>delete</mat-icon>
              }
              Delete
            </button>
          </div>
        </header>

        <!-- Stats Section (AC-2.3.2) -->
        <div class="stats-section">
          <mat-card class="stat-card expense-card">
            <mat-card-content>
              <div class="stat-icon">
                <mat-icon>trending_down</mat-icon>
              </div>
              <div class="stat-details">
                <span class="stat-label">YTD Expenses</span>
                <span class="stat-value expense-value">{{ propertyStore.selectedProperty()!.expenseTotal | currency }}</span>
              </div>
            </mat-card-content>
          </mat-card>

          <mat-card class="stat-card income-card">
            <mat-card-content>
              <div class="stat-icon">
                <mat-icon>trending_up</mat-icon>
              </div>
              <div class="stat-details">
                <span class="stat-label">YTD Income</span>
                <span class="stat-value income-value">{{ propertyStore.selectedProperty()!.incomeTotal | currency }}</span>
              </div>
            </mat-card-content>
          </mat-card>

          <mat-card class="stat-card net-card">
            <mat-card-content>
              <div class="stat-icon">
                <mat-icon>{{ propertyStore.selectedPropertyNetIncome() >= 0 ? 'account_balance' : 'warning' }}</mat-icon>
              </div>
              <div class="stat-details">
                <span class="stat-label">Net Income</span>
                <span class="stat-value" [class.negative]="propertyStore.selectedPropertyNetIncome() < 0" [class.positive]="propertyStore.selectedPropertyNetIncome() > 0">
                  {{ formatNetIncome(propertyStore.selectedPropertyNetIncome()) }}
                </span>
              </div>
            </mat-card-content>
          </mat-card>
        </div>

        <!-- Recent Activity Section (AC-2.3.3) -->
        <div class="activity-section">
          <!-- Recent Expenses -->
          <mat-card class="activity-card">
            <mat-card-header>
              <mat-card-title>Recent Expenses</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              @if (propertyStore.selectedProperty()!.recentExpenses.length === 0) {
                <div class="empty-state">
                  <mat-icon>receipt_long</mat-icon>
                  <p>No expenses yet</p>
                </div>
              } @else {
                <!-- Future: List of recent expenses -->
                <div class="activity-list">
                  @for (expense of propertyStore.selectedProperty()!.recentExpenses; track expense.id) {
                    <div class="activity-item">
                      <span class="activity-description">{{ expense.description }}</span>
                      <span class="activity-amount expense">{{ expense.amount | currency }}</span>
                    </div>
                  }
                </div>
              }
            </mat-card-content>
          </mat-card>

          <!-- Recent Income -->
          <mat-card class="activity-card">
            <mat-card-header>
              <mat-card-title>Recent Income</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              @if (propertyStore.selectedProperty()!.recentIncome.length === 0) {
                <div class="empty-state">
                  <mat-icon>payments</mat-icon>
                  <p>No income recorded yet</p>
                </div>
              } @else {
                <!-- Future: List of recent income -->
                <div class="activity-list">
                  @for (income of propertyStore.selectedProperty()!.recentIncome; track income.id) {
                    <div class="activity-item">
                      <span class="activity-description">{{ income.description }}</span>
                      <span class="activity-amount income">{{ income.amount | currency }}</span>
                    </div>
                  }
                </div>
              }
            </mat-card-content>
          </mat-card>
        </div>
      }
    </div>
  `,
  styles: [`
    .property-detail-container {
      max-width: 1200px;
      margin: 0 auto;
    }

    .loading-container {
      display: flex;
      justify-content: center;
      padding: 48px;
    }

    .error-card {
      text-align: center;
      padding: 48px;
      max-width: 400px;
      margin: 48px auto;

      mat-icon {
        font-size: 64px;
        width: 64px;
        height: 64px;
        color: var(--pm-error, #c62828);
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
      }

      button mat-icon {
        margin-right: 8px;
      }
    }

    .property-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 24px;
      flex-wrap: wrap;
      gap: 16px;

      .header-content {
        display: flex;
        align-items: flex-start;
        gap: 8px;

        .back-button {
          margin-top: 4px;
        }

        .title-section {
          h1 {
            color: var(--pm-text-primary);
            font-size: 28px;
            font-weight: 600;
            margin: 0 0 8px 0;
          }

          .address {
            color: var(--pm-text-secondary);
            font-size: 16px;
            margin: 0;
          }
        }
      }

      .action-buttons {
        display: flex;
        gap: 8px;
        flex-wrap: wrap;

        button mat-icon {
          margin-right: 4px;
        }

        button mat-spinner {
          display: inline-block;
          margin-right: 4px;
        }
      }
    }

    .stats-section {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 16px;
      margin-bottom: 24px;

      .stat-card {
        mat-card-content {
          display: flex;
          align-items: center;
          gap: 16px;
          padding: 16px;
        }

        .stat-icon {
          display: flex;
          align-items: center;
          justify-content: center;
          width: 48px;
          height: 48px;
          border-radius: 12px;
          background-color: var(--pm-primary-light);

          mat-icon {
            color: var(--pm-primary-dark);
            font-size: 24px;
            width: 24px;
            height: 24px;
          }
        }

        &.expense-card .stat-icon {
          background-color: #ffebee;

          mat-icon {
            color: #c62828;
          }
        }

        &.income-card .stat-icon {
          background-color: #e8f5e9;

          mat-icon {
            color: var(--pm-primary-dark);
          }
        }

        &.net-card .stat-icon {
          background-color: #fff8e1;

          mat-icon {
            color: #f57f17;
          }
        }

        .stat-details {
          display: flex;
          flex-direction: column;

          .stat-label {
            color: var(--pm-text-secondary);
            font-size: 14px;
            margin-bottom: 4px;
          }

          .stat-value {
            color: var(--pm-text-primary);
            font-size: 24px;
            font-weight: 600;

            &.expense-value {
              color: #c62828;
            }

            &.income-value {
              color: var(--pm-primary-dark);
            }

            &.negative {
              color: #c62828;
            }

            &.positive {
              color: var(--pm-primary-dark);
            }
          }
        }
      }
    }

    .activity-section {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
      gap: 24px;

      .activity-card {
        mat-card-header {
          margin-bottom: 16px;
        }

        .empty-state {
          text-align: center;
          padding: 32px 16px;
          color: var(--pm-text-secondary);

          mat-icon {
            font-size: 48px;
            width: 48px;
            height: 48px;
            margin-bottom: 12px;
            opacity: 0.5;
          }

          p {
            margin: 0;
            font-size: 14px;
          }
        }

        .activity-list {
          display: flex;
          flex-direction: column;
          gap: 12px;
        }

        .activity-item {
          display: flex;
          justify-content: space-between;
          align-items: center;
          padding: 8px 0;
          border-bottom: 1px solid rgba(0, 0, 0, 0.08);

          &:last-child {
            border-bottom: none;
          }

          .activity-description {
            color: var(--pm-text-primary);
          }

          .activity-amount {
            font-weight: 500;

            &.expense {
              color: #c62828;
            }

            &.income {
              color: var(--pm-primary-dark);
            }
          }
        }
      }
    }

    @media (max-width: 767px) {
      .property-header {
        flex-direction: column;

        .header-content {
          .title-section h1 {
            font-size: 24px;
          }
        }

        .action-buttons {
          width: 100%;
          justify-content: flex-start;

          button {
            flex: 1;
            min-width: 0;
          }
        }
      }

      .stats-section {
        grid-template-columns: 1fr;
      }

      .activity-section {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class PropertyDetailComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly location = inject(Location);
  private readonly dialog = inject(MatDialog);
  readonly propertyStore = inject(PropertyStore);
  readonly yearService = inject(YearSelectorService);

  private propertyId: string | null = null;

  constructor() {
    // React to year changes and reload property detail (AC-3.5.6)
    effect(() => {
      const year = this.yearService.selectedYear();
      if (this.propertyId) {
        this.propertyStore.loadPropertyById({ id: this.propertyId, year });
      }
    });
  }

  ngOnInit(): void {
    // Get property ID from route and load (AC-2.3.1)
    this.propertyId = this.route.snapshot.paramMap.get('id');
    // Initial load happens via effect when selectedYear signal is read
  }

  ngOnDestroy(): void {
    // Clear selected property when leaving the page
    this.propertyStore.clearSelectedProperty();
  }

  goBack(): void {
    this.router.navigate(['/properties']);
  }

  /**
   * Format net income with accounting format for negative values (AC-4.4.4)
   * Positive: $1,234.00
   * Negative: ($1,234.00) - accounting format with parentheses
   * Zero: $0.00
   */
  formatNetIncome(value: number): string {
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
  }

  /**
   * Opens delete confirmation dialog (AC-2.5.1)
   * On confirm: calls propertyStore.deleteProperty() which handles
   * API call, success snackbar, and navigation to dashboard (AC-2.5.4)
   */
  onDeleteClick(): void {
    const property = this.propertyStore.selectedProperty();
    if (!property) return;

    const dialogData: ConfirmDialogData = {
      title: `Delete ${property.name}?`,
      message: 'This will remove the property from your active portfolio.',
      confirmText: 'Delete',
      cancelText: 'Cancel',
      icon: 'warning',
      iconColor: 'warn',
      secondaryMessage:
        'Historical expense and income records will be preserved for tax purposes.',
      confirmIcon: 'delete',
    };

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      data: dialogData,
      width: '450px',
      disableClose: true,
      panelClass: 'confirm-dialog-panel',
    });

    dialogRef.afterClosed().subscribe((confirmed: boolean) => {
      if (confirmed) {
        this.propertyStore.deleteProperty(property.id);
      }
    });
  }
}
