import { Component } from '@angular/core';
import { TestBed, ComponentFixture } from '@angular/core/testing';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { By } from '@angular/platform-browser';
import { CurrencyInputDirective } from './currency-input.directive';

// Test host component for testing the directive with reactive forms
@Component({
  standalone: true,
  imports: [CurrencyInputDirective, ReactiveFormsModule],
  template: `
    <input
      appCurrencyInput
      [formControl]="amountControl"
      [decimals]="decimals"
      [allowNegative]="allowNegative"
    />
  `,
})
class TestHostComponent {
  amountControl = new FormControl<number | null>(null);
  decimals = 2;
  allowNegative = false;
}

describe('CurrencyInputDirective', () => {
  let fixture: ComponentFixture<TestHostComponent>;
  let component: TestHostComponent;
  let inputEl: HTMLInputElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    inputEl = fixture.debugElement.query(By.css('input')).nativeElement;
  });

  describe('initialization', () => {
    it('should create directive on input element', () => {
      const directive = fixture.debugElement.query(
        By.directive(CurrencyInputDirective)
      );
      expect(directive).toBeTruthy();
    });

    it('should set input type to text', () => {
      expect(inputEl.type).toBe('text');
    });

    it('should set inputmode to decimal for mobile keyboards', () => {
      expect(inputEl.getAttribute('inputmode')).toBe('decimal');
    });
  });

  describe('display formatting', () => {
    it('should display empty string when value is null', () => {
      component.amountControl.setValue(null);
      fixture.detectChanges();

      expect(inputEl.value).toBe('');
    });

    it('should display formatted value with 2 decimal places', () => {
      component.amountControl.setValue(15);
      fixture.detectChanges();

      expect(inputEl.value).toBe('15.00');
    });

    it('should display formatted value for decimal numbers', () => {
      component.amountControl.setValue(15.5);
      fixture.detectChanges();

      expect(inputEl.value).toBe('15.50');
    });

    it('should display formatted value preserving existing decimals', () => {
      component.amountControl.setValue(15.99);
      fixture.detectChanges();

      expect(inputEl.value).toBe('15.99');
    });

    it('should round to specified decimal places', () => {
      component.amountControl.setValue(15.999);
      fixture.detectChanges();

      expect(inputEl.value).toBe('16.00');
    });

    it('should handle zero correctly', () => {
      component.amountControl.setValue(0);
      fixture.detectChanges();

      expect(inputEl.value).toBe('0.00');
    });

    it('should handle large numbers', () => {
      component.amountControl.setValue(999999.99);
      fixture.detectChanges();

      expect(inputEl.value).toBe('999999.99');
    });
  });

  describe('focus behavior', () => {
    it('should show raw number without trailing zeros on focus', () => {
      component.amountControl.setValue(15);
      fixture.detectChanges();
      expect(inputEl.value).toBe('15.00');

      inputEl.dispatchEvent(new FocusEvent('focus'));
      fixture.detectChanges();

      expect(inputEl.value).toBe('15');
    });

    it('should show decimal value as-is on focus', () => {
      component.amountControl.setValue(15.5);
      fixture.detectChanges();

      inputEl.dispatchEvent(new FocusEvent('focus'));
      fixture.detectChanges();

      expect(inputEl.value).toBe('15.5');
    });

    it('should keep empty value empty on focus', () => {
      component.amountControl.setValue(null);
      fixture.detectChanges();

      inputEl.dispatchEvent(new FocusEvent('focus'));
      fixture.detectChanges();

      expect(inputEl.value).toBe('');
    });
  });

  describe('blur behavior', () => {
    it('should format value with decimals on blur', () => {
      component.amountControl.setValue(15);
      fixture.detectChanges();

      inputEl.dispatchEvent(new FocusEvent('focus'));
      inputEl.value = '25';
      inputEl.dispatchEvent(new Event('input'));
      inputEl.dispatchEvent(new FocusEvent('blur'));
      fixture.detectChanges();

      expect(inputEl.value).toBe('25.00');
    });

    it('should mark control as touched on blur', () => {
      expect(component.amountControl.touched).toBe(false);

      inputEl.dispatchEvent(new FocusEvent('blur'));
      fixture.detectChanges();

      expect(component.amountControl.touched).toBe(true);
    });

    it('should format partial decimal input on blur', () => {
      inputEl.dispatchEvent(new FocusEvent('focus'));
      inputEl.value = '15.5';
      inputEl.dispatchEvent(new Event('input'));
      inputEl.dispatchEvent(new FocusEvent('blur'));
      fixture.detectChanges();

      expect(inputEl.value).toBe('15.50');
    });
  });

  describe('input parsing', () => {
    it('should parse integer input', () => {
      inputEl.value = '42';
      inputEl.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      expect(component.amountControl.value).toBe(42);
    });

    it('should parse decimal input', () => {
      inputEl.value = '42.50';
      inputEl.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      expect(component.amountControl.value).toBe(42.5);
    });

    it('should parse input with leading decimal', () => {
      inputEl.value = '.99';
      inputEl.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      expect(component.amountControl.value).toBe(0.99);
    });

    it('should return null for empty input', () => {
      component.amountControl.setValue(15);
      fixture.detectChanges();

      inputEl.value = '';
      inputEl.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      expect(component.amountControl.value).toBeNull();
    });

    it('should return null for whitespace-only input', () => {
      inputEl.value = '   ';
      inputEl.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      expect(component.amountControl.value).toBeNull();
    });

    it('should handle multiple decimal points by keeping first', () => {
      inputEl.value = '15.50.25';
      inputEl.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      expect(component.amountControl.value).toBe(15.5025);
    });

    it('should strip non-numeric characters', () => {
      inputEl.value = '$1,234.56';
      inputEl.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      expect(component.amountControl.value).toBe(1234.56);
    });
  });

  describe('keyboard filtering', () => {
    function createKeyboardEvent(
      key: string,
      ctrlKey = false
    ): KeyboardEvent & { preventDefaultSpy: ReturnType<typeof vi.fn> } {
      const event = new KeyboardEvent('keydown', {
        key,
        ctrlKey,
        bubbles: true,
        cancelable: true,
      });
      const preventDefaultSpy = vi.fn();
      event.preventDefault = preventDefaultSpy;
      return Object.assign(event, { preventDefaultSpy });
    }

    it('should allow numeric keys', () => {
      const event = createKeyboardEvent('5');
      inputEl.dispatchEvent(event);

      expect(event.preventDefaultSpy).not.toHaveBeenCalled();
    });

    it('should allow decimal point', () => {
      const event = createKeyboardEvent('.');
      inputEl.dispatchEvent(event);

      expect(event.preventDefaultSpy).not.toHaveBeenCalled();
    });

    it('should allow backspace', () => {
      const event = createKeyboardEvent('Backspace');
      inputEl.dispatchEvent(event);

      expect(event.preventDefaultSpy).not.toHaveBeenCalled();
    });

    it('should allow delete', () => {
      const event = createKeyboardEvent('Delete');
      inputEl.dispatchEvent(event);

      expect(event.preventDefaultSpy).not.toHaveBeenCalled();
    });

    it('should allow tab', () => {
      const event = createKeyboardEvent('Tab');
      inputEl.dispatchEvent(event);

      expect(event.preventDefaultSpy).not.toHaveBeenCalled();
    });

    it('should allow arrow keys', () => {
      const leftEvent = createKeyboardEvent('ArrowLeft');
      inputEl.dispatchEvent(leftEvent);
      expect(leftEvent.preventDefaultSpy).not.toHaveBeenCalled();

      const rightEvent = createKeyboardEvent('ArrowRight');
      inputEl.dispatchEvent(rightEvent);
      expect(rightEvent.preventDefaultSpy).not.toHaveBeenCalled();
    });

    it('should allow Ctrl+A for select all', () => {
      const event = createKeyboardEvent('a', true);
      inputEl.dispatchEvent(event);

      expect(event.preventDefaultSpy).not.toHaveBeenCalled();
    });

    it('should allow Ctrl+C for copy', () => {
      const event = createKeyboardEvent('c', true);
      inputEl.dispatchEvent(event);

      expect(event.preventDefaultSpy).not.toHaveBeenCalled();
    });

    it('should allow Ctrl+V for paste', () => {
      const event = createKeyboardEvent('v', true);
      inputEl.dispatchEvent(event);

      expect(event.preventDefaultSpy).not.toHaveBeenCalled();
    });

    it('should prevent letter keys', () => {
      const event = createKeyboardEvent('a');
      inputEl.dispatchEvent(event);

      expect(event.preventDefaultSpy).toHaveBeenCalled();
    });

    it('should prevent special characters', () => {
      const event = createKeyboardEvent('@');
      inputEl.dispatchEvent(event);

      expect(event.preventDefaultSpy).toHaveBeenCalled();
    });

    it('should prevent minus sign by default', () => {
      const event = createKeyboardEvent('-');
      inputEl.dispatchEvent(event);

      expect(event.preventDefaultSpy).toHaveBeenCalled();
    });
  });

  describe('negative values', () => {
    beforeEach(() => {
      component.allowNegative = true;
      fixture.detectChanges();
    });

    it('should allow minus sign when allowNegative is true', () => {
      const event = new KeyboardEvent('keydown', {
        key: '-',
        bubbles: true,
        cancelable: true,
      });
      const preventDefaultSpy = vi.fn();
      event.preventDefault = preventDefaultSpy;
      inputEl.dispatchEvent(event);

      expect(preventDefaultSpy).not.toHaveBeenCalled();
    });

    it('should parse negative values', () => {
      inputEl.value = '-25.50';
      inputEl.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      expect(component.amountControl.value).toBe(-25.5);
    });

    it('should format negative values on blur', () => {
      inputEl.value = '-25';
      inputEl.dispatchEvent(new Event('input'));
      inputEl.dispatchEvent(new FocusEvent('blur'));
      fixture.detectChanges();

      expect(inputEl.value).toBe('-25.00');
    });

    it('should handle minus sign in wrong position', () => {
      inputEl.value = '25-50';
      inputEl.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      // Should strip the minus since it's not at start
      expect(component.amountControl.value).toBe(2550);
    });
  });

  describe('custom decimal places', () => {
    it('should format with 0 decimal places', () => {
      component.decimals = 0;
      fixture.detectChanges();

      component.amountControl.setValue(15.99);
      fixture.detectChanges();

      expect(inputEl.value).toBe('16');
    });

    it('should format with 3 decimal places', () => {
      component.decimals = 3;
      fixture.detectChanges();

      component.amountControl.setValue(15.5);
      fixture.detectChanges();

      expect(inputEl.value).toBe('15.500');
    });

    it('should format with 4 decimal places', () => {
      component.decimals = 4;
      fixture.detectChanges();

      component.amountControl.setValue(15);
      fixture.detectChanges();

      expect(inputEl.value).toBe('15.0000');
    });
  });

  describe('disabled state', () => {
    it('should disable input when form control is disabled', () => {
      component.amountControl.disable();
      fixture.detectChanges();

      expect(inputEl.disabled).toBe(true);
    });

    it('should enable input when form control is enabled', () => {
      component.amountControl.disable();
      fixture.detectChanges();
      expect(inputEl.disabled).toBe(true);

      component.amountControl.enable();
      fixture.detectChanges();
      expect(inputEl.disabled).toBe(false);
    });
  });

  describe('form integration', () => {
    it('should update form control value on input', () => {
      inputEl.value = '123.45';
      inputEl.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      expect(component.amountControl.value).toBe(123.45);
    });

    it('should update display when form control value changes programmatically', () => {
      component.amountControl.setValue(999.99);
      fixture.detectChanges();

      expect(inputEl.value).toBe('999.99');
    });

    it('should handle form reset', () => {
      component.amountControl.setValue(100);
      fixture.detectChanges();
      expect(inputEl.value).toBe('100.00');

      component.amountControl.reset();
      fixture.detectChanges();

      expect(inputEl.value).toBe('');
      expect(component.amountControl.value).toBeNull();
    });

    it('should work with patchValue', () => {
      component.amountControl.patchValue(50.5);
      fixture.detectChanges();

      expect(inputEl.value).toBe('50.50');
    });
  });

  describe('edge cases', () => {
    it('should handle very small numbers', () => {
      component.amountControl.setValue(0.01);
      fixture.detectChanges();

      expect(inputEl.value).toBe('0.01');
    });

    it('should handle numbers with many decimal places', () => {
      component.amountControl.setValue(15.123456789);
      fixture.detectChanges();

      expect(inputEl.value).toBe('15.12');
    });

    it('should handle negative zero', () => {
      component.allowNegative = true;
      fixture.detectChanges();

      component.amountControl.setValue(-0);
      fixture.detectChanges();

      expect(inputEl.value).toBe('0.00');
    });

    it('should handle rapid focus/blur cycles', () => {
      component.amountControl.setValue(15);
      fixture.detectChanges();

      inputEl.dispatchEvent(new FocusEvent('focus'));
      inputEl.dispatchEvent(new FocusEvent('blur'));
      inputEl.dispatchEvent(new FocusEvent('focus'));
      inputEl.dispatchEvent(new FocusEvent('blur'));
      fixture.detectChanges();

      expect(inputEl.value).toBe('15.00');
      expect(component.amountControl.value).toBe(15);
    });

    it('should handle pasting invalid data gracefully', () => {
      inputEl.value = 'abc123.45def';
      inputEl.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      expect(component.amountControl.value).toBe(123.45);
    });
  });
});
