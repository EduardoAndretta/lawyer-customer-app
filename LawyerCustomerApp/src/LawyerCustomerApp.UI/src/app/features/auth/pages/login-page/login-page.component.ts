import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../../core/services/auth.service';
import { ToastService } from '../../../../core/services/toast.service';
import { UserProfileService } from '../../../../core/services/user-profile.service';
import { finalize } from 'rxjs/operators';

@Component({
  selector: 'app-login-page',
  templateUrl: './login-page.component.html',
  styleUrls: ['./login-page.component.css']
})
export class LoginPageComponent implements OnInit {
  loginForm!: FormGroup;
  isLoading = false;
  submitted = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private toastService: ToastService,
    private userProfileService: UserProfileService
  ) {}

  ngOnInit(): void {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]]
    });
  }

  get f() { return this.loginForm.controls; }

  onSubmit(): void {
    this.submitted = true;
    if (this.loginForm.invalid) {
      return;
    }

    this.isLoading = true;
    this.authService.login(this.loginForm.value).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: (authInfo) => {
        // Fetch user details after successful login
        const userId = this.authService.getCurrentUserId(); // Implement this in AuthService
        if (userId) {
          this.userProfileService.loadUserDetails(userId).subscribe({
            next: () => {
              this.toastService.showSuccess('Login successful!');
              this.router.navigate(['/dashboard/home']);
            },
            error: (err) => {
              this.toastService.showError('Login successful, but failed to load user profile.');
              // Still navigate, sidebar will show limited info or prompt.
              this.router.navigate(['/dashboard/home']);
            }
          });
        } else {
           // Should not happen if token is valid and parsed correctly
          this.toastService.showError('Login successful, but could not retrieve user ID.');
          this.router.navigate(['/dashboard/home']); // Navigate anyway
        }
      },
      error: (error) => {
        // Error is already handled by interceptor, but you can add specific logic here
        // console.error('Login failed:', error);
      }
    });
  }
}