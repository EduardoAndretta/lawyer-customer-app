import { Component, Input, Output, EventEmitter, forwardRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';

export interface LcaSelectOption {
  value: any;
  label: string;
  disabled?: boolean;
}

@Component({
  selector: 'app-lca-select',
  templateUrl: './lca-select.component.html',
  styleUrls: ['./lca-select.component.css'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => LcaSelectComponent),
      multi: true
    }
  ]
})
export class LcaSelectComponent implements ControlValueAccessor {
  @Input() options: LcaSelectOption[] = [];
  @Input() label: string = '';
  @Input() placeholder: string = 'Select an option';
  @Input() id: string = `lca-select-${Math.random().toString(36).substring(2)}`;
  @Input() disabled: boolean = false;
  @Input() multiple: boolean = false; // For multi-select
  @Input() errorMessage: string | null = null;

  _value: any | any[]; // Can be single or array for multiple
  onChange: any = () => {};
  onTouched: any = () => {};

  get value(): any | any[] {
    return this._value;
  }

  set value(val: any | any[]) {
    this._value = val;
    this.onChange(val);
    this.onTouched();
  }

  writeValue(value: any | any[]): void {
    this._value = value;
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }

  onSelectionChange(event: Event): void {
    const selectElement = event.target as HTMLSelectElement;
    if (this.multiple) {
      this.value = Array.from(selectElement.selectedOptions).map(option => option.value);
    } else {
      this.value = selectElement.value;
    }
  }

  onBlur(): void {
    this.onTouched();
  }

  // Helper for comparing objects in select, if options have object values
  compareFn(c1: any, c2: any): boolean {
    return c1 && c2 ? c1 === c2 : c1 === c2; // Basic comparison, adjust if complex objects
  }
}