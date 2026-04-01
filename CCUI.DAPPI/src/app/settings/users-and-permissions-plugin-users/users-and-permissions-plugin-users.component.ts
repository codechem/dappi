import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatCheckboxChange, MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { forkJoin, Subscription } from 'rxjs';
import {
  UsersAndPermissionsPluginService,
  UsersAndPermissionsUserItem,
} from '../../services/auth/users-and-permissions-plugin.service';
import { UsersManagementService } from '../../services/auth/users-management.service';
import { MatDialog } from '@angular/material/dialog';
import { InviteUserData, InviteUserDialogComponent } from '../../invite-user-dialog/invite-user-dialog.component';

@Component({
  selector: 'app-users-and-permissions-users',
  standalone: true,
  imports: [MatProgressSpinnerModule, MatTableModule, MatIconModule, MatCheckboxModule],
  templateUrl: './users-and-permissions-plugin-users.component.html',
  styleUrl: './users-and-permissions-plugin-users.component.scss',
})
export class UsersAndPermissionsUsersComponent implements OnInit, OnDestroy {
  private subscription = new Subscription();

  inviteButtonText = '+ Invite Users';
  usersAndPermissionsUsers: UsersAndPermissionsUserItem[] = [];
  usersAndPermissionsUserColumns: string[] = ['select', 'userName', 'email', 'roleName', 'acceptedInvitation'];
  usersAndPermissionsUsersLoading = false;
  usersAndPermissionsUsersError = '';
  isEmailServiceAvailable = true;
  inviteError = '';
  deletingUsers = false;
  selectedUserIds = new Set<number>();


  constructor(
    private usersAndPermissionsPluginService: UsersAndPermissionsPluginService,
    private usersManagementService: UsersManagementService,
    private dialog: MatDialog
  ) { }

  ngOnInit(): void {
    this.loadUsersAndPermissionsUsers();
    this.loadPluginsState();
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  private loadUsersAndPermissionsUsers(): void {
    this.usersAndPermissionsUsersLoading = true;
    this.usersAndPermissionsUsersError = '';

    this.subscription.add(
      this.usersAndPermissionsPluginService.getAllUsers().subscribe({
        next: (users) => {
          this.usersAndPermissionsUsers = users;
          this.selectedUserIds.clear();
          this.usersAndPermissionsUsersLoading = false;
        },
        error: (error) => {
          this.usersAndPermissionsUsers = [];
          this.selectedUserIds.clear();
          this.usersAndPermissionsUsersError = this.getApiErrorMessage(error);
          this.usersAndPermissionsUsersLoading = false;
        },
      })
    );
  }

  inviteUser(): void {
    const dialogRef = this.dialog.open(InviteUserDialogComponent, {
      data: {
        isEmailServiceAvailable: this.isEmailServiceAvailable,
        usePluginRoles: true,
      },
    });

    dialogRef.afterClosed().subscribe((data: InviteUserData | null) => {
      if (!data) return;

      this.inviteError = '';

      this.subscription.add(
          this.usersAndPermissionsPluginService.inviteUser(data).subscribe({
          next: () => {
              this.loadUsersAndPermissionsUsers();
          },
          error: (error) => {
            const apiMessage = error?.error?.message || error?.error?.title;
            this.inviteError = apiMessage || 'Failed to invite user.';
          },
        })
      );
    });
  }

  get selectAllChecked(): boolean {
    return this.usersAndPermissionsUsers.length > 0
      && this.selectedUserIds.size === this.usersAndPermissionsUsers.length;
  }

  get selectAllIndeterminate(): boolean {
    return this.selectedUserIds.size > 0
      && this.selectedUserIds.size < this.usersAndPermissionsUsers.length;
  }

  toggleSelectAll(event: MatCheckboxChange): void {
    this.selectedUserIds.clear();

    if (event.checked) {
      this.usersAndPermissionsUsers.forEach((user) => this.selectedUserIds.add(user.id));
    }
  }

  toggleSelectUser(event: MatCheckboxChange, userId: number): void {
    if (event.checked) {
      this.selectedUserIds.add(userId);
      return;
    }

    this.selectedUserIds.delete(userId);
  }

  isSelected(userId: number): boolean {
    return this.selectedUserIds.has(userId);
  }

  deleteSelectedUsers(): void {
    if (!this.selectedUserIds.size || this.deletingUsers) {
      return;
    }

    const selectedCount = this.selectedUserIds.size;
    const confirmationText = selectedCount === 1
      ? 'Are you sure you want to delete the selected user?'
      : `Are you sure you want to delete ${selectedCount} selected users?`;

    if (!confirm(confirmationText)) {
      return;
    }

    this.deletingUsers = true;
    this.usersAndPermissionsUsersError = '';

    const deleteRequests = Array.from(this.selectedUserIds).map((userId) => this.usersAndPermissionsPluginService.deleteUser(userId)
    );

    this.subscription.add(
      forkJoin(deleteRequests).subscribe({
        next: () => {
          this.deletingUsers = false;
          this.loadUsersAndPermissionsUsers();
        },
        error: (error) => {
          this.deletingUsers = false;
          this.usersAndPermissionsUsersError = this.getApiErrorMessage(error);
        },
      })
    );
  }

  private getApiErrorMessage(error: any): string {
    return (
      error?.error?.message ||
      error?.error?.title ||
      error?.error?.error ||
      (error?.status
        ? `Failed to load users and permissions data (${error.status}${error?.statusText ? ` ${error.statusText}` : ''})${error?.url ? `: ${error.url}` : '.'}`
        : 'Failed to load users and permissions data.')
    );
  }

  private loadPluginsState(): void {
    this.subscription.add(
      this.usersManagementService.getPluginsState().subscribe({
        next: (response) => {
          this.isEmailServiceAvailable = !!response.services?.['IEmailService'];
          this.inviteButtonText = this.isEmailServiceAvailable ? '+ Invite Users' : 'Create User';
        },
      })
    );
  }
}
