import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { ApiConfigService } from '../../../core/services/api-config.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  CustomerDetailsParametersDto,
  CustomerDetailsInformationDto,
  CustomerRegisterParametersDto as CustomerRegisterAccountParametersDto
} from '../../../core/models/customer.models'; // Ensure DTOs are correct

@Injectable() // Provided in CustomerManagementModule
export class CustomerService {
  private baseUrl: string;

  constructor(
    private http: HttpClient,
    private apiConfig: ApiConfigService,
    private toastService: ToastService
  ) {
    this.baseUrl = `${this.apiConfig.getBaseUrl()}/customer`;
  }

  getDetails(params: CustomerDetailsParametersDto): Observable<CustomerDetailsInformationDto | null> {
    return this.http.post<CustomerDetailsInformationDto>(`${this.baseUrl}/details`, params).pipe(
      catchError(error => {
        console.error('Failed to get Customer details:', error);
        return of(null);
      })
    );
  }

  // This is for registering a Customer *account* for an existing user
  registerAccount(params: CustomerRegisterAccountParametersDto): Observable<any> {
    return this.http.post(`${this.baseUrl}/register/account`, params).pipe(
      tap(() => this.toastService.showSuccess('Customer account registered successfully!')),
      catchError(error => {
        throw error;
      })
    );
  }
}