export interface AuthenticateParametersDto {
    email?: string | null;
    password?: string | null;
  }
  
  export interface AuthenticateInformationDto {
    token?: string | null;
    refreshToken?: string | null;
  }
  
  export interface RefreshParametersDto {
    token?: string | null;
    refreshToken?: string | null;
  }
  
  export interface RefreshInformationDto {
    token?: string | null;
    refreshToken?: string | null;
  }
  
  export interface InvalidateParametersDto {
    token?: string | null;
    refreshToken?: string | null;
  }