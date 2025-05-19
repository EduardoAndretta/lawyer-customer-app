import { Component, OnInit, OnDestroy, Input } from '@angular/core';
import { Subscription } from 'rxjs';
import { ToastService, ToastMessage } from '../../../core/services/toast.service';

@Component({
  selector: 'app-lca-toast-container',
  templateUrl: './lca-toast-container.component.html',
  styleUrls: ['./lca-toast-container.component.css']
})
export class LcaToastContainerComponent implements OnInit, OnDestroy {
  toasts: ToastMessage[] = [];
  private toastSubscription!: Subscription;

  // Positions: 'top-right', 'top-left', 'bottom-right', 'bottom-left', 'top-center', 'bottom-center'
  @Input() position: string = 'top-right';

  constructor(private toastService: ToastService) {}

  trackByToastId(index: number, toast: ToastMessage): number {
    return toast.id;
  }
  
  ngOnInit(): void {
    this.toastSubscription = this.toastService.toasts$.subscribe(toasts => {
      this.toasts = toasts;
    });
  }

  onToastDismissed(id: number): void {
    this.toastService.removeToast(id);
  }

  getContainerClasses(): object {
    return {
      'lca-toast-container': true,
      [`lca-toast-container-${this.position}`]: true,
    };
  }

  ngOnDestroy(): void {
    if (this.toastSubscription) {
      this.toastSubscription.unsubscribe();
    }
  }
}