import { Component } from '@angular/core';

@Component({
  selector: 'app-auth-layout',
  template: `
    <div class="auth-layout-container">
      <router-outlet></router-outlet>
    </div>
  `,
  styles: [`
    .auth-layout-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: 100vh;
      background-color: #f0f2f5;
      padding: 20px;
    }
  `]
})
export class AuthLayoutComponent {}