import { inject } from '@angular/core';
import { Observable, of, forkJoin } from 'rxjs';
import { catchError, switchMap, tap } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { TokenStorageService } from '../services/token-storage.service';
import { UserProfileService } from '../services/user-profile.service';
import { PermissionService } from '../services/permission.service';

export function initializeApplication(): () => Observable<any> {
  const tokenStorageService = inject(TokenStorageService);
  const authService = inject(AuthService);
  const userProfileService = inject(UserProfileService);
  const permissionService = inject(PermissionService);

  return () => {
    const token = tokenStorageService.getToken();
    const refreshToken = tokenStorageService.getRefreshToken();

    if (token && refreshToken) {
      // [Assume token is valid for now, or add a refresh token call here first if it's short-lived]
      // [A better approach is if UserProfileService.loadUserDetails implicitly validates/refreshes.]
      // [For simplicity, let's assume loading user details is enough.]

      const userId = authService.getCurrentUserId(); // This needs to be reliable from token
      if (!userId) {
        console.warn("APP_INITIALIZER: No user ID found in token, clearing tokens.");
        authService.logout(); // [Perform local logout if token is malformed]
        return of(null); // [Or EMPTY]
      }

      console.log("APP_INITIALIZER: Token found, attempting to load user profile and global permissions.");

      // [Chain observables: load user profile, then global permissions]
      return userProfileService.loadUserDetails(userId).pipe(
        switchMap(userDetailsDto => {
          if (userDetailsDto && userDetailsDto.item) {
            // [User profile loaded, now load global permissions for this user]
            // [The attributeId for global permissions might be the one just set by loadUserDetails]
            const attributeIdForGlobalPerms = userProfileService.getCurrentAttributeId();

            const loadGlobalUserPerms$ = permissionService.getGlobalUserPermissions({}).pipe(
                tap(perms => console.log("APP_INITIALIZER: Global User Permissions loaded", perms)),
                catchError(err => {
                    console.error("APP_INITIALIZER: Failed to load global user permissions", err);
                    return of(null); // [Continue even if this fails]
                })
            );

            // [If an attributeId is set, also load global case permissions for that context]
            let loadGlobalCasePerms$: Observable<any> = of(null);
            if (attributeIdForGlobalPerms !== null) {
                loadGlobalCasePerms$ = permissionService.getGlobalCasePermissions({ attributeId: attributeIdForGlobalPerms }).pipe(
                    tap(perms => console.log("APP_INITIALIZER: Global Case Permissions loaded", perms)),
                    catchError(err => {
                        console.error("APP_INITIALIZER: Failed to load global case permissions", err);
                        return of(null);
                    })
                );
            }
            return forkJoin([loadGlobalUserPerms$, loadGlobalCasePerms$]);
          } else {
            console.warn("APP_INITIALIZER: Failed to load user details, logging out.");
            authService.logout(); // [Logout if user details can't be fetched]
            return of(null);
          }
        }),
        catchError((error) => {
          console.error('APP_INITIALIZER: Error during app initialization, logging out.', error);
          authService.logout(); // [Handles token clearing and redirect]
          return of(null);
        })
      );
    } else {
      console.log("APP_INITIALIZER: No token found, skipping user load.");
      return of(null); // [No token, nothing to load]
    }
  };
}