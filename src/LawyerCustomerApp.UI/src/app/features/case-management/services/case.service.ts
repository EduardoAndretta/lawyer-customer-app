import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { ApiConfigService } from '../../../core/services/api-config.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  CaseDetailsParametersDto,
  CaseDetailsInformationDto,
  CaseRegisterParametersDto,
  CaseEditParametersDto,
  CaseAssignLawyerParametersDto,
  CaseAssignCustomerParametersDto,
  CasePermissionsParametersDto,
  CasePermissionsInformationDto,
  CaseGrantPermissionsParametersDto,
  CaseRevokePermissionsParametersDto
} from '../../../core/models/case.models';

@Injectable() // Provided in CaseManagementModule
export class CaseService {
  private baseUrl: string;

  constructor(
    private http: HttpClient,
    private apiConfig: ApiConfigService,
    private toastService: ToastService
  ) {
    this.baseUrl = `${this.apiConfig.getBaseUrl()}/case`;
  }

  getDetails(params: CaseDetailsParametersDto): Observable<CaseDetailsInformationDto | null> {
    return this.http.post<CaseDetailsInformationDto>(`${this.baseUrl}/details`, params).pipe(
      catchError(error => {
        // Toast handled by interceptor
        console.error('Failed to get case details:', error);
        return of(null);
      })
    );
  }

  register(params: CaseRegisterParametersDto): Observable<any> { // API returns 200 success, no body
    return this.http.post(`${this.baseUrl}/register`, params).pipe(
      tap(() => this.toastService.showSuccess('Case registered successfully!')),
      catchError(error => {
        // Toast handled by interceptor
        throw error;
      })
    );
  }

  edit(params: CaseEditParametersDto): Observable<any> {
    return this.http.patch(`${this.baseUrl}/edit`, params).pipe(
      tap(() => this.toastService.showSuccess('Case updated successfully!')),
      catchError(error => {
        // Toast handled by interceptor
        throw error;
      })
    );
  }

  assignLawyer(params: CaseAssignLawyerParametersDto): Observable<any> {
    return this.http.put(`${this.baseUrl}/assign-lawyer`, params).pipe(
      tap(() => this.toastService.showSuccess('Lawyer assigned successfully!')),
      catchError(error => {
        throw error;
      })
    );
  }

  assignCustomer(params: CaseAssignCustomerParametersDto): Observable<any> {
    return this.http.put(`${this.baseUrl}/assign-customer`, params).pipe(
      tap(() => this.toastService.showSuccess('Customer assigned successfully!')),
      catchError(error => {
        throw error;
      })
    );
  }

  getPermissions(params: CasePermissionsParametersDto): Observable<CasePermissionsInformationDto | null> {
    return this.http.post<CasePermissionsInformationDto>(`${this.baseUrl}/permissions`, params).pipe(
      catchError(error => {
        console.error('Failed to get case permissions:', error);
        return of(null);
      })
    );
  }

  grantPermissions(params: CaseGrantPermissionsParametersDto): Observable<any> {
    return this.http.put(`${this.baseUrl}/grant-permissions`, params).pipe(
      tap(() => this.toastService.showSuccess('Permissions granted successfully!')),
      catchError(error => {
        throw error;
      })
    );
  }

  revokePermissions(params: CaseRevokePermissionsParametersDto): Observable<any> {
    return this.http.put(`${this.baseUrl}/revoke-permissions`, params).pipe(
      tap(() => this.toastService.showSuccess('Permissions revoked successfully!')),
      catchError(error => {
        throw error;
      })
    );
  }
}