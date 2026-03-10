import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { Subscription } from 'rxjs';
import { AddRoleDialogComponent } from '../../add-role-dialog/add-role-dialog.component';
import { RolesManagementService, RoleItem } from '../../services/auth/roles-management.service';

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [
    CommonModule,
    MatIconModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    MatTableModule,
  ],
  templateUrl: './roles.component.html',
  styleUrl: './roles.component.scss',
})
export class RolesComponent implements OnInit, OnDestroy {
  private subscription = new Subscription();
  displayedColumns: string[] = ['name', 'users', 'actions'];

  roles: RoleItem[] = [];
  rolesLoading = false;
  rolesError = '';
  deletingRoleId: string | null = null;

  constructor(
    private rolesManagementService: RolesManagementService,
    private dialog: MatDialog,
  ) {}

  ngOnInit(): void {
    this.loadRoles();
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  addRole(): void {
    const dialogRef = this.dialog.open(AddRoleDialogComponent);

    dialogRef.afterClosed().subscribe((roleName: string | null) => {
      if (!roleName) return;

      this.rolesLoading = true;
      this.rolesError = '';

      this.subscription.add(
        this.rolesManagementService.createRole(roleName).subscribe({
          next: () => {
            this.loadRoles();
          },
          error: (error) => {
            const apiMessage = error?.error?.message || error?.error?.title;
            this.rolesError = apiMessage || 'Failed to create role.';
            this.rolesLoading = false;
          },
        })
      );
    });
  }

  deleteRole(role: RoleItem): void {
    if (!confirm(`Are you sure you want to delete the role "${role.Name}"?`)) {
      return;
    }

    this.deletingRoleId = role.Id;
    this.rolesError = '';

    this.subscription.add(
      this.rolesManagementService.deleteRole(role.Id).subscribe({
        next: () => {
          this.deletingRoleId = null;
          this.loadRoles();
        },
        error: (error) => {
          const apiMessage = error?.error?.message || error?.error?.title;
          this.rolesError = apiMessage || 'Failed to delete role.';
          this.deletingRoleId = null;
        },
      })
    );
  }

  private loadRoles(): void {
    this.rolesLoading = true;
    this.rolesError = '';

    this.subscription.add(
      this.rolesManagementService.getRoles().subscribe({
        next: (response) => {
          this.roles = response.Data;
          this.rolesLoading = false;
        },
        error: (error) => {
          const apiMessage = error?.error?.message || error?.error?.title;
          this.rolesError = apiMessage || 'Failed to load roles.';
          this.rolesLoading = false;
        },
      })
    );
  }
}
