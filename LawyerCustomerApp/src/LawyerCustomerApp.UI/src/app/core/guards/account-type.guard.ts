import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree, Router } from '@angular/router'; // Added parameters
import { Observable } from 'rxjs';
import { map, take } from 'rxjs/operators';
import { UserProfileService } from '../services/user-profile.service';
import { ToastService } from '../services/toast.service';

@Injectable({
  providedIn: 'root'
})
export class AccountTypeGuard implements CanActivate {

  constructor(
    private userProfileService: UserProfileService,
    private toastService: ToastService,
    private router: Router
  ) {}

  canActivate( 
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean | UrlTree> | Promise<boolean | UrlTree> | boolean | UrlTree {
    return this.userProfileService.selectedAccountAttributeId$.pipe(
      take(1),
      map(attributeId => {
        if (attributeId !== null) {
          return true;
        }
        this.toastService.showInfo('Please select an account type (Lawyer/Customer) from the sidebar to access this page.');
        return this.router.createUrlTree(['/dashboard/home']);
      })
    );
  }
}