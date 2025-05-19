import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { Subscription, Observable } from 'rxjs';
import { filter, map, distinctUntilChanged } from 'rxjs/operators';
import { AuthService } from '../../../../core/services/auth.service';
import { UserProfileService, LAWYER_ATTRIBUTE_ID, CUSTOMER_ATTRIBUTE_ID } from '../../../../core/services/user-profile.service';
import { LcaSelectOption } from '../../../../shared/components/lca-select/lca-select.component';
import { ToastService } from '../../../../core/services/toast.service';
import { UserDetailsInformationItem } from '../../../../core/models/user.models';

@Component({
  selector: 'app-lca-sidebar',
  templateUrl: './lca-sidebar.component.html',
  styleUrls: ['./lca-sidebar.component.css']
})
export class LcaSidebarComponent implements OnInit, OnDestroy {
  currentUser$: Observable<UserDetailsInformationItem | null>;
  selectedAccountAttributeId$: Observable<number | null>;
  accountOptions: LcaSelectOption[] = [];
  isAccountSelectDisabled: boolean = false;

  // [Modal visibility flags]
  showLawyerRegisterModal = false;
  showCustomerRegisterModal = false;

  // [Store current user details for direct checks in template for button visibility]
  currentUserDetails: UserDetailsInformationItem | null = null;

  private subscriptions: Subscription = new Subscription();

  constructor(
    private authService: AuthService,
    public userProfileService: UserProfileService, // [Public for template access to currentUserDetails$ directly]
    private router: Router,
    private toastService: ToastService
  ) {
    this.currentUser$ = this.userProfileService.currentUserDetails$;
    this.selectedAccountAttributeId$ = this.userProfileService.selectedAccountAttributeId$;
  }

  ngOnInit(): void {
    this.currentUser$ = this.userProfileService.currentUserDetails$;
    this.selectedAccountAttributeId$ = this.userProfileService.selectedAccountAttributeId$;

    const userDetailsSub = this.currentUser$.subscribe(user => {
      this.currentUserDetails = user;
      this.updateAccountOptions(user);
    });
    this.subscriptions.add(userDetailsSub);

    const routerEventsSub = this.router.events.pipe(
      filter(event => event instanceof NavigationEnd),
      map(event => (event as NavigationEnd).urlAfterRedirects),
      distinctUntilChanged()
    ).subscribe(urlAfterRedirects => {
      const homePaths = ['/dashboard/home', '/dashboard'];
      this.isAccountSelectDisabled = !homePaths.some(path => urlAfterRedirects.endsWith(path));
    });
    this.subscriptions.add(routerEventsSub);

    // [Initial check for account select disabled state]
    const initialUrl = this.router.url;
    const homePaths = ['/dashboard/home', '/dashboard'];
    this.isAccountSelectDisabled = !homePaths.some(path => initialUrl.endsWith(path));
  }

  private updateAccountOptions(user: UserDetailsInformationItem | null): void {
    this.accountOptions = [];
    if (user) {
      if (user.hasCustomerAccount) {
        this.accountOptions.push({ label: 'Customer Account', value: CUSTOMER_ATTRIBUTE_ID });
      }
      if (user.hasLawyerAccount) {
        this.accountOptions.push({ label: 'Lawyer Account', value: LAWYER_ATTRIBUTE_ID });
      }
    }
  }

  onAccountTypeChange(attributeId: number | string | null): void {
    if (attributeId === null || attributeId === undefined) return;

    const numericAttributeId = +attributeId;

    if (this.isAccountSelectDisabled) {
        this.toastService.showInfo("Please navigate to the Home page to change your account type.");
        
        setTimeout(() => this.userProfileService.setSelectedAccount(this.userProfileService.getCurrentAttributeId()), 0);
        
        return;
    }

    this.userProfileService.setSelectedAccount(numericAttributeId);

    this.toastService.showSuccess(`Switched to ${numericAttributeId === LAWYER_ATTRIBUTE_ID ? 'Lawyer' : 'Customer'} context.`);
  }

  openRegisterLawyerModal(): void {
    if (this.currentUserDetails && this.currentUserDetails.id && !this.currentUserDetails.hasLawyerAccount) {
      this.showLawyerRegisterModal = true;
    } else if (this.currentUserDetails && this.currentUserDetails.hasLawyerAccount) {
        this.toastService.showInfo("Lawyer account already exists.");
    } else {
        this.toastService.showError("User data not fully loaded. Cannot register lawyer account yet.");
    }
  }

  onLawyerRegisterModalClosed(success: boolean): void {
    this.showLawyerRegisterModal = false;
    if (success && this.currentUserDetails?.id) {
      this.toastService.showInfo("Refreshing user profile...");
      this.userProfileService.loadUserDetails(this.currentUserDetails.id).subscribe();
    }
  }

  openRegisterCustomerModal(): void {
     if (this.currentUserDetails && this.currentUserDetails.id && !this.currentUserDetails.hasCustomerAccount) {
      this.showCustomerRegisterModal = true;
    } else if (this.currentUserDetails && this.currentUserDetails.hasCustomerAccount) {
        this.toastService.showInfo("Customer account already exists.");
    } else {
        this.toastService.showError("User data not fully loaded. Cannot register customer account yet.");
    }
  }

  onCustomerRegisterModalClosed(success: boolean): void {
    this.showCustomerRegisterModal = false;
     if (success && this.currentUserDetails?.id) {
      this.toastService.showInfo("Refreshing user profile...");
      this.userProfileService.loadUserDetails(this.currentUserDetails.id).subscribe();
    }
  }

  logout(): void {
    this.authService.logout();
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}