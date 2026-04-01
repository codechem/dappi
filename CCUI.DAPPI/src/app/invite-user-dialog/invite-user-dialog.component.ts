import { Component, Inject, OnInit } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RolesManagementService, RoleItem } from '../services/auth/roles-management.service';
import { UsersAndPermissionsPluginService } from '../services/auth/users-and-permissions-plugin.service';

export interface InviteUserData {
  username: string;
  email: string;
  password?: string;
  roles: string[];
}

export interface InviteUserDialogConfig {
  isEmailServiceAvailable: boolean;
  usePluginRoles?: boolean;
}

@Component({
  selector: 'app-invite-user-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSelectModule,
    FormsModule,
  ],
  templateUrl: './invite-user-dialog.component.html',
  styleUrl: './invite-user-dialog.component.scss',
})
export class InviteUserDialogComponent implements OnInit {
  username = '';
  email = '';
  password = '';
  selectedRoles: string[] = [];
  availableRoles: RoleItem[] = [];
  rolesLoading = false;

  constructor(
    private dialogRef: MatDialogRef<InviteUserDialogComponent>,
    private rolesManagementService: RolesManagementService,
    private usersAndPermissionsPluginService: UsersAndPermissionsPluginService,
    @Inject(MAT_DIALOG_DATA) public data: InviteUserDialogConfig,
  ) {}

  ngOnInit(): void {
    if (this.data.usePluginRoles) {
      this.loadPluginRoles();
      return;
    }

    this.loadRegularRoles();
  }

  get isValid(): boolean {
    return !!this.username.trim() && !!this.email.trim() && (this.data.isEmailServiceAvailable || !!this.password);
  }

  get isCreateMode(): boolean {
    return !this.data.isEmailServiceAvailable;
  }

  onConfirm(): void {
    if (!this.isValid) return;
    this.dialogRef.close({
      username: this.username.trim(),
      email: this.email.trim(),
      password: this.isCreateMode ? this.password : undefined,
      roles: this.selectedRoles,
    } as InviteUserData);
  }

  onCancel(): void {
    this.dialogRef.close(null);
  }

  private loadRegularRoles(): void {
    this.rolesLoading = true;
    this.rolesManagementService.getRoles().subscribe({
      next: (res) => {
        this.availableRoles = res.Data;
        this.rolesLoading = false;
      },
      error: () => {
        this.rolesLoading = false;
      },
    });
  }

  private loadPluginRoles(): void {
    this.rolesLoading = true;
    this.usersAndPermissionsPluginService.getAllRoles().subscribe({
      next: (roles) => {
        this.availableRoles = roles.map((role) => ({
          Id: role.id,
          Name: role.name,
          UserCount: 0,
        }));
        this.rolesLoading = false;
      },
      error: () => {
        this.rolesLoading = false;
      },
    });
  }
}
