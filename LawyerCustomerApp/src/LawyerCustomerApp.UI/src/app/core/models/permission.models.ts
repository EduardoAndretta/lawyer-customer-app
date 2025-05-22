import { PaginationParams } from "./common.models";

// [Global Permissions Related With User]
export interface GlobalPermissionsRelatedWithUserParametersDto {

}

export interface GlobalPermissionsRelatedWithUserInformationDto {
  grantPermissionsOwnUser?: boolean | null;
  grantPermissionsAnyUser?: boolean | null;
  grantPermissionsOwnLawyerAccountUser?: boolean | null;
  grantPermissionsAnyLawyerAccountUser?: boolean | null;
  grantPermissionsOwnCustomerAccountUser?: boolean | null;
  grantPermissionsAnyCustomerAccountUser?: boolean | null;
  revokePermissionsOwnUser?: boolean | null;
  revokePermissionsAnyUser?: boolean | null;
  revokePermissionsOwnLawyerAccountUser?: boolean | null;
  revokePermissionsAnyLawyerAccountUser?: boolean | null;
  revokePermissionsOwnCustomerAccountUser?: boolean | null;
  revokePermissionsAnyCustomerAccountUser?: boolean | null;
  registerUser?: boolean | null;
  registerLawyerAccountUser?: boolean | null;
  registerCustomerAccountUser?: boolean | null;
  editOwnUser?: boolean | null;
  editAnyUser?: boolean | null;
  editOwnLawyerAccountUser?: boolean | null;
  editAnyLawyerAccountUser?: boolean | null;
  editOwnCustomerAccountUser?: boolean | null;
  editAnyCustomerAccountUser?: boolean | null;
  viewOwnUser?: boolean | null;
  viewAnyUser?: boolean | null;
  viewPublicUser?: boolean | null;
  viewOwnLawyerAccountUser?: boolean | null;
  viewAnyLawyerAccountUser?: boolean | null;
  viewPublicLawyerAccountUser?: boolean | null;
  viewOwnCustomerAccountUser?: boolean | null;
  viewAnyCustomerAccountUser?: boolean | null;
  viewPublicCustomerAccountUser?: boolean | null;
  viewPermissionsOwnUser?: boolean | null;
  viewPermissionsAnyUser?: boolean | null;
  viewPermissionsOwnLawyerAccountUser?: boolean | null;
  viewPermissionsAnyLawyerAccountUser?: boolean | null;
  viewPermissionsOwnCustomerAccountUser?: boolean | null;
  viewPermissionsAnyCustomerAccountUser?: boolean | null;
}

// [Global Permissions Related With Case]
export interface GlobalPermissionsRelatedWithCaseParametersDto {
    attributeId?: number | null;
}

export interface GlobalPermissionsRelatedWithCaseInformationDto {
  registerCase?: boolean | null;
  editOwnCase?: boolean | null;
  editAnyCase?: boolean | null;
  viewAnyCase?: boolean | null;
  viewOwnCase?: boolean | null;
  viewPublicCase?: boolean | null;
  viewPermissionsOwnCase?: boolean | null;
  viewPermissionsAnyCase?: boolean | null;
  assignLawyerOwnCase?: boolean | null;
  assignLawyerAnyCase?: boolean | null;
  assignCustomerOwnCase?: boolean | null;
  assignCustomerAnyCase?: boolean | null;
  grantPermissionsOwnCase?: boolean | null;
  grantPermissionsAnyCase?: boolean | null;
  revokePermissionsOwnCase?: boolean | null;
  revokePermissionsAnyCase?: boolean | null;
}

// [Permissions Related With User]
export class PermissionsRelatedWithUserParametersDto {
    relatedUserId?: number | null;
}

export interface PermissionsRelatedWithUserInformationDto {
  grantPermissionsUser?: boolean | null;
  grantPermissionsLawyerAccountUser?: boolean | null;
  grantPermissionsCustomerAccountUser?: boolean | null;
  revokePermissionsUser?: boolean | null;
  revokePermissionsLawyerAccountUser?: boolean | null;
  revokePermissionsCustomerAccountUser?: boolean | null;
  editUser?: boolean | null;
  editLawyerAccountUser?: boolean | null;
  editCustomerAccountUser?: boolean | null;
  viewUser?: boolean | null;
  viewLawyerAccountUser?: boolean | null;
  viewCustomerAccountUser?: boolean | null;
  viewPermissionsUser?: boolean | null;
  viewPermissionsLawyerAccountUser?: boolean | null;
  viewPermissionsCustomerAccountUser?: boolean | null;
}

