import { Injectable, PLATFORM_ID, Inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { of } from 'rxjs';
import { catchError, map, tap, exhaustMap } from 'rxjs/operators';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { isPlatformBrowser } from '@angular/common';
import { AuthService } from '../../services/auth/auth.service';
import * as AuthActions from './auth.actions';

@Injectable()
export class AuthEffects {
  constructor(
    private actions$: Actions,
    private authService: AuthService,
    private router: Router,
    private snackBar: MatSnackBar,
    @Inject(PLATFORM_ID) private platformId: Object,
  ) {}

  login$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.login),
      exhaustMap(({ username, password }) =>
        this.authService.login(username, password).pipe(
          map((response) =>
            AuthActions.loginSuccess({
              user: { username: response.username, roles: response.roles },
              token: response.token,
            }),
          ),
          catchError((error) => of(AuthActions.loginFailure({ error: error.message }))),
        ),
      ),
    ),
  );

  loginSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(AuthActions.loginSuccess),
        tap(({ user, token }) => {
          if (isPlatformBrowser(this.platformId)) {
            localStorage.setItem('jwt_token', token);
            localStorage.setItem('user_data', JSON.stringify(user));

            this.snackBar.open('Login successful!', 'Close', { duration: 3000 });
            this.router.navigate(['/home']);
          }
        }),
      ),
    { dispatch: false },
  );

  register$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.register),
      exhaustMap(({ username, email, password }) =>
        this.authService.register(username, email, password).pipe(
          map(() =>
            AuthActions.registerSuccess({
              message: 'Registration successful! Please login.',
            }),
          ),
          catchError((error) => of(AuthActions.registerFailure({ error: error.message }))),
        ),
      ),
    ),
  );

  registerSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(AuthActions.registerSuccess),
        tap(({ message }) => {
          this.snackBar.open(message, 'Close', { duration: 3000 });
          this.router.navigate(['/auth']);
        }),
      ),
    { dispatch: false },
  );

  checkAuth$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.checkAuth),
      map(() => {
        if (isPlatformBrowser(this.platformId)) {
          const token = localStorage.getItem('jwt_token');
          const userData = localStorage.getItem('user_data');

          if (token && userData) {
            try {
              const user = JSON.parse(userData);
              return AuthActions.authStatusSuccess({ user, token });
            } catch (e) {
              return AuthActions.authStatusFailure();
            }
          }
        }
        return AuthActions.authStatusFailure();
      }),
    ),
  );

  logout$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AuthActions.logout),
      map(() => {
        if (isPlatformBrowser(this.platformId)) {
          localStorage.removeItem('jwt_token');
          localStorage.removeItem('user_data');
        }
        return AuthActions.logoutSuccess();
      }),
    ),
  );

  logoutSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(AuthActions.logoutSuccess),
        tap(() => {
          this.router.navigate(['/auth']);
        }),
      ),
    { dispatch: false },
  );
}
