import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription, of } from 'rxjs';
import { switchMap, tap, finalize } from 'rxjs/operators';

import { CustomerService } from '../../services/customer.service';
import { UserProfileService } from '../../../../core/services/user-profile.service';
import { ToastService } from '../../../../core/services/toast.service';
import { CustomerDetailsInformationItem } from '../../../../core/models/customer.models';

@Component({
  selector: 'app-customer-details-page',
  templateUrl: './customer-details-page.component.html',
  styleUrls: ['./customer-details-page.component.css'] // Can share styles with other details pages
})
export class CustomerDetailsPageComponent implements OnInit, OnDestroy {
  customerDetails: CustomerDetailsInformationItem | null = null;
  isLoadingDetails: boolean = false;
  customerId!: number;

  private routeSubscription!: Subscription;
  private attributeIdSubscription!: Subscription;
  private loggedInUserAttributeId: number | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private customerService: CustomerService,
    private userProfileService: UserProfileService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.attributeIdSubscription = this.userProfileService.selectedAccountAttributeId$.subscribe(id => {
      this.loggedInUserAttributeId = id;
      if (this.customerId && this.loggedInUserAttributeId) {
        this.loadCustomerDetails();
      }
    });

    this.routeSubscription = this.route.paramMap.pipe(
      tap(params => {
        const id = params.get('id');
        if (!id) {
          this.toastService.showError("Customer ID not found in URL.");
          this.router.navigate(['/dashboard/home']);
          return;
        }
        this.customerId = +id;
      }),
      switchMap(() => {
        if (this.customerId && this.loggedInUserAttributeId) {
          this.loadCustomerDetails();
        }
        return of(null);
      })
    ).subscribe();
  }

  loadCustomerDetails(): void {
    if (!this.customerId || !this.loggedInUserAttributeId) return;
    this.isLoadingDetails = true;
    this.customerService.getDetails({ customerId: this.customerId, attributeId: this.loggedInUserAttributeId }).pipe(
        finalize(() => this.isLoadingDetails = false)
    ).subscribe(response => {
      if (response && response.item) {
        this.customerDetails = response.item;
      } else {
        this.toastService.showError('Could not load customer details.');
        this.customerDetails = null;
      }
    });
  }

  // No edit/permissions buttons here as they are handled at the User level
  navigateToUser(): void {
      if (this.customerDetails && this.customerDetails.userId) {
          this.router.navigate(['/dashboard/users', this.customerDetails.userId]);
      } else {
          this.toastService.showInfo("Associated user ID not found for this customer.");
      }
  }

  ngOnDestroy(): void {
    if (this.routeSubscription) this.routeSubscription.unsubscribe();
    if (this.attributeIdSubscription) this.attributeIdSubscription.unsubscribe();
  }
}