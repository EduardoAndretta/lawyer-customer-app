import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms'; // Add FormArray if accounts can be multiple
import { UserService } from '../../services/user.service';
import { UserDetailsInformationItem, UserEditParametersDto, UserEditValues } from '../../../../core/models/user.models';
import { ToastService } from '../../../../core/services/toast.service';
import { finalize } from 'rxjs/operators';

@Component({
  selector: 'app-user-edit-modal',
  templateUrl: './user-edit-modal.component.html',
  styleUrls: ['./user-edit-modal.component.css']
})
export class UserEditModalComponent implements OnInit {
  @Input() isOpen: boolean = false;
  @Input() userData!: UserDetailsInformationItem; // Contains ID and current values (might need more from UserDetails DTO)

  @Output() closed = new EventEmitter<boolean>(); // Emits true if data changed

  editForm!: FormGroup;
  isLoading: boolean = false;
  submitted: boolean = false;

  // To reflect what the Swagger example shows, we'll need to fetch more detailed user data
  // than what UserDetailsInformationItem might currently hold, or assume userData has these details.
  // For simplicity, we'll assume userData is populated enough, or you'd fetch extended details.

  constructor(
    private fb: FormBuilder,
    private userService: UserService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    if (!this.userData) {
      this.toastService.showError("User data is missing for edit modal.");
      this.closeModal(false);
      return;
    }

    // This form structure needs to closely match the `UserEditValues` interface
    // and the expected payload for the API.
    this.editForm = this.fb.group({
      // General User Info (from UserEditValues example)
      private: [this.userData.private || false], // Assuming an 'private' field on UserDetailsInformationItem

      // Address (nested FormGroup)
      address: this.fb.group({
        zipCode: [this.userData.address?.zipCode || ''],
        houseNumber: [this.userData.address?.houseNumber || ''],
        complement: [this.userData.address?.complement || ''],
        district: [this.userData.address?.district || ''],
        city: [this.userData.address?.city || ''],
        state: [this.userData.address?.state || ''],
        country: [this.userData.address?.country || '']
      }),

      // Document (nested FormGroup)
      document: this.fb.group({
        type: [this.userData.document?.type || ''],
        identifierDocument: [this.userData.document?.identifierDocument || '']
      }),

      // Accounts (nested FormGroup for lawyer and customer)
      // This part is highly dependent on whether these are always present or optional.
      // The example shows them as potentially editable sub-objects.
      accounts: this.fb.group({
        lawyer: this.fb.group({ // Only if editing lawyer-specific account details
          phone: [this.userData.accounts?.lawyer?.phone || ''],
          private: [this.userData.accounts?.lawyer?.private || false],
          // Nested address and document for lawyer account
          address: this.fb.group({
            zipCode: [this.userData.accounts?.lawyer?.address?.zipCode || ''],
            // ... other lawyer address fields
             houseNumber: [this.userData.accounts?.lawyer?.address?.houseNumber || ''],
             complement: [this.userData.accounts?.lawyer?.address?.complement || ''],
             district: [this.userData.accounts?.lawyer?.address?.district || ''],
             city: [this.userData.accounts?.lawyer?.address?.city || ''],
             state: [this.userData.accounts?.lawyer?.address?.state || ''],
             country: [this.userData.accounts?.lawyer?.address?.country || '']
          }),
          document: this.fb.group({
            type: [this.userData.accounts?.lawyer?.document?.type || ''],
            identifierDocument: [this.userData.accounts?.lawyer?.document?.identifierDocument || '']
          })
        }),
        customer: this.fb.group({ // Only if editing customer-specific account details
          phone: [this.userData.accounts?.customer?.phone || ''],
          private: [this.userData.accounts?.customer?.private || false],
          // Nested address and document for customer account
          address: this.fb.group({
            zipCode: [this.userData.accounts?.customer?.address?.zipCode || ''],
            // ... other customer address fields
             houseNumber: [this.userData.accounts?.customer?.address?.houseNumber || ''],
             complement: [this.userData.accounts?.customer?.address?.complement || ''],
             district: [this.userData.accounts?.customer?.address?.district || ''],
             city: [this.userData.accounts?.customer?.address?.city || ''],
             state: [this.userData.accounts?.customer?.address?.state || ''],
             country: [this.userData.accounts?.customer?.address?.country || '']
          }),
          document: this.fb.group({
            type: [this.userData.accounts?.customer?.document?.type || ''],
            identifierDocument: [this.userData.accounts?.customer?.document?.identifierDocument || '']
          })
        })
      })
    });

    // Conditionally disable lawyer/customer account sections if the user doesn't have them
    // This assumes `hasLawyerAccount` and `hasCustomerAccount` are available on `this.userData`
    if (!this.userData.hasLawyerAccount) {
      this.editForm.get('accounts.lawyer')?.disable(); 
    }
    else 
    {
      if (!this.userData?.accounts?.lawyer?.hasAddress) {
        this.editForm.get('accounts.lawyer.address')?.disable();
      }
      if (!this.userData?.accounts?.lawyer?.hasDocument) {
        this.editForm.get('accounts.lawyer.document')?.disable();
      }
    }

    if (!this.userData.hasCustomerAccount) {
      this.editForm.get('accounts.customer')?.disable(); 
    }
    else 
    {
      if (!this.userData?.accounts?.customer?.hasAddress) {
        this.editForm.get('accounts.customer.address')?.disable();
      }
      if (!this.userData?.accounts?.customer?.hasDocument) {
        this.editForm.get('accounts.customer.document')?.disable();
      }
    }

    if (!this.userData.hasAddress) {
        this.editForm.get('address')?.disable();
    }
    if (!this.userData.hasCustomerAccount) {
        this.editForm.get('document')?.disable();
    }
  }


