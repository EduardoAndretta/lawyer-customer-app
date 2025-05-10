import { Component, Input, Output, EventEmitter, ElementRef, HostListener, forwardRef, OnInit } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormControl } from '@angular/forms';
import { Observable, Subject, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, catchError, tap } from 'rxjs/operators';

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
export class LcaAutoCompleteComponent implements ControlValueAccessor, OnInit {
  @Input() searchFunction!: (query: string) => Observable<any[]>; // Function to fetch suggestions
  @Input() displayProperty: string = 'name'; // Property of suggestion object to display
  @Input() placeholder: string = 'Search...';
  @Input() label: string = '';
  @Input() minLength: number = 2; // Minimum characters to trigger search
  @Input() debounceMs: number = 300;
  @Input() id: string = `lca-ac-${Math.random().toString(36).substring(2)}`;
  @Input() disabled: boolean = false;
  @Input() noResultsText: string = 'No results found';

  @Output() itemSelected = new EventEmitter<any>(); // Emits the selected suggestion object

  suggestions: any[] = [];
  showSuggestions: boolean = false;
  isLoading: boolean = false;
  activeIndex: number = -1; // For keyboard navigation

  public searchTerms = new Subject<string>();
  _inputValue: string = ''; // The text displayed in the input

  onChange: any = () => {};
  onTouched: any = () => {};

  constructor(private elementRef: ElementRef) {}

  ngOnInit(): void {
    this.searchTerms.pipe(
      debounceTime(this.debounceMs),
      distinctUntilChanged(),
      tap(() => {
        this.suggestions = []; // Clear previous suggestions immediately
        if (this._inputValue.length >= this.minLength) {
            this.isLoading = true;
            this.showSuggestions = true; // Show loading state in dropdown
        } else {
            this.showSuggestions = false;
            this.isLoading = false;
        }
      }),
      switchMap((term: string) => {
        if (term.length >= this.minLength && this.searchFunction) {
          return this.searchFunction(term).pipe(
            catchError(() => {
              this.isLoading = false;
              return of([]); // Handle error by returning empty array
            })
          );
        } else {
          return of([]); // Return empty if term is too short or no search function
        }
      })
    ).subscribe(results => {
      this.isLoading = false;
      this.suggestions = results;
      this.showSuggestions = this._inputValue.length >= this.minLength && (this.suggestions.length > 0 || this.isLoading); // Keep open if loading
      this.activeIndex = -1; // Reset active index
    });
  }

  onInput(event: Event): void {
    const term = (event.target as HTMLInputElement).value;
    this._inputValue = term; // Update internal input value
    this.onChange(term); // Propagate change for ngModel or formControl (could be text or object)
    this.searchTerms.next(term);
  }

  selectSuggestion(suggestion: any): void {
    this._inputValue = suggestion[this.displayProperty] || '';
    this.onChange(suggestion); // For form control, set the whole object
    this.itemSelected.emit(suggestion);
    this.showSuggestions = false;
    this.suggestions = [];
    this.onTouched();
  }

  writeValue(value: any): void {
    // If value is an object, display its 'displayProperty'. If string, display as is.
    if (value && typeof value === 'object' && value[this.displayProperty]) {
      this._inputValue = value[this.displayProperty];
    } else if (typeof value === 'string') {
      this._inputValue = value;
    } else {
      this._inputValue = '';
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
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.showSuggestions = false; // Close suggestions when clicking outside
    }
  }

  onFocus(): void {
    // Optionally, reshow suggestions if input has content
     if (this._inputValue.length >= this.minLength && this.suggestions.length > 0) {
       this.showSuggestions = true;
     }
     this.onTouched(); // Mark as touched on focus
  }

  // Keyboard navigation
  onKeyDown(event: KeyboardEvent): void {
    if (!this.showSuggestions || this.suggestions.length === 0) {
      return;
    }

    switch (event.key) {
      case 'ArrowDown':
        event.preventDefault();
        this.activeIndex = (this.activeIndex + 1) % this.suggestions.length;
        break;
      case 'ArrowUp':
        event.preventDefault();
        this.activeIndex = (this.activeIndex - 1 + this.suggestions.length) % this.suggestions.length;
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
}