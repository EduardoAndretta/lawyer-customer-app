import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription, Observable, of } from 'rxjs';
import { switchMap, tap, finalize } from 'rxjs/operators';

import { UserService } from '../../services/user.service';
import { UserProfileService } from '../../../../core/services/user-profile.service';
import { ToastService } from '../../../../core/services/toast.service';
import { UserDetailsInformationItem, UserPermissionsInformationItem } from '../../../../core/models/user.models';
import { LcaPermissionItem } from '../../../../shared/components/lca-permissions-list/lca-permissions-list.component';

@Component({
  selector: 'app-user-details-page',
  templateUrl: './user-details-page.component.html',
  styleUrls: ['./user-details-page.component.css']
})
export class UserDetailsPageComponent implements OnInit, OnDestroy {
  userDetails: UserDetailsInformationItem | null = null;
  activePermissions: LcaPermissionItem[] = [];
  isLoadingDetails: boolean = false;
  isLoadingPermissions: boolean = false;
  userId!: number; // The ID of the user being viewed

  showEditModal: boolean = false;
  showPermissionsModal: boolean = false;
  permissionsModalMode: 'grant' | 'revoke' = 'grant';

  private routeSubscription!: Subscription;
  private attributeIdSubscription!: Subscription;
  private loggedInUserAttributeId: number | null = null; // Attribute of the logged-in user making requests

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private userService: UserService,
    private userProfileService: UserProfileService, // For logged-in user's context
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.attributeIdSubscription = this.userProfileService.selectedAccountAttributeId$.subscribe(id => {
      this.loggedInUserAttributeId = id;
      if (this.userId && this.loggedInUserAttributeId) {
        this.loadUserDetails();
        this.loadPermissions();
      }
    });

    this.routeSubscription = this.route.paramMap.pipe(
      tap(params => {
        const id = params.get('id');
        if (!id) {
          this.toastService.showError("User ID not found in URL.");
          this.router.navigate(['/dashboard/home']);
          return;
        }
        this.userId = +id;
      }),
      switchMap(() => {
        if (this.userId && this.loggedInUserAttributeId) {
          this.loadUserDetails();
          this.loadPermissions();
        }
        return of(null);
      })
    ).subscribe();
  }

  loadUserDetails(): void {
    if (!this.userId || !this.loggedInUserAttributeId) return;
    this.isLoadingDetails = true;
    this.userService.getDetails({ relatedUserId: this.userId, attributeId: this.loggedInUserAttributeId }).pipe(
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
    if (!this.userId || !this.loggedInUserAttributeId) return;
    this.isLoadingPermissions = true;
    this.userService.getPermissions({ relatedUserId: this.userId, attributeId: this.loggedInUserAttributeId }).pipe(
        finalize(() => this.isLoadingPermissions = false)
    ).subscribe(response => {
      if (response && response.items) {
        this.activePermissions = response.items.map(p => ({ ...p } as LcaPermissionItem));
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
    this.permissionsModalMode = 'grant';
    this.showPermissionsModal = true;
  }

  openRevokePermissionsModal(): void {
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
    if (!this.loggedInUserAttributeId) {
        this.toastService.showError("Your account context not set.");
        return;
    }
    const payload = {
        relatedUserId: this.userId,
        attributeId: this.loggedInUserAttributeId,
        permissions: [{
            attributeId: permissionToRevoke.attributeId,
            permissionId: permissionToRevoke.permissionId,
            userId: permissionToRevoke.userId, // Should be this.userId
            roleId: permissionToRevoke.roleId
        }]
    };
    this.isLoadingPermissions = true;
    this.userService.revokePermissions(payload).pipe(
        finalize(() => this.isLoadingPermissions = false)
    ).subscribe({
        next: () => this.loadPermissions(),
        error: () => {}
    });
  }

  ngOnDestroy(): void {
    if (this.routeSubscription) this.routeSubscription.unsubscribe();
    if (this.attributeIdSubscription) this.attributeIdSubscription.unsubscribe();
  }
}