import { Component, Input, Output, EventEmitter, ElementRef, Renderer2, OnInit, OnDestroy } from '@angular/core';

@Component({
  selector: 'app-lca-modal',
  templateUrl: './lca-modal.component.html',
  styleUrls: ['./lca-modal.component.css']
})
export class LcaModalComponent implements OnInit, OnDestroy {
  @Input() title: string = '';
  _isOpen: boolean = false;
  @Input()
  get isOpen(): boolean {
    return this._isOpen;
  }
  set isOpen(value: boolean) {
    this._isOpen = value;
    if (this._isOpen) {
      this.renderer.addClass(document.body, 'lca-modal-open');
    } else {
      this.renderer.removeClass(document.body, 'lca-modal-open');
    }
  }

  @Input() closeOnBackdropClick: boolean = true;
  @Input() closeOnEscape: boolean = true;
  @Input() size: 'sm' | 'md' | 'lg' | 'xl' = 'md'; // Modal size
  @Input() hideHeader: boolean = false;
  @Input() hideFooter: boolean = false;

  @Output() opened = new EventEmitter<void>();
  @Output() closed = new EventEmitter<any>(); // Can emit data on close
  @Output() lcaSubmit = new EventEmitter<any>(); // For a default submit action

  private escapeListener!: () => void;

  constructor(private el: ElementRef, private renderer: Renderer2) {}

  ngOnInit(): void {
    if (this.closeOnEscape) {
      this.escapeListener = this.renderer.listen('document', 'keydown.escape', () => {
        if (this.isOpen) {
          this.closeModal();
        }
      });
    }
  }

  openModal(): void {
    this.isOpen = true;
    this.opened.emit();
  }

  closeModal(data?: any): void {
    this.isOpen = false;
    this.closed.emit(data);
  }

  onBackdropClick(event: MouseEvent): void {
    if (this.closeOnBackdropClick && event.target === this.el.nativeElement.querySelector('.lca-modal-overlay')) {
      this.closeModal();
    }
  }

  // Can be called by a button in the projected footer
  submitModal(data?: any): void {
      this.lcaSubmit.emit(data);
      // Optionally close modal after submit, or let the parent component decide
      // this.closeModal(data);
  }

  ngOnDestroy(): void {
    if (this.escapeListener) {
      this.escapeListener();
    }
    // Ensure body class is removed if component is destroyed while open
    this.renderer.removeClass(document.body, 'lca-modal-open');
  }
}