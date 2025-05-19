import { Component, Input, Output, EventEmitter } from '@angular/core';
import { BasePermissionsInformationItem } from '../../../core/models/common.models'; // Use a common base

// Define a more specific type if needed, or use the base.
export interface LcaPermissionItem extends BasePermissionsInformationItem {
  // any additional properties specific to display or interaction
}

@Component({
  selector: 'app-lca-permissions-list',
  templateUrl: './lca-permissions-list.component.html',
  styleUrls: ['./lca-permissions-list.component.css']
})
export class LcaPermissionsListComponent {
  @Input() permissions: LcaPermissionItem[] = [];
  @Input() title: string = 'Active Permissions';
  @Input() canRevoke: boolean = false; // If true, shows a revoke button per item
  @Input() isLoading: boolean = false;
  @Input() noPermissionsMessage: string = "No active permissions found.";

  @Output() revokePermission = new EventEmitter<LcaPermissionItem>(); // Emits the permission to be revoked

  onRevoke(permission: LcaPermissionItem): void {
    this.revokePermission.emit(permission);
  }

  trackByPermission(index: number, item: LcaPermissionItem): string {
    // Create a unique key for trackBy based on its properties
    return `${item.userId}-${item.permissionId}-${item.roleId}-${item.attributeId}`;
  }
}