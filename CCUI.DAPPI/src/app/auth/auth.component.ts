import { Component, OnDestroy, OnInit } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
  AbstractControl,
  ValidatorFn,
} from '@angular/forms';
import { CommonModule } from '@angular/common';
import { Store } from '@ngrx/store';
import { Observable, Subscription } from 'rxjs';
import { selectAuthLoading, selectAuthError } from '../state/auth/auth.selectors';
import * as AuthActions from '../state/auth/auth.actions';
import { Actions, ofType } from '@ngrx/effects';

export function passwordMatchValidator(): ValidatorFn {
  return (control: AbstractControl): { [key: string]: any } | null => {
    const password = control.get('password');
    const confirmPassword = control.get('confirmPassword');

    if (!password || !confirmPassword) {
      return null;
    }

    return password.value === confirmPassword.value ? null : { passwordMismatch: true };
  };
}

@Component({
  selector: 'app-auth',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './auth.component.html',
  styleUrl: './auth.component.scss',
})
export class AuthComponent implements OnInit, OnDestroy {
  authForm!: FormGroup;
  isLoginMode = true;
  isLoading$: Observable<boolean>;
  errorMessage$: Observable<string | null>;
  private registerSuccessSub?: Subscription;

  constructor(
    private fb: FormBuilder,
    private store: Store,
    private actions$: Actions,
  ) {
    this.isLoading$ = this.store.select(selectAuthLoading);
    this.errorMessage$ = this.store.select(selectAuthError);
  }

  ngOnInit(): void {
    this.initializeForm();
    this.registerSuccessSub = this.actions$
      .pipe(ofType(AuthActions.registerSuccess))
      .subscribe(() => {
        this.isLoginMode = true;
      });
  }

  ngOnDestroy() {
    if (this.registerSuccessSub) {
      this.registerSuccessSub.unsubscribe();
    }
  }

  initializeForm(): void {
    if (this.isLoginMode) {
      this.authForm = this.fb.group({
        username: ['', Validators.required],
        password: ['', Validators.required],
        termsAccepted: [false, Validators.requiredTrue],
      });
    } else {
      this.authForm = this.fb.group(
        {
          username: ['', Validators.required],
          email: ['', [Validators.required, Validators.email]],
          password: ['', [Validators.required, Validators.minLength(8)]],
          confirmPassword: ['', Validators.required],
          termsAccepted: [false, Validators.requiredTrue],
        },
        { validators: passwordMatchValidator() },
      );
    }
  }

  toggleAuthMode(): void {
    this.isLoginMode = !this.isLoginMode;
    this.initializeForm();
  }

  onSubmit(): void {
    if (this.authForm.valid) {
      if (this.isLoginMode) {
        const { username, password } = this.authForm.value;
        this.store.dispatch(AuthActions.login({ username, password }));
      } else {
        const { username, email, password } = this.authForm.value;
        this.store.dispatch(AuthActions.register({ username, email, password }));
      }
    }
  }
}
