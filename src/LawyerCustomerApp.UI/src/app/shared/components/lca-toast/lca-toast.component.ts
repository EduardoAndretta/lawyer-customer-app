import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { ToastMessage } from '../../../core/services/toast.service'; // Import the interface

@Component({
  selector: 'app-lca-toast',
  templateUrl: './lca-toast.component.html',
  styleUrls: ['./lca-toast.component.css']
})
export class LcaToastComponent implements OnInit, OnDestroy {
  @Input() toast!: ToastMessage; // Expect a ToastMessage object
  @Output() dismissed = new EventEmitter<number>(); // Emits the ID of the toast to dismiss

  private timer: any;

  ngOnInit(): void {
    if (this.toast && this.toast.duration && this.toast.duration > 0) {
      this.timer = setTimeout(() => {
        this.dismiss();
      }, this.toast.duration);
    }
  }

  dismiss(): void {
    clearTimeout(this.timer);
    this.dismissed.emit(this.toast.id);
  }

  ngOnDestroy(): void {
    clearTimeout(this.timer);
  }

  get toastClasses(): object {
    return {
      'lca-toast': true,
      [`lca-toast-${this.toast.type}`]: true,
    };
  }
}