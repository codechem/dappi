import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Store } from '@ngrx/store';
import { Subscription } from 'rxjs';
import { ContentTableComponent } from '../../content-table/content-table.component';
import { InviteUserDialogComponent, InviteUserData } from '../../invite-user-dialog/invite-user-dialog.component';
import { ContentItem, PaginatedResponse } from '../../models/content.model';
import { UsersManagementService } from '../../services/auth/users-management.service';
import * as CollectionActions from '../../state/collection/collection.actions';
import * as ContentActions from '../../state/content/content.actions';
import {
  selectIsSearching,
  selectItems,
  selectLoading,
} from '../../state/content/content.selectors';

@Component({
  selector: 'app-users',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    ContentTableComponent,
  ],
  templateUrl: './users.component.html',
  styleUrl: './users.component.scss',
})
export class UsersComponent implements OnInit, OnDestroy {
  private subscription = new Subscription();

  isSearching = false;
  items: ContentItem[] = [];
  inviteError = '';
  inviteButtonText = '+ Invite Users';
  isEmailServiceAvailable = true;

  isLoading$ = this.store.select(selectLoading);
  private isSearching$ = this.store.select(selectIsSearching);
  private items$ = this.store.select(selectItems);

  constructor(
    private store: Store,
    private dialog: MatDialog,
    private usersManagementService: UsersManagementService,
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

    this.loadPluginsState();
    this.loadUsers();
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  inviteUser(): void {
    const dialogRef = this.dialog.open(InviteUserDialogComponent, {
      data: { isEmailServiceAvailable: this.isEmailServiceAvailable },
    });

    dialogRef.afterClosed().subscribe((data: InviteUserData | null) => {
      if (!data) return;

      this.inviteError = '';

      this.subscription.add(
        this.usersManagementService.inviteUser(data).subscribe({
          next: () => {
            this.loadUsers();
          },
          error: (error) => {
            const apiMessage = error?.error?.message || error?.error?.title;
            this.inviteError = apiMessage || 'Failed to invite user.';
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
