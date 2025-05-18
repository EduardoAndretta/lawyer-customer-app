import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription, Observable, of } from 'rxjs';
import { switchMap, tap, finalize } from 'rxjs/operators';

import { UserService } from '../../services/user.service';
import { PermissionService } from '../../../../core/services/permission.service';
import { UserProfileService } from '../../../../core/services/user-profile.service';
import { ToastService } from '../../../../core/services/toast.service';
import { UserDetailsInformationItem } from '../../../../core/models/user.models';
import { LcaPermissionItem } from '../../../../shared/components/lca-permissions-list/lca-permissions-list.component';

import {
  EnlistPermissionsFromUserParametersDto,
  RevokePermissionsToUserParametersDto,
  GrantPermissionsToUserParametersDtoPermissionProperties
} from '../../../../core/models/permission.models';


@Component({
  selector: 'app-user-details-page',
  templateUrl: './user-details-page.component.html',
  styleUrls: ['./user-details-page.component.css']
})
export class UserDetailsPageComponent implements OnInit, OnDestroy {
  userDetails: UserDetailsInformationItem | null = null;
  activePermissions: LcaPermissionItem[] = [];
  isLoadingPermissions: boolean = false;
  isLoadingDetails: boolean = false;
  viewedUserId!: number;

  showEditModal: boolean = false;
  showPermissionsModal: boolean = false;
  permissionsModalMode: 'grant' | 'revoke' = 'grant';

  private subscriptions = new Subscription();
  
  private loggedInUserAttributeId: number | null = null;


  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private userService: UserService,
    private permissionService: PermissionService,
    private userProfileService: UserProfileService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    const attributeSub = this.userProfileService.selectedAccountAttributeId$.subscribe(id => {
      this.loggedInUserAttributeId = id;
      if (this.viewedUserId) {
        this.loadPermissions();
      }
    });
    this.subscriptions.add(attributeSub);

    const routeSub = this.route.paramMap.pipe(
      tap(params => {
        const id = params.get('id');
        if (!id) {
          this.toastService.showError("User ID not found in URL.");
          this.router.navigate(['/dashboard/home']);
          return;
        }
        this.viewedUserId = +id;
      }),
      switchMap(() => {
        if (this.viewedUserId) {
          this.loadUserDetails();
          this.loadPermissions();
        }
        return of(null);
      })
    ).subscribe();
    this.subscriptions.add(routeSub);
  }

  loadUserDetails(): void {
    if (!this.viewedUserId) return;
    this.isLoadingDetails = true;
   
    this.userService.getDetails({ relatedUserId: this.viewedUserId }).pipe(
        finalize(() => this.isLoadingDetails = false)
    ).subscribe(response => {
      if (response && response.item) {
        this.userDetails = response.item;
      } else {
        this.toastService.showError('Could not load user details.');
        this.userDetails = null;
      }
    });
  }

  loadPermissions(): void {
    if (!this.viewedUserId) return;
    this.isLoadingPermissions = true;
  
    const params: EnlistPermissionsFromUserParametersDto = {
        relatedUserId: this.viewedUserId
    };
    this.permissionService.enlistPermissionsFromUser(params).pipe(
        finalize(() => this.isLoadingPermissions = false)
    ).subscribe(response => {
      if (response && response.items) {
        this.activePermissions = response.items.map(p => ({
            userName: p.userName,
            permissionName: p.permissionName,
            roleName: p.roleName,
            attributeName: (p as any).attributeName || 'N/A (User-Level)',
            userId: p.userId,
            permissionId: p.permissionId,
            roleId: p.roleId,
            attributeId: (p as any).attributeId || null
        } as LcaPermissionItem));
      } else {
        this.activePermissions = [];
      }
    });
  }

  openEditModal(): void {
    if (this.userDetails) {
      this.showEditModal = true;
    } else {
        this.toastService.showError("Cannot edit: User details not loaded.");
    }
  }
  onEditModalClosed(refresh: boolean): void {
    this.showEditModal = false;
    if (refresh) {
      this.loadUserDetails();
    }
  }

  openGrantPermissionsModal(): void {
    if (!this.userDetails) {
      this.toastService.showError("User details not loaded. Cannot grant permissions.");
      return;
    }
    this.permissionsModalMode = 'grant';
    this.showPermissionsModal = true;
  }

  openRevokePermissionsModal(): void {
     if (!this.userDetails) {
      this.toastService.showError("User details not loaded. Cannot revoke permissions.");
      return;
    }
     if (this.activePermissions.length === 0) {
        this.toastService.showInfo("No permissions to revoke for this user.");
        return;
    }
    this.permissionsModalMode = 'revoke';
    this.showPermissionsModal = true;
  }

  onPermissionsModalClosed(refresh: boolean): void {
    this.showPermissionsModal = false;
    if (refresh) {
      this.loadPermissions();
    }
  }

  handleRevokePermissionFromList(permissionToRevoke: LcaPermissionItem): void {
    const permissionsToRevokePayload: GrantPermissionsToUserParametersDtoPermissionProperties[] = [{ // Using this DTO type as it's more complete
        permissionId: permissionToRevoke.permissionId!,
        userId: this.viewedUserId,
        roleId: permissionToRevoke.roleId!,
        attributeId: permissionToRevoke.attributeId
    }];


    const payload: RevokePermissionsToUserParametersDto = {
        relatedUserId: this.viewedUserId,
        permissions: permissionsToRevokePayload
    };

    this.isLoadingPermissions = true;
    this.permissionService.revokePermissionsFromUser(payload).pipe(
        finalize(() => this.isLoadingPermissions = false)
    ).subscribe({
        next: () => this.loadPermissions(),
        error: (err) => { }
    });
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}