import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi, afterEach } from 'vitest';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { By } from '@angular/platform-browser';
import { signal, computed } from '@angular/core';
import { DashboardComponent } from './dashboard.component';
import { AuthService, User } from '../../core/services/auth.service';
import { PropertyStore } from '../properties/stores/property.store';
import { PropertySummaryDto } from '../properties/services/property.service';

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;
  let router: Router;

  const mockUser: User = {
    userId: 'test-user-id',
    accountId: 'test-account-id',
    role: 'Owner',
  };

  const mockProperties: PropertySummaryDto[] = [
    {
      id: 'prop-1',
      name: 'Oak Street Duplex',
      street: '123 Oak St',
      city: 'Austin',
      state: 'TX',
      zipCode: '78701',
      expenseTotal: 1500,
      incomeTotal: 3000,
    },
    {
      id: 'prop-2',
      name: 'Pine Avenue House',
      street: '456 Pine Ave',
      city: 'Dallas',
      state: 'TX',
      zipCode: '75201',
      expenseTotal: 800,
      incomeTotal: 2000,
    },
  ];

  function createMockPropertyStore(
    properties: PropertySummaryDto[] = [],
    isLoading = false,
    error: string | null = null
  ) {
    const propertiesSignal = signal(properties);
    const isLoadingSignal = signal(isLoading);
    const errorSignal = signal(error);
    const selectedYearSignal = signal<number | null>(null);

    return {
      properties: propertiesSignal,
      isLoading: isLoadingSignal,
      error: errorSignal,
      selectedYear: selectedYearSignal,
      totalCount: computed(() => propertiesSignal().length),
      totalExpenses: computed(() =>
        propertiesSignal().reduce((sum, p) => sum + p.expenseTotal, 0)
      ),
      totalIncome: computed(() =>
        propertiesSignal().reduce((sum, p) => sum + p.incomeTotal, 0)
      ),
      netIncome: computed(() => {
        const income = propertiesSignal().reduce((sum, p) => sum + p.incomeTotal, 0);
        const expenses = propertiesSignal().reduce((sum, p) => sum + p.expenseTotal, 0);
        return income - expenses;
      }),
      isEmpty: computed(() => propertiesSignal().length === 0),
      hasProperties: computed(() => !isLoadingSignal() && propertiesSignal().length > 0),
      loadProperties: vi.fn(),
      clearError: vi.fn(),
      reset: vi.fn(),
      setSelectedYear: vi.fn(),
    };
  }

  async function setupTest(
    properties: PropertySummaryDto[] = [],
    isLoading = false,
    error: string | null = null
  ) {
    const mockAuthService = {
      currentUser: signal<User | null>(mockUser),
    };

    const mockPropertyStore = createMockPropertyStore(properties, isLoading, error);

    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [DashboardComponent, NoopAnimationsModule],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AuthService, useValue: mockAuthService },
        { provide: PropertyStore, useValue: mockPropertyStore },
      ],
    }).compileComponents();

    router = TestBed.inject(Router);
    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    return { mockPropertyStore };
  }

  afterEach(() => {
    TestBed.resetTestingModule();
  });

  it('should create', async () => {
    await setupTest();
    expect(component).toBeTruthy();
  });

  it('should load properties on init', async () => {
    const { mockPropertyStore } = await setupTest();
    expect(mockPropertyStore.loadProperties).toHaveBeenCalled();
  });

  it('should display welcome header', async () => {
    await setupTest();
    const header = fixture.debugElement.query(By.css('.dashboard-header h1'));
    expect(header.nativeElement.textContent).toContain('Welcome back');
  });

  it('should have Add Property button in header (AC-2.1.1)', async () => {
    await setupTest();
    const button = fixture.debugElement.query(By.css('.dashboard-header button'));
    expect(button.nativeElement.textContent).toContain('Add Property');
  });

  describe('stats bar (AC-2.2.1)', () => {
    it('should display stats bar component', async () => {
      await setupTest();
      const statsBar = fixture.debugElement.query(By.css('app-stats-bar'));
      expect(statsBar).toBeTruthy();
    });

    it('should display expense total in stats bar', async () => {
      await setupTest(mockProperties);
      // Check that the stats bar renders with the correct expense total (1500 + 800 = 2300)
      const expenseCard = fixture.nativeElement.querySelector('.expense-card .stat-value');
      expect(expenseCard.textContent).toContain('$2,300.00');
    });

    it('should display income total in stats bar', async () => {
      await setupTest(mockProperties);
      // Check that the stats bar renders with the correct income total (3000 + 2000 = 5000)
      const incomeCard = fixture.nativeElement.querySelector('.income-card .stat-value');
      expect(incomeCard.textContent).toContain('$5,000.00');
    });
  });

  describe('empty state (AC-2.2.3)', () => {
    it('should display empty state with "No properties yet"', async () => {
      await setupTest([]);
      const heading = fixture.debugElement.query(By.css('.empty-state-card h2'));
      expect(heading.nativeElement.textContent).toContain('No properties yet');
    });

    it('should display help message', async () => {
      await setupTest([]);
      const message = fixture.debugElement.query(By.css('.empty-state-card p'));
      expect(message.nativeElement.textContent).toContain('Add your first property to get started');
    });

    it('should have Add Property button in empty state', async () => {
      await setupTest([]);
      const button = fixture.debugElement.query(By.css('.empty-state-card button'));
      expect(button.nativeElement.textContent).toContain('Add Property');
    });

    it('should have mat-card for empty state content', async () => {
      await setupTest([]);
      const card = fixture.debugElement.query(By.css('.empty-state-card'));
      expect(card).toBeTruthy();
    });
  });

  describe('with properties (AC-2.2.2, AC-2.2.4)', () => {
    it('should display properties list when properties exist', async () => {
      await setupTest(mockProperties);
      const listCard = fixture.debugElement.query(By.css('.properties-list-card'));
      expect(listCard).toBeTruthy();
    });

    it('should show correct property count', async () => {
      await setupTest(mockProperties);
      const subtitle = fixture.debugElement.query(By.css('mat-card-subtitle'));
      expect(subtitle.nativeElement.textContent).toContain('2 properties');
    });

    it('should render PropertyRowComponent for each property', async () => {
      await setupTest(mockProperties);
      const rows = fixture.debugElement.queryAll(By.css('app-property-row'));
      expect(rows.length).toBe(2);
    });

    it('should not show empty state when properties exist', async () => {
      await setupTest(mockProperties);
      const emptyCard = fixture.debugElement.query(By.css('.empty-state-card'));
      expect(emptyCard).toBeFalsy();
    });

    it('should navigate to property detail on row click', async () => {
      await setupTest(mockProperties);
      const navigateSpy = vi.spyOn(router, 'navigate');
      component.navigateToProperty('prop-1');
      expect(navigateSpy).toHaveBeenCalledWith(['/properties', 'prop-1']);
    });
  });

  describe('loading state', () => {
    it('should show loading spinner while fetching', async () => {
      await setupTest([], true);
      const spinner = fixture.debugElement.query(By.css('mat-spinner'));
      expect(spinner).toBeTruthy();
    });

    it('should not show properties list while loading', async () => {
      await setupTest([], true);
      const listCard = fixture.debugElement.query(By.css('.properties-list-card'));
      expect(listCard).toBeFalsy();
    });
  });

  describe('error state', () => {
    it('should display error message on API failure', async () => {
      await setupTest([], false, 'Failed to load properties. Please try again.');
      const errorCard = fixture.debugElement.query(By.css('.error-card'));
      expect(errorCard).toBeTruthy();
      expect(errorCard.nativeElement.textContent).toContain('Failed to load properties');
    });

    it('should have retry button on error', async () => {
      await setupTest([], false, 'Failed to load properties. Please try again.');
      const retryButton = fixture.debugElement.query(By.css('.error-card button'));
      expect(retryButton.nativeElement.textContent).toContain('Try Again');
    });

    it('should reload properties when Try Again is clicked', async () => {
      const { mockPropertyStore } = await setupTest([], false, 'Failed to load properties. Please try again.');
      const retryButton = fixture.debugElement.query(By.css('.error-card button'));
      retryButton.nativeElement.click();
      // loadProperties is called once on init, and once on retry
      expect(mockPropertyStore.loadProperties).toHaveBeenCalledTimes(2);
    });
  });

  describe('singular/plural property count', () => {
    it('should display "property" for single property', async () => {
      await setupTest([mockProperties[0]]);
      const subtitle = fixture.debugElement.query(By.css('mat-card-subtitle'));
      expect(subtitle.nativeElement.textContent).toContain('1 property');
    });

    it('should display "properties" for multiple properties', async () => {
      await setupTest(mockProperties);
      const subtitle = fixture.debugElement.query(By.css('mat-card-subtitle'));
      expect(subtitle.nativeElement.textContent).toContain('2 properties');
    });
  });
});
