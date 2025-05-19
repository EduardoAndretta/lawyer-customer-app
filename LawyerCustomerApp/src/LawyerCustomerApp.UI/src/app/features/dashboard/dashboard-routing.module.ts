import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DashboardComponent } from './dashboard.component';
import { AccountTypeGuard } from '../../core/guards/account-type.guard';

const routes: Routes = [
  {
    path: '', // [Base for /dashboard]
    component: DashboardComponent,
    children: [
      {
        path: 'home',
        loadChildren: () => import('../home/home.module').then(m => m.HomeModule)
      },
      {
        path: 'search',
        canActivate: [AccountTypeGuard],
        loadChildren: () => import('../search/search.module').then(m => m.SearchModule)
      },
      {
        path: 'cases', // [Will be /dashboard/cases]
        canActivate: [AccountTypeGuard],
        loadChildren: () => import('../case-management/case-management.module').then(m => m.CaseManagementModule)
      },
      {
        path: 'users', // [Will be /dashboard/users]
        loadChildren: () => import('../user-management/user-management.module').then(m => m.UserManagementModule)
      },
      {
        path: 'lawyers', // [Will be /dashboard/lawyers]
        canActivate: [AccountTypeGuard],
        loadChildren: () => import('../lawyer-management/lawyer-management.module').then(m => m.LawyerManagementModule)
      },
      {
        path: 'customers', // [Will be /dashboard/customers]
        canActivate: [AccountTypeGuard],
        loadChildren: () => import('../customer-management/customer-management.module').then(m => m.CustomerManagementModule)
      },
      { path: '', redirectTo: 'home', pathMatch: 'full' } // [Default child for /dashboard is /dashboard/home]
    ]
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class DashboardRoutingModule { }