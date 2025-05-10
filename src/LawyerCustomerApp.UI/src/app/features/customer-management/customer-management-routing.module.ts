import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CustomerDetailsPageComponent } from './pages/customer-details-page/customer-details-page.component';

const routes: Routes = [
  { path: ':id', component: CustomerDetailsPageComponent },
  // No 'new' route here as registration is via modal from sidebar/user edit
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class CustomerManagementRoutingModule { }