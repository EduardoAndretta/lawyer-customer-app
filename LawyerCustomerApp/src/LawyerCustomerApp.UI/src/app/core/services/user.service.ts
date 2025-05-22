import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { ApiConfigService } from './api-config.service';
import { ToastService } from './toast.service';
import {
  UserDetailsParametersDto, UserDetailsInformationDto,
  RegisterUserParametersDto,
  UserEditParametersDto
} from '../models/user.models';

@Injectable({
  providedIn: 'root'
})
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
    return this.http.patch(`${this.baseUrl}/edit`, params).pipe(
      tap(() => this.toastService.showSuccess('User updated successfully!')),
      catchError(error => {
        throw error;
      })
    );
  }
}