import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-lca-loading-spinner',
  templateUrl: './lca-loading-spinner.component.html',
  styleUrls: ['./lca-loading-spinner.component.css']
})
export class LcaLoadingSpinnerComponent {
  @Input() isLoading: boolean = false;
  @Input() size: 'sm' | 'md' | 'lg' = 'md'; // small, medium, large
  @Input() overlay: boolean = false; // If true, shows a full-page overlay
  @Input() message: string = ''; // Optional message below spinner
}