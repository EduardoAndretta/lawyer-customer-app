import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-lca-button',
  templateUrl: './lca-button.component.html',
  styleUrls: ['./lca-button.component.css']
})
export class LcaButtonComponent {
  @Input() type: 'button' | 'submit' = 'button';
  @Input() lcaStyle: 'primary' | 'secondary' | 'danger' | 'success' | 'warning' | 'info' = 'primary';
  @Input() disabled: boolean = false;
  @Input() fullWidth: boolean = false;
  @Input() isLoading: boolean = false; // For showing a loading state

  @Output() lcaClick = new EventEmitter<MouseEvent>();

  onClick(event: MouseEvent): void {
    if (!this.disabled && !this.isLoading) {
      this.lcaClick.emit(event);
    }
  }
}