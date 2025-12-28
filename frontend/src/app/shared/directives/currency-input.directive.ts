import {
  Directive,
  ElementRef,
  forwardRef,
  HostListener,
  inject,
  input,
  OnInit,
  Renderer2,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

/**
 * CurrencyInputDirective
 *
 * A reusable directive for currency input fields that:
 * - Displays formatted currency (e.g., "15.00") when not focused
 * - Shows raw number when focused for easy editing
 * - Works with reactive forms via ControlValueAccessor
 * - Stores the actual numeric value in the form control
 *
 * Usage:
 * <input matInput appCurrencyInput formControlName="amount" />
 *
 * Options:
 * - decimals: Number of decimal places (default: 2)
 * - allowNegative: Allow negative values (default: false)
 */
@Directive({
  selector: 'input[appCurrencyInput]',
  standalone: true,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => CurrencyInputDirective),
      multi: true,
    },
  ],
})
export class CurrencyInputDirective implements ControlValueAccessor, OnInit {
  private readonly el = inject(ElementRef<HTMLInputElement>);
  private readonly renderer = inject(Renderer2);

  /** Number of decimal places to display */
  decimals = input<number>(2);

  /** Whether to allow negative values */
  allowNegative = input<boolean>(false);

  private innerValue: number | null = null;
  private isFocused = false;

  // ControlValueAccessor callbacks
  private onChange: (value: number | null) => void = () => {};
  private onTouched: () => void = () => {};

  ngOnInit(): void {
    // Set input type to text to allow formatted display
    this.renderer.setAttribute(this.el.nativeElement, 'type', 'text');
    this.renderer.setAttribute(this.el.nativeElement, 'inputmode', 'decimal');
  }

  @HostListener('input', ['$event'])
  onInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    const parsed = this.parseValue(value);
    this.innerValue = parsed;
    this.onChange(parsed);
  }

  @HostListener('focus')
  onFocus(): void {
    this.isFocused = true;
    // Show raw number for editing (without trailing zeros)
    if (this.innerValue !== null) {
      this.setDisplayValue(this.innerValue.toString());
    }
  }

  @HostListener('blur')
  onBlur(): void {
    this.isFocused = false;
    this.onTouched();
    // Format with decimal places when leaving field
    this.updateDisplay();
  }

  @HostListener('keydown', ['$event'])
  onKeyDown(event: KeyboardEvent): void {
    // Allow: backspace, delete, tab, escape, enter, decimal point
    const allowedKeys = [
      'Backspace',
      'Delete',
      'Tab',
      'Escape',
      'Enter',
      'ArrowLeft',
      'ArrowRight',
      'ArrowUp',
      'ArrowDown',
      'Home',
      'End',
      '.',
    ];

    // Allow minus sign if negative values are permitted
    if (this.allowNegative()) {
      allowedKeys.push('-');
    }

    // Allow Ctrl+A, Ctrl+C, Ctrl+V, Ctrl+X
    if (
      event.ctrlKey ||
      event.metaKey ||
      allowedKeys.includes(event.key) ||
      /^[0-9]$/.test(event.key)
    ) {
      return;
    }

    // Prevent other keys
    event.preventDefault();
  }

  // ControlValueAccessor implementation
  writeValue(value: number | null): void {
    this.innerValue = value;
    this.updateDisplay();
  }

  registerOnChange(fn: (value: number | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.renderer.setProperty(this.el.nativeElement, 'disabled', isDisabled);
  }

  /**
   * Parse user input to a number, handling various formats
   */
  private parseValue(value: string): number | null {
    if (!value || value.trim() === '') {
      return null;
    }

    // Remove any non-numeric characters except decimal point and minus
    let cleaned = value.replace(/[^0-9.\-]/g, '');

    // Handle multiple decimal points - keep only the first
    const parts = cleaned.split('.');
    if (parts.length > 2) {
      cleaned = parts[0] + '.' + parts.slice(1).join('');
    }

    // Handle multiple minus signs - keep only at start
    if (cleaned.includes('-')) {
      const isNegative = cleaned.startsWith('-') && this.allowNegative();
      cleaned = cleaned.replace(/-/g, '');
      if (isNegative) {
        cleaned = '-' + cleaned;
      }
    }

    const parsed = parseFloat(cleaned);
    return isNaN(parsed) ? null : parsed;
  }

  /**
   * Format a number for display with specified decimal places
   */
  private formatValue(value: number): string {
    return value.toFixed(this.decimals());
  }

  /**
   * Update the displayed value based on focus state
   */
  private updateDisplay(): void {
    if (this.innerValue === null) {
      this.setDisplayValue('');
    } else if (this.isFocused) {
      // When focused, show raw number
      this.setDisplayValue(this.innerValue.toString());
    } else {
      // When not focused, show formatted value
      this.setDisplayValue(this.formatValue(this.innerValue));
    }
  }

  /**
   * Set the input's display value
   */
  private setDisplayValue(value: string): void {
    this.renderer.setProperty(this.el.nativeElement, 'value', value);
  }
}
