import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms'; // For forms in modals

import { CaseManagementRoutingModule } from './case-management-routing.module';
import { SharedModule } from '../../shared/shared.module';

import { CaseDetailsPageComponent } from './pages/case-details-page/case-details-page.component';
import { CaseEditModalComponent } from './components/case-edit-modal/case-edit-modal.component';
import { CasePermissionsModalComponent } from './components/case-permissions-modal/case-permissions-modal.component';
import { CaseRegisterPageComponent } from './pages/case-register-page/case-register-page.component'; // If you have a dedicated page

import { CaseService } from './services/case.service';

@NgModule({
  declarations: [
    CaseDetailsPageComponent,
    CaseEditModalComponent,
    CasePermissionsModalComponent,
    CaseRegisterPageComponent
  ],
  imports: [
    CommonModule,
    CaseManagementRoutingModule,
    ReactiveFormsModule,
    SharedModule
  ],
  providers: [
    CaseService // Service specific to case management
  ]
})
export class CaseManagementModule { }