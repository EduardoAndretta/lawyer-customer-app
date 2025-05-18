import { PaginationParams, PermissionDetail, BasePermissionsInformationItem } from './common.models';

// [Case Search]
export interface CaseSearchParametersDto {
  attributeId?: number | null;
  query?: string | null;
  pagination?: PaginationParams | null;
}

export interface CaseSearchInformationItem {
  title?: string | null;
  description?: string | null;
  id?: number | null;
  userId?: number | null;
  customerId?: number | null;
  lawyerId?: number | null;
}

export interface CaseSearchInformationDto {
  items: CaseSearchInformationItem[] | null;
}

// [attributeId] => [Attribute of the current logged-in user for context]

// [Case Count]
export interface CaseCountParametersDto {
  attributeId?: number | null;
  query?: string | null;
}

export interface CaseCountInformationDto {
  count: number | null;
}

// [Case Details]
export interface CaseDetailsParametersDto {
  caseId?: number | null;
  attributeId?: number | null;
}

export interface CaseDetailsInformationItem {
  title?: string | null;
  description?: string | null;
  status?: string | null;
  private?: boolean | null;
  id?: number | null;
  userId?: number | null;
  customerId?: number | null;
  lawyerId?: number | null;
}

export interface CaseDetailsInformationDto {
  item: CaseDetailsInformationItem | null;
}

// [Case Register]
export interface CaseRegisterParametersDto {
  title?: string | null;
  description?: string | null;
}

// [Case Edit]
export interface CaseEditValues {
  private?: boolean;
  title?: string | null;
  description?: string | null;
  status?: string | null;
}
export interface CaseEditParametersDto {
  relatedCaseId?: number | null;
  values?: CaseEditValues | null;
}

// [Case Assign Lawyer]
export interface CaseAssignLawyerParametersDto {
  caseId?: number | null;
  attributeId?: number | null;
  lawyerId?: number | null;
}

// [Case Assign Customer]
export interface CaseAssignCustomerParametersDto {
  caseId?: number | null;
  attributeId?: number | null;
  customerId?: number | null;
}

// [Case Permissions]
export interface CasePermissionsParametersDto {
  relatedCaseId?: number | null;
  attributeId?: number | null;
}

export interface CasePermissionsInformationItem extends BasePermissionsInformationItem {}

export interface CasePermissionsInformationDto {
  items: CasePermissionsInformationItem[] | null;
}

// [Case Grant Permissions]
export interface CaseGrantPermissionsParametersDto {
  caseId?: number | null;
  attributeId?: number | null;
  permissions: PermissionDetail[] | null;
}

// [Case Revoke Permissions]
export interface CaseRevokePermissionsParametersDto {
  caseId?: number | null;
  attributeId?: number | null;
  permissions: PermissionDetail[] | null;
}