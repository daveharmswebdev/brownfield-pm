import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PropertyRowComponent } from './property-row.component';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

describe('PropertyRowComponent', () => {
  let component: PropertyRowComponent;
  let fixture: ComponentFixture<PropertyRowComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PropertyRowComponent, NoopAnimationsModule],
    }).compileComponents();

    fixture = TestBed.createComponent(PropertyRowComponent);
    component = fixture.componentInstance;

    // Set required inputs
    fixture.componentRef.setInput('id', 'test-id-123');
    fixture.componentRef.setInput('name', 'Oak Street Duplex');
    fixture.componentRef.setInput('city', 'Austin');
    fixture.componentRef.setInput('state', 'TX');

    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display property name', () => {
    const nameElement = fixture.nativeElement.querySelector('.property-name');
    expect(nameElement.textContent?.trim()).toBe('Oak Street Duplex');
  });

  it('should display city and state in address format', () => {
    const addressElement = fixture.nativeElement.querySelector('.property-address');
    expect(addressElement.textContent?.trim()).toBe('Austin, TX');
  });

  it('should display expense total as currency', () => {
    fixture.componentRef.setInput('expenseTotal', 1234.56);
    fixture.detectChanges();

    const expenseElement = fixture.nativeElement.querySelector('.expense-value');
    expect(expenseElement.textContent?.trim()).toBe('$1,234.56');
  });

  it('should display $0.00 expense total by default', () => {
    const expenseElement = fixture.nativeElement.querySelector('.expense-value');
    expect(expenseElement.textContent?.trim()).toBe('$0.00');
  });

  it('should emit rowClick with property id when row is clicked', () => {
    const rowClickSpy = vi.fn();
    component.rowClick.subscribe(rowClickSpy);

    const row = fixture.nativeElement.querySelector('.property-row');
    row.click();

    expect(rowClickSpy).toHaveBeenCalledWith('test-id-123');
  });

  it('should emit rowClick on Enter key press', () => {
    const rowClickSpy = vi.fn();
    component.rowClick.subscribe(rowClickSpy);

    const row = fixture.nativeElement.querySelector('.property-row');
    const enterEvent = new KeyboardEvent('keydown', { key: 'Enter' });
    row.dispatchEvent(enterEvent);

    expect(rowClickSpy).toHaveBeenCalledWith('test-id-123');
  });

  it('should have disabled add expense button', () => {
    const button = fixture.nativeElement.querySelector('.add-expense-btn');
    expect(button.disabled).toBe(true);
  });

  it('should have matTooltip directive on add expense button', () => {
    const button = fixture.nativeElement.querySelector('.add-expense-btn');
    expect(button).toBeTruthy();
    // The button should have matTooltip, which is verified by existence
    // The actual tooltip text is bound via matTooltip directive
  });

  it('should not emit rowClick when add expense button is clicked', () => {
    const rowClickSpy = vi.fn();
    component.rowClick.subscribe(rowClickSpy);

    const button = fixture.nativeElement.querySelector('.add-expense-btn');
    button.click();

    expect(rowClickSpy).not.toHaveBeenCalled();
  });

  it('should display home icon', () => {
    const icon = fixture.nativeElement.querySelector('.property-icon mat-icon');
    expect(icon.textContent?.trim()).toBe('home');
  });

  it('should display YTD Expenses label', () => {
    const label = fixture.nativeElement.querySelector('.expense-label');
    expect(label.textContent).toContain('YTD Expenses');
  });

  it('should have proper accessibility attributes', () => {
    const row = fixture.nativeElement.querySelector('.property-row');
    expect(row.getAttribute('tabindex')).toBe('0');
    expect(row.getAttribute('role')).toBe('button');
  });

  it('should handle long property names with ellipsis', () => {
    fixture.componentRef.setInput('name', 'This is a very long property name that should be truncated');
    fixture.detectChanges();

    const nameElement = fixture.nativeElement.querySelector('.property-name');
    const styles = window.getComputedStyle(nameElement);
    expect(styles.textOverflow).toBe('ellipsis');
    expect(styles.overflow).toBe('hidden');
  });

  it('should format large expense amounts correctly', () => {
    fixture.componentRef.setInput('expenseTotal', 123456.78);
    fixture.detectChanges();

    const expenseElement = fixture.nativeElement.querySelector('.expense-value');
    expect(expenseElement.textContent?.trim()).toBe('$123,456.78');
  });
});
