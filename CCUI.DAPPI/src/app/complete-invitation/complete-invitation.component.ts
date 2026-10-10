import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, ValidatorFn, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { UsersManagementService } from '../services/auth/users-management.service';
import { UsersAndPermissionsPluginService } from '../services/auth/users-and-permissions-plugin.service';

export function passwordMatchValidator(): ValidatorFn {
  return (control: AbstractControl): { [key: string]: any } | null => {
    const newPassword = control.get('newPassword');
    const confirmNewPassword = control.get('confirmNewPassword');

    if (!newPassword || !confirmNewPassword) {
      return null;
    }

    return newPassword.value === confirmNewPassword.value ? null : { passwordMismatch: true };
  };
}

@Component({
  selector: 'app-complete-invitation',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './complete-invitation.component.html',
  styleUrl: './complete-invitation.component.scss',
})
export class CompleteInvitationComponent implements OnInit {
  form: FormGroup;
  token = '';
  flow = '';
  isSubmitting = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private route: ActivatedRoute,
    private router: Router,
    private usersManagementService: UsersManagementService,
    private usersAndPermissionsPluginService: UsersAndPermissionsPluginService
  ) {
    this.form = this.fb.group(
      {
        oldPassword: ['', Validators.required],
        newPassword: ['', [Validators.required, Validators.minLength(8)]],
        confirmNewPassword: ['', Validators.required],
      },
      { validators: passwordMatchValidator() }
    );
  }

  ngOnInit(): void {
    this.token = this.route.snapshot.queryParamMap.get('token') ?? '';
    this.flow = (this.route.snapshot.queryParamMap.get('flow') ?? '').toLowerCase();

    if (!this.token) {
      this.errorMessage = 'Invitation token is missing.';
    }
  }

  onSubmit(): void {
    if (!this.token) {
      this.errorMessage = 'Invitation token is missing.';
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    const oldPassword = this.form.get('oldPassword')?.value;
    const newPassword = this.form.get('newPassword')?.value;

    const completeInvitation$ = this.flow === 'usersandpermissions'
      ? this.usersAndPermissionsPluginService.completeInvitation({ token: this.token, oldPassword, newPassword })
      : this.usersManagementService.completeInvitation({ token: this.token, oldPassword, newPassword });

    completeInvitation$.subscribe({
      next: () => {
        this.isSubmitting = false;
        this.router.navigate(['/auth'], { queryParams: { invitationCompleted: 'true' } });
      },
      error: (error) => {
        this.isSubmitting = false;
        this.errorMessage =
          error?.error?.message || error?.error?.title || error?.message || 'Failed to complete invitation.';
      },
    });
  }
}
