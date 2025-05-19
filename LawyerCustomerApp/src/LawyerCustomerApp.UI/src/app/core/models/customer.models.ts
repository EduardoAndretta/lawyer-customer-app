import { PaginationParams, PermissionDetail, BasePermissionsInformationItem } from './common.models';

// [Customer Search]
export interface CustomerSearchParametersDto {
  attributeId?: number | null;
  query?: string | null;
  pagination?: PaginationParams | null;
}

export interface CustomerSearchInformationItem {
  name?: string | null;
  userId?: number | null;
  customerId?: number | null;
}

export interface CustomerSearchInformationDto {
  items: CustomerSearchInformationItem[] | null;
}

// [attributeId] => [Attribute of the current logged-in user for context]

// [Customer Count]
export interface CustomerCountParametersDto {
  attributeId?: number | null;
  query?: string | null;
}

export interface CustomerCountInformationDto {
  count: number | null;
}

// [Customer Details]
export interface CustomerDetailsParametersDto {
  customerId?: number | null;
  attributeId?: number | null;
}

export interface CustomerDetailsInformationItem {
  name?: string | null;
  userId?: number | null;
  customerId?: number | null;

  // [Add all informations about email, address and document after]
}

export interface CustomerDetailsInformationDto {
  item: CustomerDetailsInformationItem | null;
}

// [Customer Register]
export interface CustomerRegisterParametersDto {
  phone?: string | null;
  address?: string | null;
}