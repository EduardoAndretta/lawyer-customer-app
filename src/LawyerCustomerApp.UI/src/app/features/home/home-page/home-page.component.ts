import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { UserProfileService, LAWYER_ATTRIBUTE_ID, CUSTOMER_ATTRIBUTE_ID } from '../../../core/services/user-profile.service';
import { UserDetailsInformationItem } from '../../../core/models/user.models';

@Component({
  selector: 'app-home-page',
  templateUrl: './home-page.component.html',
  styleUrls: ['./home-page.component.css']
})
export class HomePageComponent implements OnInit {
  currentUser$: Observable<UserDetailsInformationItem | null>;
  selectedAccountAttributeId$: Observable<number | null>;
  LAWYER_CONTEXT = LAWYER_ATTRIBUTE_ID;
  CUSTOMER_CONTEXT = CUSTOMER_ATTRIBUTE_ID;

  constructor(public userProfileService: UserProfileService) {
    this.currentUser$ = this.userProfileService.currentUserDetails$;
    this.selectedAccountAttributeId$ = this.userProfileService.selectedAccountAttributeId$;
  }

  ngOnInit(): void {
    // Home page logic can go here, e.g., display summary based on selected account.
  }
}