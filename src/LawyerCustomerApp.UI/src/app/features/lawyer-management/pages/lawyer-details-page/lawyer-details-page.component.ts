import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription, of } from 'rxjs';
import { switchMap, tap, finalize } from 'rxjs/operators';

import { LawyerService } from '../../services/lawyer.service';
import { UserProfileService } from '../../../../core/services/user-profile.service';
import { ToastService } from '../../../../core/services/toast.service';
import { LawyerDetailsInformationItem } from '../../../../core/models/lawyer.models';

@Component({
  selector: 'app-lawyer-details-page',
  templateUrl: './lawyer-details-page.component.html',
  styleUrls: ['./lawyer-details-page.component.css'] // Can share styles with other details pages
})
export class LawyerDetailsPageComponent implements OnInit, OnDestroy {
  lawyerDetails: LawyerDetailsInformationItem | null = null;
  isLoadingDetails: boolean = false;
  lawyerId!: number;

  private routeSubscription!: Subscription;
  private attributeIdSubscription!: Subscription;
  private loggedInUserAttributeId: number | null = null;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private lawyerService: LawyerService,
    private userProfileService: UserProfileService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.attributeIdSubscription = this.userProfileService.selectedAccountAttributeId$.subscribe(id => {
      this.loggedInUserAttributeId = id;
      if (this.lawyerId && this.loggedInUserAttributeId) {
        this.loadLawyerDetails();
      }
    });

    this.routeSubscription = this.route.paramMap.pipe(
      tap(params => {
        const id = params.get('id');
        if (!id) {
          this.toastService.showError("Lawyer ID not found in URL.");
          this.router.navigate(['/dashboard/home']);
          return;
        }
        this.lawyerId = +id;
      }),
      switchMap(() => {
        if (this.lawyerId && this.loggedInUserAttributeId) {
          this.loadLawyerDetails();
        }
        return of(null);
      })
    ).subscribe();
  }

  loadLawyerDetails(): void {
    if (!this.lawyerId || !this.loggedInUserAttributeId) return;
    this.isLoadingDetails = true;
    this.lawyerService.getDetails({ lawyerId: this.lawyerId, attributeId: this.loggedInUserAttributeId }).pipe(
        finalize(() => this.isLoadingDetails = false)
    ).subscribe(response => {
      if (response && response.item) {
        this.lawyerDetails = response.item;
      } else {
        this.toastService.showError('Could not load lawyer details.');
        this.lawyerDetails = null;
      }
    });
  }

  // No edit/permissions buttons here as they are handled at the User level
  navigateToUser(): void {
      if (this.lawyerDetails && this.lawyerDetails.userId) {
          this.router.navigate(['/dashboard/users', this.lawyerDetails.userId]);
      } else {
          this.toastService.showInfo("Associated user ID not found for this lawyer.");
      }
  }

  ngOnDestroy(): void {
    if (this.routeSubscription) this.routeSubscription.unsubscribe();
    if (this.attributeIdSubscription) this.attributeIdSubscription.unsubscribe();
  }
}