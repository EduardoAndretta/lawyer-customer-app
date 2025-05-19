import { NgModule, Optional, SkipSelf } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HTTP_INTERCEPTORS } from '@angular/common/http';

import { AuthGuard } from './guards/auth.guard';
import { AuthInterceptor } from './interceptors/auth.interceptor';
import { ErrorInterceptor } from './interceptors/error.interceptor';

import { ApiConfigService } from './services/api-config.service';
import { AuthService } from './services/auth.service';
import { TokenStorageService } from './services/token-storage.service';
import { UserProfileService } from './services/user-profile.service';
import { ToastService } from './services/toast.service';
import { ComboDataService } from './services/combo-data.service';
import { UserService } from './services/user.service';
import { CustomerService } from './services/customer.service';
import { LawyerService } from './services/lawyer.service';
import { CaseService } from './services/case.service';

@NgModule({
  imports: [
    CommonModule
  ],
  providers: [
    AuthGuard,
    ApiConfigService,
    AuthService,
    TokenStorageService,
    UserProfileService,
    ToastService,
    ComboDataService,
    UserService,
    CaseService,
    CustomerService,
    LawyerService,
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
  ]
})
export class CoreModule {
  constructor(@Optional() @SkipSelf() parentModule: CoreModule) {
    if (parentModule) {
      throw new Error('CoreModule is already loaded. Import it in the AppModule only');
    }
  }
}