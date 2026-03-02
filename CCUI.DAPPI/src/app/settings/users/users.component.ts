import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Store } from '@ngrx/store';
import { Subscription } from 'rxjs';
import { ContentTableComponent } from '../../content-table/content-table.component';
import { ContentItem, PaginatedResponse } from '../../models/content.model';
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

  isLoading$ = this.store.select(selectLoading);
  private isSearching$ = this.store.select(selectIsSearching);
  private items$ = this.store.select(selectItems);

  constructor(private store: Store) {}

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

    this.loadUsers();
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
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
}
