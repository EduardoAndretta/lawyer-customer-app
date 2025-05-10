import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ApiConfigService } from '../../../core/services/api-config.service';
import { ToastService } from '../../../core/services/toast.service';
import {
  UserSearchParametersDto, UserSearchInformationDto,
  UserCountParametersDto, UserCountInformationDto
} from '../../../core/models/user.models'; // Adjust path if models are structured differently

@Injectable({
  providedIn: 'root'
})
export class UserSearchService {
  private baseUrl: string;

  constructor(
    private http: HttpClient,
    private apiConfig: ApiConfigService,
    private toastService: ToastService
  ) {
    this.baseUrl = `${this.apiConfig.getBaseUrl()}/user`;
  }

  search(params: UserSearchParametersDto): Observable<UserSearchInformationDto> {
    return this.http.post<UserSearchInformationDto>(`${this.baseUrl}/search`, params).pipe(
      catchError(error => {
        // Toast handled by interceptor
        console.error('Case search failed:', error);
        return of({ items: [] }); // Return empty on error to prevent breaking UI
      })
    );
  }

  count(params: UserCountParametersDto): Observable<UserCountInformationDto> {
    return this.http.post<UserCountInformationDto>(`${this.baseUrl}/search/count`, params).pipe(
      catchError(error => {
        // Toast handled by interceptor
        console.error('Case count failed:', error);
        return of({ count: 0 }); // Return zero count on error
      })
    );
  }
}