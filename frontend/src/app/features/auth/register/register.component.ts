import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { HttpErrorResponse } from '@angular/common/http';
import { AuthService, ValidationError } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatIconModule,
  ],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly loading = signal(false);
  protected readonly success = signal(false);
  protected readonly serverErrors = signal<Record<string, string[]>>({});
  protected readonly hidePassword = signal(true);
  protected readonly hideConfirmPassword = signal(true);

  protected readonly form: FormGroup = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [
      Validators.required,
      Validators.minLength(8),
      Validators.pattern(/[A-Z]/), // At least one uppercase
      Validators.pattern(/[a-z]/), // At least one lowercase
      Validators.pattern(/[0-9]/), // At least one number
      Validators.pattern(/[!@#$%^&*()_+\-=\[\]{}|;':",.<>\/?]/), // At least one special char
    ]],
    confirmPassword: ['', [Validators.required]],
    accountName: ['', [Validators.required, Validators.maxLength(255)]],
  });

  protected getPasswordErrors(): string[] {
    const errors: string[] = [];
    const control = this.form.get('password');

    if (control?.hasError('required')) {
      errors.push('Password is required');
    }
    if (control?.hasError('minlength')) {
      errors.push('Password must be at least 8 characters');
    }
    if (control?.value && !/[A-Z]/.test(control.value)) {
      errors.push('Password must contain at least one uppercase letter');
    }
    if (control?.value && !/[a-z]/.test(control.value)) {
      errors.push('Password must contain at least one lowercase letter');
    }
    if (control?.value && !/[0-9]/.test(control.value)) {
      errors.push('Password must contain at least one number');
    }
    if (control?.value && !/[!@#$%^&*()_+\-=\[\]{}|;':",.<>\/?]/.test(control.value)) {
      errors.push('Password must contain at least one special character');
    }

    return errors;
  }

  protected passwordsMatch(): boolean {
    return this.form.get('password')?.value === this.form.get('confirmPassword')?.value;
  }

  protected onSubmit(): void {
    if (!this.form.valid || !this.passwordsMatch()) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.serverErrors.set({});

    const { email, password, accountName } = this.form.value;

    this.authService.register({ email, password, name: accountName }).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set(true);
      },
      error: (error: HttpErrorResponse) => {
        this.loading.set(false);
        if (error.status === 400 && error.error?.errors) {
          const validationError = error.error as ValidationError;
          this.serverErrors.set(validationError.errors);
        } else if (error.status === 400 && error.error?.detail) {
          this.serverErrors.set({ general: [error.error.detail] });
        } else {
          this.serverErrors.set({ general: ['An unexpected error occurred. Please try again.'] });
        }
      },
    });
  }

  protected getServerError(field: string): string | null {
    const errors = this.serverErrors();
    const fieldErrors = errors[field] || errors[field.charAt(0).toUpperCase() + field.slice(1)];
    return fieldErrors?.[0] || null;
  }

  protected hasMinLength(): boolean {
    return (this.form.get('password')?.value?.length ?? 0) >= 8;
  }

  protected hasUppercase(): boolean {
    return /[A-Z]/.test(this.form.get('password')?.value ?? '');
  }

  protected hasLowercase(): boolean {
    return /[a-z]/.test(this.form.get('password')?.value ?? '');
  }

  protected hasNumber(): boolean {
    return /[0-9]/.test(this.form.get('password')?.value ?? '');
  }

  protected hasSpecialChar(): boolean {
    return /[!@#$%^&*()_+\-=\[\]{}|;':",.<>\/?]/.test(this.form.get('password')?.value ?? '');
  }
}
