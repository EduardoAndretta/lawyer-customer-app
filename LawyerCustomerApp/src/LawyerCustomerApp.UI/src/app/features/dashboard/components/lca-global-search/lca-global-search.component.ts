import { Component, OnInit, OnDestroy, ChangeDetectorRef, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, of, Subscription, Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, catchError, map, finalize } from 'rxjs/operators';

import { CaseSearchService } from '../../../search/services/case-search.service';
import { UserSearchService } from '../../../search/services/user-search.service';
import { LawyerSearchService } from '../../../search/services/lawyer-search.service';
import { CustomerSearchService } from '../../../search/services/customer-search.service';
import { UserProfileService } from '../../../../core/services/user-profile.service';
import { LcaSelectOption } from '../../../../shared/components/lca-select/lca-select.component';
import { ToastService } from '../../../../core/services/toast.service';
import { PaginationParams } from '../../../../core/models/common.models';
import { LcaAutoCompleteComponent } from '../../../../shared/components/lca-auto-complete/lca-auto-complete.component';

interface SearchResultItem {
  id: any;
  name: string;
  type: 'case' | 'user' | 'lawyer' | 'customer';
  path: string;
  originalItem: any;
}

@Component({
  selector: 'app-lca-global-search',
  templateUrl: './lca-global-search.component.html',
  styleUrls: ['./lca-global-search.component.css']
})
export class LcaGlobalSearchComponent implements OnInit, OnDestroy {
  @ViewChild('autoComplete') autoCompleteComponent!: LcaAutoCompleteComponent;

  searchQueryForDisplay: string = '';
  selectedObjectFromAutocomplete: any = null;

  selectedSearchType: 'case' | 'user' | 'lawyer' | 'customer' = 'case';
  searchTypeOptions: LcaSelectOption[] = [
    { label: 'Search Cases', value: 'case' },
    { label: 'Search Users', value: 'user' },
    { label: 'Search Lawyers', value: 'lawyer' },
    { label: 'Search Customers', value: 'customer' }
  ];

  autocompleteItemsSource$: BehaviorSubject<any[]> = new BehaviorSubject<any[]>([]);
  isLoadingList: boolean = false;

  private currentAttributeId: number | null = null;
  private subscriptions = new Subscription();
  private readonly GLOBAL_SEARCH_SUGGESTION_LIMIT = 15;

  private searchTermsSubject = new Subject<string>();

  constructor(
    private router: Router,
    private caseSearchService: CaseSearchService,
    private userSearchService: UserSearchService,
    private lawyerSearchService: LawyerSearchService,
    private customerSearchService: CustomerSearchService,
    private userProfileService: UserProfileService,
    private toastService: ToastService,
    private cdRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const attributeSub = this.userProfileService.selectedAccountAttributeId$
      .pipe(distinctUntilChanged())
      .subscribe(id => {
        this.currentAttributeId = id;
        this.autocompleteItemsSource$.next([]);
        this.searchQueryForDisplay = '';
        if (this.autoCompleteComponent) {
          this.autoCompleteComponent.writeValue('');
        }
      });
    this.subscriptions.add(attributeSub);

    const searchSub = this.searchTermsSubject.pipe(
      debounceTime(300),
      distinctUntilChanged()
    ).subscribe(searchTerm => {
      this.performSearch(searchTerm);
    });
    this.subscriptions.add(searchSub);
  }

  private transformToSearchResultItem(item: any, type: 'case' | 'user' | 'lawyer' | 'customer'): SearchResultItem | null {
  if (!item) return null;
  let nameOrTitle = '';
  let path = '';
  let idValue = null;

  console.log(item);

  let parsedType = Array.isArray(type) ? type[0] : type;

  switch (parsedType) {
    case 'case':

      if (item?.originalItem != null)
      {
        nameOrTitle = item.originalItem?.name || 'Unnamed Case';
        idValue = item.originalItem?.id;
        path = `/dashboard/cases/${idValue}`;

        break;
      }

      nameOrTitle = item.title || 'Untitled Case';
      idValue = item.id;
      path = `/dashboard/cases/${idValue}`;
      break;
    case 'user':

      if (item?.originalItem != null)
      {
        nameOrTitle = item.originalItem?.name || 'Unnamed User';
        idValue = item.originalItem?.id;
        path = `/dashboard/users/${idValue}`;

        break;
      }

      nameOrTitle = item.name || 'Unnamed User';
      idValue = item.id;
      path = `/dashboard/users/${idValue}`;
      break;
    case 'lawyer':

      if (item?.originalItem != null)
      {
        nameOrTitle = item.originalItem?.name || 'Unnamed Lawyer';
        idValue = item.originalItem?.lawyerId;
        path = `/dashboard/lawyers/${idValue}`;

        break;
      }

      nameOrTitle = item.name || 'Unnamed Lawyer';
      idValue = item.lawyerId;
      path = `/dashboard/lawyers/${idValue}`;
      break;
    case 'customer':

      if (item?.originalItem != null)
      {
        nameOrTitle = item.originalItem?.name || 'Unnamed Customer';
        idValue = item.originalItem?.customerId;
        path = `/dashboard/customers/${idValue}`;

        break;
      }

      nameOrTitle = item.name || 'Unnamed Customer';
      idValue = item.customerId;
      path = `/dashboard/customers/${idValue}`;
      break;
    default:
      return null;
  }

  return { id: idValue, name: nameOrTitle, type: parsedType, path, originalItem: item };
}

onSuggestionSelectedFromAutocomplete(selectedItemFullObject: any): void {
  if (!this.selectedSearchType) return;
  if (!selectedItemFullObject) return;

  const searchType = Array.isArray(this.selectedSearchType) ? this.selectedSearchType[0] : this.selectedSearchType;

  console.log("---------------")

  const searchResult = this.transformToSearchResultItem(selectedItemFullObject, searchType);

  console.log(searchResult);

  console.log("---------------")

  if (searchResult && searchResult.path) {
    this.router.navigate([searchResult.path]);

    this.selectedObjectFromAutocomplete = null;
    this.searchQueryForDisplay = '';

    if (this.autoCompleteComponent) {
      this.autoCompleteComponent.writeValue('');
      this.autoCompleteComponent.suggestions = [];
      this.autoCompleteComponent.showSuggestions = false;
    }
  }
}

