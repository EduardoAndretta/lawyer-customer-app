import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { ApiConfigService } from './api-config.service';
import { UserDetailsInformationDto, UserDetailsParametersDto, UserDetailsInformationItem } from '../models/user.models';
import { ToastService } from './toast.service';

// [Define constants for attribute IDs]
export const LAWYER_ATTRIBUTE_ID = 1;
export const CUSTOMER_ATTRIBUTE_ID = 2;

@Injectable({
  providedIn: 'root'
})
export class UserProfileService {
  private baseUrl: string;
  private currentUserDetailsSubject = new BehaviorSubject<UserDetailsInformationItem | null>(null);
  public currentUserDetails$ = this.currentUserDetailsSubject.asObservable();

  // [Default to customer if available, then lawyer, then null.]
  private selectedAccountAttributeIdSubject = new BehaviorSubject<number | null>(null);
  public selectedAccountAttributeId$ = this.selectedAccountAttributeIdSubject.asObservable();

  constructor(
    private http: HttpClient,
    private apiConfig: ApiConfigService,
    private toastService: ToastService,
  ) {
    this.baseUrl = `${this.apiConfig.getBaseUrl()}/user`;
  }

  loadUserDetails(userId: number): Observable<UserDetailsInformationDto | null> {
    if (!userId) {
        this.toastService.showError('User ID is required to load details.');
        return of(null);
    }
    // The attributeId for /api/user/details is usually the *requesting* user's current role context.
    // If not logged in or no role selected, this might be tricky.
    // For simplicity, let's assume the API can handle a null or default attributeId for fetching own details.
    // Or, if the details endpoint is always for the logged-in user, it might not need attributeId.
    // We'll pass the currently selected account for context, or a default if none selected.
    const currentSelectedAttributeId = this.selectedAccountAttributeIdSubject.value;

    const params: UserDetailsParametersDto = {
      relatedUserId: userId,
      attributeId: currentSelectedAttributeId // [This could be null or a default. API needs to handle this.]
    };

    return this.http.post<UserDetailsInformationDto>(`${this.baseUrl}/details`, params).pipe(
      tap(response => {
        if (response && response.item) {
          this.currentUserDetailsSubject.next(response.item);
          this.setDefaultSelectedAccount(response.item);
        } else {
          this.currentUserDetailsSubject.next(null);
          this.selectedAccountAttributeIdSubject.next(null);
          this.toastService.showError('Failed to load user details.');
        }
      }),
      catchError(error => {
        this.currentUserDetailsSubject.next(null);
        this.selectedAccountAttributeIdSubject.next(null);

        return throwError(() => error);
      })
    );
  }

  private setDefaultSelectedAccount(userDetails: UserDetailsInformationItem): void {
    if (userDetails.hasCustomerAccount) {
      this.selectedAccountAttributeIdSubject.next(CUSTOMER_ATTRIBUTE_ID);
    } else if (userDetails.hasLawyerAccount) {
      this.selectedAccountAttributeIdSubject.next(LAWYER_ATTRIBUTE_ID);
    } else {
      this.selectedAccountAttributeIdSubject.next(null);
    }
  }

  setSelectedAccount(attributeId: number | null): void {
    const currentUser = this.currentUserDetailsSubject.value;
    if (attributeId === LAWYER_ATTRIBUTE_ID && !currentUser?.hasLawyerAccount) {
        this.toastService.showError("Lawyer account not available.");
        return;
    }
    if (attributeId === CUSTOMER_ATTRIBUTE_ID && !currentUser?.hasCustomerAccount) {
        this.toastService.showError("Customer account not available.");
        return;
    }
    this.selectedAccountAttributeIdSubject.next(attributeId);
    // Potentially reload data or trigger other actions based on account switch
    // For now, we just update the BehaviorSubject.
    // If user details need to be re-fetched with new attribute context:
    
    if (currentUser && currentUser.id && attributeId) {
      this.loadUserDetails(currentUser.id).subscribe();
    }
  }

  getCurrentAttributeId(): number | null {
    return this.selectedAccountAttributeIdSubject.value;
  }

  getCurrentUserDetails(): UserDetailsInformationItem | null {
    return this.currentUserDetailsSubject.value;
  }

  clearUserProfile(): void {
    this.currentUserDetailsSubject.next(null);
    this.selectedAccountAttributeIdSubject.next(null);
  }
}