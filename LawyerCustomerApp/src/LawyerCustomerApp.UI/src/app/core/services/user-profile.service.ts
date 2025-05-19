import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';
import { ApiConfigService } from './api-config.service';
import { UserDetailsInformationDto, UserDetailsParametersDto, UserDetailsInformationItem } from '../models/user.models';
import { ToastService } from './toast.service';

export const LAWYER_ATTRIBUTE_ID = 1;
export const CUSTOMER_ATTRIBUTE_ID = 2;

@Injectable({
  providedIn: 'root'
})
export class UserProfileService {
  private baseUrl: string;
  private currentUserDetailsSubject = new BehaviorSubject<UserDetailsInformationItem | null>(null);
  public currentUserDetails$ = this.currentUserDetailsSubject.asObservable();

  private selectedAccountAttributeIdSubject = new BehaviorSubject<number | null>(null);
  public selectedAccountAttributeId$ = this.selectedAccountAttributeIdSubject.asObservable();

  // [Flag to track if the initial user load (e.g., after login) has set a default account.]
  private isInitialDefaultAccountSet: boolean = false;

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
        // [Clear profile if essential data is missing for the call]
        this.clearUserProfileData();
        return of(null);
    }

    const params: UserDetailsParametersDto = {
      relatedUserId: userId,
      attributeId: this.selectedAccountAttributeIdSubject.value // Pass current context
    };

    return this.http.post<UserDetailsInformationDto>(`${this.baseUrl}/details`, params).pipe(
      tap(response => {
        if (response && response.item) {
          const newDetails = response.item;
          const oldDetails = this.currentUserDetailsSubject.value; // For comparison if needed
          this.currentUserDetailsSubject.next(newDetails);

          const currentSelectedId = this.selectedAccountAttributeIdSubject.value;
          let selectionStillValid = false;

          if (currentSelectedId !== null) {
            if (currentSelectedId === LAWYER_ATTRIBUTE_ID && newDetails.hasLawyerAccount) {
              selectionStillValid = true;
            } else if (currentSelectedId === CUSTOMER_ATTRIBUTE_ID && newDetails.hasCustomerAccount) {
              selectionStillValid = true;
            }
          } else {
            // [If current selection is null, it's "valid" in the sense that it doesn't need to be forcibly changed
            // unless this is the initial load and accounts *are* available.]
            selectionStillValid = true;
          }

          // [Set default if]
          // 1. This is the first successful user load for the session (isInitialDefaultAccountSet is false).
          // 2. OR the currently selected account ID is no longer valid based on newDetails.
          if (!this.isInitialDefaultAccountSet || !selectionStillValid) {
            this.setDefaultSelectedAccount(newDetails);
            if (!oldDetails || !this.isInitialDefaultAccountSet) { // Mark initial default set only on first actual data population
                this.isInitialDefaultAccountSet = true;
            }
          }
          // [Otherwise, if initial default is set and current selection is still valid,
          // we respect the user's explicit choice (already in selectedAccountAttributeIdSubject).]

        } else {
          this.toastService.showError('Failed to load user details or no data returned.');
          this.clearUserProfileData();
        }
      }),
      catchError(error => {
        this.toastService.showError('Error loading user details.');
        this.clearUserProfileData();
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

    if (!currentUser) {
        this.toastService.showError("User details not loaded. Cannot change account.");
        // [Attempt to revert to the last known good value if the UI tries to change without user context]
        // [This depends on how the select component handles this.]
        // [Emitting the current value of the subject might trigger a UI refresh if it's bound with async pipe.]
        this.selectedAccountAttributeIdSubject.next(this.selectedAccountAttributeIdSubject.value);
        return;
    }

    // [Validate if the user actually has the account they are trying to switch to.]
    if (attributeId === LAWYER_ATTRIBUTE_ID && !currentUser.hasLawyerAccount) {
        this.toastService.showError("Lawyer account not available for this user.");
        this.selectedAccountAttributeIdSubject.next(this.selectedAccountAttributeIdSubject.value); // Revert
        return;
    }
    if (attributeId === CUSTOMER_ATTRIBUTE_ID && !currentUser.hasCustomerAccount) {
        this.toastService.showError("Customer account not available for this user.");
        this.selectedAccountAttributeIdSubject.next(this.selectedAccountAttributeIdSubject.value); // Revert
        return;
    }

    // [Only proceed if the new selection is different from the current one.]
    if (this.selectedAccountAttributeIdSubject.value !== attributeId) {
        this.selectedAccountAttributeIdSubject.next(attributeId);

        // [If user details need to be re-fetched with the new attribute context (e.g., API returns
        // different permissions or data based on the active attributeId).]
        if (currentUser.id) {
            this.loadUserDetails(currentUser.id).subscribe({
                error: () => {
                }
            });
        }
    }
  }

  getCurrentAttributeId(): number | null {
    return this.selectedAccountAttributeIdSubject.value;
  }

  getCurrentUserDetails(): UserDetailsInformationItem | null {
    return this.currentUserDetailsSubject.value;
  }

  private clearUserProfileData(): void {
    this.currentUserDetailsSubject.next(null);
    this.selectedAccountAttributeIdSubject.next(null);
    this.isInitialDefaultAccountSet = false; // Reset this flag
  }

  public clearUserProfile(): void {
      this.clearUserProfileData();
  }
}