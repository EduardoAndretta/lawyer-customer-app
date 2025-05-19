export interface PaginationParams {
    begin?: number | null;
    end?: number | null;
  }
  
  export interface CountInformationDto {
    count?: number | null;
  }
  
  export interface KeyValueItem<TValue> {
    key: string | null;
    value: TValue;
  }
  
  export interface KeyValueInformationDto<TValue> {
    items: KeyValueItem<TValue>[] | null;
  }
  
  export interface KeyValueParametersDto {
    pagination?: PaginationParams | null;
  }
  
  // [Generic permission structure (adjust if needed based on specific grant/revoke DTOs)]
  export interface PermissionDetail {
    attributeId?: number | null;
    permissionId?: number | null;
    userId?: number | null;
    roleId?: number | null;
  }
  
  export interface BasePermissionsInformationItem {
    userName?: string | null;
    permissionName?: string | null;
    roleName?: string | null;
    attributeName?: string | null;
    userId?: number | null;
    permissionId?: number | null;
    attributeId?: number | null;
    roleId?: number | null;
  }