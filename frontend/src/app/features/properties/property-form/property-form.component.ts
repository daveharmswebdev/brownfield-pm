import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatIconModule } from '@angular/material/icon';
import { HttpErrorResponse } from '@angular/common/http';
import { PropertyService } from '../services/property.service';
import { US_STATES } from './us-states';

/**
 * Property Form Component (AC-2.1.2, AC-2.1.3)
 *
 * Provides a form to create a new property with:
 * - Name, Street, City, State (dropdown), ZIP Code fields
 * - Client-side validation matching backend rules
 * - Validation errors shown on blur
 * - Success snackbar and redirect to dashboard on save
 */
@Component({
  selector: 'app-property-form',
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
  ],
  templateUrl: './property-form.component.html',
  styleUrl: './property-form.component.scss',
})
export class PropertyFormComponent {
  private readonly fb = inject(FormBuilder);
  private readonly propertyService = inject(PropertyService);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly loading = signal(false);
  protected readonly serverErrors = signal<Record<string, string[]>>({});

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

  protected onSubmit(): void {
    if (!this.form.valid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.serverErrors.set({});

    const { name, street, city, state, zipCode } = this.form.value;

    this.propertyService
      .createProperty({ name, street, city, state, zipCode })
      .subscribe({
        next: () => {
          this.loading.set(false);
          this.snackBar.open('Property added', '', {
            duration: 3000,
            horizontalPosition: 'center',
            verticalPosition: 'bottom',
          });
          this.router.navigate(['/dashboard']);
        },
        error: (error: HttpErrorResponse) => {
          this.loading.set(false);
          if (error.status === 400 && error.error?.errors) {
            this.serverErrors.set(error.error.errors);
          } else if (error.status === 400 && error.error?.detail) {
            this.serverErrors.set({ general: [error.error.detail] });
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

  protected cancel(): void {
    this.router.navigate(['/dashboard']);
  }
}
