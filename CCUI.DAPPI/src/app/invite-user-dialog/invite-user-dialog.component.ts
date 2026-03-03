import { Component, OnInit } from '@angular/core';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RolesManagementService, RoleItem } from '../services/auth/roles-management.service';

export interface InviteUserData {
  username: string;
  email: string;
  password: string;
  roles: string[];
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
  ) {}

  ngOnInit(): void {
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

  get isValid(): boolean {
    return !!this.username.trim() && !!this.email.trim() && !!this.password.trim();
  }

  onConfirm(): void {
    if (!this.isValid) return;
    this.dialogRef.close({
      username: this.username.trim(),
      email: this.email.trim(),
      password: this.password,
      roles: this.selectedRoles,
    } as InviteUserData);
  }

  onCancel(): void {
    this.dialogRef.close(null);
  }
}
