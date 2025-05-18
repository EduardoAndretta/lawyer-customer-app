import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { tap, catchError, mapTo } from 'rxjs/operators';
import { ApiConfigService } from './api-config.service';
import { TokenStorageService } from './token-storage.service';
import { UserProfileService } from './user-profile.service';
import {
  AuthenticateParametersDto, AuthenticateInformationDto,
  RefreshParametersDto, RefreshInformationDto,
  InvalidateParametersDto
} from '../models/auth.models';
import { Router } from '@angular/router';
import { PermissionService } from './permission.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private baseUrl: string;
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(this.hasToken());
  public isAuthenticated = this.isAuthenticatedSubject.asObservable();

  constructor(
    private http: HttpClient,
    private permissionService: PermissionService,
    private apiConfig: ApiConfigService,
    private tokenStorage: TokenStorageService,
    private userProfileService: UserProfileService,
    private router: Router
  ) {
    this.baseUrl = `${this.apiConfig.getBaseUrl()}/auth`;
  }

  private hasToken(): boolean {
    return !!this.tokenStorage.getToken();
  }

  login(credentials: AuthenticateParametersDto): Observable<AuthenticateInformationDto> {
    return this.http.post<AuthenticateInformationDto>(`${this.baseUrl}/authenticate`, credentials).pipe(
      tap(response => {
        if (response.token && response.refreshToken) {
          this.tokenStorage.saveToken(response.token);
          this.tokenStorage.saveRefreshToken(response.refreshToken);
          this.isAuthenticatedSubject.next(true);
          // Potentially fetch user details here or let the component do it
        }
      })
    );
  }

  refreshToken(): Observable<RefreshInformationDto> {
    const params: RefreshParametersDto = {
      token: this.tokenStorage.getToken(),
      refreshToken: this.tokenStorage.getRefreshToken()
    };
    return this.http.post<RefreshInformationDto>(`${this.baseUrl}/refresh`, params).pipe(
      tap(response => {
        if (response.token && response.refreshToken) {
          this.tokenStorage.saveToken(response.token);
          this.tokenStorage.saveRefreshToken(response.refreshToken);
          this.isAuthenticatedSubject.next(true);
        } else {
          // If refresh fails to return new tokens, it's a problem.
          this.logout();
        }
      }),
      catchError(error => {
        this.logout(); // Logout on refresh token failure
        throw error;
      })
    );
  }

  logout(): void {
    const params: InvalidateParametersDto = {
      token: this.tokenStorage.getToken(),
      refreshToken: this.tokenStorage.getRefreshToken()
    };

    // Call invalidate endpoint, but proceed with local logout regardless of API success
    // to ensure user is logged out frontend-wise.
    this.http.put(`${this.baseUrl}/invalidate`, params).pipe(
        catchError(err => {
            console.warn("Invalidate token call failed, proceeding with local logout.", err);
            return of(null); // Continue logout flow
        })
    ).subscribe(() => {
        this.performLocalLogout();
    });
  }

  private performLocalLogout(): void {
    this.tokenStorage.clearTokens();
    this.userProfileService.clearUserProfile();
    this.permissionService.clearAllLoadedPermissions();
    this.isAuthenticatedSubject.next(false);
    this.router.navigate(['/login']);
  }

  // Helper to get user ID from token (if stored in token, otherwise needs another mechanism)
  // THIS IS A SIMPLIFIED EXAMPLE. Real JWT parsing should be done carefully.
  // Or, ideally, backend provides a /me endpoint to get user ID.
  getCurrentUserId(): number | null {
    const token = this.tokenStorage.getToken();
    if (token) {
      try {
        // WARNING: THIS IS A VERY BASIC AND INSECURE WAY TO GET INFO FROM A JWT
        // A proper library should be used for JWT parsing if this is the approach.
        // Or, better, get user ID from a dedicated /me endpoint after login.
        const payload = JSON.parse(atob(token.split('.')[1]));
        // Assuming 'nameid' or 'sub' claim contains the user ID. Adjust as per your JWT structure.
        return payload.nameid || payload.sub || null;
      } catch (e) {
        console.error("Error parsing token for user ID", e);
        return null;
      }
    }
    return null;
  }
}