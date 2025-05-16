import { Router } from '@angular/router';
import { Component, OnInit, OnDestroy } from '@angular/core';
import { ContentTableComponent } from '../content-table/content-table.component';
import { MatIconModule } from '@angular/material/icon';
import { ButtonComponent } from '../button/button.component';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { Subject, takeUntil, Observable, take, Subscription } from 'rxjs';
import { MatSpinner } from '@angular/material/progress-spinner';
import { Store } from '@ngrx/store';
import {
  selectItems,
  selectHeaders,
  selectLoading,
  selectSelectedType,
  selectTotalItems,
  selectError,
  selectIsSearching,
} from '../state/content/content.selectors';
import * as ContentActions from '../state/content/content.actions';
import { ContentItem, PaginatedResponse, TableHeader } from '../models/content.model';

@Component({
  selector: 'app-content-manager',
  standalone: true,
  imports: [
    SidebarComponent,
    ContentTableComponent,
    MatIconModule,
    MatButtonModule,
    ButtonComponent,
    CommonModule,
    MatSpinner,
  ],
  templateUrl: './content-manager.component.html',
  styleUrl: './content-manager.component.scss',
})
export class ContentManagerComponent implements OnInit, OnDestroy {
  disabled = false;
  isSearching = false;

  private subscription: Subscription = new Subscription();

  items: ContentItem[] = [];

  isSearching$ = this.store.select(selectIsSearching);
  items$: Observable<PaginatedResponse | undefined> = this.store.select(selectItems);
  headers$: Observable<TableHeader[]> = this.store.select(selectHeaders);
  error$ = this.store.select(selectError);
  retry: number = 0;
  isLoading$: Observable<boolean> = this.store.select(selectLoading);
  selectedType$: Observable<string> = this.store.select(selectSelectedType);
  totalItems$: Observable<number> = this.store.select(selectTotalItems);

  constructor(
    private router: Router,
    private store: Store,
  ) {}

  ngOnInit(): void {
    this.subscription.add(
      this.isSearching$.subscribe((searching) => (this.isSearching = searching)),
    );

    this.subscription.add(
      this.selectedType$.subscribe((selectedType) => {
        if (selectedType) {
          this.store.dispatch(ContentActions.loadHeaders({ selectedType: selectedType }));

          this.store.dispatch(
            ContentActions.loadContent({
              selectedType,
              page: 1,
              limit: 10,
              searchText: '',
            }),
          );
        }
      }),
    );
    this.subscription.add(
      this.items$.subscribe((items) => {
        this.items = items?.Data ?? [];
      }),
    );
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  navigateToCreate(): void {
    this.router.navigate(['/content-create']);
  }
}