  // Helper to access nested form groups easily in the template
  get addressForm(): FormGroup { return this.editForm.get('address') as FormGroup; }
  get documentForm(): FormGroup { return this.editForm.get('document') as FormGroup; }
  get accountsForm(): FormGroup { return this.editForm.get('accounts') as FormGroup; }
  get lawyerAccountForm(): FormGroup { return this.accountsForm.get('lawyer') as FormGroup; }
  get customerAccountForm(): FormGroup { return this.accountsForm.get('customer') as FormGroup; }

  get lawyerAddressForm(): FormGroup { return this.lawyerAccountForm.get('address') as FormGroup; }
  get lawyerDocumentForm(): FormGroup { return this.lawyerAccountForm.get('document') as FormGroup; }
  get customerAddressForm(): FormGroup { return this.customerAccountForm.get('address') as FormGroup; }
  get customerDocumentForm(): FormGroup { return this.customerAccountForm.get('document') as FormGroup; }


  onSubmit(): void {
    this.submitted = true;
    if (this.editForm.invalid) {
      this.toastService.showError("Please correct the errors in the form.");
      return;
    }

    this.isLoading = true;
    // Construct the 'values' payload carefully based on the form and API expectations.
    // Only include sections that are actually editable and have changed.
    // The API might handle partial updates gracefully.
    const formValues = this.editForm.getRawValue(); // Use getRawValue to include disabled controls if needed by backend logic

    const payloadValues: UserEditValues = {
      private: formValues.private,
      address: formValues.address,
      document: formValues.document,
      accounts: {
        lawyer: this.userData.hasLawyerAccount ? formValues.accounts.lawyer : undefined, // Only send if user has this account
        customer: this.userData.hasCustomerAccount ? formValues.accounts.customer : undefined, // Only send if user has this account
      }
    };

    // Remove undefined account sections if they were not applicable
    if (!payloadValues.accounts?.lawyer) delete payloadValues.accounts?.lawyer;
    if (!payloadValues.accounts?.customer) delete payloadValues.accounts?.customer;
    if (Object.keys(payloadValues.accounts || {}).length === 0) delete payloadValues.accounts;


    const payload: UserEditParametersDto = {
      relatedUserId: this.userData.id,
      values: payloadValues
    };

    this.userService.edit(payload).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: () => {
        this.closeModal(true);
      },
      error: (err) => {
        // Error handled by interceptor
      }
    });
  }

  closeModal(dataChanged: boolean = false): void {
    this.isOpen = false;
    this.closed.emit(dataChanged);
  }
}