import { Component, inject, OnInit, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { PropertyStore } from './stores/property.store';
import { YearSelectorService } from '../../core/services/year-selector.service';
import { PropertyRowComponent } from '../../shared/components/property-row/property-row.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { ErrorCardComponent } from '../../shared/components/error-card/error-card.component';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner.component';

/**
 * Properties list component (AC-2.1.1)
 * Shows list of properties with Add Property button
 */
@Component({
  selector: 'app-properties',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    PropertyRowComponent,
    EmptyStateComponent,
    ErrorCardComponent,
    LoadingSpinnerComponent,
  ],
  template: `
    <div class="properties-container">
      <header class="properties-header">
        <h1>Properties</h1>
        <button mat-raised-button color="primary" routerLink="/properties/new">
          <mat-icon>add</mat-icon>
          Add Property
        </button>
      </header>

      <!-- Loading State -->
      @if (propertyStore.isLoading()) {
        <app-loading-spinner />
      }

      <!-- Error State -->
      @if (propertyStore.error()) {
        <app-error-card
          [message]="propertyStore.error()!"
          (retry)="loadProperties()" />
      }

      <!-- Properties List or Empty State -->
      @if (!propertyStore.isLoading() && !propertyStore.error()) {
        <div class="properties-content">
          @if (propertyStore.isEmpty()) {
            <app-empty-state
              icon="home_work"
              title="No properties yet"
              message="Add your first property to get started."
              actionLabel="Add Property"
              actionRoute="/properties/new"
              actionIcon="add" />
          } @else {
            <mat-card class="properties-list-card">
              <mat-card-header>
                <mat-card-title>Your Properties</mat-card-title>
                <mat-card-subtitle>{{ propertyStore.totalCount() }} {{ propertyStore.totalCount() === 1 ? 'property' : 'properties' }}</mat-card-subtitle>
              </mat-card-header>
              <mat-card-content>
                <div class="property-list">
                  @for (property of propertyStore.properties(); track property.id) {
                    <app-property-row
                      [id]="property.id"
                      [name]="property.name"
                      [city]="property.city"
                      [state]="property.state"
                      [expenseTotal]="property.expenseTotal"
                      [incomeTotal]="property.incomeTotal"
                      (rowClick)="navigateToProperty($event)" />
                  }
                </div>
              </mat-card-content>
            </mat-card>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .properties-container {
      max-width: 1200px;
      margin: 0 auto;
    }

    .properties-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 24px;

      h1 {
        color: var(--pm-text-primary);
        font-size: 28px;
        font-weight: 600;
        margin: 0;
      }

      button mat-icon {
        margin-right: 8px;
      }
    }

    .properties-content {
      display: flex;
      justify-content: center;
    }

    .properties-list-card {
      width: 100%;

      mat-card-header {
        margin-bottom: 8px;
      }

      mat-card-content {
        padding: 0;
      }

      .property-list {
        display: flex;
        flex-direction: column;
      }
    }

    @media (max-width: 767px) {
      .properties-header {
        flex-direction: column;
        gap: 16px;
        align-items: stretch;

        h1 {
          font-size: 24px;
        }

        button {
          width: 100%;
        }
      }
    }
  `]
})
export class PropertiesComponent implements OnInit {
  private readonly router = inject(Router);
  readonly propertyStore = inject(PropertyStore);
  readonly yearService = inject(YearSelectorService);

  constructor() {
    // React to year changes and reload properties (AC-3.5.8)
    effect(() => {
      const year = this.yearService.selectedYear();
      this.propertyStore.loadProperties(year);
    });
  }

  ngOnInit(): void {
    // Initial load happens via effect when selectedYear signal is read
  }

  loadProperties(): void {
    this.propertyStore.loadProperties(this.yearService.selectedYear());
  }

  navigateToProperty(propertyId: string): void {
    this.router.navigate(['/properties', propertyId]);
  }
}
