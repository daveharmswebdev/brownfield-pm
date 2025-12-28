import { Component, inject, signal, computed, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { HttpErrorResponse } from '@angular/common/http';
import { PropertyService, PropertyDetailDto } from '../services/property.service';
import { US_STATES } from '../property-form/us-states';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

/**
 * Interface for components that can have unsaved changes.
 * Used by UnsavedChangesGuard.
 */
export interface HasUnsavedChanges {
  hasUnsavedChanges(): boolean;
}

/**
 * Property Edit Component (AC-2.4.1, AC-2.4.2, AC-2.4.3, AC-2.4.4)
 *
 * Provides a form to edit an existing property with:
 * - Pre-populated form fields from current property values
 * - Client-side validation matching backend rules
 * - Unsaved changes confirmation on navigation
 * - Success snackbar and redirect to property detail on save
 */
@Component({
  selector: 'app-property-edit',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatIconModule,
    MatDialogModule,
  ],
  templateUrl: './property-edit.component.html',
  styleUrl: './property-edit.component.scss',
})
export class PropertyEditComponent implements OnInit, HasUnsavedChanges {
  private readonly fb = inject(FormBuilder);
  private readonly propertyService = inject(PropertyService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);

  protected readonly loading = signal(false);
  protected readonly loadingProperty = signal(true);
  protected readonly serverErrors = signal<Record<string, string[]>>({});
  protected readonly loadError = signal<string | null>(null);

  protected readonly propertyId = signal<string>('');
  protected readonly property = signal<PropertyDetailDto | null>(null);

  protected readonly states = US_STATES;

  protected readonly form: FormGroup = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(255)]],
    street: ['', [Validators.required, Validators.maxLength(255)]],
    city: ['', [Validators.required, Validators.maxLength(100)]],
    state: ['', [Validators.required]],
    zipCode: [
      '',
      [Validators.required, Validators.pattern(/^\d{5}$/)],
    ],
  });

  // Track initial form values for dirty checking
  private initialFormValue: Record<string, string> | null = null;

  protected readonly pageTitle = computed(() => {
    const prop = this.property();
    return prop ? `Edit Property` : 'Edit Property';
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.propertyId.set(id);
      this.loadProperty(id);
    } else {
      this.loadError.set('Invalid property ID');
      this.loadingProperty.set(false);
    }
  }

  private loadProperty(id: string): void {
    this.loadingProperty.set(true);
    this.loadError.set(null);

    this.propertyService.getPropertyById(id).subscribe({
      next: (property) => {
        this.property.set(property);
        this.populateForm(property);
        this.loadingProperty.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.loadingProperty.set(false);
        if (error.status === 404) {
          this.loadError.set('Property not found');
        } else {
          this.loadError.set('Failed to load property. Please try again.');
        }
      },
    });
  }

  private populateForm(property: PropertyDetailDto): void {
    const formValue = {
      name: property.name,
      street: property.street,
      city: property.city,
      state: property.state,
      zipCode: property.zipCode,
    };
    this.form.patchValue(formValue);
    this.initialFormValue = { ...formValue };
  }

  /**
   * Check if form has unsaved changes (AC-2.4.3)
   */
  hasUnsavedChanges(): boolean {
    if (!this.initialFormValue) return false;
    const currentValue = this.form.value;
    return Object.keys(this.initialFormValue).some(
      (key) => this.initialFormValue![key] !== currentValue[key]
    );
  }

  protected onSubmit(): void {
    if (!this.form.valid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.serverErrors.set({});

    const { name, street, city, state, zipCode } = this.form.value;
    const id = this.propertyId();

    this.propertyService
      .updateProperty(id, { name, street, city, state, zipCode })
      .subscribe({
        next: () => {
          this.loading.set(false);
          // Update initial form value to prevent guard from blocking navigation
          this.initialFormValue = { ...this.form.value };
          this.snackBar.open('Property updated', '', {
            duration: 3000,
            horizontalPosition: 'center',
            verticalPosition: 'bottom',
          });
          this.router.navigate(['/properties', id]);
        },
        error: (error: HttpErrorResponse) => {
          this.loading.set(false);
          if (error.status === 400 && error.error?.errors) {
            this.serverErrors.set(error.error.errors);
          } else if (error.status === 400 && error.error?.detail) {
            this.serverErrors.set({ general: [error.error.detail] });
          } else if (error.status === 404) {
            this.serverErrors.set({
              general: ['Property not found. It may have been deleted.'],
            });
          } else {
            this.serverErrors.set({
              general: ['An unexpected error occurred. Please try again.'],
            });
          }
        },
      });
  }

  protected getServerError(field: string): string | null {
    const errors = this.serverErrors();
    const fieldErrors =
      errors[field] || errors[field.charAt(0).toUpperCase() + field.slice(1)];
    return fieldErrors?.[0] || null;
  }

  /**
   * Handle cancel with unsaved changes check (AC-2.4.3)
   */
  protected cancel(): void {
    if (this.hasUnsavedChanges()) {
      const dialogRef = this.dialog.open(ConfirmDialogComponent, {
        data: {
          title: 'Unsaved Changes',
          message: 'You have unsaved changes. Discard changes?',
          confirmText: 'Discard',
          cancelText: 'Cancel',
        },
        width: '400px',
      });

      dialogRef.afterClosed().subscribe((result) => {
        if (result === true) {
          this.navigateBack();
        }
      });
    } else {
      this.navigateBack();
    }
  }

  private navigateBack(): void {
    const id = this.propertyId();
    if (id) {
      this.router.navigate(['/properties', id]);
    } else {
      this.router.navigate(['/properties']);
    }
  }
}
