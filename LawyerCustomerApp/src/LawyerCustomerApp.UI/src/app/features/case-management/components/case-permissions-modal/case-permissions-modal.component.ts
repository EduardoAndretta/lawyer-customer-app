import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators, AbstractControl } from '@angular/forms';
import { Observable, of, Subscription, BehaviorSubject, Subject, combineLatest } from 'rxjs';
import { map, finalize, catchError, tap, distinctUntilChanged, switchMap, debounceTime, startWith } from 'rxjs/operators';

import { PermissionService } from '../../../../core/services/permission.service';
import { ComboDataService } from '../../../../core/services/combo-data.service';
import { UserProfileService, LAWYER_ATTRIBUTE_ID, CUSTOMER_ATTRIBUTE_ID } from '../../../../core/services/user-profile.service';
import { ToastService } from '../../../../core/services/toast.service';

import { KeyValueItem, KeyValueParametersDto, PaginationParams } from '../../../../core/models/common.models';
import { LcaPermissionItem } from '../../../../shared/components/lca-permissions-list/lca-permissions-list.component';
import {
  GrantPermissionsToCaseParametersDto,
  RevokePermissionsToCaseParametersDto,
  EnableUserToGrantPermissionsItem,
  EnableUserToRevokePermissionsItem,
  GrantPermissionsToCaseParametersDtoPermissionProperties,
  EnableUsersToGrantPermissionsParametersDto,
  EnableUsersToRevokePermissionsParametersDto
} from '../../../../core/models/permission.models';

type UserForPermissionSelection = EnableUserToGrantPermissionsItem | EnableUserToRevokePermissionsItem;

@Component({
  selector: 'app-case-permissions-modal',
  templateUrl: './case-permissions-modal.component.html',
  styleUrls: ['./case-permissions-modal.component.css']
})
export class CasePermissionsModalComponent implements OnInit, OnDestroy {
  @Input() isOpen: boolean = false;
  @Input() caseId!: number;
  @Input() currentPermissions: LcaPermissionItem[] = [];
  @Input() mode: 'grant' | 'revoke' = 'grant';
  @Output() closed = new EventEmitter<boolean>();

  permissionsForm!: FormGroup;
  isLoadingSubmission: boolean = false;
  submitted: boolean = false;

  // [Users]
  usersSearchTerms = new Subject<string>();
  usersItemsSource$: BehaviorSubject<UserForPermissionSelection[]> = new BehaviorSubject<UserForPermissionSelection[]>([]);
  isLoadingUsers: boolean = false;

  // [Permissions (Grant/Revoke)]
  permissionsSearchTerms = new Subject<string>();
  permissionsItemsSource$: BehaviorSubject<KeyValueItem<number>[]> = new BehaviorSubject<KeyValueItem<number>[]>([]);
  isLoadingPermissionsField: boolean = false;

  // [Roles]
  rolesSearchTerms = new Subject<string>();
  rolesItemsSource$: BehaviorSubject<KeyValueItem<number>[]> = new BehaviorSubject<KeyValueItem<number>[]>([]);
  isLoadingRoles: boolean = false;

  // [Attributes]
  attributesSearchTerms = new Subject<string>();
  attributesItemsSource$: BehaviorSubject<KeyValueItem<number>[]> = new BehaviorSubject<KeyValueItem<number>[]>([]);
  isLoadingAttributes: boolean = false;

  private subscriptions = new Subscription();
  private currentLoggedInUserAttributeId: number | null = null;
  private readonly AUTOCOMPLETE_API_PAGE_SIZE = 15;

  constructor(
    private fb: FormBuilder,
    private permissionService: PermissionService,
    private comboDataService: ComboDataService,
    private userProfileService: UserProfileService,
    private toastService: ToastService,
    private cdRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    const attributeSub = this.userProfileService.selectedAccountAttributeId$.subscribe(id => {
        this.currentLoggedInUserAttributeId = id;
        this.clearAllAutoCompleteLists();
    });
    this.subscriptions.add(attributeSub);

    this.permissionsForm = this.fb.group({
      permissionsToModify: this.fb.array([])
    });

    this.setupAllSearchTermSubscriptions();
    this.addPermissionEntry();
  }

  private clearAllAutoCompleteLists(): void {
    this.usersItemsSource$.next([]);
    this.permissionsItemsSource$.next([]);
    this.rolesItemsSource$.next([]);
    this.attributesItemsSource$.next([]);
  }

  setupAllSearchTermSubscriptions(): void {
    const pagination: PaginationParams = { begin: 0, end: this.AUTOCOMPLETE_API_PAGE_SIZE - 1 };
    const comboPagination: KeyValueParametersDto = { pagination }; // For combo service

    // [Users]
    this.subscriptions.add(
      this.usersSearchTerms.pipe(
        debounceTime(300),
        distinctUntilChanged(),
        tap(() => { this.isLoadingUsers = true; this.usersItemsSource$.next([]); }),
        switchMap(term => {
          if (term.length < 1 && !this.isOpen) return of([]);
          let userSearchObs$: Observable<EnableUserToGrantPermissionsItem[] | EnableUserToRevokePermissionsItem[] | []>;
          if (this.mode === 'grant') {
            const params: EnableUsersToGrantPermissionsParametersDto = { query: term, pagination };
            userSearchObs$ = this.permissionService.getUsersEnabledToGrantPermissions(params).pipe(
                map(res => res?.items || [])
            );
          } else {
            const params: EnableUsersToRevokePermissionsParametersDto = { query: term, pagination };
            userSearchObs$ = this.permissionService.getUsersEnabledToRevokePermissions(params).pipe(
                map(res => res?.items || [])
            );
          }
          return userSearchObs$.pipe(catchError(() => {this.isLoadingUsers = false; return of([])}));
        }),
        finalize(() => this.isLoadingUsers = false)
      ).subscribe(items => {

        console.log(items)

        this.usersItemsSource$.next(items as UserForPermissionSelection[]);
        this.isLoadingUsers = false;
      })
    );

    // ]Permissions (Grant/Revoke)]
    this.subscriptions.add(
      this.permissionsSearchTerms.pipe(
        debounceTime(300),
        distinctUntilChanged(),
        tap(() => { this.isLoadingPermissionsField = true; this.permissionsItemsSource$.next([]); }),
        switchMap(term => {
         
          const params: KeyValueParametersDto = { ...comboPagination /*, query: term */ }; // Add query if supported by API
          let comboObs$: Observable<KeyValueItem<number>[]>;
          if (this.mode === 'grant') {
            comboObs$ = this.comboDataService.getPermissionsEnabledForGrantCase(params).pipe(map(res => res?.items || []));
          } else {
            comboObs$ = this.comboDataService.getPermissionsEnabledForRevokeCase(params).pipe(map(res => res?.items || []));
          }
          return comboObs$.pipe(
              map(items => this.filterKeyValueItems(items, term)), // Client-side filter if API didn't
              catchError(() => {this.isLoadingPermissionsField = false; return of([])})
          );
        }),
        finalize(() => this.isLoadingPermissionsField = false)
      ).subscribe(items => {
        this.permissionsItemsSource$.next(items);
        this.isLoadingPermissionsField = false;
      })
    );

    // [Roles]
    this.subscriptions.add(
      this.rolesSearchTerms.pipe(
        debounceTime(300),
        distinctUntilChanged(),
        tap(() => { this.isLoadingRoles = true; this.rolesItemsSource$.next([]); }),
        switchMap(term =>
          this.comboDataService.getRoles({ ...comboPagination /*, query: term */ }).pipe(
            map(res => this.filterKeyValueItems(res?.items, term)),
            catchError(() => {this.isLoadingRoles = false; return of([])})
          )
        ),
        finalize(() => this.isLoadingRoles = false)
      ).subscribe(items => {
        this.rolesItemsSource$.next(items);
        this.isLoadingRoles = false;
      })
    );

    // [Attributes]
    this.subscriptions.add(
      this.attributesSearchTerms.pipe(
        debounceTime(300),
        distinctUntilChanged(),
        tap(() => { this.isLoadingAttributes = true; this.attributesItemsSource$.next([]); }),
        switchMap(term =>
          this.comboDataService.getAttributes({ ...comboPagination /*, query: term */ }).pipe(
            map(res => this.filterKeyValueItems(res?.items, term)),
            catchError(() => {this.isLoadingAttributes = false; return of([])})
          )
        ),
        finalize(() => this.isLoadingAttributes = false)
      ).subscribe(items => {
        this.attributesItemsSource$.next(items);
        this.isLoadingAttributes = false;
      })
    );
  }

  private filterKeyValueItems(items: KeyValueItem<number>[] | null, query: string): KeyValueItem<number>[] {
    if (!items) return [];
    if (!query) return items;
    const lowerQuery = query.toLowerCase();
    return items.filter(item => item.key?.toLowerCase().includes(lowerQuery));
  }

  onUsersSearchChanged(term: string): void { this.usersSearchTerms.next(term); }
  onPermissionsSearchChanged(term: string): void { this.permissionsSearchTerms.next(term); }
  onRolesSearchChanged(term: string): void { this.rolesSearchTerms.next(term); }
  onAttributesSearchChanged(term: string): void { this.attributesSearchTerms.next(term); }

  get permissionsToModify(): FormArray {
    return this.permissionsForm.get('permissionsToModify') as FormArray;
  }

