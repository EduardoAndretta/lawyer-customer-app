import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators, AbstractControl } from '@angular/forms';
import { forkJoin, Observable, of, Subscription } from 'rxjs';
import { map, finalize, startWith, tap } from 'rxjs/operators';

import { CaseService } from '../../services/case.service';
import { ComboDataService } from '../../../../core/services/combo-data.service';
import { UserSearchService } from '../../../search/services/user-search.service'; // [For user lookup]
import { UserProfileService } from '../../../../core/services/user-profile.service';
import { ToastService } from '../../../../core/services/toast.service';

import { LcaSelectOption } from '../../../../shared/components/lca-select/lca-select.component';
import { KeyValueItem, PermissionDetail } from '../../../../core/models/common.models';
import { UserSearchInformationItem } from '../../../../core/models/user.models';
import { LcaPermissionItem } from '../../../../shared/components/lca-permissions-list/lca-permissions-list.component';


@Component({
  selector: 'app-case-permissions-modal',
  templateUrl: './case-permissions-modal.component.html',
  styleUrls: ['./case-permissions-modal.component.css']
})
export class CasePermissionsModalComponent implements OnInit, OnDestroy {
  @Input() isOpen: boolean = false;
  @Input() caseId!: number;
  @Input() currentPermissions: LcaPermissionItem[] = []; // Used for revoke mode and to avoid duplicate grants
  @Input() mode: 'grant' | 'revoke' = 'grant';

  @Output() closed = new EventEmitter<boolean>(); // Emits true if data changed

  permissionsForm!: FormGroup;
  isLoading: boolean = false;
  isDataLoading: boolean = false; // For loading combo box data
  submitted: boolean = false;

  // Combo box options
  users$: Observable<LcaSelectOption[]> = of([]);
  grantablePermissions$: Observable<LcaSelectOption[]> = of([]);
  revokablePermissions$: Observable<LcaSelectOption[]> = of([]); // Based on existing permissions
  roles$: Observable<LcaSelectOption[]> = of([]);
  attributes$: Observable<LcaSelectOption[]> = of([]); // For permission context

  private attributeIdSubscription!: Subscription;
  private currentLoggedInUserAttributeId: number | null = null;

  constructor(
    private fb: FormBuilder,
    private caseService: CaseService,
    private comboDataService: ComboDataService,
    private userSearchService: UserSearchService, // For a basic user search for the dropdown
    private userProfileService: UserProfileService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.attributeIdSubscription = this.userProfileService.selectedAccountAttributeId$.subscribe(id => {
        this.currentLoggedInUserAttributeId = id;
    });

    this.permissionsForm = this.fb.group({
      permissionsToModify: this.fb.array([])
    });

    if (this.mode === 'grant') {
      this.loadGrantComboData();
      this.addPermissionEntry(); // Add one entry by default for grant
    } else { // revoke mode
      this.loadRevokeComboData();
      // For revoke, pre-populate with existing permissions if designed that way,
      // or use a multi-select of existing permissions.
      // Here, we'll assume user selects from a dropdown of *their* existing permissions.
      this.addPermissionEntry();
    }
  }

  get permissionsToModify(): FormArray {
    return this.permissionsForm.get('permissionsToModify') as FormArray;
  }

  createPermissionEntry(): FormGroup {
    return this.fb.group({
      userId: [null, Validators.required],
      permissionId: [null, Validators.required],
      roleId: [null, Validators.required],
      attributeId: [null, Validators.required] // The attribute context OF THE PERMISSION being granted/revoked
    });
  }

  addPermissionEntry(): void {
    this.permissionsToModify.push(this.createPermissionEntry());
  }

  removePermissionEntry(index: number): void {
    this.permissionsToModify.removeAt(index);
  }

  private mapToLcaSelectOption(items: KeyValueItem<number>[] | null): LcaSelectOption[] {
    if (!items) return [];
    return items.map(item => ({ label: item.key || 'Unknown', value: item.value }));
  }

  private mapUsersToLcaSelectOption(users: UserSearchInformationItem[] | null): LcaSelectOption[] {
    if (!users) return [];
    return users.map(user => ({ label: `${user.name} (ID: ${user.id})`, value: user.id! }));
  }

  loadGrantComboData(): void {
    this.isDataLoading = true;
    const comboPagination = { pagination: { begin: 0, end: 999 } }; // Fetch all for combos

    // Basic user search for dropdown - this might need a more specific "combo" endpoint for users
    // Or a typeahead if list is very large. For now, a simple search.
    // Assuming userSearchService.search can be called with an empty query to list users (or a specific combo endpoint)
    this.users$ = this.userSearchService.search({ query: '', attributeId: this.currentLoggedInUserAttributeId, pagination: { begin: comboPagination.pagination.begin, end: comboPagination.pagination.end }}).pipe(
        map(response => this.mapUsersToLcaSelectOption(response.items))
    );

    this.grantablePermissions$ = this.comboDataService.getPermissionsEnabledForGrantCase(comboPagination).pipe(
        map(response => this.mapToLcaSelectOption(response.items))
    );
    this.roles$ = this.comboDataService.getRoles(comboPagination).pipe(
        map(response => this.mapToLcaSelectOption(response.items))
    );
    this.attributes$ = this.comboDataService.getAttributes(comboPagination).pipe(
        map(response => this.mapToLcaSelectOption(response.items))
    );

    // Check when all data is loaded
    forkJoin([this.users$, this.grantablePermissions$, this.roles$, this.attributes$]).pipe(
        finalize(() => this.isDataLoading = false)
    ).subscribe({
        error: () => this.toastService.showError("Failed to load data for permissions form.")
    });
  }

