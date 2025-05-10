import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription, forkJoin, of, Observable } from 'rxjs';
import { switchMap, tap, catchError, finalize } from 'rxjs/operators';

import { UserProfileService } from '../../../core/services/user-profile.service';
import { ToastService } from '../../../core/services/toast.service';
import { LcaTableColumn } from '../../../shared/components/lca-table/lca-table.component';

// Import search services
import { CaseSearchService } from '../services/case-search.service';
import { UserSearchService } from '../services/user-search.service';
import { LawyerSearchService } from '../services/lawyer-search.service';
import { CustomerSearchService } from '../services/customer-search.service';

// Import DTOs for parameters (ensure these are correctly defined)
import { CaseSearchParametersDto, CaseSearchInformationItem, CaseCountParametersDto } from '../../../core/models/case.models';
import { UserSearchParametersDto, UserSearchInformationItem, UserCountParametersDto } from '../../../core/models/user.models';
import { LawyerSearchParametersDto, LawyerSearchInformationItem, LawyerCountParametersDto } from '../../../core/models/lawyer.models';
import { CustomerSearchParametersDto, CustomerSearchInformationItem, CustomerCountParametersDto } from '../../../core/models/customer.models';
import { CountInformationDto } from '../../../core/models/common.models';


type SearchableEntityType = 'case' | 'user' | 'lawyer' | 'customer';

@Component({
  selector: 'app-search-results-page',
  templateUrl: './search-results-page.component.html',
  styleUrls: ['./search-results-page.component.css']
})
export class SearchResultsPageComponent implements OnInit, OnDestroy {
  searchQuery: string = '';
  searchType: SearchableEntityType = 'case';
  results: any[] = [];
  tableColumns: LcaTableColumn[] = [];
  isLoading: boolean = false;
  totalItems: number = 0;
  currentPage: number = 1;
  itemsPerPage: number = 10; // Or make this configurable

