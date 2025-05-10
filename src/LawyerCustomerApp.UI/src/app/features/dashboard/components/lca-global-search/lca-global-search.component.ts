import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, of, Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, catchError, take, map } from 'rxjs/operators';

// Import services and DTOs - you'll need to create these search services
import { CaseSearchService } from '../../../search/services/case-search.service';
import { UserSearchService } from '../../../search/services/user-search.service';
import { LawyerSearchService } from '../../../search/services/lawyer-search.service';
import { CustomerSearchService } from '../../../search/services/customer-search.service';
import { UserProfileService } from '../../../../core/services/user-profile.service';

import { LcaSelectOption } from '../../../../shared/components/lca-select/lca-select.component';
import { LcaAutoCompleteComponent } from '../../../../shared/components/lca-auto-complete/lca-auto-complete.component';
import { ToastService } from '../../../../core/services/toast.service';


interface SearchResultItem {
  id: number;
  name: string; // Or title for cases
  type: 'case' | 'user' | 'lawyer' | 'customer';
  path: string; // Path to details page
  originalItem: any; // Store the original API response item
}

@Component({
  selector: 'app-lca-global-search',
  templateUrl: './lca-global-search.component.html',
  styleUrls: ['./lca-global-search.component.css']
})
export class LcaGlobalSearchComponent implements OnInit, OnDestroy {
  @ViewChild('autoComplete') autoCompleteComponent!: LcaAutoCompleteComponent;

  searchQuery: string = '';
  selectedSearchType: 'case' | 'user' | 'lawyer' | 'customer' = 'case'; // Default search type
  searchTypeOptions: LcaSelectOption[] = [
    { label: 'Search Cases', value: 'case' },
    { label: 'Search Users', value: 'user' },
    { label: 'Search Lawyers', value: 'lawyer' },
    { label: 'Search Customers', value: 'customer' }
  ];

  private currentAttributeId: number | null = null;
  private attributeIdSubscription!: Subscription;

  constructor(
    private router: Router,
    private caseSearchService: CaseSearchService,
    private userSearchService: UserSearchService,
    private lawyerSearchService: LawyerSearchService,
    private customerSearchService: CustomerSearchService,
    private userProfileService: UserProfileService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
     this.attributeIdSubscription = this.userProfileService.selectedAccountAttributeId$.subscribe(id => {
        this.currentAttributeId = id;
        // If search type or query already exists, and attribute changes, you might want to re-trigger or clear search
    });
  }

  // This function will be passed to lca-auto-complete
  fetchSuggestions = (query: string): Observable<SearchResultItem[]> => {
    if (!this.currentAttributeId) {
        this.toastService.showError("Account context (Attribute ID) is not set. Cannot perform search.");
        return of([]);
    }

    const pagination = { begin: 0, end: 4 }; // For top 5 items (0-indexed)
    let searchObservable: Observable<any>;

    switch (this.selectedSearchType) {
      case 'case':
        searchObservable = this.caseSearchService.search({ query, attributeId: this.currentAttributeId, pagination });
        break;
      case 'user':
        searchObservable = this.userSearchService.search({ query, attributeId: this.currentAttributeId, pagination });
        break;
      case 'lawyer':
        searchObservable = this.lawyerSearchService.search({ query, attributeId: this.currentAttributeId, pagination });
        break;
      case 'customer':
        searchObservable = this.customerSearchService.search({ query, attributeId: this.currentAttributeId, pagination });
        break;
      default:
        return of([]);
    }

    return searchObservable.pipe(
      map((response: any) => {
        if (response && response.items) {
          return response.items.map((item: any) => this.transformToSearchResultItem(item, this.selectedSearchType));
        }
        return [];
      }),
      catchError(err => {
        // console.error(`Error searching ${this.selectedSearchType}:`, err);
        // Error handled by interceptor
        return of([]);
      })
    );
  }

  private transformToSearchResultItem(item: any, type: 'case' | 'user' | 'lawyer' | 'customer'): SearchResultItem {
    let nameOrTitle = '';
    let path = '';
    let id = item.id || (type === 'lawyer' ? item.lawyerId : null) || (type === 'customer' ? item.customerId : null) || item.caseId;

    switch (type) {
      case 'case':
        nameOrTitle = item.title || 'Untitled Case';
        path = `/dashboard/cases/${item.id}`;
        id = item.id;
        break;
      case 'user':
        nameOrTitle = item.name || 'Unnamed User';
        path = `/dashboard/users/${item.id}`;
        id = item.id;
        break;
      case 'lawyer':
        nameOrTitle = item.name || 'Unnamed Lawyer';
        path = `/dashboard/lawyers/${item.lawyerId}`; // Use lawyerId for lawyers
        id = item.lawyerId;
        break;
      case 'customer':
        nameOrTitle = item.name || 'Unnamed Customer';
        path = `/dashboard/customers/${item.customerId}`; // Use customerId for customers
        id = item.customerId;
        break;
    }
    return { id, name: nameOrTitle, type, path, originalItem: item };
  }

  onSuggestionSelected(selectedItem: SearchResultItem): void {
    if (selectedItem && selectedItem.path) {
      this.router.navigate([selectedItem.path]);
      this.searchQuery = ''; // Clear search input after navigation
      if (this.autoCompleteComponent) {
        this.autoCompleteComponent.writeValue(''); // Clear lca-auto-complete's internal value
        this.autoCompleteComponent.showSuggestions = false;
      }
    }
  }

  onSearchTypeChange(type: 'case' | 'user' | 'lawyer' | 'customer'): void {
    this.selectedSearchType = type;
    // Optionally clear previous query or trigger a new search if query exists
    if (this.searchQuery && this.searchQuery.length >= (this.autoCompleteComponent?.minLength || 2)) {
        this.autoCompleteComponent?.searchTerms.next(this.searchQuery); // Trigger search with new type
    }
  }

  expandSearch(): void {
    if (!this.searchQuery.trim()) {
        this.toastService.showInfo("Please enter a search term.");
        return;
    }
    this.router.navigate(['/dashboard/search'], {
      queryParams: {
        query: this.searchQuery,
        type: this.selectedSearchType
      }
    });
  }

  ngOnDestroy(): void {
    if(this.attributeIdSubscription) {
        this.attributeIdSubscription.unsubscribe();
    }
  }
}