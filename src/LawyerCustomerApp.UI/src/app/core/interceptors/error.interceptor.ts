import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ToastService } from '../services/toast.service';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {

  constructor(private toastService: ToastService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status !== 401) { // [401 is handled by AuthInterceptor]
          let errorMessage = 'An unknown error occurred!';
          if (error.error instanceof ErrorEvent) {
            // C[lient-side error]
            errorMessage = `Error: ${error.error.message}`;
          } else {
            // [Server-side error]
            errorMessage = `Error Code: ${error.status}\nMessage: ${error.message}`;
            if (error.error && typeof error.error === 'string') {
                 errorMessage = error.error;
            } else if (error.error && error.error.title) {
                 errorMessage = error.error.title;
            } else if (error.error && error.error.message) {
                 errorMessage = error.error.message;
            } else if (error.message) {
                 errorMessage = error.message;
            }
          }
          this.toastService.showError(errorMessage);
        }
        return throwError(() => error);
      })
    );
  }
}