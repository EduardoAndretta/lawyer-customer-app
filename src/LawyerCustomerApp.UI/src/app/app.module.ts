import { APP_INITIALIZER, NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { CoreModule } from './core/core.module';
import { initializeApplication } from './core/initializers/app-load.initializer';

@NgModule({
  declarations: [
    AppComponent
  ],
  imports: [
    BrowserModule,
    AppRoutingModule,
    HttpClientModule,
    CoreModule
  ],
 providers: [
    {
      provide: APP_INITIALIZER,
      useFactory: initializeApplication,
      multi: true,
      //deps: [AuthService, UserProfileService, TokenStorageService, PermissionService] // Deps are auto-injected by inject() now
    }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }