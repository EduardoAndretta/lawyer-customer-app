import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators, AbstractControl } from '@angular/forms';
import { forkJoin, Observable, of, Subscription } from 'rxjs';
import { map, finalize, startWith, tap } from 'rxjs/operators';

import { UserService } from '../../services/user.service';
import { ComboDataService } from '../../../../core/services/combo-data.service';
// UserSearchService is NOT needed here, as we are granting TO a specific user, not selecting from a list.
// For the 'User' field in permission entry, it will be pre-filled with targetUserId.
import { UserProfileService } from '../../../../core/services/user-profile.service';
import { ToastService } from '../../../../core/services/toast.service';

import { LcaSelectOption } from '../../../../shared/components/lca-select/lca-select.component';
import { KeyValueItem, PermissionDetail } from '../../../../core/models/common.models';
import { LcaPermissionItem } from '../../../../shared/components/lca-permissions-list/lca-permissions-list.component';


@Component({
  selector: 'app-user-permissions-modal',
  templateUrl: './user-permissions-modal.component.html',
  styleUrls: ['./user-permissions-modal.component.css'] // Can share styles with case-permissions-modal
})
export class UserPermissionsModalComponent implements OnInit, OnDestroy {
  @Input() isOpen: boolean = false;
  @Input() targetUserId!: number; // The user FOR WHOM permissions are being changed
  @Input() currentPermissions: LcaPermissionItem[] = [];
  @Input() mode: 'grant' | 'revoke' = 'grant';

  @Output() closed = new EventEmitter<boolean>();

  permissionsForm!: FormGroup;
  isLoading: boolean = false;
  isDataLoading: boolean = false;
  submitted: boolean = false;

  // Combo box options
  grantablePermissions$: Observable<LcaSelectOption[]> = of([]);
  revokablePermissions$: Observable<LcaSelectOption[]> = of([]);
  roles$: Observable<LcaSelectOption[]> = of([]);
  attributes$: Observable<LcaSelectOption[]> = of([]);

  private attributeIdSubscription!: Subscription;
  private currentLoggedInUserAttributeId: number | null = null; // Attribute of the user performing the action

  constructor(
    private fb: FormBuilder,
    private userService: UserService,
    private comboDataService: ComboDataService,
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
      this.addPermissionEntry();
    } else { // revoke
      this.loadRevokeComboData();
      this.addPermissionEntry();
    }
  }

  get permissionsToModify(): FormArray {
    return this.permissionsForm.get('permissionsToModify') as FormArray;
  }

  createPermissionEntry(): FormGroup {
    return this.fb.group({
      // userId is fixed to targetUserId and not directly editable in this form for User Permissions.
      // It will be part of the payload.
      permissionId: [null, Validators.required],
      roleId: [null, Validators.required],
      attributeId: [null, Validators.required] // The attribute context OF THE PERMISSION
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

  loadGrantComboData(): void {
    this.isDataLoading = true;
    const comboPagination = { pagination: { begin: 0, end: 999 } };

    this.grantablePermissions$ = this.comboDataService.getPermissionsEnabledForGrantUser(comboPagination).pipe(
        map(response => this.mapToLcaSelectOption(response.items))
    );
    this.roles$ = this.comboDataService.getRoles(comboPagination).pipe(
        map(response => this.mapToLcaSelectOption(response.items))
    );
    this.attributes$ = this.comboDataService.getAttributes(comboPagination).pipe(
        map(response => this.mapToLcaSelectOption(response.items))
    );

    forkJoin([this.grantablePermissions$, this.roles$, this.attributes$]).pipe(
        finalize(() => this.isDataLoading = false)
    ).subscribe({
        error: () => this.toastService.showError("Failed to load data for permissions form.")
    });
  }

  loadRevokeComboData(): void {
    this.isDataLoading = true;
    const comboPagination = { pagination: { begin: 0, end: 999 } };

    // For revoke, populate dropdowns based on available system permissions or specific revoke-enabled ones
    this.revokablePermissions$ = this.comboDataService.getPermissionsEnabledForRevokeUser(comboPagination).pipe(
        map(response => this.mapToLcaSelectOption(response.items))
    );
    this.roles$ = this.comboDataService.getRoles(comboPagination).pipe(
        map(response => this.mapToLcaSelectOption(response.items))
    );
    this.attributes$ = this.comboDataService.getAttributes(comboPagination).pipe(
        map(response => this.mapToLcaSelectOption(response.items))
    );

     forkJoin([this.revokablePermissions$, this.roles$, this.attributes$]).pipe(
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
      userId: this.targetUserId, // Crucial: Permission is FOR the targetUserId
      permissionId: +p.permissionId,
      roleId: +p.roleId,
      attributeId: +p.attributeId
    }));

    let apiCall: Observable<any>;

    if (this.mode === 'grant') {
      const newPermissions = permissionsPayload.filter(np =>
        !this.currentPermissions.some(cp => // currentPermissions are for this targetUserId
            // cp.userId === np.userId && // userId is already targetUserId
            cp.permissionId === np.permissionId &&
            cp.roleId === np.roleId &&
            cp.attributeId === np.attributeId
        )
      );
      if (newPermissions.length === 0 && permissionsPayload.length > 0) {
          this.toastService.showInfo("All selected permissions are already granted to this user.");
          this.isLoading = false;
          return;
      }
       if (newPermissions.length === 0 && permissionsPayload.length === 0) {
          this.toastService.showInfo("No permissions selected to grant.");
          this.isLoading = false;
          return;
      }
      apiCall = this.userService.grantPermissions({
        relatedUserId: this.targetUserId, // User being affected
        attributeId: this.currentLoggedInUserAttributeId, // Attribute of the user granting
        permissions: newPermissions
      });
    } else { // revoke
      apiCall = this.userService.revokePermissions({
        relatedUserId: this.targetUserId, // User being affected
        attributeId: this.currentLoggedInUserAttributeId, // Attribute of the user revoking
        permissions: permissionsPayload
      });
    }

    apiCall.pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: () => {
        this.closeModal(true);
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

  getFormControl(index: number, controlName: string): AbstractControl | null {
    return this.permissionsToModify.at(index)?.get(controlName) || null;
  }

  ngOnDestroy(): void {
    if (this.attributeIdSubscription) {
        this.attributeIdSubscription.unsubscribe();
    }
  }
}