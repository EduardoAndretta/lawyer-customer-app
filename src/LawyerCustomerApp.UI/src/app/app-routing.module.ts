// src/app/app-routing.module.ts
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './core/guards/auth.guard';

const routes: Routes = [
  {
    path: 'login',
    loadChildren: () => import('./features/auth/auth.module').then(m => m.AuthModule)
  },
  {
    path: 'register', // For user self-registration, if you want it separate from login area
    loadChildren: () => import('./features/user-management/user-management.module').then(m => m.UserManagementModule),
    // This assumes UserManagementModule has a route for 'new' or '' that points to UserRegisterPageComponent
    // If UserRegisterPageComponent is standalone, create a small module for it.
    // For now, to match your description of a "login screen like" register page:
    // We might need a separate AuthLayoutComponent if register shouldn't have sidebar.
    // Let's assume UserRegisterPageComponent is designed to be standalone for now.
    // If it's meant for admins *inside* the dashboard, then it's a child of /dashboard/users/new.
  },
  {
    path: 'dashboard',
    loadChildren: () => import('./features/dashboard/dashboard.module').then(m => m.DashboardModule),
    canActivate: [AuthGuard]
    // All other feature modules (home, search, case, user, lawyer, customer, config)
    // will be routed as children within dashboard-routing.module.ts
  },
  { path: '', redirectTo: '/dashboard/home', pathMatch: 'full' }, // Default to dashboard home
  { path: '**', redirectTo: '/dashboard/home' } // Catch-all, or a 404 page
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }