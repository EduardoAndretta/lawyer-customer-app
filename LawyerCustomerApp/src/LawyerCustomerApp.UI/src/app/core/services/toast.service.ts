import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface ToastMessage {
  id: number;
  message: string;
  type: 'success' | 'error' | 'info';
  duration?: number;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private toastsSubject = new BehaviorSubject<ToastMessage[]>([]);
  toasts$: Observable<ToastMessage[]> = this.toastsSubject.asObservable();
  private toastIdCounter = 0;

  constructor() { }

  private addToast(message: string, type: 'success' | 'error' | 'info', duration: number = 5000): void {
    const id = this.toastIdCounter++;
    const newToast: ToastMessage = { id, message, type, duration };
    const currentToasts = this.toastsSubject.getValue();
    this.toastsSubject.next([...currentToasts, newToast]);

    if (duration > 0) {
        setTimeout(() => this.removeToast(id), duration);
    }
  }

  showSuccess(message: string, duration: number = 3000): void {
    this.addToast(message, 'success', duration);
  }

  showError(message: string, duration: number = 7000): void {
    this.addToast(message, 'error', duration);
  }

  showInfo(message: string, duration: number = 5000): void {
    this.addToast(message, 'info', duration);
  }

  removeToast(id: number): void {
    const currentToasts = this.toastsSubject.getValue();
    this.toastsSubject.next(currentToasts.filter(toast => toast.id !== id));
  }
}