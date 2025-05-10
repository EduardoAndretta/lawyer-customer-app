import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';

import { CustomerManagementRoutingModule } from './customer-management-routing.module';
import { SharedModule } from '../../shared/shared.module';

import { CustomerDetailsPageComponent } from './pages/customer-details-page/customer-details-page.component';
import { CustomerRegisterModalComponent } from './components/customer-register-modal/customer-register-modal.component';

import { CustomerService } from './services/customer.service';

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
  providers: [
    CustomerService
  ],
  exports: [ // Export the modal component so it can be used in DashboardModule's sidebar
    CustomerRegisterModalComponent
  ]
})
export class CustomerManagementModule { }