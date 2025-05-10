import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';

import { LcaButtonComponent } from './components/lca-button/lca-button.component';
import { LcaInputComponent } from './components/lca-input/lca-input.component';
import { LcaSelectComponent } from './components/lca-select/lca-select.component';
import { LcaAutoCompleteComponent } from './components/lca-auto-complete/lca-auto-complete.component';
import { LcaTableComponent } from './components/lca-table/lca-table.component';
import { LcaModalComponent } from './components/lca-modal/lca-modal.component';
import { LcaToastComponent } from './components/lca-toast/lca-toast.component';
import { LcaToastContainerComponent } from './components/lca-toast-container/lca-toast-container.component';
import { LcaPaginationComponent } from './components/lca-pagination/lca-pagination.component';
import { LcaLoadingSpinnerComponent } from './components/lca-loading-spinner/lca-loading-spinner.component';
import { LcaPermissionsListComponent } from './components/lca-permissions-list/lca-permissions-list.component';

// Example Pipe (if needed)
// import { LcaSafeHtmlPipe } from './pipes/lca-safe-html.pipe.ts';

// Example Directive (if needed)
// import { LcaClickOutsideDirective } from './directives/lca-click-outside.directive.ts';

const COMPONENTS = [
  LcaButtonComponent,
  LcaInputComponent,
  LcaSelectComponent,
  LcaAutoCompleteComponent,
  LcaTableComponent,
  LcaModalComponent,
  LcaToastComponent,
  LcaToastContainerComponent,
  LcaPaginationComponent,
  LcaLoadingSpinnerComponent,
  LcaPermissionsListComponent
];

// const PIPES = [LcaSafeHtmlPipe];
// const DIRECTIVES = [LcaClickOutsideDirective];

@NgModule({
  declarations: [
    ...COMPONENTS,
    // ...PIPES,
    // ...DIRECTIVES
  ],
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule
  ],
  exports: [
    ...COMPONENTS,
    // ...PIPES,
    // ...DIRECTIVES,
    FormsModule, // Re-export FormsModule and ReactiveFormsModule if used by feature modules directly
    ReactiveFormsModule
  ]
})
export class SharedModule { }