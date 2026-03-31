import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { Subscription } from 'rxjs';
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
  imports: [MatProgressSpinnerModule, MatTableModule, MatIconModule],
  templateUrl: './users-and-permissions-plugin-users.component.html',
  styleUrl: './users-and-permissions-plugin-users.component.scss',
})
export class UsersAndPermissionsUsersComponent implements OnInit, OnDestroy {
  private subscription = new Subscription();

  inviteButtonText = '+ Invite Users';
  usersAndPermissionsUsers: UsersAndPermissionsUserItem[] = [];
  usersAndPermissionsUserColumns: string[] = ['userName', 'email', 'roleName', 'emailConfirmed'];
  usersAndPermissionsUsersLoading = false;
  usersAndPermissionsUsersError = '';
  isEmailServiceAvailable = true;
  inviteError = '';


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
          this.usersAndPermissionsUsersLoading = false;
        },
        error: (error) => {
          this.usersAndPermissionsUsers = [];
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
