import { Component, Input, Output, EventEmitter, ElementRef, HostListener, forwardRef, OnInit, OnChanges, SimpleChanges } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormControl, AbstractControl } from '@angular/forms';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, map, startWith } from 'rxjs/operators';

@Component({
  selector: 'app-lca-auto-complete',
  templateUrl: './lca-auto-complete.component.html',
  styleUrls: ['./lca-auto-complete.component.css'],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => LcaAutoCompleteComponent),
      multi: true
    }
  ]
})
export class LcaAutoCompleteComponent implements ControlValueAccessor, OnInit, OnChanges {
  @Input() items: any[] = [];
  @Input() displayProperty: string = 'name';
  @Input() valueProperty: string | null = null;

  @Input() placeholder: string = 'Search...';
  @Input() label: string = '';
  @Input() minLength: number = 0;
  @Input() debounceMs: number = 200;
  @Input() id: string = `lca-ac-${Math.random().toString(36).substring(2)}`;
  @Input() disabled: boolean = false;
  @Input() serverSideFiltering: boolean = false;
  @Input() noResultsText: string = 'No results found';
  @Input() formControlForValidation: AbstractControl | null = null;
  @Output() itemSelected = new EventEmitter<any>();
  @Output() inputTextChanged = new EventEmitter<string>();

  suggestions: any[] = [];
  showSuggestions: boolean = false;
  isLoading: boolean = false;
  activeIndex: number = -1;

  private searchTerms = new Subject<string>();
  _inputValue: string = '';

  onChange: any = () => {};
  onTouched: any = () => {};

  constructor(private elementRef: ElementRef) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['items'] && !changes['items'].firstChange && this._inputValue) {
      this.searchTerms.next(this._inputValue);
    }
  }

  ngOnInit(): void {
  this.searchTerms.pipe(
    debounceTime(this.debounceMs),
    distinctUntilChanged(),
    map((term: string) => {
      this._inputValue = term;
      if (this.serverSideFiltering) {
        return this.items || [];
      } else {
        if (!term || term.length < this.minLength || !this.items || this.items.length === 0) {
          return [];
        }
        const lowerTerm = term.toLowerCase();
        return this.items.filter(item => {
          const displayVal = item[this.displayProperty];
          return displayVal && typeof displayVal === 'string' && displayVal.toLowerCase().includes(lowerTerm);
        });
      }
    })
  ).subscribe(filteredItems => {
    this.suggestions = filteredItems;
    this.showSuggestions = this._inputValue.length >= this.minLength && 
                          (this.suggestions.length > 0 || 
                           (this._inputValue.length > 0 && this.noResultsText !== ''));
    this.activeIndex = -1;
  });
}

  onInput(event: Event): void {
    const term = (event.target as HTMLInputElement).value;
    this._inputValue = term;
    this.onChange(term);
    this.inputTextChanged.emit(term);
    this.searchTerms.next(term);
  }

   selectSuggestion(suggestion: any): void {
    this._inputValue = suggestion[this.displayProperty] || '';
    const emitValue = this.valueProperty ? suggestion[this.valueProperty] : suggestion;
    this.onChange(emitValue);
    this.itemSelected.emit(suggestion);
    this.inputTextChanged.emit(this._inputValue);

    this.showSuggestions = false;
    this.onTouched();
  }

  writeValue(value: any): void {
    if (value && typeof value === 'object' && this.valueProperty && value[this.valueProperty] !== undefined) {    
      const matchedItem = this.items.find(item => item[this.valueProperty!] === value[this.valueProperty!]); 
      this._inputValue = matchedItem ? matchedItem[this.displayProperty] : '';
    } else if (value && typeof value === 'object' && !this.valueProperty) {  
      this._inputValue = value[this.displayProperty] || '';
    } else if (value && (typeof value === 'string' || typeof value === 'number')) {
        
      if (this.valueProperty && this.items && this.items.length > 0 && typeof this.items[0][this.valueProperty!] !== 'undefined') {
        const matchedItem = this.items.find(item => item[this.valueProperty!] === value);
        this._inputValue = matchedItem ? matchedItem[this.displayProperty] : value.toString();
      } else {
          this._inputValue = value.toString();
      }
    }
    else {
      this._inputValue = '';
    }
    this.inputTextChanged.emit(this._inputValue);
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

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.showSuggestions = false;
    }
  }

  onFocus(): void {
     if (this._inputValue.length >= this.minLength && this.suggestions.length > 0) {
       this.showSuggestions = true;
     } else if (this._inputValue.length === 0 && this.minLength === 0) {
        this.searchTerms.next('');
     }
     this.onTouched();
  }

  onKeyDown(event: KeyboardEvent): void {
    if (!this.showSuggestions || this.suggestions.length === 0) {
      if (event.key === 'ArrowDown' && this._inputValue.length >= this.minLength) {
          this.searchTerms.next(this._inputValue);
      }
      return;
    }

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.activeIndex = (this.activeIndex + 1) % this.suggestions.length;
        this.scrollToActive();
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.activeIndex = (this.activeIndex - 1 + this.suggestions.length) % this.suggestions.length;
        this.scrollToActive();
        break;
      case 'Enter':
        if (this.activeIndex > -1 && this.suggestions[this.activeIndex]) {
          event.preventDefault();
          this.selectSuggestion(this.suggestions[this.activeIndex]);
        }
        break;
      case 'Escape':
        this.showSuggestions = false;
        break;
    }
  }

  private scrollToActive(): void {
    const suggestionsList = this.elementRef.nativeElement.querySelector('.lca-suggestions-list');
    if (suggestionsList && this.activeIndex > -1) {
      const activeItem = suggestionsList.children[this.activeIndex];
      if (activeItem) {
        activeItem.scrollIntoView({ block: 'nearest' });
      }
    }
  }
}