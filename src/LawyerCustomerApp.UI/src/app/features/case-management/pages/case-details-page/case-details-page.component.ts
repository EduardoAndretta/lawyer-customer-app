import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription, Observable, of } from 'rxjs';
import { switchMap, tap, finalize } from 'rxjs/operators';

import { CaseService } from '../../services/case.service';
import { UserProfileService } from '../../../../core/services/user-profile.service';
import { ToastService } from '../../../../core/services/toast.service';
import { CaseDetailsInformationItem } from '../../../../core/models/case.models';
import { LcaPermissionItem } from '../../../../shared/components/lca-permissions-list/lca-permissions-list.component';
import { PermissionService } from '../../../../core/services/permission.service';


@Component({
  selector: 'app-case-details-page',
  templateUrl: './case-details-page.component.html',
  styleUrls: ['./case-details-page.component.css']
})
export class CaseDetailsPageComponent implements OnInit, OnDestroy {
  caseDetails: CaseDetailsInformationItem | null = null;
  activePermissions: LcaPermissionItem[] = [];
  isLoadingDetails: boolean = false;
  isLoadingPermissions: boolean = false;
  caseId!: number;

  showEditModal: boolean = false;
  showPermissionsModal: boolean = false;
  permissionsModalMode: 'grant' | 'revoke' = 'grant';

  private routeSubscription!: Subscription;
  private attributeIdSubscription!: Subscription;
  private currentAttributeId: number | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private permissionService: PermissionService,
    private caseService: CaseService,
    private userProfileService: UserProfileService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.attributeIdSubscription = this.userProfileService.selectedAccountAttributeId$.subscribe(id => {
      this.currentAttributeId = id;
      if (this.caseId && this.currentAttributeId) {
        this.loadCaseDetails();
        this.loadPermissions();
      }
    });

    this.routeSubscription = this.route.paramMap.pipe(
      tap(params => {
        const id = params.get('id');
        if (!id) {
          this.toastService.showError("Case ID not found in URL.");
          this.router.navigate(['/dashboard/home']); // Or to a cases list
          return;
        }
        this.caseId = +id;
      }),
      switchMap(() => {
        if (this.caseId && this.currentAttributeId) {
          this.loadCaseDetails();
          this.loadPermissions();
        }
        return of(null); // switchMap needs to return an observable
      })
    ).subscribe();
  }

  loadCaseDetails(): void {
    if (!this.caseId || !this.currentAttributeId) return;
    this.isLoadingDetails = true;
    this.caseService.getDetails({ caseId: this.caseId, attributeId: this.currentAttributeId }).pipe(
        finalize(() => this.isLoadingDetails = false)
    ).subscribe(response => {
      if (response && response.item) {
        this.caseDetails = response.item;
      } else {
        this.toastService.showError('Could not load case details.');
        this.caseDetails = null;
        // Optionally navigate away if details are crucial and not found
        // this.router.navigate(['/dashboard/cases']);
      }
    });
  }

loadPermissions(): void {
  if (!this.caseId || !this.currentAttributeId) return;
  this.isLoadingPermissions = true;

  this.permissionService.enlistPermissionsFromCase({ relatedCaseId: this.caseId, attributeId: this.currentAttributeId })
    .pipe(finalize(() => this.isLoadingPermissions = false))
    .subscribe(response => {
      if (response && response.items) {
        this.activePermissions = response.items.map(p => ({ ...p } as LcaPermissionItem));
      } else { this.activePermissions = []; }
    });
}

  openEditModal(): void {
    if (this.caseDetails) {
      this.showEditModal = true;
    } else {
        this.toastService.showError("Cannot edit: Case details not loaded.");
    }
  }
  onEditModalClosed(refresh: boolean): void {
    this.showEditModal = false;
    if (refresh) {
      this.loadCaseDetails();
    }
  }

  openGrantPermissionsModal(): void {
    this.permissionsModalMode = 'grant';
    this.showPermissionsModal = true;
  }

  openRevokePermissionsModal(): void {
    if (this.activePermissions.length === 0) {
        this.toastService.showInfo("No permissions to revoke.");
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

  // For direct revoke from list (optional)
  handleRevokePermissionFromList(permissionToRevoke: LcaPermissionItem): void {
    if (!this.currentAttributeId) {
        this.toastService.showError("User context not set.");
        return;
    }
    const payload = {
        caseId: this.caseId,
        attributeId: this.currentAttributeId, // Context of the revoker
        permissions: [{
            attributeId: permissionToRevoke.attributeId,
            permissionId: permissionToRevoke.permissionId,
            userId: permissionToRevoke.userId,
            roleId: permissionToRevoke.roleId
        }]
    };
    this.isLoadingPermissions = true; // Indicate activity
    this.caseService.revokePermissions(payload).pipe(
        finalize(() => this.isLoadingPermissions = false)
    ).subscribe({
        next: () => this.loadPermissions(),
        error: () => {} // Handled by interceptor
    });
  }


  ngOnDestroy(): void {
    if (this.routeSubscription) {
      this.routeSubscription.unsubscribe();
    }
    if (this.attributeIdSubscription) {
      this.attributeIdSubscription.unsubscribe();
    }
  }
}