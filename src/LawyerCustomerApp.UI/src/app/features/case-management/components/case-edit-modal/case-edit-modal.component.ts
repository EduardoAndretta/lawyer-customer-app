import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { CaseService } from '../../services/case.service';
import { CaseDetailsInformationItem, CaseEditParametersDto } from '../../../../core/models/case.models';
import { ToastService } from '../../../../core/services/toast.service';
import { finalize } from 'rxjs/operators';

@Component({
  selector: 'app-case-edit-modal',
  templateUrl: './case-edit-modal.component.html',
  styleUrls: ['./case-edit-modal.component.css']
})
export class CaseEditModalComponent implements OnInit {
  @Input() isOpen: boolean = false;
  @Input() caseData!: CaseDetailsInformationItem; // Assuming this has ID and current values

  @Output() closed = new EventEmitter<boolean>(); // Emits true if data was changed and needs refresh

  editForm!: FormGroup;
  isLoading: boolean = false;
  submitted: boolean = false;

  constructor(
    private fb: FormBuilder,
    private caseService: CaseService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    if (!this.caseData) {
      this.toastService.showError("Case data is missing for edit modal.");
      this.closeModal(false);
      return;
    }
    this.editForm = this.fb.group({
      title: [this.caseData.title || '', Validators.required],
      description: [this.caseData.description || '', Validators.required],
      // Add other editable fields from your Swagger's example for /api/case/edit
      // For example:
      // status: [this.caseData.status || '', Validators.required],
      // private: [this.caseData.private || false]
    });
  }

  get f() { return this.editForm.controls; }

  onSubmit(): void {
    this.submitted = true;
    if (this.editForm.invalid) {
      return;
    }

    this.isLoading = true;
    const formValues = this.editForm.value;

    const payload: CaseEditParametersDto = {
      relatedCaseId: this.caseData.id,
      values: { // Structure this according to your API's "values" object for edit
        title: formValues.title,
        description: formValues.description,
        // status: formValues.status,
        // private: formValues.private
      }
    };

    this.caseService.edit(payload).pipe(
      finalize(() => this.isLoading = false)
    ).subscribe({
      next: () => {
        this.closeModal(true); // Signal that data changed
      },
      error: (err) => {
        // Error handled by interceptor
      }
    });
  }

  closeModal(dataChanged: boolean = false): void {
    this.isOpen = false; // To ensure lca-modal reacts if parent doesn't re-render immediately
    this.closed.emit(dataChanged);
  }
}