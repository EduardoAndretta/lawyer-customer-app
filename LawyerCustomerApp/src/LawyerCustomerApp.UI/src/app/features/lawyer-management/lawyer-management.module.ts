import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';

import { LawyerManagementRoutingModule } from './lawyer-management-routing.module';
import { SharedModule } from '../../shared/shared.module';

import { LawyerDetailsPageComponent } from './pages/lawyer-details-page/lawyer-details-page.component';
import { LawyerRegisterModalComponent } from './components/lawyer-register-modal/lawyer-register-modal.component';

@NgModule({
  declarations: [
    LawyerDetailsPageComponent,
    LawyerRegisterModalComponent
  ],
  imports: [
    CommonModule,
    LawyerManagementRoutingModule,
    ReactiveFormsModule,
    SharedModule
  ],
  exports: [
    LawyerRegisterModalComponent
  ]
})
export class LawyerManagementModule { }