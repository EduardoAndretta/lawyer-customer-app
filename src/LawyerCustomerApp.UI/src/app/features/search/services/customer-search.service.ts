import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ApiConfigService } from '../../../core/services/api-config.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  CustomerSearchParametersDto, CustomerSearchInformationDto,
  CustomerCountParametersDto, CustomerCountInformationDto
} from '../../../core/models/customer.models.js';

@Injectable({
  providedIn: 'root'
})
export class CustomerSearchService {
  private baseUrl: string;

  constructor(
    private http: HttpClient,
    private apiConfig: ApiConfigService,
    private toastService: ToastService
  ) {
    this.baseUrl = `${this.apiConfig.getBaseUrl()}/customer`;
  }

  search(params: CustomerSearchParametersDto): Observable<CustomerSearchInformationDto> {
    return this.http.post<CustomerSearchInformationDto>(`${this.baseUrl}/search`, params).pipe(
      catchError(error => {
        // Toast handled by interceptor
        console.error('Case search failed:', error);
        return of({ items: [] }); // Return empty on error to prevent breaking UI
      })
    );
  }

  count(params: CustomerCountParametersDto): Observable<CustomerCountInformationDto> {
    return this.http.post<CustomerCountInformationDto>(`${this.baseUrl}/search/count`, params).pipe(
      catchError(error => {
        // Toast handled by interceptor
        console.error('Case count failed:', error);
        return of({ count: 0 }); // Return zero count on error
      })
    );
  }
}