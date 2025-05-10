import { Component, OnInit, OnDestroy } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { CaseService } from '../../services/case.service';
import { UserProfileService } from '../../../../core/services/user-profile.service';
import { ToastService } from '../../../../core/services/toast.service';
import { finalize, take, switchMap } from 'rxjs/operators';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-case-register-page',
  templateUrl: './case-register-page.component.html',
  styleUrls: ['./case-register-page.component.css']
})
export class CaseRegisterPageComponent implements OnInit, OnDestroy {
  registerForm!: FormGroup;
  isLoading: boolean = false;
  submitted: boolean = false;
  private attributeIdSubscription!: Subscription;
  private currentAttributeId: number | null = null;

  constructor(
    private fb: FormBuilder,
    private caseService: CaseService,
    private userProfileService: UserProfileService,
    private toastService: ToastService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.attributeIdSubscription = this.userProfileService.selectedAccountAttributeId$.subscribe(id => {
        this.currentAttributeId = id;
    });

    this.registerForm = this.fb.group({
      title: ['', Validators.required],
      description: ['', Validators.required]
      // attributeId will be added dynamically
    });
  }

  get f() { return this.registerForm.controls; }

  onSubmit(): void {
    this.submitted = true;
    if (this.registerForm.invalid) {
      return;
    }
    if (!this.currentAttributeId) {
        this.toastService.showError("User account context (Attribute ID) is not set. Cannot register case.");
        return;
    }

    this.isLoading = true;
    const formValue = this.registerForm.value;
    const payload = {
      ...formValue,
      attributeId: this.currentAttributeId
    };

    this.caseService.register(payload).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: () => {
        // On success, navigate to cases list or the new case's details page
        // For now, navigate to home. Ideally, API would return new case ID.
        this.router.navigate(['/dashboard/home']);
      },
      error: (err) => {
        // Error handled by interceptor
      }
    });
  }

  ngOnDestroy(): void {
      if (this.attributeIdSubscription) {
          this.attributeIdSubscription.unsubscribe();
      }
  }
}