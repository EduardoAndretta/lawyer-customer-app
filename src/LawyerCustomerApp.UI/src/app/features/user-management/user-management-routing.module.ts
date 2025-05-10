import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { UserDetailsPageComponent } from './pages/user-details-page/user-details-page.component';
import { UserRegisterPageComponent } from './pages/user-register-page/user-register-page.component';

const routes: Routes = [
  // [This route is for when UserManagementModule is loaded under /register (top-level)]
  
  { path: '', component: UserRegisterPageComponent, data: { standalone: true } }, // F[or /register]
  
  // [These routes are for when UserManagementModule is loaded under /dashboard/users]
  { path: 'new', component: UserRegisterPageComponent }, // For /dashboard/users/new (admin creates user)
  { path: ':id', component: UserDetailsPageComponent },  // For /dashboard/users/:id
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class UserManagementRoutingModule { }