  loadRevokeComboData(): void {
    this.isDataLoading = true;
    const comboPagination = { pagination: { begin: 0, end: 999 } };

    // For revoke, users are derived from currentPermissions
    const uniqueUserIds = [...new Set(this.currentPermissions.map(p => p.userId).filter(id => id != null))];
    if (uniqueUserIds.length > 0) {
        // This part is tricky: you need to fetch names for these user IDs.
        // A bulk user details endpoint or multiple calls would be needed.
        // For simplicity, we'll just show IDs or assume names are in currentPermissions.
        this.users$ = of(this.currentPermissions.map(p => ({
            label: `${p.userName || 'User'} (ID: ${p.userId})`,
            value: p.userId!
        })).filter((item, index, self) => self.findIndex(t => t.value === item.value) === index)); // Unique users
    } else {
        this.users$ = of([]);
    }


    // Revokable permissions, roles, attributes should be derived from existing permissions or combo endpoints
    this.revokablePermissions$ = this.comboDataService.getPermissionsEnabledForRevokeCase(comboPagination).pipe(
        map(response => this.mapToLcaSelectOption(response.items))
    );
    // Or, more accurately, populate based on distinct permissions in `this.currentPermissions`
    // this.revokablePermissions$ = of([...new Set(this.currentPermissions.map(p => p.permissionId))]
    // .map(id => ({ value: id, label: this.currentPermissions.find(p=>p.permissionId === id)?.permissionName || `Perm ID: ${id}` })));


    this.roles$ = this.comboDataService.getRoles(comboPagination).pipe(
        map(response => this.mapToLcaSelectOption(response.items))
    );
    this.attributes$ = this.comboDataService.getAttributes(comboPagination).pipe(
        map(response => this.mapToLcaSelectOption(response.items))
    );

    forkJoin([this.users$, this.revokablePermissions$, this.roles$, this.attributes$]).pipe(
        finalize(() => this.isDataLoading = false)
    ).subscribe({
        error: () => this.toastService.showError("Failed to load data for permissions form.")
    });
  }

  onSubmit(): void {
    this.submitted = true;
    if (this.permissionsForm.invalid || this.permissionsToModify.length === 0) {
      this.toastService.showError("Please fill all required fields for each permission entry.");
      return;
    }
    if (!this.currentLoggedInUserAttributeId) {
        this.toastService.showError("Your current account context is not set. Cannot modify permissions.");
        return;
    }

    this.isLoading = true;
    const formValue = this.permissionsForm.value;
    const permissionsPayload: PermissionDetail[] = formValue.permissionsToModify.map((p: any) => ({
      userId: +p.userId,
      permissionId: +p.permissionId,
      roleId: +p.roleId,
      attributeId: +p.attributeId // Attribute of the permission itself
    }));

    let apiCall: Observable<any>;

    if (this.mode === 'grant') {
      // Optional: Filter out permissions that already exist to prevent errors.
      // This depends on how your backend handles duplicate grants.
      const newPermissions = permissionsPayload.filter(np =>
        !this.currentPermissions.some(cp =>
            cp.userId === np.userId &&
            cp.permissionId === np.permissionId &&
            cp.roleId === np.roleId &&
            cp.attributeId === np.attributeId
        )
      );
      if (newPermissions.length === 0 && permissionsPayload.length > 0) {
          this.toastService.showInfo("All selected permissions are already granted.");
          this.isLoading = false;
          return;
      }
      if (newPermissions.length === 0 && permissionsPayload.length === 0) {
          this.toastService.showInfo("No permissions selected to grant.");
          this.isLoading = false;
          return;
      }

      apiCall = this.caseService.grantPermissions({
        caseId: this.caseId,
        attributeId: this.currentLoggedInUserAttributeId, // Attribute of the user granting
        permissions: newPermissions
      });
    } else { // revoke
      apiCall = this.caseService.revokePermissions({
        caseId: this.caseId,
        attributeId: this.currentLoggedInUserAttributeId, // Attribute of the user revoking
        permissions: permissionsPayload
      });
    }

    apiCall.pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: () => {
        this.closeModal(true); // Signal refresh
      },
      error: (err) => {
        // Handled by interceptor
      }
    });
  }

  closeModal(dataChanged: boolean = false): void {
    this.isOpen = false;
    this.closed.emit(dataChanged);
  }

  // Helper to get control for validation messages in template
  getFormControl(index: number, controlName: string): AbstractControl | null {
    return this.permissionsToModify.at(index)?.get(controlName) || null;
  }


  ngOnDestroy(): void {
      if (this.attributeIdSubscription) {
          this.attributeIdSubscription.unsubscribe();
      }
  }
}