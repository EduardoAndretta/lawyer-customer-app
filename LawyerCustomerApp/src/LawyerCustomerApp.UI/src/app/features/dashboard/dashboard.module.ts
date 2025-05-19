import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router'; // Import RouterModule for router-outlet in dashboard.component

import { DashboardRoutingModule } from './dashboard-routing.module';
import { DashboardComponent } from './dashboard.component';
import { LcaSidebarComponent } from './components/lca-sidebar/lca-sidebar.component';
import { LcaNavbarComponent } from './components/lca-navbar/lca-navbar.component';
import { LcaGlobalSearchComponent } from './components/lca-global-search/lca-global-search.component';

import { SharedModule } from '../../shared/shared.module'; // For lca-components

// Modals for account registration (will be created in respective management modules, but imported here for sidebar use)
// For now, these will be dummy components in this module, or you can move them later.
import { LawyerManagementModule } from '../lawyer-management/lawyer-management.module';
import { CustomerManagementModule } from '../customer-management/customer-management.module';
import { CaseSearchService } from '../search/services/case-search.service';
import { UserSearchService } from '../search/services/user-search.service';
import { LawyerSearchService } from '../search/services/lawyer-search.service';
import { CustomerSearchService } from '../search/services/customer-search.service';
import { LawyerRegisterModalComponent } from '../lawyer-management/components/lawyer-register-modal/lawyer-register-modal.component';
import { CustomerRegisterModalComponent } from '../customer-management/components/customer-register-modal/customer-register-modal.component';


@NgModule({
  declarations: [
    DashboardComponent,
    LcaSidebarComponent,
    LcaNavbarComponent,
    LcaGlobalSearchComponent
  ],
  imports: [
    CommonModule,
    DashboardRoutingModule,
    SharedModule,
    RouterModule,
    LawyerManagementModule,
    CustomerManagementModule
  ],
  providers: [
    CaseSearchService,
    UserSearchService,
    LawyerSearchService,
    CustomerSearchService
  ]

  // If modals are opened dynamically and not via routing, add them to entryComponents (Angular < 9) or ensure they are part of a module.
  // For Angular 9+, they just need to be declared and exported by their own modules if loaded dynamically.
  // For simplicity, if you directly use <app-lawyer-register-modal> in sidebar, they need to be in this module or imported.
})
export class DashboardModule { }