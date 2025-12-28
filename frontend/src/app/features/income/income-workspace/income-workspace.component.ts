import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { IncomeStore } from '../stores/income.store';
import { IncomeFormComponent } from '../components/income-form/income-form.component';
import { IncomeRowComponent } from '../components/income-row/income-row.component';
import { PropertyService, PropertyDetailDto } from '../../properties/services/property.service';
import { UpdateIncomeRequest } from '../services/income.service';

/**
 * IncomeWorkspaceComponent (AC-4.1.1, AC-4.1.2, AC-4.1.4, AC-4.2.3, AC-4.2.6)
 *
 * Main workspace for income management:
 * - Property name header for context
 * - New income form at top
 * - Previous income list below
 * - YTD income total
 * - Edit and delete functionality (AC-4.2)
 */
@Component({
  selector: 'app-income-workspace',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatDividerModule,
    CurrencyPipe,
    IncomeFormComponent,
    IncomeRowComponent,
  ],
  template: `
    <div class="income-workspace">
      <!-- Header with Property Context -->
      <header class="workspace-header">
        <button mat-icon-button (click)="goBack()" class="back-button">
          <mat-icon>arrow_back</mat-icon>
        </button>
        <div class="header-content">
          <h1>{{ propertyName() }}</h1>
          <p class="header-subtitle">Add and manage income</p>
        </div>
      </header>

      @if (isLoadingProperty()) {
        <div class="loading-container">
          <mat-spinner diameter="40" />
          <p>Loading property...</p>
        </div>
      } @else if (propertyError()) {
        <mat-card class="error-card">
          <mat-card-content>
            <mat-icon>error_outline</mat-icon>
            <p>{{ propertyError() }}</p>
            <button mat-button color="primary" (click)="goBack()">Go Back</button>
          </mat-card-content>
        </mat-card>
      } @else {
        <!-- New Income Form -->
        <app-income-form
          [propertyId]="propertyId()"
          (incomeCreated)="onIncomeCreated()"
        />

        <!-- Income List (AC-4.1.2, AC-4.1.6) -->
        <mat-card class="income-list-card">
          <mat-card-header>
            <mat-card-title>Previous Income</mat-card-title>
            <div class="ytd-total">
              <span class="ytd-label">YTD Total:</span>
              <span class="ytd-amount">{{ store.ytdTotal() | currency }}</span>
            </div>
          </mat-card-header>
          <mat-card-content>
            @if (store.isLoading()) {
              <div class="loading-container">
                <mat-spinner diameter="32" />
                <p>Loading income...</p>
              </div>
            } @else if (store.isEmpty()) {
              <div class="empty-state">
                <mat-icon>attach_money</mat-icon>
                <p>No income recorded yet for this property.</p>
                <p class="empty-hint">Use the form above to add your first income entry.</p>
              </div>
            } @else {
              <div class="income-list">
                @for (income of store.incomeEntries(); track income.id) {
                  <app-income-row
                    [income]="income"
                    [isEditing]="store.editingIncomeId() === income.id"
                    [isSaving]="store.isUpdating()"
                    [isDeleting]="store.isDeleting()"
                    (edit)="onEditIncome($event)"
                    (save)="onSaveIncome($event)"
                    (cancelEdit)="onCancelEdit()"
                    (delete)="onDeleteIncome($event)"
                  />
                }
              </div>
            }
          </mat-card-content>
        </mat-card>
      }
    </div>
  `,
  styles: [`
    .income-workspace {
      max-width: 800px;
      margin: 0 auto;
      padding: 16px;
    }

    .workspace-header {
      display: flex;
      align-items: center;
      gap: 16px;
      margin-bottom: 24px;
    }

    .back-button {
      flex-shrink: 0;
    }

    .header-content h1 {
      margin: 0;
      font-size: 1.5rem;
    }

    .header-subtitle {
      margin: 4px 0 0 0;
      color: var(--mat-sys-on-surface-variant);
      font-size: 0.9rem;
    }

    .loading-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 48px;
      gap: 16px;
      color: var(--mat-sys-on-surface-variant);
    }

    .error-card {
      text-align: center;
      padding: 24px;
    }

    .error-card mat-icon {
      font-size: 48px;
      width: 48px;
      height: 48px;
      color: var(--mat-sys-error);
      margin-bottom: 16px;
    }

    .income-list-card {
      margin-top: 24px;
    }

    .income-list-card mat-card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding-bottom: 16px;

      ::ng-deep .mat-mdc-card-header-text {
        flex: 1;
      }
    }

    .ytd-total {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .ytd-label {
      color: var(--mat-sys-on-surface-variant);
      font-size: 0.9rem;
    }

    .ytd-amount {
      font-weight: 600;
      font-size: 1.1rem;
      color: var(--mat-sys-primary);
    }

    .income-list {
      margin: 0 -16px -16px -16px;
    }

    .empty-state {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 48px 24px;
      text-align: center;
      color: var(--mat-sys-on-surface-variant);
    }

    .empty-state mat-icon {
      font-size: 64px;
      width: 64px;
      height: 64px;
      margin-bottom: 16px;
      opacity: 0.5;
    }

    .empty-hint {
      font-size: 0.9rem;
      margin-top: 8px;
    }
  `],
})
export class IncomeWorkspaceComponent implements OnInit {
  protected readonly store = inject(IncomeStore);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly propertyService = inject(PropertyService);

  protected readonly propertyId = signal<string>('');
  protected readonly propertyName = signal<string>('');
  protected readonly isLoadingProperty = signal(true);
  protected readonly propertyError = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.propertyError.set('Property ID is required');
      this.isLoadingProperty.set(false);
      return;
    }

    this.propertyId.set(id);
    this.loadProperty(id);
  }

  private loadProperty(propertyId: string): void {
    this.isLoadingProperty.set(true);
    this.propertyError.set(null);

    this.propertyService.getPropertyById(propertyId).subscribe({
      next: (property: PropertyDetailDto) => {
        this.propertyName.set(property.name);
        this.isLoadingProperty.set(false);

        // Load income for this property
        this.store.loadIncomeByProperty({
          propertyId,
          propertyName: property.name,
        });
      },
      error: (error) => {
        this.isLoadingProperty.set(false);
        if (error.status === 404) {
          this.propertyError.set('Property not found');
        } else {
          this.propertyError.set('Failed to load property. Please try again.');
        }
        console.error('Error loading property:', error);
      },
    });
  }

  protected onIncomeCreated(): void {
    // Form handles the snackbar and state update
  }

  /**
   * Handle edit button click (AC-4.2.1)
   * Sets the editing income ID in store to show inline edit form
   */
  protected onEditIncome(incomeId: string): void {
    this.store.setEditingIncome(incomeId);
  }

  /**
   * Handle save edit (AC-4.2.3)
   * Calls store.updateIncome with the updated data
   */
  protected onSaveIncome(event: { incomeId: string; request: UpdateIncomeRequest }): void {
    this.store.updateIncome(event);
  }

  /**
   * Handle cancel edit (AC-4.2.7)
   * Clears the editing income ID in store
   */
  protected onCancelEdit(): void {
    this.store.cancelEditing();
  }

  /**
   * Handle delete confirmed (AC-4.2.6)
   * Calls store.deleteIncome to soft-delete the income
   */
  protected onDeleteIncome(incomeId: string): void {
    this.store.deleteIncome(incomeId);
  }

  protected goBack(): void {
    const propertyId = this.propertyId();
    if (propertyId) {
      this.router.navigate(['/properties', propertyId]);
    } else {
      this.router.navigate(['/dashboard']);
    }
  }
}
