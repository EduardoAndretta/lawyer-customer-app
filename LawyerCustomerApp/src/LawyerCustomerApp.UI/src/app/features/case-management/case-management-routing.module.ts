import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CaseDetailsPageComponent } from './pages/case-details-page/case-details-page.component';
import { CaseRegisterPageComponent } from './pages/case-register-page/case-register-page.component';

const routes: Routes = [
  { path: 'new', component: CaseRegisterPageComponent }, // Optional: For creating new cases via a page
  { path: ':id', component: CaseDetailsPageComponent },
  { path: '', redirectTo: 'new', pathMatch: 'full' } // Default to new or a list page if you have one
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class CaseManagementRoutingModule { }