  private queryParamsSubscription!: Subscription;
  private attributeIdSubscription!: Subscription;
  private currentAttributeId: number | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private userProfileService: UserProfileService,
    private toastService: ToastService,
    private caseSearchService: CaseSearchService,
    private userSearchService: UserSearchService,
    private lawyerSearchService: LawyerSearchService,
    private customerSearchService: CustomerSearchService
  ) {}

  ngOnInit(): void {
  this.attributeIdSubscription = this.userProfileService.selectedAccountAttributeId$.subscribe(id => {
      this.currentAttributeId = id;
      // Only proceed if we have an attribute ID AND a search query
      if (this.searchQuery && this.currentAttributeId) {
          this.setupTableColumns();
          this.performSearch(this.currentPage);
      } else if (this.searchQuery && !this.currentAttributeId) {
          this.results = []; // Clear previous results
          this.totalItems = 0;
          this.toastService.showInfo("Please select an account (Lawyer/Customer) from the sidebar to perform a search.");
      }
  });

  this.queryParamsSubscription = this.route.queryParams.subscribe(params => {
    this.searchQuery = params['query'] || '';
    const typeParam = params['type'] as SearchableEntityType;
    this.searchType = ['case', 'user', 'lawyer', 'customer'].includes(typeParam) ? typeParam : 'case'; // Default to 'case' if invalid
    this.currentPage = params['page'] ? +params['page'] : 1;

    if (this.searchQuery && this.currentAttributeId) {
      this.setupTableColumns();
      this.performSearch(this.currentPage);
    } else if (this.searchQuery && !this.currentAttributeId) {
      this.results = [];
      this.totalItems = 0;
      // Message already handled by attributeIdSubscription or will be shown when it emits null
    } else {
      this.results = [];
      this.totalItems = 0;
    }
  });
}

  private setupTableColumns(): void {
    this.tableColumns = []; // Reset
    switch (this.searchType) {
      case 'case':
        this.tableColumns = [
          { key: 'id', header: 'ID' },
          { key: 'title', header: 'Title' },
          { key: 'description', header: 'Description' },
          // { key: 'userId', header: 'User ID' },
          // { key: 'customerId', header: 'Customer ID' },
          // { key: 'lawyerId', header: 'Lawyer ID' }
        ];
        break;
      case 'user':
        this.tableColumns = [
          { key: 'id', header: 'ID' },
          { key: 'name', header: 'Name' },
          { key: 'hasLawyerAccount', header: 'Lawyer Acc.'},
          { key: 'hasCustomerAccount', header: 'Customer Acc.'},
        ];
        break;
      case 'lawyer':
        this.tableColumns = [
          { key: 'lawyerId', header: 'Lawyer ID' },
          { key: 'name', header: 'Name' },
          // { key: 'userId', header: 'User ID' },
        ];
        break;
      case 'customer':
        this.tableColumns = [
          { key: 'customerId', header: 'Customer ID' },
          { key: 'name', header: 'Name' },
          // { key: 'userId', header: 'User ID' },
        ];
        break;
    }
  }

  performSearch(page: number): void {
    if (!this.searchQuery) {
      this.results = []; this.totalItems = 0; return;
    }
    if (!this.currentAttributeId) {
      this.toastService.showError("Account context not set. Cannot perform search.");
      this.results = []; this.totalItems = 0; this.isLoading = false; return;
    }

    this.isLoading = true;
    this.currentPage = page;

    // Update URL with current page
    this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { page: this.currentPage },
        queryParamsHandling: 'merge', // Keep other query params
        replaceUrl: true // Avoid adding to browser history for pagination
    });


    const paginationParams = {
      begin: (this.currentPage - 1) * this.itemsPerPage,
      end: this.currentPage * this.itemsPerPage -1 // API might be 0-indexed for 'end' or expect 'count'
    };

    let count$: Observable<CountInformationDto>;
    let search$: Observable<any>; // Type will be specific DTO like CaseSearchInformationDto

    switch (this.searchType) {
      case 'case':
        const caseCountParams: CaseCountParametersDto = { query: this.searchQuery, attributeId: this.currentAttributeId };
        const caseSearchParams: CaseSearchParametersDto = { ...caseCountParams, pagination: paginationParams };
        count$ = this.caseSearchService.count(caseCountParams);
        search$ = this.caseSearchService.search(caseSearchParams);
        break;
      case 'user':
        const userCountParams: UserCountParametersDto = { query: this.searchQuery, attributeId: this.currentAttributeId };
        const userSearchParams: UserSearchParametersDto = { ...userCountParams, pagination: paginationParams };
        count$ = this.userSearchService.count(userCountParams);
        search$ = this.userSearchService.search(userSearchParams);
        break;
      case 'lawyer':
        const lawyerCountParams: LawyerCountParametersDto = { query: this.searchQuery, attributeId: this.currentAttributeId }; // Assuming DTO similarity
        const lawyerSearchParams: LawyerSearchParametersDto = { ...lawyerCountParams, pagination: paginationParams };
        count$ = this.lawyerSearchService.count(lawyerCountParams);
        search$ = this.lawyerSearchService.search(lawyerSearchParams);
        break;
      case 'customer':
        const customerCountParams: CustomerCountParametersDto = { query: this.searchQuery, attributeId: this.currentAttributeId };
        const customerSearchParams: CustomerSearchParametersDto = { ...customerCountParams, pagination: paginationParams };
        count$ = this.customerSearchService.count(customerCountParams);
        search$ = this.customerSearchService.search(customerSearchParams);
        break;
      default:
        this.isLoading = false;
        return;
    }

    forkJoin([count$, search$]).pipe(
        finalize(() => this.isLoading = false)
    ).subscribe({
      next: ([countResponse, searchResponse]) => {
        this.totalItems = countResponse.count || 0;
        this.results = searchResponse.items || [];
        if (this.results.length === 0 && this.totalItems > 0 && this.currentPage > 1) {
            // If on a page with no results but there are results on previous pages (e.g., after deleting last item on a page)
            this.performSearch(this.currentPage - 1);
        }
      },
      error: (err) => {
        // console.error("Search operation failed:", err);
        // Toast handled by interceptor
        this.results = [];
        this.totalItems = 0;
      }
    });
  }

  onPageChange(page: number): void {
    if (page !== this.currentPage) {
      this.performSearch(page);
    }
  }

  onRowClicked(item: any): void {
    let path = '';
    switch (this.searchType) {
      case 'case': path = `/dashboard/cases/${item.id}`; break;
      case 'user': path = `/dashboard/users/${item.id}`; break;
      case 'lawyer': path = `/dashboard/lawyers/${item.lawyerId}`; break; // Use correct ID property
      case 'customer': path = `/dashboard/customers/${item.customerId}`; break; // Use correct ID property
    }
    if (path) {
      this.router.navigate([path]);
    }
  }

  ngOnDestroy(): void {
    if (this.queryParamsSubscription) {
      this.queryParamsSubscription.unsubscribe();
    }
    if (this.attributeIdSubscription) {
        this.attributeIdSubscription.unsubscribe();
    }
  }
}