import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';

import { CustomerManagementRoutingModule } from './customer-management-routing.module';
import { SharedModule } from '../../shared/shared.module';

import { CustomerDetailsPageComponent } from './pages/customer-details-page/customer-details-page.component';
import { CustomerRegisterModalComponent } from './components/customer-register-modal/customer-register-modal.component';

@NgModule({
  declarations: [
    CustomerDetailsPageComponent,
    CustomerRegisterModalComponent
  ],
  imports: [
    CommonModule,
    CustomerManagementRoutingModule,
    ReactiveFormsModule,
    SharedModule
  ],
  exports: [
    CustomerRegisterModalComponent
  ]
})
export class CustomerManagementModule { }