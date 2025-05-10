import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { ApiConfigService } from '../../../core/services/api-config.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  UserDetailsParametersDto, UserDetailsInformationDto,
  RegisterUserParametersDto, // Renamed from RegisterParametersDto for clarity
  UserEditParametersDto,
  UserPermissionsParametersDto, UserPermissionsInformationDto,
  UserGrantPermissionsParametersDto, UserRevokePermissionsParametersDto,
  UserDetailsInformationItem
} from '../../../core/models/user.models';

@Injectable() // Provided in UserManagementModule
export class UserService {
  private baseUrl: string;

  constructor(
    private http: HttpClient,
    private apiConfig: ApiConfigService,
    private toastService: ToastService
  ) {
    this.baseUrl = `${this.apiConfig.getBaseUrl()}/user`;
  }

  getDetails(params: UserDetailsParametersDto): Observable<UserDetailsInformationDto | null> {
    return this.http.post<UserDetailsInformationDto>(`${this.baseUrl}/details`, params).pipe(
      catchError(error => {
        console.error('Failed to get user details:', error);
        return of(null);
      })
    );
  }

  register(params: RegisterUserParametersDto): Observable<any> {
    return this.http.post(`${this.baseUrl}/register`, params).pipe(
      tap(() => this.toastService.showSuccess('User registered successfully!')),
      catchError(error => {
        throw error;
      })
    );
  }

  edit(params: UserEditParametersDto): Observable<any> {
    // Note: The example payload for User Edit is complex.
    // Ensure your form and DTO mapping are correct.
    return this.http.patch(`${this.baseUrl}/edit`, params).pipe(
      tap(() => this.toastService.showSuccess('User updated successfully!')),
      catchError(error => {
        throw error;
      })
    );
  }

  getPermissions(params: UserPermissionsParametersDto): Observable<UserPermissionsInformationDto | null> {
    return this.http.post<UserPermissionsInformationDto>(`${this.baseUrl}/permissions`, params).pipe(
      catchError(error => {
        console.error('Failed to get user permissions:', error);
        return of(null);
      })
    );
  }

  grantPermissions(params: UserGrantPermissionsParametersDto): Observable<any> {
    return this.http.put(`${this.baseUrl}/grant-permissions`, params).pipe(
      tap(() => this.toastService.showSuccess('Permissions granted successfully to user!')),
      catchError(error => {
        throw error;
      })
    );
  }

  revokePermissions(params: UserRevokePermissionsParametersDto): Observable<any> {
    return this.http.put(`${this.baseUrl}/revoke-permissions`, params).pipe(
      tap(() => this.toastService.showSuccess('Permissions revoked successfully from user!')),
      catchError(error => {
        throw error;
      })
    );
  }
}