import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LawyerDetailsPageComponent } from './pages/lawyer-details-page/lawyer-details-page.component';

const routes: Routes = [
  { path: ':id', component: LawyerDetailsPageComponent },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class LawyerManagementRoutingModule { }