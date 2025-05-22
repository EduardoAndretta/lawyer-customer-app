import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators, AbstractControl } from '@angular/forms';
import { Observable, of, Subscription, forkJoin, BehaviorSubject } from 'rxjs';
import { map, finalize, catchError, tap } from 'rxjs/operators';

import { PermissionService } from '../../../../core/services/permission.service';
import { ComboDataService } from '../../../../core/services/combo-data.service';
import { UserProfileService } from '../../../../core/services/user-profile.service';
import { ToastService } from '../../../../core/services/toast.service';

import { KeyValueItem, KeyValueParametersDto, PaginationParams } from '../../../../core/models/common.models';
import { LcaPermissionItem } from '../../../../shared/components/lca-permissions-list/lca-permissions-list.component';
import {
  GrantPermissionsToUserParametersDto,
  RevokePermissionsToUserParametersDto,
  GrantPermissionsToUserParametersDtoPermissionProperties
} from '../../../../core/models/permission.models';

@Component({
  selector: 'app-user-permissions-modal',
  templateUrl: './user-permissions-modal.component.html',
  styleUrls: ['./user-permissions-modal.component.css']
})
export class UserPermissionsModalComponent implements OnInit, OnDestroy {
  @Input() isOpen: boolean = false;
  @Input() targetUserId!: number;
  @Input() targetUserName: string = 'User';
  @Input() currentPermissions: LcaPermissionItem[] = [];
  @Input() mode: 'grant' | 'revoke' = 'grant';

  @Output() closed = new EventEmitter<boolean>();

  permissionsForm!: FormGroup;
  isLoading: boolean = false;
  isDataLoading: boolean = true;
  submitted: boolean = false;

  grantablePermissionsList$: BehaviorSubject<KeyValueItem<number>[]> = new BehaviorSubject<KeyValueItem<number>[]>([]);
  revokablePermissionsList$: BehaviorSubject<KeyValueItem<number>[]> = new BehaviorSubject<KeyValueItem<number>[]>([]);
  rolesList$: BehaviorSubject<KeyValueItem<number>[]> = new BehaviorSubject<KeyValueItem<number>[]>([]);
  attributesList$: BehaviorSubject<KeyValueItem<number>[]> = new BehaviorSubject<KeyValueItem<number>[]>([]); // Attributes are back

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

    this.loadAllAutoCompleteData();
    this.addPermissionEntry();
  }

  private clearAllLists(): void {
    this.grantablePermissionsList$.next([]);
    this.revokablePermissionsList$.next([]);
    this.rolesList$.next([]);
    this.attributesList$.next([]);
  }

  loadAllAutoCompleteData(): void {
    this.isDataLoading = true;
    this.clearAllLists();
    const comboPagination: KeyValueParametersDto = {
        pagination: { begin: 0, end: this.COMBO_LIST_PAGE_SIZE - 1 }
    };

    const grantPermsObs$ = this.comboDataService.getPermissionsEnabledForGrantUser(comboPagination).pipe(
        map(response => response?.items || []),
        catchError(() => { this.toastService.showError("Failed to load grantable user permissions."); return of([]); })
    );
    const revokePermsObs$ = this.comboDataService.getPermissionsEnabledForRevokeUser(comboPagination).pipe(
        map(response => response?.items || []),
        catchError(() => { this.toastService.showError("Failed to load revokable user permissions."); return of([]); })
    );
    const rolesObs$ = this.comboDataService.getRoles(comboPagination).pipe(
        map(response => response?.items || []),
        catchError(() => { this.toastService.showError("Failed to load roles."); return of([]); })
    );
    const attributesObs$ = this.comboDataService.getAttributes(comboPagination).pipe(
        map(response => response?.items || []),
        catchError(() => { this.toastService.showError("Failed to load attributes."); return of([]); })
    );

    this.subscriptions.add(
        forkJoin([grantPermsObs$, revokePermsObs$, rolesObs$, attributesObs$])
        .pipe(finalize(() => {
            this.isDataLoading = false;
            this.cdRef.detectChanges();
        }))
        .subscribe(([grantPerms, revokePerms, roles, attributes]) => {
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
    return this.fb.group({
      permissionId: [null, Validators.required],
      roleId: [null, Validators.required],
      attributeId: [null, Validators.required]
    });
  }

  addPermissionEntry(): void {
    this.permissionsToModify.push(this.createPermissionEntry());
  }
  removePermissionEntry(index: number): void {
    this.permissionsToModify.removeAt(index);
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
    if (this.permissionsForm.invalid || this.permissionsToModify.length === 0) {
      this.toastService.showError("Please fill all required fields for each permission entry.");
      return;
    }

    this.isLoading = true;
    const formValue = this.permissionsForm.value;

    const permissionsPayload: GrantPermissionsToUserParametersDtoPermissionProperties[] =
        formValue.permissionsToModify.map((p: any) => ({
            userId: this.targetUserId,
            permissionId: +p.permissionId,
            roleId: +p.roleId,
            attributeId: +p.attributeId
    }));

    let apiCall: Observable<any>;
    if (this.mode === 'grant') {
      const newPermissions = permissionsPayload.filter((np: GrantPermissionsToUserParametersDtoPermissionProperties) =>
        !this.currentPermissions.some(cp =>
            cp.userId       === np.userId &&
            cp.permissionId === np.permissionId &&
            cp.roleId       === np.roleId &&
            cp.attributeId  === np.attributeId
        )
      );

      if (newPermissions.length === 0 && permissionsPayload.length > 0) { this.isLoading = false; return; }
      if (newPermissions.length === 0 && permissionsPayload.length === 0) { this.isLoading = false; return; }
      const grantParams: GrantPermissionsToUserParametersDto = {
        relatedUserId: this.targetUserId,
        permissions: newPermissions
      };
      apiCall = this.permissionService.grantPermissionsToUser(grantParams);
    } else { // revoke
      const revokeParams: RevokePermissionsToUserParametersDto = {
        relatedUserId: this.targetUserId,
        permissions: permissionsPayload
      };
      apiCall = this.permissionService.revokePermissionsFromUser(revokeParams);
    }

    this.subscriptions.add(
        apiCall.pipe(finalize(() => this.isLoading = false))
        .subscribe({ next: () => this.closeModal(true), error: (err) => {} })
    );
  }

  closeModal(dataChanged: boolean = false): void {
    this.isOpen = false; this.closed.emit(dataChanged);
  }
  getFormControl(index: number, controlName: string): AbstractControl | null {
    return this.permissionsToModify.at(index)?.get(controlName) || null;
  }
  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    this.grantablePermissionsList$.complete();
    this.revokablePermissionsList$.complete();
    this.rolesList$.complete();
    this.attributesList$.complete();
  }
}