import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule } from '@angular/forms';

import { UserManagementRoutingModule } from './user-management-routing.module';
import { SharedModule } from '../../shared/shared.module';

import { UserDetailsPageComponent } from './pages/user-details-page/user-details-page.component';
import { UserEditModalComponent } from './components/user-edit-modal/user-edit-modal.component';
import { UserPermissionsModalComponent } from './components/user-permissions-modal/user-permissions-modal.component';
import { UserRegisterPageComponent } from './pages/user-register-page/user-register-page.component';

import { UserService } from './services/user.service';

@NgModule({
  declarations: [
    UserDetailsPageComponent,
    UserEditModalComponent,
    UserPermissionsModalComponent,
    UserRegisterPageComponent
  ],
  imports: [
    CommonModule,
    UserManagementRoutingModule,
    ReactiveFormsModule,
    SharedModule
  ],
  providers: [
    UserService
  ]
})
export class UserManagementModule { }