  createPermissionEntry(): FormGroup {
    const entry = this.fb.group({
      userId: [null, Validators.required],
      permissionId: [null, Validators.required],
      roleId: [null, Validators.required],
      attributeId: [null, Validators.required],
      _selectedUserCanBeGrantedForSelectedAttribute: [true]
    });

    const userIdCtrl = entry.get('userId');
    const attributeIdCtrl = entry.get('attributeId');

    if (userIdCtrl && attributeIdCtrl) {
        this.subscriptions.add(
            combineLatest([
                userIdCtrl.valueChanges.pipe(startWith(userIdCtrl.value), distinctUntilChanged()),
                attributeIdCtrl.valueChanges.pipe(startWith(attributeIdCtrl.value), distinctUntilChanged())
            ]).pipe(
            ).subscribe(([selectedUserId, selectedAttributeId]) => {
                let canBeGranted = true;
                if (selectedUserId != null && selectedAttributeId != null) {
                    const user = this.usersItemsSource$.value.find(u => u.userId === selectedUserId);
                    if (user) {
                        canBeGranted = this.checkIfUserCanHavePermissionInContext(user, selectedAttributeId);
                    } else {
                         canBeGranted = true;
                    }
                }
                entry.get('_selectedUserCanBeGrantedForSelectedAttribute')?.setValue(canBeGranted);
            })
        );
    }
    return entry;
  }

  private checkIfUserCanHavePermissionInContext(user: UserForPermissionSelection, selectedAttributeId: number): boolean {
    if (this.mode === 'grant') {
        const grantUser = user as EnableUserToGrantPermissionsItem;
        if (selectedAttributeId === LAWYER_ATTRIBUTE_ID) return grantUser.canBeGrantAsLawyer;
        if (selectedAttributeId === CUSTOMER_ATTRIBUTE_ID) return grantUser.canBeGrantAsCustomer;
       
        if (selectedAttributeId === 0) return grantUser.canBeGrantAsUser;
    } else {
        const revokeUser = user as EnableUserToRevokePermissionsItem;
        if (selectedAttributeId === LAWYER_ATTRIBUTE_ID) return revokeUser.canBeRevokeAsLawyer;
        if (selectedAttributeId === CUSTOMER_ATTRIBUTE_ID) return revokeUser.canBeRevokeAsCustomer;
        if (selectedAttributeId === 0) return revokeUser.canBeRevokeAsUser;
    }
    return false; // Default if no specific rule matches
  }

  addPermissionEntry(): void { this.permissionsToModify.push(this.createPermissionEntry()); }
  removePermissionEntry(index: number): void { this.permissionsToModify.removeAt(index); }

  onSubmit(): void {
    this.submitted = true;
    this.permissionsToModify.controls.forEach(control => control.markAllAsTouched());

    if (this.permissionsForm.invalid || this.permissionsToModify.length === 0) { return; }
    if (!this.currentLoggedInUserAttributeId) { return; }

    let allEntriesProgrammaticallyValid = true;
    for (let i = 0; i < this.permissionsToModify.length; i++) {
        const entry = this.permissionsToModify.at(i) as FormGroup;
        if (entry.get('userId')?.value == null || entry.get('attributeId')?.value == null) continue;

        if (!entry.get('_selectedUserCanBeGrantedForSelectedAttribute')?.value) {
            allEntriesProgrammaticallyValid = false;
        }
    }
    if (!allEntriesProgrammaticallyValid) {
        this.isLoadingSubmission = false; return;
    }

    this.isLoadingSubmission = true;
    const formValue = this.permissionsForm.value;
    const permissionsPayload: GrantPermissionsToCaseParametersDtoPermissionProperties[] =
        formValue.permissionsToModify.map((p: any) => ({
            userId: +p.userId,
            permissionId: +p.permissionId,
            roleId: +p.roleId,
            attributeId: +p.attributeId
        }));

    let apiCall: Observable<any>;
    // ... (grant/revoke API call logic as before, using this.permissionService) ...
    if (this.mode === 'grant') {
      // ... filter newPermissions ...
      const grantParams: GrantPermissionsToCaseParametersDto = {
        relatedCaseId: this.caseId,
        attributeId: this.currentLoggedInUserAttributeId,
        permissions: permissionsPayload // Pass filtered newPermissions here
      };
      apiCall = this.permissionService.grantPermissionsToCase(grantParams);
    } else {
      const revokeParams: RevokePermissionsToCaseParametersDto = {
        relatedCaseId: this.caseId,
        attributeId: this.currentLoggedInUserAttributeId,
        permissions: permissionsPayload
      };
      apiCall = this.permissionService.revokePermissionsFromCase(revokeParams);
    }


    this.subscriptions.add(
        apiCall.pipe(finalize(() => this.isLoadingSubmission = false))
        .subscribe({ next: () => this.closeModal(true), error: () => {} })
    );
  }

  closeModal(dataChanged: boolean = false): void { /* ... */ this.isOpen = false; this.closed.emit(dataChanged); }
  getFormControl(index: number, controlName: string): AbstractControl | null { /* ... */ return this.permissionsToModify.at(index)?.get(controlName) || null; }
  isPermissionEntryUserAttributeValid(index: number): boolean { /* ... */ return (this.permissionsToModify.at(index) as FormGroup).get('_selectedUserCanBeGrantedForSelectedAttribute')?.value === true; }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    this.usersSearchTerms.complete(); this.usersItemsSource$.complete();
    this.permissionsSearchTerms.complete(); this.permissionsItemsSource$.complete();
    this.rolesSearchTerms.complete(); this.rolesItemsSource$.complete();
    this.attributesSearchTerms.complete(); this.attributesItemsSource$.complete();
  }
}