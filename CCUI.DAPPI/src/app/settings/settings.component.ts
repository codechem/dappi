import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { Subscription } from 'rxjs';
import { DataStorageComponent } from './data-storage/data-storage.component';
import { UsersComponent } from './users/users.component';
import { RolesComponent } from './roles/roles.component';
import { UsersManagementService } from '../services/auth/users-management.service';
import {
  UsersAndPermissionsPluginService,
  UsersAndPermissionsRoleItem,
} from '../services/auth/users-and-permissions-plugin.service';

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

type SettingsTab = 'storage' | 'users' | 'roles' | 'usersAndPermissionsPlugin';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    MatButtonToggleModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    DataStorageComponent,
    UsersComponent,
    RolesComponent,
  ],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
})
export class SettingsComponent implements OnInit, OnDestroy {
  private subscription = new Subscription();

  activeTab: SettingsTab = 'storage';
  usersAndPermissionsEnabled = false;
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
  private usersAndPermissionsRolesLoaded = false;

  constructor(
    private usersManagementService: UsersManagementService,
    private usersAndPermissionsPluginService: UsersAndPermissionsPluginService,
  ) {}

  ngOnInit(): void {
    this.loadPluginsState();
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  selectTab(tab: SettingsTab): void {
    if (tab === 'usersAndPermissionsPlugin' && !this.usersAndPermissionsEnabled) {
      return;
    }

    this.activeTab = tab;

    if (tab === 'usersAndPermissionsPlugin' && !this.usersAndPermissionsRolesLoaded) {
      this.loadUsersAndPermissionsRoles();
    }
  }

  private loadPluginsState(): void {
    this.subscription.add(
      this.usersManagementService.getPluginsState().subscribe({
        next: (response) => {
          this.usersAndPermissionsEnabled = !!response.services?.['usersAndPermissions'];

          if (!this.usersAndPermissionsEnabled && this.activeTab === 'usersAndPermissionsPlugin') {
            this.activeTab = 'storage';
          }
        },
      })
    );
  }

  private loadUsersAndPermissionsRoles(): void {
    this.usersAndPermissionsRolesLoading = true;
    this.usersAndPermissionsRolesError = '';

    this.subscription.add(
      this.usersAndPermissionsPluginService.getAllRoles().subscribe({
        next: (roles) => {
          this.usersAndPermissionsRoles = roles;
          this.usersAndPermissionsRolesLoading = false;
          this.usersAndPermissionsRolesLoaded = true;

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

  private getApiErrorMessage(error: any): string {
    return (
      error?.error?.message
      || error?.error?.title
      || error?.error?.error
      || 'Failed to load roles from /api/UsersAndPermissions/roles.'
    );
  }

  selectUsersAndPermissionsRole(role: UsersAndPermissionsRoleItem): void {
    this.selectedUsersAndPermissionsRole = role;
    this.expandedControllers.clear();
    this.loadUsersAndPermissionsRoleDetails(role.Name);
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
                  permissionName: permission.PermissionName,
                  description: permission.Description || '-',
                  selected: permission.Selected,
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
            this.expandedControllers.add(this.selectedUsersAndPermissionsRolePermissionGroups[0].controller);
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
}
