import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { ApiConfigService } from './api-config.service';
import { ToastService } from './toast.service';

import {
  GlobalPermissionsRelatedWithUserParametersDto,
  GlobalPermissionsRelatedWithUserInformationDto,
  GlobalPermissionsRelatedWithCaseParametersDto,
  GlobalPermissionsRelatedWithCaseInformationDto,
  PermissionsRelatedWithUserParametersDto,
  PermissionsRelatedWithUserInformationDto,
  PermissionsRelatedWithCaseParametersDto,
  PermissionsRelatedWithCaseInformationDto,
  EnlistPermissionsFromUserParametersDto,
  EnlistedPermissionsFromUserInformationDto,
  EnlistPermissionsFromCaseParametersDto,
  EnlistedPermissionsFromCaseInformationDto,
  GrantPermissionsToUserParametersDto,
  RevokePermissionsToUserParametersDto,
  GrantPermissionsToCaseParametersDto,
  RevokePermissionsToCaseParametersDto,
  EnableUsersToGrantPermissionsInformationDto,
  EnableUsersToRevokePermissionsInformationDto,
  EnableUsersToRevokePermissionsParametersDto,
  EnableUsersToGrantPermissionsParametersDto
} from '../models/permission.models';

@Injectable({
  providedIn: 'root'
})
export class PermissionService {
  private baseUrl: string;

  // [Observables to hold loaded permissions]
  private globalUserPermissionsSubject = new BehaviorSubject<GlobalPermissionsRelatedWithUserInformationDto | null>(null);
  public globalUserPermissions$ = this.globalUserPermissionsSubject.asObservable();

  private globalCasePermissionsSubject = new BehaviorSubject<GlobalPermissionsRelatedWithCaseInformationDto | null>(null);
  public globalCasePermissions$ = this.globalCasePermissionsSubject.asObservable();

  constructor(
    private http: HttpClient,
    private apiConfig: ApiConfigService,
    private toastService: ToastService
  ) {
    this.baseUrl = `${this.apiConfig.getBaseUrl()}/permission`;
  }

  getUsersEnabledToGrantPermissions(params: EnableUsersToGrantPermissionsParametersDto): Observable<EnableUsersToGrantPermissionsInformationDto | null> {
    return this.http.post<EnableUsersToGrantPermissionsInformationDto>(`${this.baseUrl}/search/enable-users-to-grant-permissions`, params).pipe(
      catchError(err => {
        console.error("Error fetching users enabled to grant permissions", err);
        return of(null);
      })
    );
  }

  getUsersEnabledToRevokePermissions(params: EnableUsersToRevokePermissionsParametersDto): Observable<EnableUsersToRevokePermissionsInformationDto | null> {
    return this.http.post<EnableUsersToRevokePermissionsInformationDto>(`${this.baseUrl}/search/enable-users-to-revoke-permissions`, params).pipe(
      catchError(err => {
        console.error("Error fetching users enabled to revoke permissions", err);
        return of(null);
      })
    );
  }

  // [Global Permissions]
  getGlobalUserPermissions(params: GlobalPermissionsRelatedWithUserParametersDto): Observable<GlobalPermissionsRelatedWithUserInformationDto | null> {
    return this.http.post<GlobalPermissionsRelatedWithUserInformationDto>(`${this.baseUrl}/user/global-permissions`, params).pipe(
      tap(data => this.globalUserPermissionsSubject.next(data)),
      catchError(err => {
        console.error("Error fetching global user permissions", err);
        this.globalUserPermissionsSubject.next(null);
        return of(null);
      })
    );
  }

  getGlobalCasePermissions(params: GlobalPermissionsRelatedWithCaseParametersDto): Observable<GlobalPermissionsRelatedWithCaseInformationDto | null> {
    return this.http.post<GlobalPermissionsRelatedWithCaseInformationDto>(`${this.baseUrl}/case/global-permissions`, params).pipe(
      tap(data => this.globalCasePermissionsSubject.next(data)),
      catchError(err => {
        console.error("Error fetching global case permissions", err);
        this.globalCasePermissionsSubject.next(null);
        return of(null);
      })
    );
  }

  // [Specific Entity Permissions (View what permissions are active for current user on an entity)]
  getPermissionsForUser(params: PermissionsRelatedWithUserParametersDto): Observable<PermissionsRelatedWithUserInformationDto | null> {
    return this.http.post<PermissionsRelatedWithUserInformationDto>(`${this.baseUrl}/user/permissions`, params).pipe(
      catchError(err => { console.error("Error fetching permissions for user", err); return of(null); })
    );
  }

  getPermissionsForCase(params: PermissionsRelatedWithCaseParametersDto): Observable<PermissionsRelatedWithCaseInformationDto | null> {
    return this.http.post<PermissionsRelatedWithCaseInformationDto>(`${this.baseUrl}/case/permissions`, params).pipe(
      catchError(err => { console.error("Error fetching permissions for case", err); return of(null); })
    );
  }

  // [Enlist Permissions (List permissions assigned to an entity - more for admin view)]
  enlistPermissionsFromUser(params: EnlistPermissionsFromUserParametersDto): Observable<EnlistedPermissionsFromUserInformationDto | null> {
    return this.http.post<EnlistedPermissionsFromUserInformationDto>(`${this.baseUrl}/user/enlist-permissions`, params).pipe(
      catchError(err => { console.error("Error enlisting permissions from user", err); return of(null); })
    );
  }

  enlistPermissionsFromCase(params: EnlistPermissionsFromCaseParametersDto): Observable<EnlistedPermissionsFromCaseInformationDto | null> {
    return this.http.post<EnlistedPermissionsFromCaseInformationDto>(`${this.baseUrl}/case/enlist-permissions`, params).pipe(
      catchError(err => { console.error("Error enlisting permissions from case", err); return of(null); })
    );
  }

  // [Grant/Revoke]
  grantPermissionsToUser(params: GrantPermissionsToUserParametersDto): Observable<any> {
    return this.http.post(`${this.baseUrl}/user/grant-permissions`, params).pipe(
      tap(() => this.toastService.showSuccess('Permissions granted to user successfully!')),
      catchError(err => { throw err; })
    );
  }

  revokePermissionsFromUser(params: RevokePermissionsToUserParametersDto): Observable<any> {
    return this.http.post(`${this.baseUrl}/user/revoke-permissions`, params).pipe(
      tap(() => this.toastService.showSuccess('Permissions revoked from user successfully!')),
      catchError(err => { throw err; })
    );
  }

  grantPermissionsToCase(params: GrantPermissionsToCaseParametersDto): Observable<any> {
    return this.http.post(`${this.baseUrl}/case/grant-permissions`, params).pipe(
      tap(() => this.toastService.showSuccess('Permissions granted to case successfully!')),
      catchError(err => { throw err; })
    );
  }

  revokePermissionsFromCase(params: RevokePermissionsToCaseParametersDto): Observable<any> {
    return this.http.post(`${this.baseUrl}/case/revoke-permissions`, params).pipe(
      tap(() => this.toastService.showSuccess('Permissions revoked from case successfully!')),
      catchError(err => { throw err; })
    );
  }

  // [Method to clear permissions on logout]
  clearAllLoadedPermissions(): void {
    this.globalUserPermissionsSubject.next(null);
    this.globalCasePermissionsSubject.next(null);
  }
}