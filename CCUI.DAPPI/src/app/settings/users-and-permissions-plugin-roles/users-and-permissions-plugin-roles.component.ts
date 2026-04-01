import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { Subscription } from 'rxjs';
import {
  UsersAndPermissionsPluginService,
  UsersAndPermissionsRoleItem,
} from '../../services/auth/users-and-permissions-plugin.service';

interface PermissionTableRow {
  controller: string;
  permissionName: string;
  description: string;
  selected: boolean;
}

interface ControllerPermissionGroup {
  controller: string;
  rows: PermissionTableRow[];
  allowedCount: number;
}

@Component({
  selector: 'app-users-and-permissions-plugin',
  standalone: true,
  imports: [MatButtonToggleModule, MatIconModule, MatProgressSpinnerModule, MatTableModule],
  templateUrl: './users-and-permissions-plugin-roles.component.html',
  styleUrl: './users-and-permissions-plugin-roles.component.scss',
})
export class UsersAndPermissionsPluginComponent implements OnInit, OnDestroy {
  private subscription = new Subscription();

  usersAndPermissionsRoles: UsersAndPermissionsRoleItem[] = [];
  usersAndPermissionsRoleColumns: string[] = ['name'];
  selectedUsersAndPermissionsRole: UsersAndPermissionsRoleItem | null = null;
  selectedUsersAndPermissionsRolePermissionGroups: ControllerPermissionGroup[] = [];
  usersAndPermissionsControllerColumns: string[] = ['controller', 'summary'];
  usersAndPermissionsPermissionColumns: string[] = ['permission', 'description', 'state'];
  expandedControllers = new Set<string>();
  usersAndPermissionsRolesLoading = false;
  usersAndPermissionsRolesError = '';
  usersAndPermissionsRoleDetailsLoading = false;
  usersAndPermissionsRoleDetailsError = '';

  constructor(private usersAndPermissionsPluginService: UsersAndPermissionsPluginService) {}

  ngOnInit(): void {
    this.loadUsersAndPermissionsRoles();
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  selectUsersAndPermissionsRole(role: UsersAndPermissionsRoleItem): void {
    this.selectedUsersAndPermissionsRole = role;
    this.expandedControllers.clear();
    this.loadUsersAndPermissionsRoleDetails(role.name);
  }

  toggleController(controller: string): void {
    if (this.expandedControllers.has(controller)) {
      this.expandedControllers.delete(controller);
      return;
    }

    this.expandedControllers.add(controller);
  }

  isControllerExpanded(controller: string): boolean {
    return this.expandedControllers.has(controller);
  }

  getEndpointMethodColor(description: string): string {
    const method = (description || '').split('/')[0].trim().toUpperCase();

    switch (method) {
      case 'GET':
        return '#164891';
      case 'PUT':
        return '#ae6303';
      case 'PATCH':
        return '#2f9480';
      case 'POST':
        return '#2e9471';
      case 'DELETE':
        return '#a40504';
      default:
        return '';
    }
  }

  private loadUsersAndPermissionsRoles(): void {
    this.usersAndPermissionsRolesLoading = true;
    this.usersAndPermissionsRolesError = '';

    this.subscription.add(
      this.usersAndPermissionsPluginService.getAllRoles().subscribe({
        next: (roles) => {
          this.usersAndPermissionsRoles = roles;
          this.usersAndPermissionsRolesLoading = false;

          if (!this.selectedUsersAndPermissionsRole && roles.length) {
            this.selectUsersAndPermissionsRole(roles[0]);
          }
        },
        error: (error) => {
          this.usersAndPermissionsRoles = [];
          this.selectedUsersAndPermissionsRole = null;
          this.selectedUsersAndPermissionsRolePermissionGroups = [];
          this.expandedControllers.clear();
          this.usersAndPermissionsRolesError = this.getApiErrorMessage(error);
          this.usersAndPermissionsRolesLoading = false;
        },
      })
    );
  }

  private loadUsersAndPermissionsRoleDetails(roleName: string): void {
    this.usersAndPermissionsRoleDetailsLoading = true;
    this.usersAndPermissionsRoleDetailsError = '';

    this.subscription.add(
      this.usersAndPermissionsPluginService.getRolePermissions(roleName).subscribe({
        next: (permissions) => {
          this.selectedUsersAndPermissionsRolePermissionGroups = Object.entries(permissions)
            .map(([controller, permissionItems]) => {
              const rows = permissionItems
                .map((permission) => ({
                  controller,
                  permissionName: permission.permissionName,
                  description: permission.description || '-',
                  selected: permission.selected,
                }))
                .sort((a, b) => a.permissionName.localeCompare(b.permissionName));

              return {
                controller,
                rows,
                allowedCount: rows.filter((row) => row.selected).length,
              };
            })
            .sort((a, b) => a.controller.localeCompare(b.controller));

          if (this.selectedUsersAndPermissionsRolePermissionGroups.length) {
            this.expandedControllers.add(
              this.selectedUsersAndPermissionsRolePermissionGroups[0].controller
            );
          }
          this.usersAndPermissionsRoleDetailsLoading = false;
        },
        error: (error) => {
          this.selectedUsersAndPermissionsRolePermissionGroups = [];
          this.expandedControllers.clear();
          this.usersAndPermissionsRoleDetailsError = this.getApiErrorMessage(error);
          this.usersAndPermissionsRoleDetailsLoading = false;
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
}