// [Permissions Related With Case]
export class PermissionsRelatedWithCaseParametersDto {
    attributeId?: number | null;
    relatedCaseId?: number | null;
}

export interface PermissionsRelatedWithCaseInformationDto {
  editCase?: boolean | null;
  viewCase?: boolean | null;
  viewPermissionsCase?: boolean | null;
  assignLawyerCase?: boolean | null;
  assignCustomerCase?: boolean | null;
  grantPermissionsCase?: boolean | null;
  revokePermissionsCase?: boolean | null;
}

// [Enlisted Permissions From User]
export class EnlistPermissionsFromUserParametersDto {
    
}

export interface EnlistedPermissionsFromUserInformationDto {
    items?: {
        userName?: string | null;
        permissionName?: string | null;
        roleName?: string | null;       
        attributeName?: string | null;
        userId?: number | null;
        permissionId?: number | null;
        roleId?: number | null;
        attributeId?: number | null;
    }[]
}

// [Enlisted Permissions From Case]
export class EnlistPermissionsFromCaseParametersDto {
    attributeId?: number | null;
    relatedCaseId?: number | null;
}

export interface EnlistedPermissionsFromCaseInformationDto {
    items?: {
        userName?: string | null;
        permissionName?: string | null;
        roleName?: string | null;
        attributeName?: string | null;
        userId?: number | null;
        permissionId?: number | null;
        roleId?: number | null;
        attributeId?: number | null;
    }[]
}

// [Grant Permissions To User]
export interface GrantPermissionsToUserParametersDto {
    relatedUserId?: number | null;
    attributeId?: number | null;
    permissions?: GrantPermissionsToUserParametersDtoPermissionProperties[]
}

// [Revoke Permissions To User]
export interface RevokePermissionsToUserParametersDto {
    relatedUserId?: number | null;
    attributeId?: number | null;
    permissions?: RevokePermissionsToUserParametersDtoPermissionProperties[]
}

export interface GrantPermissionsToUserParametersDtoPermissionProperties {
    permissionId?: number | null;
    userId?: number | null;
    roleId?: number | null;
    attributeId?: number | null;
}

export interface RevokePermissionsToUserParametersDtoPermissionProperties {
    permissionId?: number | null;
    userId?: number | null;
    roleId?: number | null;
    attributeId?: number | null;
}

// [Grant Permissions To Case]
export interface GrantPermissionsToCaseParametersDto {
    relatedCaseId?: number | null;
    attributeId?: number | null;
    permissions?: GrantPermissionsToCaseParametersDtoPermissionProperties[]
}

// [Revoke Permissions To Case]
export interface RevokePermissionsToCaseParametersDto {
    relatedCaseId?: number | null;
    attributeId?: number | null;
    permissions?: RevokePermissionsToCaseParametersDtoPermissionProperties[]
}

export interface GrantPermissionsToCaseParametersDtoPermissionProperties {
    permissionId?: number | null;
    userId?: number | null;
    roleId?: number | null;
    attributeId?: number | null;
}

export interface RevokePermissionsToCaseParametersDtoPermissionProperties {
    permissionId?: number | null;
    userId?: number | null;
    roleId?: number | null;
    attributeId?: number | null;
}

export interface EnableUserToGrantPermissionsItem {
  name: string | null;
  userId: number;
  hasLawyerAccount: boolean;
  hasCustomerAccount: boolean;
  canBeGrantAsUser: boolean;
  canBeGrantAsLawyer: boolean;
  canBeGrantAsCustomer: boolean;
}
export interface EnableUsersToGrantPermissionsInformationDto {
  items: EnableUserToGrantPermissionsItem[] | null;
}

export interface EnableUsersToGrantPermissionsParametersDto {
  query?: string;
  pagination?: PaginationParams;
}


export interface EnableUserToRevokePermissionsItem {
  name: string | null;
  userId: number;
  hasLawyerAccount: boolean;
  hasCustomerAccount: boolean;
  canBeRevokeAsUser: boolean;
  canBeRevokeAsLawyer: boolean;
  canBeRevokeAsCustomer: boolean;
}
export interface EnableUsersToRevokePermissionsInformationDto {
  items: EnableUserToRevokePermissionsItem[] | null;
}
export interface EnableUsersToRevokePermissionsParametersDto {
  query?: string;
  pagination?: PaginationParams;
}