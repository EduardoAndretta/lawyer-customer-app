import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription, forkJoin, Observable, combineLatest, of } from 'rxjs';
import { switchMap, finalize, distinctUntilChanged, map, catchError, tap } from 'rxjs/operators';

import { UserProfileService } from '../../../core/services/user-profile.service';
import { ToastService } from '../../../core/services/toast.service';
import { LcaTableColumn } from '../../../shared/components/lca-table/lca-table.component';

import { CaseSearchService } from '../services/case-search.service';
import { UserSearchService } from '../services/user-search.service';
import { LawyerSearchService } from '../services/lawyer-search.service';
import { CustomerSearchService } from '../services/customer-search.service';

import { CaseSearchParametersDto, CaseCountParametersDto, CaseSearchInformationDto } from '../../../core/models/case.models';
import { UserSearchParametersDto, UserCountParametersDto, UserSearchInformationDto } from '../../../core/models/user.models';
import { LawyerSearchParametersDto, LawyerCountParametersDto, LawyerSearchInformationDto } from '../../../core/models/lawyer.models';
import { CustomerSearchParametersDto, CustomerCountParametersDto, CustomerSearchInformationDto } from '../../../core/models/customer.models';
import { CountInformationDto, PaginationParams } from '../../../core/models/common.models';


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
  itemsPerPage: number = 10;

  private currentAttributeId: number | null = null;
  private subscriptions = new Subscription();

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
    const queryParams$ = this.route.queryParams.pipe(
      distinctUntilChanged((prev, curr) =>
        prev['query'] === curr['query'] &&
        prev['type'] === curr['type'] &&
        prev['page'] === curr['page']
      )
    );

    const attributeId$ = this.userProfileService.selectedAccountAttributeId$.pipe(
      distinctUntilChanged()
    );

    let combinedSearchTrigger$ = combineLatest([queryParams$, attributeId$]).pipe(
      switchMap(([params, attrId]) => {

        this.searchType = this.parseTypeParam(params['type']);
        this.searchQuery = params['query']

        this.currentPage = params['page'] ? +params['page'] : 1;
        this.currentAttributeId = attrId;

        this.setupTableColumns();

        if (this.searchQuery && this.currentAttributeId !== null) {
          return this.executeSearchLogic(this.currentPage, this.searchQuery, this.searchType, this.currentAttributeId);
        } else if (this.searchQuery && this.currentAttributeId === null) {
          this.results = [];
          this.totalItems = 0;
          this.toastService.showInfo("Please select an account (Lawyer/Customer) from the sidebar to perform a search.");
          return of({ results: [], totalItems: 0, isLoading: false });
        } else {
          this.results = [];
          this.totalItems = 0;
          return of({ results: [], totalItems: 0, isLoading: false });
        }
      })
    );

    this.subscriptions.add(
      combinedSearchTrigger$.subscribe(searchOutcome => {
        this.results = searchOutcome.results;
        this.totalItems = searchOutcome.totalItems;
        this.isLoading = searchOutcome.isLoading;
      })
    );
  }

  private parseTypeParam(raw: string | string[] | undefined): SearchableEntityType {
    if (!raw) return 'case';
    const value = Array.isArray(raw) ? raw[0] : raw;
    return this.toSearchableEntityType(value);
  }

  private toSearchableEntityType(value: string): SearchableEntityType {
    const validTypes: SearchableEntityType[] = ['case', 'user', 'lawyer', 'customer'];
    return validTypes.includes(value as SearchableEntityType) ? value as SearchableEntityType : 'case';
  }

  private executeSearchLogic(
    page: number,
    query: string,
    type: SearchableEntityType,
    attributeId: number
  ): Observable<{ results: any[], totalItems: number, isLoading: boolean }> {
    this.isLoading = true;

    const paginationParams: PaginationParams = {
      begin: (page - 1) * this.itemsPerPage,
      end: page * this.itemsPerPage - 1
    };

    let count$: Observable<CountInformationDto>;
    let search$: Observable<CaseSearchInformationDto | UserSearchInformationDto | LawyerSearchInformationDto | CustomerSearchInformationDto | null>;

    switch (type) {
      case 'case':
        const caseCountParams: CaseCountParametersDto = { query, attributeId };
        const caseSearchParams: CaseSearchParametersDto = { ...caseCountParams, pagination: paginationParams };
        count$ = this.caseSearchService.count(caseCountParams);
        search$ = this.caseSearchService.search(caseSearchParams);
        break;
      case 'user':
        const userCountParams: UserCountParametersDto = { query, attributeId };
        const userSearchParams: UserSearchParametersDto = { ...userCountParams, pagination: paginationParams };
        count$ = this.userSearchService.count(userCountParams);
        search$ = this.userSearchService.search(userSearchParams);
        break;
      case 'lawyer':
        const lawyerCountParams: LawyerCountParametersDto = { query, attributeId };
        const lawyerSearchParams: LawyerSearchParametersDto = { ...lawyerCountParams, pagination: paginationParams };
        count$ = this.lawyerSearchService.count(lawyerCountParams);
        search$ = this.lawyerSearchService.search(lawyerSearchParams);
        break;
      case 'customer':
        const customerCountParams: CustomerCountParametersDto = { query, attributeId };
        const customerSearchParams: CustomerSearchParametersDto = { ...customerCountParams, pagination: paginationParams };
        count$ = this.customerSearchService.count(customerCountParams);
        search$ = this.customerSearchService.search(customerSearchParams);
        break;
      default:
        this.isLoading = false;
        return of({ results: [], totalItems: 0, isLoading: false });
    }

    return forkJoin([count$, search$]).pipe(
      map(([countResponse, searchResponseData]) => {
        const total = countResponse?.count || 0;

        const items = searchResponseData?.items || [];

        console.log(total)
        console.log(items)

        if (items.length === 0 && total > 0 && page > 1) {
           
        }
        return { results: items, totalItems: total, isLoading: false };
      }),
      catchError(err => {
        console.error(`Search operation failed for type ${type}:`, err);

        return of({ results: [], totalItems: 0, isLoading: false });
      }),
      finalize(() => this.isLoading = false)
    );
  }

  private setupTableColumns(): void {
    this.tableColumns = [];
    switch (this.searchType) {
      case 'case':
        this.tableColumns = [
          { key: 'id', header: 'ID' },
          { key: 'title', header: 'Title' },
          { key: 'description', header: 'Description' },
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
        ];
        break;
      case 'customer':
        this.tableColumns = [
          { key: 'customerId', header: 'Customer ID' },
          { key: 'name', header: 'Name' },
        ];
        break;
    }
  }

  onPageChange(page: number): void {
    if (page !== this.currentPage) {
      this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { page: page },
        queryParamsHandling: 'merge'
      });
    }
  }

  onRowClicked(item: any): void {
    let path = '';
    switch (this.searchType) {
      case 'case': path = `/dashboard/cases/${item.id}`; break;
      case 'user': path = `/dashboard/users/${item.id}`; break;
      case 'lawyer': path = `/dashboard/lawyers/${item.lawyerId}`; break;
      case 'customer': path = `/dashboard/customers/${item.customerId}`; break;
    }
    if (path) {
      this.router.navigate([path]);
    }
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}