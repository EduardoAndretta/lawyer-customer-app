import { Component, Input, forwardRef, OnInit, Injector } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, NgControl } from '@angular/forms';

export interface LcaSelectOption {
  value: any; // This can be an object
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
export class LcaSelectComponent implements ControlValueAccessor, OnInit {
  @Input() options: LcaSelectOption[] = [];
  @Input() label: string = '';
  @Input() placeholder: string = 'Select an option';
  @Input() id: string = `lca-select-${Math.random().toString(36).substring(2)}`;
  @Input() disabled: boolean = false;
  @Input() multiple: boolean = false;
  @Input() errorMessage: string | null = null;
  @Input() valueProperty: string | null = null;
  @Input() compareWithFn: (o1: any, o2: any) => boolean =
  (o1: any, o2: any) => this.defaultCompareFn(o1, o2);

  _value: any | any[];
  ngControl: NgControl | null = null;

  onChange: any = () => {};
  onTouched: any = () => {};

  constructor(private injector: Injector) {
    this.compareWithFn = this.defaultCompareFn.bind(this);
  }

  ngOnInit(): void {
    this.ngControl = this.injector.get(NgControl, null);
    if (this.ngControl) {
      this.ngControl.valueAccessor = this;
    }
  }

 get compareFunctionBinding(): (o1: any, o2: any) => boolean {
    return this.compareWithFn;
  }

  private defaultCompareFn(o1: any, o2: any): boolean {
    if (o1 && o2 && this.valueProperty) {
      return o1[this.valueProperty] === o2[this.valueProperty];
    }
    return o1 === o2;
  }

  get value(): any | any[] {
    return this._value;
  }

  set value(val: any | any[]) {
    if (!this.compareWithFn(this._value, val)) {
      this._value = val;
      this.onChange(this._value);
    }
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

  onModelChange(newValue: any | any[]): void {
    this.value = newValue; // Updates _value and calls onChange
    this.onTouched();
  }

  onBlur(): void {
    this.onTouched();
  }
}