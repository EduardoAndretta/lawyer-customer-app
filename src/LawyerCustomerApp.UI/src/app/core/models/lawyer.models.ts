import { PaginationParams, PermissionDetail, BasePermissionsInformationItem } from './common.models';

// [Lawyer Search]
export interface LawyerSearchParametersDto {
  attributeId?: number | null;
  query?: string | null;
  pagination?: PaginationParams | null;
}

export interface LawyerSearchInformationItem {
  name?: string | null;
  userId?: number | null;
  lawyerId?: number | null;
}

export interface LawyerSearchInformationDto {
  items: LawyerSearchInformationItem[] | null;
}

// [attributeId] => [Attribute of the current logged-in user for context]

// [Lawyer Count]
export interface LawyerCountParametersDto {
  attributeId?: number | null;
  query?: string | null;
}

export interface LawyerCountInformationDto {
  count: number | null;
}

// [Lawyer Details]
export interface LawyerDetailsParametersDto {
  lawyerId?: number | null;
  attributeId?: number | null;
}

export interface LawyerDetailsInformationItem {
  name?: string | null;
  userId?: number | null;
  lawyerId?: number | null;

  // [Add all informations about email, address and document after]
}

export interface LawyerDetailsInformationDto {
  item: LawyerDetailsInformationItem | null;
}

// [Lawyer Register]
export interface LawyerRegisterParametersDto {
  phone?: string | null;
  address?: string | null;
}