import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { ApiConfigService } from './api-config.service';
import { ToastService } from './toast.service';
import {
  CaseDetailsParametersDto,
  CaseDetailsInformationDto,
  CaseRegisterParametersDto,
  CaseEditParametersDto,
  CaseAssignLawyerParametersDto,
  CaseAssignCustomerParametersDto
} from '../models/case.models';

@Injectable({
  providedIn: 'root'
})
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
        console.error('Failed to get case details:', error);
        return of(null);
      })
    );
  }

  register(params: CaseRegisterParametersDto): Observable<any> {
    return this.http.post(`${this.baseUrl}/register`, params).pipe(
      tap(() => this.toastService.showSuccess('Case registered successfully!')),
      catchError(error => {
        throw error;
      })
    );
  }

  edit(params: CaseEditParametersDto): Observable<any> {
    return this.http.patch(`${this.baseUrl}/edit`, params).pipe(
      tap(() => this.toastService.showSuccess('Case updated successfully!')),
      catchError(error => {
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
}