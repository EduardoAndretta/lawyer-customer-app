import { Component, Input, Output, EventEmitter, forwardRef, OnInit, Injector, Optional, Self } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormControl, NgControl } from '@angular/forms';

@Component({
  selector: 'app-lca-input',
  templateUrl: './lca-input.component.html',
  styleUrls: ['./lca-input.component.css'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => LcaInputComponent),
      multi: true
    }
  ]
})
export class LcaInputComponent implements ControlValueAccessor, OnInit {
  @Input() type: string = 'text';
  @Input() placeholder: string = '';
  @Input() label: string = '';
  @Input() id: string = `lca-input-${Math.random().toString(36).substring(2)}`;
  @Input() disabled: boolean = false;
  @Input() readonly: boolean = false;
  @Input() errorMessage: string | null = null;

  // For Reactive Forms integration
  public control!: FormControl; // Will be initialized in ngOnInit
  public ngControl: NgControl | null = null; // Store NgControl instance

  _value: any = '';
  onChange: any = () => {};
  onTouched: any = () => {};

  constructor(private injector: Injector) { // Inject Injector instead of NgControl directly
  }

  ngOnInit(): void {
    // Manually get NgControl from the injector.
    // @Self() and @Optional() can also be used on constructor parameters,
    // but getting it from Injector is a robust way to break cycles.
    this.ngControl = this.injector.get(NgControl, null);

    if (this.ngControl != null) {
      // Set the value accessor directly on the NgControl instance
      this.ngControl.valueAccessor = this;
      // Assign the control from NgControl to our local property
      if (this.ngControl.control) {
        this.control = this.ngControl.control as FormControl;
      } else {
        // Fallback if control is not yet available (less common for formControlName)
        this.control = new FormControl();
      }
    } else {
      // If not used with NgControl (e.g. standalone ngModel), create a default FormControl
      this.control = new FormControl();
    }
  }

  get value(): any {
    return this._value;
  }

  set value(val: any) {
    this._value = val;
    this.onChange(val);
    this.onTouched();
  }

  writeValue(value: any): void {
    this._value = value;
    // If we have a standalone control, update it too
    if (this.control && (!this.ngControl || !this.ngControl.control)) {
        this.control.setValue(value, { emitEvent: false });
    }
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    this.disabled = isDisabled;
    if (this.control) {
        isDisabled ? this.control.disable({ emitEvent: false }) : this.control.enable({ emitEvent: false });
    }
  }

  onInput(event: Event): void {
    const inputElement = event.target as HTMLInputElement;
    this.value = inputElement.value;
  }

  onBlur(): void {
    this.onTouched();
  }
}