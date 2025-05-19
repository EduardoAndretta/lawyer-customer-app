import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators, AbstractControl } from '@angular/forms';
import { Observable, of, Subscription, forkJoin, BehaviorSubject, combineLatest } from 'rxjs';
import { map, finalize, catchError, tap, distinctUntilChanged, switchMap, startWith } from 'rxjs/operators';

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
  GrantPermissionsToCaseParametersDtoPermissionProperties
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
  isLoading: boolean = false;
  isDataLoading: boolean = true;
  submitted: boolean = false;

  usersList$: BehaviorSubject<UserForPermissionSelection[]> = new BehaviorSubject<UserForPermissionSelection[]>([]);
  grantablePermissionsList$: BehaviorSubject<KeyValueItem<number>[]> = new BehaviorSubject<KeyValueItem<number>[]>([]);
  revokablePermissionsList$: BehaviorSubject<KeyValueItem<number>[]> = new BehaviorSubject<KeyValueItem<number>[]>([]);
  rolesList$: BehaviorSubject<KeyValueItem<number>[]> = new BehaviorSubject<KeyValueItem<number>[]>([]);
  attributesList$: BehaviorSubject<KeyValueItem<number>[]> = new BehaviorSubject<KeyValueItem<number>[]>([]);

  private subscriptions = new Subscription();
  private currentLoggedInUserAttributeId: number | null = null;
  private readonly COMBO_LIST_PAGE_SIZE = 100;

  constructor(
    private fb: FormBuilder,
    private permissionService: PermissionService,
    private comboDataService: ComboDataService,
    private userProfileService: UserProfileService,
    private toastService: ToastService,
    private cdRef: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.isDataLoading = true;

    const attributeSub = this.userProfileService.selectedAccountAttributeId$.subscribe(id => {
        this.currentLoggedInUserAttributeId = id;
        if (this.isOpen) {
            this.loadAllAutoCompleteData();
        }
    });
    this.subscriptions.add(attributeSub);

    this.permissionsForm = this.fb.group({
      permissionsToModify: this.fb.array([])
    });

    if (this.currentLoggedInUserAttributeId !== null) {
        this.loadAllAutoCompleteData();
    } else {
        this.toastService.showInfo("Waiting for user context to load permission options...");
        this.isDataLoading = false; // Allow UI to render, message shown
    }
    this.addPermissionEntry();
  }

  private clearAllLists(): void {
    this.usersList$.next([]);
    this.grantablePermissionsList$.next([]);
    this.revokablePermissionsList$.next([]);
    this.rolesList$.next([]);
    this.attributesList$.next([]);
  }

  loadAllAutoCompleteData(): void {
    if (!this.currentLoggedInUserAttributeId) {
        this.toastService.showInfo("User context (Attribute ID) is not set. Options might be limited.");
    }

    this.isDataLoading = true;
    this.clearAllLists();

    const comboPagination: KeyValueParametersDto = {
        pagination: { begin: 0, end: this.COMBO_LIST_PAGE_SIZE - 1 }
    };

    let usersObs$: Observable<UserForPermissionSelection[]>;
    if (this.mode === 'grant') {
        usersObs$ = this.permissionService.getUsersEnabledToGrantPermissions({}).pipe( // Empty params for now
            map(response => response?.items || []),
            catchError(() => { this.toastService.showError("Failed to load users list for granting."); return of([]); })
        );
    } else {
        usersObs$ = this.permissionService.getUsersEnabledToRevokePermissions({}).pipe( // Empty params for now
            map(response => response?.items || []),
            catchError(() => { this.toastService.showError("Failed to load users list for revoking."); return of([]); })
        );
    }

    const grantPermsObs$ = this.comboDataService.getPermissionsEnabledForGrantCase(comboPagination).pipe(
        map(response => response?.items || []),
        catchError(() => { this.toastService.showError("Failed to load grantable permissions list."); return of([]); })
    );
    const revokePermsObs$ = this.comboDataService.getPermissionsEnabledForRevokeCase(comboPagination).pipe(
        map(response => response?.items || []),
        catchError(() => { this.toastService.showError("Failed to load revokable permissions list."); return of([]); })
    );
    const rolesObs$ = this.comboDataService.getRoles(comboPagination).pipe(
        map(response => response?.items || []),
        catchError(() => { this.toastService.showError("Failed to load roles list."); return of([]); })
    );
    const attributesObs$ = this.comboDataService.getAttributes(comboPagination).pipe(
        map(response => response?.items || []),
        catchError(() => { this.toastService.showError("Failed to load attributes list."); return of([]); })
    );

    this.subscriptions.add(
        forkJoin([ usersObs$, grantPermsObs$, revokePermsObs$, rolesObs$, attributesObs$ ])
        .pipe(finalize(() => {
            this.isDataLoading = false;
            this.cdRef.detectChanges();
        }))
        .subscribe(([users, grantPerms, revokePerms, roles, attributes]) => {
            this.usersList$.next(users as UserForPermissionSelection[]); // Cast if necessary
            this.grantablePermissionsList$.next(grantPerms);
            this.revokablePermissionsList$.next(revokePerms);
            this.rolesList$.next(roles);
            this.attributesList$.next(attributes);
        })
    );
  }

  get permissionsToModify(): FormArray {
    return this.permissionsForm.get('permissionsToModify') as FormArray;
  }

  createPermissionEntry(): FormGroup {
    const entry = this.fb.group({
      userId: [null, Validators.required],
      permissionId: [null, Validators.required],
      roleId: [null, Validators.required],
      attributeId: [null, Validators.required],
      // [Internal state for validation based on selected user and attribute]
      _selectedUserCanBeGrantedForSelectedAttribute: [false]
    });

    // Subscribe to changes in userId and attributeId for this specific entry
    const userIdControl = entry.get('userId');
    const attributeIdControl = entry.get('attributeId');

    if (userIdControl && attributeIdControl) {
        this.subscriptions.add(
            combineLatest([
                userIdControl.valueChanges.pipe(startWith(userIdControl.value)),
                attributeIdControl.valueChanges.pipe(startWith(attributeIdControl.value))
            ]).pipe(
                distinctUntilChanged(([prevUser, prevAttr], [currUser, currAttr]) => prevUser === currUser && prevAttr === currAttr),
                switchMap(([selectedUserId, selectedAttributeId]) => {
                    if (selectedUserId && selectedAttributeId !== null && selectedAttributeId !== undefined) {
                        // [Find the full user object from the loaded list]
                        const user = this.usersList$.value.find(u => u.userId === selectedUserId);
                        if (user) {
                           return of(this.checkIfUserCanBeGranted(user, selectedAttributeId));
                        }
                    }
                    return of(false); // [Default to false if not enough info]
                })
            ).subscribe(canBeGranted => {
                entry.get('_selectedUserCanBeGrantedForSelectedAttribute')?.setValue(canBeGranted);
            })
        );
    }
    return entry;
  }

  private checkIfUserCanBeGranted(user: UserForPermissionSelection, selectedAttributeId: number): boolean {
    if (this.mode === 'grant') {
        const grantUser = user as EnableUserToGrantPermissionsItem;
        if (selectedAttributeId === 0 || selectedAttributeId === null) { // [Assuming 0 or null means 'As User' (no specific attribute)]
            return grantUser.canBeGrantAsUser;
        } else if (selectedAttributeId === LAWYER_ATTRIBUTE_ID) { // [LAWYER_ATTRIBUTE_ID = 1]
            return grantUser.canBeGrantAsLawyer;
        } else if (selectedAttributeId === CUSTOMER_ATTRIBUTE_ID) { // [CUSTOMER_ATTRIBUTE_ID = 2]
            return grantUser.canBeGrantAsCustomer;
        }
    } else { // revoke mode
        const revokeUser = user as EnableUserToRevokePermissionsItem;
         if (selectedAttributeId === 0 || selectedAttributeId === null) {
            return revokeUser.canBeRevokeAsUser;
        } else if (selectedAttributeId === LAWYER_ATTRIBUTE_ID) {
            return revokeUser.canBeRevokeAsLawyer;
        } else if (selectedAttributeId === CUSTOMER_ATTRIBUTE_ID) {
            return revokeUser.canBeRevokeAsCustomer;
        }
    }
    return false;
  }

  addPermissionEntry(): void {
    this.permissionsToModify.push(this.createPermissionEntry());
  }

  removePermissionEntry(index: number): void {
    this.permissionsToModify.removeAt(index);
  }

  // [Item selected handlers for Autocomplete (updates the form control with ID)]
  onUserSelected(item: UserForPermissionSelection, index: number): void {
    this.permissionsToModify.at(index).get('userId')?.setValue(item.userId);
  }
  onPermissionSelected(item: KeyValueItem<number>, index: number): void {
    this.permissionsToModify.at(index).get('permissionId')?.setValue(item.value);
  }
  onRoleSelected(item: KeyValueItem<number>, index: number): void {
    this.permissionsToModify.at(index).get('roleId')?.setValue(item.value);
  }
  onAttributeSelected(item: KeyValueItem<number>, index: number): void {
    this.permissionsToModify.at(index).get('attributeId')?.setValue(item.value);
  }

  onSubmit(): void {
    this.submitted = true;
    this.permissionsToModify.controls.forEach((control: AbstractControl) => {
        control.markAllAsTouched();
    });


    if (this.permissionsForm.invalid || this.permissionsToModify.length === 0) {
      this.toastService.showError("Please fill all required fields for each permission entry.");
      return;
    }
    if (!this.currentLoggedInUserAttributeId) {
        this.toastService.showError("Your current account context is not set. Cannot modify permissions.");
        return;
    }

    let allEntriesValid = true;
    for (let i = 0; i < this.permissionsToModify.length; i++) {
        const entry = this.permissionsToModify.at(i) as FormGroup;
        if (!entry.get('_selectedUserCanBeGrantedForSelectedAttribute')?.value) {
            const selectedUserValue = entry.get('userId')?.value;
            const selectedUser = this.usersList$.value.find(u => u.userId === selectedUserValue);
            const userName = selectedUser?.name || `User ID ${selectedUserValue}`;
            const attributeId = entry.get('attributeId')?.value;
            const attribute = this.attributesList$.value.find(a => a.value === attributeId);
            const attributeName = attribute?.key || `Attribute ID ${attributeId}`;

            this.toastService.showError(
                `Entry #${i + 1}: User "${userName}" cannot have permissions ${this.mode === 'grant' ? 'granted' : 'revoked'} for attribute context "${attributeName}".`
            );
            allEntriesValid = false;
        }
    }
    if (!allEntriesValid) {
        this.isLoading = false;
        return;
    }

    this.isLoading = true;
    const formValue = this.permissionsForm.value;
    const permissionsPayload: GrantPermissionsToCaseParametersDtoPermissionProperties[] =
        formValue.permissionsToModify.map((p: any) => ({
            userId: +p.userId,
            permissionId: +p.permissionId,
            roleId: +p.roleId,
            attributeId: +p.attributeId
    }));

    let apiCall: Observable<any>;
    if (this.mode === 'grant') {
      const newPermissions = permissionsPayload.filter((np: GrantPermissionsToCaseParametersDtoPermissionProperties) =>
        !this.currentPermissions.some(cp =>
            cp.userId       === np.userId &&
            cp.permissionId === np.permissionId &&
            cp.roleId       === np.roleId &&
            cp.attributeId  === np.attributeId
        )
      );
      if (newPermissions.length === 0 && permissionsPayload.length > 0) {
          this.toastService.showInfo("All selected permissions are already granted for the given users/attributes.");
          this.isLoading = false; return;
      }
      if (newPermissions.length === 0 && permissionsPayload.length === 0) {
           this.toastService.showInfo("No new permissions selected to grant.");
           this.isLoading = false; return;
      }
      const grantParams: GrantPermissionsToCaseParametersDto = {
        relatedCaseId: this.caseId,
        attributeId: this.currentLoggedInUserAttributeId,
        permissions: newPermissions
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
        apiCall.pipe(
        finalize(() => this.isLoading = false)
        ).subscribe({
        next: () => this.closeModal(true),
        error: (err) => { }
        })
    );
  }

  closeModal(dataChanged: boolean = false): void {
    this.isOpen = false;
    this.closed.emit(dataChanged);
  }

  getFormControl(index: number, controlName: string): AbstractControl | null {
    return this.permissionsToModify.at(index)?.get(controlName) || null;
  }

  isPermissionEntryUserAttributeValid(index: number): boolean {
    const entry = this.permissionsToModify.at(index) as FormGroup;
    return entry.get('_selectedUserCanBeGrantedForSelectedAttribute')?.value === true;
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    this.usersList$.complete();
    this.grantablePermissionsList$.complete();
    this.revokablePermissionsList$.complete();
    this.rolesList$.complete();
    this.attributesList$.complete();
  }
}