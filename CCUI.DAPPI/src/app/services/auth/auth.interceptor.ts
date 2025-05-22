import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse,
} from '@angular/common/http';
import { Observable, catchError, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Store } from '@ngrx/store';
import { first, switchMap } from 'rxjs/operators';
import { selectToken } from '../../state/auth/auth.selectors';
import { logout } from '../../state/auth/auth.actions';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(
    private store: Store,
    private router: Router,
    private snackBar: MatSnackBar
  ) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return this.store.select(selectToken).pipe(
      first(),
      switchMap((token) => {
        if (token) {
          request = request.clone({
            setHeaders: {
              Authorization: `Bearer ${token}`,
            },
          });
        }

        return next.handle(request).pipe(
          catchError((error: HttpErrorResponse) => {
            if (error.status === 401) {
              this.store.dispatch(logout());
              this.snackBar.open('Your session has expired. Please log in again.', 'Close', {
                duration: 5000,
              });
            }

            if (error.status === 403) {
              this.snackBar.open(
                'Oops! It looks like you don’t have permission to do that. Reach out to your administrator if you need access.',
                'Close',
                {
                  duration: 5000,
                }
              );
            }
            return throwError(() => error);
          })
        );
      })
    );
  }
}
