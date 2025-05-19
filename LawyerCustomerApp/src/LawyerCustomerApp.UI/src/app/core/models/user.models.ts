import { PaginationParams, PermissionDetail, BasePermissionsInformationItem } from './common.models';

// [User Search]
export interface UserSearchParametersDto {
  attributeId?: number | null;
  query?: string | null;
  pagination?: PaginationParams | null;
}

export interface UserSearchInformationItem {
  name?: string | null;
  id?: number | null;
  customerId?: number | null;
  lawyerId?: number | null;
  hasCustomerAccount?: boolean | null;
  hasLawyerAccount?: boolean | null;
}

export interface UserSearchInformationDto {
  items: UserSearchInformationItem[] | null;
}

// [attributeId] => [Attribute of the current logged-in user for context]

// [User Count]
export interface UserCountParametersDto {
  attributeId?: number | null;
  query?: string | null;
}

export interface UserCountInformationDto {
  count: number | null;
}

// [User Details]
export interface UserDetailsParametersDto {
  relatedUserId?: number | null;
  attributeId?: number | null;
}

export interface UserDetailsInformationItem {
  name?: string | null;
  id?: number | null;

  customerId?: number | null;
  lawyerId?: number | null;

  hasCustomerAccount?: boolean | null;
  hasLawyerAccount?: boolean | null;

  hasAddress?: boolean | null;
  hasDocument?: boolean | null;

  private?: boolean | null;

  address?: {
    zipCode?: string | null;
    houseNumber?: string | null;
    complement?: string | null;
    district?: string | null;
    city?: string | null;
    state?: string | null;
    country?: string | null;
  };

  document?: {
    type?: string | null;
    identifierDocument?: string | null;
  };

  accounts?: {
    lawyer?: { 
      phone?: string | null;
      private?: boolean | null;

      hasAddress?: boolean | null;
      hasDocument?: boolean | null;

      address?: {
        zipCode?: string | null;
        houseNumber?: string | null;
        complement?: string | null;
        district?: string | null;
        city?: string | null;
        state?: string | null;
        country?: string | null;
      };
      document?: {
        type?: string | null;
        identifierDocument?: string | null;
      };
    };
    customer?: { 
      phone?: string | null;
      private?: boolean | null;

      hasAddress?: boolean | null;
      hasDocument?: boolean | null;

      address?: {
        zipCode?: string | null;
        houseNumber?: string | null;
        complement?: string | null;
        district?: string | null;
        city?: string | null;
        state?: string | null;
        country?: string | null;
      };
      document?: {
        type?: string | null;
        identifierDocument?: string | null;
      };
    };
  };
}

export interface UserDetailsInformationDto {
  item: UserDetailsInformationItem | null;
}

// [User Register]
export interface RegisterUserParametersDto {
  email?: string | null;
  password?: string | null;
  name?: string | null;
}

// [User Edit]
export interface UserEditValues {
  private?: boolean;
  address?: {
    zipCode?: string;
    houseNumber?: string;
    complement?: string;
    district?: string;
    city?: string;
    state?: string;
    country?: string;
  };
  document?: {
    type?: string;
    identifierDocument?: string;
  };
  accounts?: {
    lawyer?: { 
      phone?: string;
      private?: boolean;
      address?: {
        zipCode?: string;
        houseNumber?: string;
        complement?: string;
        district?: string;
        city?: string;
        state?: string;
        country?: string;
      };
      document?: {
        type?: string;
        identifierDocument?: string;
      };
    };
    customer?: { 
      phone?: string;
      private?: boolean;
      address?: {
        zipCode?: string;
        houseNumber?: string;
        complement?: string;
        district?: string;
        city?: string;
        state?: string;
        country?: string;
      };
      document?: {
        type?: string;
        identifierDocument?: string;
      };
    };
  };
}
export interface UserEditParametersDto {
  relatedUserId?: number | null;
  values?: UserEditValues | null;
}

// [User Permissions]
export interface UserPermissionsParametersDto {
  relatedUserId?: number | null;
  attributeId?: number | null;
}

export interface UserPermissionsInformationItem extends BasePermissionsInformationItem {}

export interface UserPermissionsInformationDto {
  items: UserPermissionsInformationItem[] | null;
}

// [User Grant Permissions]
export interface UserGrantPermissionsParametersDto {
  relatedUserId?: number | null;
  attributeId?: number | null;
  permissions: PermissionDetail[] | null;
}

// [User Revoke Permissions]
export interface UserRevokePermissionsParametersDto {
  relatedUserId?: number | null;
  attributeId?: number | null;
  permissions: PermissionDetail[] | null;
}