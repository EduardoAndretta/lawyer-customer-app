import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { UserService } from '../../services/user.service';
import { ToastService } from '../../../../core/services/toast.service';
import { finalize } from 'rxjs/operators';
import { RegisterUserParametersDto } from '../../../../core/models/user.models';

@Component({
  selector: 'app-user-register-page',
  templateUrl: './user-register-page.component.html',
  styleUrls: ['./user-register-page.component.css'] // Can share styles with case-register
})
export class UserRegisterPageComponent implements OnInit {
  registerForm!: FormGroup;
  isLoading: boolean = false;
  submitted: boolean = false;

  constructor(
    private fb: FormBuilder,
    private userService: UserService,
    private toastService: ToastService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.registerForm = this.fb.group({
      name: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      // confirmPassword: ['', Validators.required] // Add confirm password if needed
    }
    // , { validator: this.passwordMatchValidator } // Add custom validator for password match
    );
  }

  // Example custom validator (if you add confirmPassword)
  // passwordMatchValidator(group: FormGroup) {
  //   const password = group.get('password')?.value;
  //   const confirmPassword = group.get('confirmPassword')?.value;
  //   return password === confirmPassword ? null : { mismatch: true };
  // }

  get f() { return this.registerForm.controls; }

  onSubmit(): void {
    this.submitted = true;
    if (this.registerForm.invalid) {
      return;
    }

    this.isLoading = true;
    const payload: RegisterUserParametersDto = {
      name: this.f['name'].value,
      email: this.f['email'].value,
      password: this.f['password'].value
    };

    this.userService.register(payload).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: () => {
        // Navigate to user list or login page
        this.toastService.showSuccess('User registered successfully. They can now log in.');
        this.router.navigate(['/login']); // Or /dashboard/users if you have a list
      },
      error: (err) => {
        // Error handled by interceptor
      }
    });
  }
}