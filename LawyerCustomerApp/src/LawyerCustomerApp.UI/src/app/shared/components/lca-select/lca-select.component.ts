import { Component, Input, forwardRef, OnInit, OnDestroy, Injector, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, NgControl } from '@angular/forms';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

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
  ],
  // Consider OnPush if you manage changes carefully, but default is fine too.
  // changeDetection: ChangeDetectionStrategy.OnPush 
})
export class LcaSelectComponent implements ControlValueAccessor, OnInit, OnDestroy {
  @Input() options: LcaSelectOption[] = [];
  @Input() label: string = '';
  @Input() placeholder: string = 'Select an option';
  @Input() id: string = `lca-select-${Math.random().toString(36).substring(2)}`;
  @Input() disabled: boolean = false;
  @Input() multiple: boolean = false;
  @Input() errorMessage: string | null = null;
  @Input() valueProperty: string | null = null; // Used if option.value are objects
  
  // Default compare function or allow a custom one
  @Input() compareWithFn: (o1: any, o2: any) => boolean = this.defaultCompareFn;

  _value: any | any[]; // Internal value for the ngModel on the <select>
  ngControl: NgControl | null = null;

  private destroy$ = new Subject<void>();

  // To satisfy ControlValueAccessor
  onChange: (value: any) => void = () => {};
  onTouched: () => void = () => {};

  constructor(private injector: Injector, private cdr: ChangeDetectorRef) {} // Inject ChangeDetectorRef

  ngOnInit(): void {
    // console.log(`[LcaSelect ${this.label || this.id}] OnInit, initial value:`, this._value);
    this.ngControl = this.injector.get(NgControl, null);
    if (this.ngControl) {
      this.ngControl.valueAccessor = this;

      // Optional: If you want to react to status changes (e.g., for styling invalid state)
      if (this.ngControl.statusChanges) {
        this.ngControl.statusChanges.pipe(takeUntil(this.destroy$)).subscribe(() => {
          //this.cdr.markForCheck(); // If using OnPush
        });
      }
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Getter for template binding to avoid issues if compareWithFn is not set initially
  get compareFunctionBinding(): (o1: any, o2: any) => boolean {
    return this.compareWithFn || this.defaultCompareFn;
  }

  private defaultCompareFn(o1: any, o2: any): boolean {
    // Handle cases where o1 or o2 might be the option object itself vs. just the value
    const val1 = (o1 && typeof o1 === 'object' && o1.hasOwnProperty('value')) ? o1.value : o1;
    const val2 = (o2 && typeof o2 === 'object' && o2.hasOwnProperty('value')) ? o2.value : o2;

    if (this.valueProperty &&
        val1 !== null && typeof val1 === 'object' && val1.hasOwnProperty(this.valueProperty) &&
        val2 !== null && typeof val2 === 'object' && val2.hasOwnProperty(this.valueProperty)) {
      return val1[this.valueProperty] === val2[this.valueProperty];
    }
    return val1 === val2;
  }

  // --- ControlValueAccessor IMPLEMENTATION ---

  writeValue(value: any | any[]): void {
    // console.log(`[LcaSelect ${this.label || this.id}] writeValue called with:`, value, 'Current _value:', this._value);
    let newValue: any;
    if (this.multiple) {
      newValue = Array.isArray(value) ? value : (value !== null && value !== undefined ? [value] : []);
    } else {
      newValue = Array.isArray(value) ? value[0] : value;
    }

    // Only update and trigger change detection if the value has actually changed
    // according to the comparison function. This prevents infinite loops if
    // an [(ngModel)] binding causes writeValue to be called with the same object instance.
    const changed = this.multiple ? !this.arraysEqual(this._value, newValue) : !this.compareFunctionBinding(this._value, newValue);

    if (changed) {
      this._value = newValue;
      // console.log(`[LcaSelect ${this.label || this.id}] _value set to:`, this._value);
      // this.cdr.detectChanges(); // Use detectChanges if things are not updating visually, especially with OnPush or complex scenarios
                                // For default change detection, this might not be strictly necessary but can help ensure view updates.
    }
  }

  registerOnChange(fn: (value: any) => void): void {
    // console.log(`[LcaSelect ${this.label || this.id}] registerOnChange`);
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    // console.log(`[LcaSelect ${this.label || this.id}] registerOnTouched`);
    this.onTouched = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    // console.log(`[LcaSelect ${this.label || this.id}] setDisabledState called with:`, isDisabled);
    this.disabled = isDisabled;
    // this.cdr.markForCheck(); // If using OnPush
  }

  // Called when the native select's value changes
  onModelChange(newValueFromSelect: any | any[]): void {
    // console.log(`[LcaSelect ${this.label || this.id}] onModelChange from select element:`, newValueFromSelect);
    
    // The value from the native select (newValueFromSelect) will already be the correct primitive or object
    // based on [ngValue] of the selected option.
    // We need to update our internal _value and then propagate it upwards.
    
    const changed = this.multiple ? !this.arraysEqual(this._value, newValueFromSelect) : !this.compareFunctionBinding(this._value, newValueFromSelect);

    if (changed) {
        this._value = newValueFromSelect;
        this.onChange(this._value); // Propagate the change to the parent form control
        // console.log(`[LcaSelect ${this.label || this.id}] propagating change to parent:`, this._value);
    }
  }

  // Called on blur
  onBlur(): void {
    // console.log(`[LcaSelect ${this.label || this.id}] onBlur`);
    this.onTouched();
  }

  private arraysEqual(arr1: any[], arr2: any[]): boolean {
    if (arr1 === arr2) return true; // Same instance or both null/undefined
    if (!arr1 || !arr2 || arr1.length !== arr2.length) return false;
    
    const sortedArr1 = [...arr1].sort(); // Sort copies to handle order differences if needed
    const sortedArr2 = [...arr2].sort(); // (May not be necessary if order is guaranteed)

    for (let i = 0; i < sortedArr1.length; i++) {
      if (!this.compareFunctionBinding(sortedArr1[i], sortedArr2[i])) {
        return false;
      }
    }
    return true;
  }
}