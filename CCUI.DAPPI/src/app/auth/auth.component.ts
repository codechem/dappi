import { Component, OnInit } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
  AbstractControl,
  ValidatorFn,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { finalize } from 'rxjs';
import { AuthService } from '../services/auth/auth.service';

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
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatSnackBarModule,
  ],
  templateUrl: './auth.component.html',
  styleUrl: './auth.component.scss',
})
export class AuthComponent implements OnInit {
  authForm!: FormGroup;
  isLoginMode = true;
  isLoading = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private router: Router,
    private authService: AuthService,
    private snackBar: MatSnackBar,
  ) {}

  ngOnInit(): void {
    this.initializeForm();
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
    this.errorMessage = '';
  }

  toggleAuthMode(): void {
    this.isLoginMode = !this.isLoginMode;
    this.initializeForm();
  }

  onSubmit(): void {
    if (this.authForm.valid) {
      this.isLoading = true;
      this.errorMessage = '';

      if (this.isLoginMode) {
        const { username, password } = this.authForm.value;

        this.authService
          .login(username, password)
          .pipe(finalize(() => (this.isLoading = false)))
          .subscribe({
            next: () => {
              this.snackBar.open('Login successful!', 'Close', { duration: 3000 });
              setTimeout(() => {
                this.router.navigate(['/home']);
              }, 100);
            },
            error: (error: Error) => {
              this.errorMessage = error.message;
              this.snackBar.open(error.message, 'Close', { duration: 5000 });
            },
          });
      } else {
        const { username, email, password } = this.authForm.value;

        this.authService
          .register(username, email, password)
          .pipe(finalize(() => (this.isLoading = false)))
          .subscribe({
            next: () => {
              this.snackBar.open('Registration successful! Please login.', 'Close', {
                duration: 3000,
              });
              this.isLoginMode = true;
              this.initializeForm();
            },
            error: (error: Error) => {
              this.errorMessage = error.message;
              this.snackBar.open(error.message, 'Close', { duration: 5000 });
            },
          });
      }
    }
  }
}
