import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StatsBarComponent } from './stats-bar.component';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

describe('StatsBarComponent', () => {
  let component: StatsBarComponent;
  let fixture: ComponentFixture<StatsBarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StatsBarComponent, NoopAnimationsModule],
    }).compileComponents();

    fixture = TestBed.createComponent(StatsBarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display three stat cards', () => {
    const cards = fixture.nativeElement.querySelectorAll('.stat-card');
    expect(cards.length).toBe(3);
  });

  it('should display $0.00 values by default', () => {
    const values = fixture.nativeElement.querySelectorAll('.stat-value');
    expect(values.length).toBe(3);
    values.forEach((value: HTMLElement) => {
      expect(value.textContent?.trim()).toBe('$0.00');
    });
  });

  it('should display correct labels', () => {
    const labels = fixture.nativeElement.querySelectorAll('.stat-label');
    expect(labels[0].textContent).toContain('Total Expenses YTD');
    expect(labels[1].textContent).toContain('Total Income YTD');
    expect(labels[2].textContent).toContain('Net Income YTD');
  });

  it('should calculate net income correctly', () => {
    expect(component.netIncome()).toBe(0);
  });

  it('should format expense total as currency', async () => {
    fixture.componentRef.setInput('expenseTotal', 1234.56);
    fixture.detectChanges();

    const expenseValue = fixture.nativeElement.querySelector('.expense-card .stat-value');
    expect(expenseValue.textContent?.trim()).toBe('$1,234.56');
  });

  it('should format income total as currency', async () => {
    fixture.componentRef.setInput('incomeTotal', 5678.90);
    fixture.detectChanges();

    const incomeValue = fixture.nativeElement.querySelector('.income-card .stat-value');
    expect(incomeValue.textContent?.trim()).toBe('$5,678.90');
  });

  it('should calculate positive net income (income > expenses)', async () => {
    fixture.componentRef.setInput('incomeTotal', 10000);
    fixture.componentRef.setInput('expenseTotal', 3000);
    fixture.detectChanges();

    expect(component.netIncome()).toBe(7000);

    const netCard = fixture.nativeElement.querySelector('.net-card');
    expect(netCard.classList.contains('positive')).toBe(true);
    expect(netCard.classList.contains('negative')).toBe(false);
  });

  it('should calculate negative net income (expenses > income)', async () => {
    fixture.componentRef.setInput('incomeTotal', 2000);
    fixture.componentRef.setInput('expenseTotal', 5000);
    fixture.detectChanges();

    expect(component.netIncome()).toBe(-3000);

    const netCard = fixture.nativeElement.querySelector('.net-card');
    expect(netCard.classList.contains('negative')).toBe(true);
    expect(netCard.classList.contains('positive')).toBe(false);
  });

  it('should display zero net income when both are zero', () => {
    expect(component.netIncome()).toBe(0);

    const netValue = fixture.nativeElement.querySelector('.net-card .stat-value');
    expect(netValue.textContent?.trim()).toBe('$0.00');
  });

  it('should handle large numbers with proper formatting', async () => {
    fixture.componentRef.setInput('expenseTotal', 1234567.89);
    fixture.componentRef.setInput('incomeTotal', 9876543.21);
    fixture.detectChanges();

    const expenseValue = fixture.nativeElement.querySelector('.expense-card .stat-value');
    const incomeValue = fixture.nativeElement.querySelector('.income-card .stat-value');

    expect(expenseValue.textContent?.trim()).toBe('$1,234,567.89');
    expect(incomeValue.textContent?.trim()).toBe('$9,876,543.21');
  });

  it('should display appropriate icons', () => {
    const icons = fixture.nativeElement.querySelectorAll('.stat-icon');
    expect(icons[0].textContent?.trim()).toBe('trending_down');
    expect(icons[1].textContent?.trim()).toBe('trending_up');
    expect(icons[2].textContent?.trim()).toBe('account_balance');
  });

  it('should show warning icon for negative net income', async () => {
    fixture.componentRef.setInput('expenseTotal', 5000);
    fixture.componentRef.setInput('incomeTotal', 1000);
    fixture.detectChanges();

    const netIcon = fixture.nativeElement.querySelector('.net-card .stat-icon');
    expect(netIcon.textContent?.trim()).toBe('warning');
  });
});
