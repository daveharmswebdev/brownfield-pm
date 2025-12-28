import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { signal } from '@angular/core';
import { Location } from '@angular/common';
import { PropertyDetailComponent } from './property-detail.component';
import { PropertyStore } from '../stores/property.store';
import { PropertyService, PropertyDetailDto } from '../services/property.service';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialogModule } from '@angular/material/dialog';

describe('PropertyDetailComponent', () => {
  let component: PropertyDetailComponent;
  let fixture: ComponentFixture<PropertyDetailComponent>;
  let mockPropertyStore: {
    selectedProperty: ReturnType<typeof signal>;
    isLoadingDetail: ReturnType<typeof signal>;
    detailError: ReturnType<typeof signal>;
    selectedPropertyNetIncome: ReturnType<typeof signal>;
    selectedPropertyFullAddress: ReturnType<typeof signal>;
    isDeleting: ReturnType<typeof signal>;
    deleteError: ReturnType<typeof signal>;
    loadPropertyById: ReturnType<typeof vi.fn>;
    clearSelectedProperty: ReturnType<typeof vi.fn>;
    deleteProperty: ReturnType<typeof vi.fn>;
  };
  let mockRouter: { navigate: ReturnType<typeof vi.fn> };
  let mockLocation: { back: ReturnType<typeof vi.fn> };
  let mockPropertyService: { getPropertyById: ReturnType<typeof vi.fn> };

  const mockProperty: PropertyDetailDto = {
    id: 'test-property-id',
    name: 'Oak Street Duplex',
    street: '123 Oak St',
    city: 'Austin',
    state: 'TX',
    zipCode: '78701',
    expenseTotal: 1500,
    incomeTotal: 3000,
    createdAt: '2025-01-15T10:30:00Z',
    updatedAt: '2025-01-20T14:45:00Z',
    recentExpenses: [],
    recentIncome: [],
  };

  beforeEach(async () => {
    mockPropertyStore = {
      selectedProperty: signal<PropertyDetailDto | null>(null),
      isLoadingDetail: signal(false),
      detailError: signal<string | null>(null),
      selectedPropertyNetIncome: signal(0),
      selectedPropertyFullAddress: signal(''),
      isDeleting: signal(false),
      deleteError: signal<string | null>(null),
      loadPropertyById: vi.fn(),
      clearSelectedProperty: vi.fn(),
      deleteProperty: vi.fn(),
    };

    mockRouter = {
      navigate: vi.fn(),
    };

    mockLocation = {
      back: vi.fn(),
    };

    mockPropertyService = {
      getPropertyById: vi.fn().mockReturnValue(of(mockProperty)),
    };

    await TestBed.configureTestingModule({
      imports: [
        PropertyDetailComponent,
        NoopAnimationsModule,
        MatDialogModule,
      ],
      providers: [
        { provide: PropertyStore, useValue: mockPropertyStore },
        { provide: Router, useValue: mockRouter },
        { provide: Location, useValue: mockLocation },
        { provide: PropertyService, useValue: mockPropertyService },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap({ id: 'test-property-id' }),
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PropertyDetailComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('initialization', () => {
    it('should load property on init using route param', () => {
      fixture.detectChanges();
      // loadPropertyById now takes { id, year } object (AC-3.5.6)
      expect(mockPropertyStore.loadPropertyById).toHaveBeenCalledWith(
        expect.objectContaining({ id: 'test-property-id' })
      );
    });

    it('should clear selected property on destroy', () => {
      fixture.detectChanges();
      fixture.destroy();
      expect(mockPropertyStore.clearSelectedProperty).toHaveBeenCalled();
    });
  });

  describe('loading state', () => {
    it('should show spinner when loading', () => {
      mockPropertyStore.isLoadingDetail = signal(true);
      fixture = TestBed.createComponent(PropertyDetailComponent);
      fixture.detectChanges();

      const spinner = fixture.nativeElement.querySelector('mat-spinner');
      expect(spinner).toBeTruthy();
    });

    it('should hide spinner when not loading', () => {
      mockPropertyStore.isLoadingDetail = signal(false);
      mockPropertyStore.selectedProperty = signal(mockProperty);
      mockPropertyStore.selectedPropertyFullAddress = signal('123 Oak St, Austin, TX 78701');
      mockPropertyStore.selectedPropertyNetIncome = signal(1500);
      fixture = TestBed.createComponent(PropertyDetailComponent);
      fixture.detectChanges();

      const spinner = fixture.nativeElement.querySelector('mat-spinner');
      expect(spinner).toBeFalsy();
    });
  });

  describe('error state (AC-2.3.6)', () => {
    it('should show error message when property not found', () => {
      mockPropertyStore.detailError = signal('Property not found');
      fixture = TestBed.createComponent(PropertyDetailComponent);
      fixture.detectChanges();

      const errorCard = fixture.nativeElement.querySelector('.error-card');
      expect(errorCard).toBeTruthy();
      expect(errorCard.textContent).toContain('Property not found');
    });

    it('should show go back button on error', () => {
      mockPropertyStore.detailError = signal('Property not found');
      fixture = TestBed.createComponent(PropertyDetailComponent);
      fixture.detectChanges();

      const backButton = fixture.nativeElement.querySelector('.error-card button');
      expect(backButton).toBeTruthy();
      expect(backButton.textContent).toContain('Go Back');
    });

    it('should navigate to properties list when go back button clicked on error', () => {
      mockPropertyStore.detailError = signal('Property not found');
      fixture = TestBed.createComponent(PropertyDetailComponent);
      fixture.detectChanges();

      const backButton = fixture.nativeElement.querySelector('.error-card button');
      backButton.click();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/properties']);
    });
  });

  describe('property display (AC-2.3.2)', () => {
    beforeEach(() => {
      mockPropertyStore.selectedProperty = signal(mockProperty);
      mockPropertyStore.selectedPropertyFullAddress = signal('123 Oak St, Austin, TX 78701');
      mockPropertyStore.selectedPropertyNetIncome = signal(1500);
      fixture = TestBed.createComponent(PropertyDetailComponent);
      fixture.detectChanges();
    });

    it('should display property name as title', () => {
      const title = fixture.nativeElement.querySelector('h1');
      expect(title.textContent).toContain('Oak Street Duplex');
    });

    it('should display full address', () => {
      const address = fixture.nativeElement.querySelector('.address');
      expect(address.textContent).toContain('123 Oak St, Austin, TX 78701');
    });

    it('should display YTD expenses', () => {
      const expenseCard = fixture.nativeElement.querySelector('.expense-card');
      expect(expenseCard.textContent).toContain('YTD Expenses');
      expect(expenseCard.textContent).toContain('$1,500');
    });

    it('should display YTD income', () => {
      const incomeCard = fixture.nativeElement.querySelector('.income-card');
      expect(incomeCard.textContent).toContain('YTD Income');
      expect(incomeCard.textContent).toContain('$3,000');
    });

    it('should display net income', () => {
      const netCard = fixture.nativeElement.querySelector('.net-card');
      expect(netCard.textContent).toContain('Net Income');
      expect(netCard.textContent).toContain('$1,500');
    });
  });

  describe('empty states (AC-2.3.3)', () => {
    beforeEach(() => {
      mockPropertyStore.selectedProperty = signal(mockProperty);
      mockPropertyStore.selectedPropertyFullAddress = signal('123 Oak St, Austin, TX 78701');
      mockPropertyStore.selectedPropertyNetIncome = signal(1500);
      fixture = TestBed.createComponent(PropertyDetailComponent);
      fixture.detectChanges();
    });

    it('should show empty state for recent expenses', () => {
      const emptyState = fixture.nativeElement.querySelector('.activity-card .empty-state');
      expect(emptyState.textContent).toContain('No expenses yet');
    });

    it('should show empty state for recent income', () => {
      const activityCards = fixture.nativeElement.querySelectorAll('.activity-card');
      const incomeCard = activityCards[1];
      expect(incomeCard.textContent).toContain('No income recorded yet');
    });
  });

  describe('action buttons (AC-2.3.4)', () => {
    beforeEach(() => {
      mockPropertyStore.selectedProperty = signal(mockProperty);
      mockPropertyStore.selectedPropertyFullAddress = signal('123 Oak St, Austin, TX 78701');
      mockPropertyStore.selectedPropertyNetIncome = signal(1500);
      fixture = TestBed.createComponent(PropertyDetailComponent);
      fixture.detectChanges();
    });

    it('should have Add Expense button enabled (AC-3.1.1)', () => {
      const buttons = fixture.nativeElement.querySelectorAll('.action-buttons button') as NodeListOf<HTMLButtonElement>;
      const addExpenseBtn = Array.from(buttons).find((btn) =>
        btn.textContent?.includes('Add Expense')
      );
      expect(addExpenseBtn).toBeTruthy();
      expect(addExpenseBtn!.disabled).toBe(false);
    });

    it('should have Add Income button enabled (AC-4.1.1)', () => {
      const buttons = fixture.nativeElement.querySelectorAll('.action-buttons button') as NodeListOf<HTMLButtonElement>;
      const addIncomeBtn = Array.from(buttons).find((btn) =>
        btn.textContent?.includes('Add Income')
      );
      expect(addIncomeBtn).toBeTruthy();
      expect(addIncomeBtn!.disabled).toBe(false);
    });

    it('should have Edit button enabled', () => {
      const buttons = fixture.nativeElement.querySelectorAll('.action-buttons button') as NodeListOf<HTMLButtonElement>;
      const editBtn = Array.from(buttons).find((btn) =>
        btn.textContent?.includes('Edit')
      );
      expect(editBtn).toBeTruthy();
      expect(editBtn!.disabled).toBe(false);
    });

    it('should have Delete button enabled', () => {
      const buttons = fixture.nativeElement.querySelectorAll('.action-buttons button') as NodeListOf<HTMLButtonElement>;
      const deleteBtn = Array.from(buttons).find((btn) =>
        btn.textContent?.includes('Delete')
      );
      expect(deleteBtn).toBeTruthy();
      expect(deleteBtn!.disabled).toBe(false);
    });
  });

  describe('navigation (AC-2.3.1)', () => {
    beforeEach(() => {
      mockPropertyStore.selectedProperty = signal(mockProperty);
      mockPropertyStore.selectedPropertyFullAddress = signal('123 Oak St, Austin, TX 78701');
      mockPropertyStore.selectedPropertyNetIncome = signal(1500);
      fixture = TestBed.createComponent(PropertyDetailComponent);
      fixture.detectChanges();
    });

    it('should have back button', () => {
      const backButton = fixture.nativeElement.querySelector('.back-button');
      expect(backButton).toBeTruthy();
      expect(backButton.getAttribute('aria-label')).toBe('Go back');
    });

    it('should navigate to properties list when back button clicked', () => {
      const backButton = fixture.nativeElement.querySelector('.back-button');
      backButton.click();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/properties']);
    });
  });

  describe('delete flow (AC-2.5.1)', () => {
    beforeEach(() => {
      mockPropertyStore.selectedProperty = signal(mockProperty);
      mockPropertyStore.selectedPropertyFullAddress = signal('123 Oak St, Austin, TX 78701');
      mockPropertyStore.selectedPropertyNetIncome = signal(1500);
      mockPropertyStore.isDeleting = signal(false);
      mockPropertyStore.deleteError = signal<string | null>(null);
    });

    it('should call onDeleteClick method when delete button clicked', () => {
      fixture = TestBed.createComponent(PropertyDetailComponent);
      fixture.detectChanges();

      // Mock onDeleteClick to prevent actual dialog opening
      const onDeleteClickSpy = vi.spyOn(fixture.componentInstance, 'onDeleteClick').mockImplementation(() => {});

      const buttons = fixture.nativeElement.querySelectorAll('.action-buttons button') as NodeListOf<HTMLButtonElement>;
      const deleteBtn = Array.from(buttons).find((btn) =>
        btn.textContent?.includes('Delete')
      );

      deleteBtn!.click();

      expect(onDeleteClickSpy).toHaveBeenCalled();
    });

    it('should disable delete button when isDeleting is true', () => {
      mockPropertyStore.isDeleting = signal(true);

      fixture = TestBed.createComponent(PropertyDetailComponent);
      fixture.detectChanges();

      const buttons = fixture.nativeElement.querySelectorAll('.action-buttons button') as NodeListOf<HTMLButtonElement>;
      const deleteBtn = Array.from(buttons).find((btn) =>
        btn.textContent?.includes('Delete')
      );

      expect(deleteBtn!.disabled).toBe(true);
    });

    it('should show spinner on delete button when isDeleting is true', () => {
      mockPropertyStore.isDeleting = signal(true);

      fixture = TestBed.createComponent(PropertyDetailComponent);
      fixture.detectChanges();

      const buttons = fixture.nativeElement.querySelectorAll('.action-buttons button') as NodeListOf<HTMLButtonElement>;
      const deleteBtn = Array.from(buttons).find((btn) =>
        btn.textContent?.includes('Delete')
      );

      // Check for spinner inside delete button
      const spinner = deleteBtn!.querySelector('mat-spinner');
      expect(spinner).toBeTruthy();
    });
  });
});
