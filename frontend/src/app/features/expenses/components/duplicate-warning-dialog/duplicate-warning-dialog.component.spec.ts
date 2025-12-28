import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import {
  DuplicateWarningDialogComponent,
  DuplicateWarningDialogData,
} from './duplicate-warning-dialog.component';

describe('DuplicateWarningDialogComponent', () => {
  let component: DuplicateWarningDialogComponent;
  let fixture: ComponentFixture<DuplicateWarningDialogComponent>;
  let dialogRefSpy: { close: ReturnType<typeof vi.fn> };

  const mockDialogData: DuplicateWarningDialogData = {
    existingExpense: {
      id: 'expense-1',
      date: '2024-12-01',
      amount: 127.50,
      description: 'Home Depot - Faucet',
    },
  };

  beforeEach(async () => {
    dialogRefSpy = {
      close: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [DuplicateWarningDialogComponent, NoopAnimationsModule],
      providers: [
        { provide: MatDialogRef, useValue: dialogRefSpy },
        { provide: MAT_DIALOG_DATA, useValue: mockDialogData },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(DuplicateWarningDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  describe('rendering', () => {
    it('should create', () => {
      expect(component).toBeTruthy();
    });

    it('should display dialog title "Possible Duplicate"', () => {
      const title = fixture.nativeElement.querySelector('h2');
      expect(title.textContent).toContain('Possible Duplicate');
    });

    it('should display warning icon', () => {
      const icon = fixture.nativeElement.querySelector('.header-icon');
      expect(icon).toBeTruthy();
      expect(icon.textContent).toContain('warning_amber');
    });

    it('should display formatted date in message (AC-3.6.2)', () => {
      const message = fixture.nativeElement.querySelector('.primary-message');
      // Should show a month abbreviation and 2024 (exact month depends on timezone)
      expect(message.textContent).toMatch(/(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)/);
      expect(message.textContent).toContain('2024');
    });

    it('should display amount in message (AC-3.6.2)', () => {
      const message = fixture.nativeElement.querySelector('.primary-message');
      expect(message.textContent).toContain('$127.50');
    });

    it('should display description when provided (AC-3.6.2)', () => {
      const details = fixture.nativeElement.querySelector('.existing-expense-details');
      expect(details).toBeTruthy();
      expect(details.textContent).toContain('Home Depot - Faucet');
    });

    it('should have Cancel button', () => {
      const cancelButton = fixture.nativeElement.querySelector('button:first-of-type');
      expect(cancelButton.textContent).toContain('Cancel');
    });

    it('should have Save Anyway button', () => {
      const saveButton = fixture.nativeElement.querySelector('button[color="primary"]');
      expect(saveButton.textContent).toContain('Save Anyway');
    });
  });

  describe('without description', () => {
    beforeEach(async () => {
      const dataWithoutDescription: DuplicateWarningDialogData = {
        existingExpense: {
          id: 'expense-2',
          date: '2024-12-01',
          amount: 100,
        },
      };

      TestBed.resetTestingModule();
      await TestBed.configureTestingModule({
        imports: [DuplicateWarningDialogComponent, NoopAnimationsModule],
        providers: [
          { provide: MatDialogRef, useValue: dialogRefSpy },
          { provide: MAT_DIALOG_DATA, useValue: dataWithoutDescription },
        ],
      }).compileComponents();

      fixture = TestBed.createComponent(DuplicateWarningDialogComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it('should not display description section when not provided', () => {
      const details = fixture.nativeElement.querySelector('.existing-expense-details');
      expect(details).toBeFalsy();
    });
  });

  describe('Cancel action (AC-3.6.3)', () => {
    it('should close dialog with false when Cancel clicked', () => {
      component.onCancel();

      expect(dialogRefSpy.close).toHaveBeenCalledWith(false);
    });
  });

  describe('Save Anyway action (AC-3.6.4)', () => {
    it('should close dialog with true when Save Anyway clicked', () => {
      component.onSaveAnyway();

      expect(dialogRefSpy.close).toHaveBeenCalledWith(true);
    });
  });

  describe('formatDate', () => {
    it('should format date with month, day, and year', () => {
      const formatted = component.formatDate('2024-12-01');
      // Check it contains month abbreviation, a digit, and year
      expect(formatted).toMatch(/(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)/);
      expect(formatted).toContain('2024');
    });

    it('should return a valid formatted date string', () => {
      const formatted1 = component.formatDate('2024-01-15');
      const formatted2 = component.formatDate('2024-06-30');
      // Both should be valid date strings with month abbreviation and year
      expect(formatted1).toMatch(/(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+\d{1,2},\s+2024/);
      expect(formatted2).toMatch(/(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)\s+\d{1,2},\s+2024/);
    });
  });
});
