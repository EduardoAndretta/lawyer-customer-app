import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { ApiConfigService } from './api-config.service';
import { ToastService } from './toast.service';
import {
  LawyerDetailsParametersDto,
  LawyerDetailsInformationDto,
  LawyerRegisterParametersDto
} from '../models/lawyer.models';

@Injectable({
  providedIn: 'root'
})
export class LawyerService {
  private baseUrl: string;

  constructor(
    private http: HttpClient,
    private apiConfig: ApiConfigService,
    private toastService: ToastService
  ) {
    this.baseUrl = `${this.apiConfig.getBaseUrl()}/lawyer`;
  }

  getDetails(params: LawyerDetailsParametersDto): Observable<LawyerDetailsInformationDto | null> {
    return this.http.post<LawyerDetailsInformationDto>(`${this.baseUrl}/details`, params).pipe(
      catchError(error => {
        console.error('Failed to get lawyer details:', error);
        return of(null);
      })
    );
  }

  registerAccount(params: LawyerRegisterParametersDto): Observable<any> {
    return this.http.post(`${this.baseUrl}/register/account`, params).pipe(
      tap(() => this.toastService.showSuccess('Lawyer account registered successfully!')),
      catchError(error => {
        throw error;
      })
    );
  }
}