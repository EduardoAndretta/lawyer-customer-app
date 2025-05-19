import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CustomerService } from '../../../../core/services/customer.service';
import { UserProfileService } from '../../../../core/services/user-profile.service'; // To get current user ID
import { ToastService } from '../../../../core/services/toast.service';
import { CustomerRegisterParametersDto } from '../../../../core/models/customer.models';
import { finalize } from 'rxjs/operators';

@Component({
  selector: 'app-customer-register-modal',
  templateUrl: './customer-register-modal.component.html',
  styleUrls: ['./customer-register-modal.component.css']
})
export class CustomerRegisterModalComponent implements OnInit {
  @Input() isOpen: boolean = false;
  @Output() closed = new EventEmitter<boolean>(); // Emits true on successful registration

  registerForm!: FormGroup;
  isLoading: boolean = false;
  submitted: boolean = false;

  constructor(
    private fb: FormBuilder,
    private customerService: CustomerService,
    private userProfileService: UserProfileService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.registerForm = this.fb.group({
      // The API for /api/customer/register/account expects 'phone' and 'address'.
      // It implicitly links to the currently authenticated user by the backend.
      phone: ['', Validators.required],
      address: ['', Validators.required] // This might be a simplified address string
                                         // or you might need a nested address form group
                                         // if the API expects a complex address object.
                                         // For simplicity, using a string for now.
    });
  }

  get f() { return this.registerForm.controls; }

  onSubmit(): void {
    this.submitted = true;
    if (this.registerForm.invalid) {
      return;
    }

    const currentUserId = this.userProfileService.getCurrentUserDetails()?.id;
    if (!currentUserId) {
        this.toastService.showError("Cannot register customer account: User not identified.");
        return;
    }

    this.isLoading = true;
    const payload: CustomerRegisterParametersDto = {
      // The DTO in Swagger might not explicitly ask for userId if it's derived from auth token.
      // However, if it does, or if your DTO is different:
      // userId: currentUserId,
      phone: this.f['phone'].value,
      address: this.f['address'].value
    };

    this.customerService.registerAccount(payload).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: () => {
        this.closeModal(true); // Signal success
      },
      error: (err) => {
        // Error handled by interceptor
      }
    });
  }

  closeModal(success: boolean = false): void {
    this.isOpen = false;
    this.closed.emit(success);
  }
}