import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';
import { By } from '@angular/platform-browser';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { PropertyFormComponent } from './property-form.component';
import { PropertyService } from '../services/property.service';

// Helper type to access protected members for testing
type TestablePropertyFormComponent = PropertyFormComponent & {
  form: PropertyFormComponent['form'];
  onSubmit: PropertyFormComponent['onSubmit'];
  loading: PropertyFormComponent['loading'];
  serverErrors: PropertyFormComponent['serverErrors'];
};

describe('PropertyFormComponent', () => {
  let component: TestablePropertyFormComponent;
  let fixture: ComponentFixture<PropertyFormComponent>;
  let propertyServiceSpy: { createProperty: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    propertyServiceSpy = {
      createProperty: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [PropertyFormComponent, NoopAnimationsModule],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: PropertyService, useValue: propertyServiceSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PropertyFormComponent);
    component = fixture.componentInstance as TestablePropertyFormComponent;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display Add Property header', () => {
    const header = fixture.debugElement.query(By.css('mat-card-title'));
    expect(header.nativeElement.textContent).toContain('Add Property');
  });

  it('should have all required form fields', () => {
    const nameField = fixture.debugElement.query(By.css('input[formControlName="name"]'));
    const streetField = fixture.debugElement.query(By.css('input[formControlName="street"]'));
    const cityField = fixture.debugElement.query(By.css('input[formControlName="city"]'));
    const stateField = fixture.debugElement.query(By.css('mat-select[formControlName="state"]'));
    const zipField = fixture.debugElement.query(By.css('input[formControlName="zipCode"]'));

    expect(nameField).toBeTruthy();
    expect(streetField).toBeTruthy();
    expect(cityField).toBeTruthy();
    expect(stateField).toBeTruthy();
    expect(zipField).toBeTruthy();
  });

  it('should have Save and Cancel buttons', () => {
    const buttons = fixture.debugElement.queryAll(By.css('button'));
    const buttonTexts = buttons.map(b => b.nativeElement.textContent.trim());

    expect(buttonTexts.some(t => t.includes('Cancel'))).toBe(true);
    expect(buttonTexts.some(t => t.includes('Save Property'))).toBe(true);
  });

  it('should not call service when form is invalid', () => {
    component.onSubmit();
    expect(propertyServiceSpy.createProperty).not.toHaveBeenCalled();
  });

  it('should call PropertyService when form is valid', () => {
    propertyServiceSpy.createProperty.mockReturnValue(of({ id: '123' }));

    component.form.setValue({
      name: 'Test Property',
      street: '123 Test St',
      city: 'Austin',
      state: 'TX',
      zipCode: '78701',
    });

    component.onSubmit();

    expect(propertyServiceSpy.createProperty).toHaveBeenCalledWith({
      name: 'Test Property',
      street: '123 Test St',
      city: 'Austin',
      state: 'TX',
      zipCode: '78701',
    });
  });

  it('should set loading state during submission', () => {
    propertyServiceSpy.createProperty.mockReturnValue(of({ id: '123' }));

    component.form.setValue({
      name: 'Test Property',
      street: '123 Test St',
      city: 'Austin',
      state: 'TX',
      zipCode: '78701',
    });

    // loading should start as false
    expect(component.loading()).toBe(false);

    component.onSubmit();

    // After successful submission, loading should be false
    expect(component.loading()).toBe(false);
  });

  it('should display server errors on API validation failure', () => {
    const errorResponse = new HttpErrorResponse({
      error: { errors: { Name: ['Name is required'] } },
      status: 400,
    });
    propertyServiceSpy.createProperty.mockReturnValue(throwError(() => errorResponse));

    component.form.setValue({
      name: 'Test Property',
      street: '123 Test St',
      city: 'Austin',
      state: 'TX',
      zipCode: '78701',
    });

    component.onSubmit();

    expect(component.serverErrors()).toEqual({ Name: ['Name is required'] });
  });

  it('should display general error on unexpected failure', () => {
    const errorResponse = new HttpErrorResponse({
      error: { detail: 'Something went wrong' },
      status: 400,
    });
    propertyServiceSpy.createProperty.mockReturnValue(throwError(() => errorResponse));

    component.form.setValue({
      name: 'Test Property',
      street: '123 Test St',
      city: 'Austin',
      state: 'TX',
      zipCode: '78701',
    });

    component.onSubmit();

    expect(component.serverErrors()).toEqual({ general: ['Something went wrong'] });
  });

  it('should validate name is required', () => {
    const nameControl = component.form.get('name');
    nameControl?.setValue('');
    expect(nameControl?.hasError('required')).toBe(true);
  });

  it('should validate ZIP code format', () => {
    const zipControl = component.form.get('zipCode');

    zipControl?.setValue('1234');
    expect(zipControl?.hasError('pattern')).toBe(true);

    zipControl?.setValue('ABCDE');
    expect(zipControl?.hasError('pattern')).toBe(true);

    zipControl?.setValue('78701');
    expect(zipControl?.hasError('pattern')).toBe(false);
  });

  it('should validate state is required', () => {
    const stateControl = component.form.get('state');
    stateControl?.setValue('');
    expect(stateControl?.hasError('required')).toBe(true);
  });
});