  onSearchTypeChange(type: 'case' | 'user' | 'lawyer' | 'customer'): void {
    if (this.selectedSearchType === type) return;

    this.selectedSearchType = type;
    this.searchQueryForDisplay = '';
    this.selectedObjectFromAutocomplete = null;

    if (this.autoCompleteComponent) {
      this.autoCompleteComponent.writeValue('');
    }

    this.autocompleteItemsSource$.next([]);
}

  onSearchInputChanged(searchTerm: string): void {
    if (!this.selectedSearchType) return;

    // [Emit the search term to the Subject instead of searching immediately]
    this.searchTermsSubject.next(searchTerm);
  }


  private performSearch(searchTerm: string): void {
    if (!this.selectedSearchType) return;

    if (this.currentAttributeId === null) {
      this.autocompleteItemsSource$.next([]);
      return;
    }

    this.isLoadingList = true;
    this.autocompleteItemsSource$.next([]);

    const pagination: PaginationParams = { begin: 0, end: this.GLOBAL_SEARCH_SUGGESTION_LIMIT };

    let searchObservable: Observable<any>;

    let parsedType = Array.isArray(this.selectedSearchType) ? this.selectedSearchType[0] : this.selectedSearchType;

    switch (parsedType) {
      case 'case':
        searchObservable = this.caseSearchService.search({
          query: searchTerm,
          attributeId: this.currentAttributeId,
          pagination
        });
        break;
      case 'user':
        searchObservable = this.userSearchService.search({
          query: searchTerm,
          attributeId: this.currentAttributeId,
          pagination
        });
        break;
      case 'lawyer':
        searchObservable = this.lawyerSearchService.search({
          query: searchTerm,
          attributeId: this.currentAttributeId,
          pagination
        });
        break;
      case 'customer':
        searchObservable = this.customerSearchService.search({
          query: searchTerm,
          attributeId: this.currentAttributeId,
          pagination
        });
        break;
      default:
        return;
    }

    searchObservable.pipe(
      map(response => (response.items || [])
        .map((item: any) => this.transformToSearchResultItem(item, parsedType))
        .filter((item: SearchResultItem): item is SearchResultItem => item !== null)
      ),
      catchError(err => {
        this.toastService.showError(`Failed to search ${this.selectedSearchType}s.`);
        return of([]);
      }),
      finalize(() => {
        this.isLoadingList = false;
        this.cdRef.detectChanges();
      })
    ).subscribe(items => {
      this.autocompleteItemsSource$.next(items);
    });
  }

  getAutocompleteDisplayProperty(): string {
    switch (this.selectedSearchType) {
      case 'case': return 'title';
      case 'user':
      case 'lawyer':
      case 'customer':
        return 'name';
      default: return 'name';
    }
  }

  getAutocompleteValueProperty(): string | null {
    return null;
  }

  expandSearch(): void {
    if (!this.selectedSearchType) return;


    if (!this.searchQueryForDisplay?.trim()) {
      this.toastService.showInfo("Please enter a search term.");
      return;
    }
    if (this.currentAttributeId === null) {
      this.toastService.showInfo("Please select an account context before expanding search.");
      return;
    }
    this.router.navigate(['/dashboard/search'], {
      queryParams: {
        query: this.searchQueryForDisplay,
        type: this.selectedSearchType
      }
    });
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    this.autocompleteItemsSource$.complete();
    this.searchTermsSubject.complete();
  }
}