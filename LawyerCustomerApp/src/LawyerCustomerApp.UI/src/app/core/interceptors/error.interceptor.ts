import { Injectable } from '@angular/core';
import {
  HttpRequest, HttpHandler, HttpEvent, HttpInterceptor, HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ToastService } from '../services/toast.service';

interface ApiErrorProperties {
  statusNumber: number;
  title: string;
  message: string;
  warnings?: ApiWarningResponse[];
  details?: any;
}

interface ApiErrorResponse {
  status: string;
  error: ApiErrorProperties;
}

interface ApiWarningResponse {
    type: string;
    data: { title: string; message: string; details?: any; };
}


@Injectable()
export class ErrorInterceptor implements HttpInterceptor {

  constructor(private toastService: ToastService) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status !== 401) { // [401 is handled by AuthInterceptor]
          let displayMessage = 'An unexpected error occurred. Please try again.';
          let errorTitle = 'Error';

          if (error.error && typeof error.error === 'object' && error.error.error && error.error.error.message) {

            const apiError = error.error as ApiErrorResponse;
            errorTitle = apiError.error.title || 'Error';
            displayMessage = apiError.error.message;

            // [Optionally display warnings from the error response]
            if (apiError.error.warnings && apiError.error.warnings.length > 0) {
              apiError.error.warnings.forEach(w => {
                this.toastService.showInfo(`Warning: ${w.data.title} - ${w.data.message}`, 10000);
              });
            }

            if(apiError.error.details){
                console.error("Error Details:", apiError.error.details);
            }

          } else if (error.error instanceof ErrorEvent) {

            errorTitle = 'Network/Client Error';
            displayMessage = error.error.message;
          } else if (typeof error.error === 'string') {

            displayMessage = error.error;
          } else if (error.message) {
            displayMessage = error.message;
          }

          this.toastService.showError(`${errorTitle}: ${displayMessage}`, 7000);
        }
        return throwError(() => error); // [Rethrow the original error]
      })
    );
  }
}