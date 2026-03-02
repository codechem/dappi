import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AddRoleDialogComponent } from '../add-role-dialog/add-role-dialog.component';
import { Store } from '@ngrx/store';
import { Subscription } from 'rxjs';
import { ContentTableComponent } from '../content-table/content-table.component';
import { ContentItem, PaginatedResponse } from '../models/content.model';
import { RolesManagementService, RoleItem } from '../services/auth/roles-management.service';
import * as CollectionActions from '../state/collection/collection.actions';
import * as ContentActions from '../state/content/content.actions';
import {
  selectIsSearching,
  selectItems,
  selectLoading,
  selectSelectedType,
} from '../state/content/content.selectors';

type SettingsTab = 'users' | 'roles';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatIconModule,
    MatButtonModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    ContentTableComponent,
  ],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
})
export class SettingsComponent implements OnInit, OnDestroy {
  private subscription = new Subscription();

  activeTab: SettingsTab = 'users';

  isSearching = false;
  items: ContentItem[] = [];

  roles: RoleItem[] = [];
  rolesLoading = false;
  rolesError = '';
  deletingRoleId: string | null = null;

  selectedType$ = this.store.select(selectSelectedType);
  isLoading$ = this.store.select(selectLoading);

  private isSearching$ = this.store.select(selectIsSearching);
  private items$ = this.store.select(selectItems);

  constructor(
    private store: Store,
    private rolesManagementService: RolesManagementService,
    private dialog: MatDialog,
  ) {}

  ngOnInit(): void {
    this.subscription.add(
      this.isSearching$.subscribe((searching) => {
        this.isSearching = searching;
      })
    );

    this.subscription.add(
      this.items$.subscribe((items: PaginatedResponse | undefined) => {
        this.items = items?.Data ?? [];
      })
    );

    this.selectTab('users');
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  selectTab(tab: SettingsTab): void {
    this.activeTab = tab;

    if (tab === 'users') {
      this.loadUsers();
      return;
    }

    this.loadRoles();
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

  private loadUsers(): void {
    this.store.dispatch(ContentActions.setIsSearching({ isSearching: false }));
    this.store.dispatch(ContentActions.setSearchText({ searchText: '' }));
    this.store.dispatch(ContentActions.setContentType({ selectedType: 'Users' }));
    this.store.dispatch(CollectionActions.loadFields({ modelType: 'Users' }));
    this.store.dispatch(ContentActions.loadHeaders({ selectedType: 'Users' }));
    this.store.dispatch(
      ContentActions.loadContent({
        selectedType: 'Users',
        page: 1,
        limit: 10,
        searchText: '',
      })
    );
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
