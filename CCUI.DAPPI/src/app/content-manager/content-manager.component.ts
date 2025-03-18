import { Router } from '@angular/router';
import { Component, OnInit, OnDestroy } from '@angular/core';
import { ContentTableComponent } from '../content-table/content-table.component';
import { MatIconModule } from '@angular/material/icon';
import { ButtonComponent } from '../button/button.component';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { Subject, takeUntil, Observable, take } from 'rxjs';
import { MatSpinner } from '@angular/material/progress-spinner';
import { ContentStateService } from './content-state.service';
import { Store } from '@ngrx/store';
import {
  selectItems,
  selectHeaders,
  selectLoading,
  selectSelectedType,
  selectTotalItems,
  selectError,
} from '../state/content/content.selectors';
import * as ContentActions from '../state/content/content.actions';
import {
  ContentItem,
  PaginatedResponse,
  TableHeader,
} from '../models/content.model';
import { error } from 'console';

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

  private destroy$ = new Subject<void>();

  items: ContentItem[] = [];

  items$: Observable<PaginatedResponse | undefined> =
    this.store.select(selectItems);
  headers$: Observable<TableHeader[]> = this.store.select(selectHeaders);
  error$ = this.store.select(selectError);
  retry: number = 0;
  isLoading$: Observable<boolean> = this.store.select(selectLoading);
  selectedType$: Observable<string> = this.store.select(selectSelectedType);
  totalItems$: Observable<number> = this.store.select(selectTotalItems);

  constructor(
    private router: Router,
    private contentStateService: ContentStateService,
    private store: Store
  ) {}

  ngOnInit(): void {
    this.selectedType$.subscribe((selectedType) => {
      if (selectedType) {
        this.store.dispatch(
          ContentActions.loadHeaders({ selectedType: selectedType })
        );

        this.store.dispatch(
          ContentActions.loadContent({
            selectedType,
            page: 1,
            limit: 10,
            searchText: '',
          })
        );
      }
    });

    this.items$.subscribe((items) => {
      this.items = items?.data ?? [];
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  navigateToCreate(): void {
    this.headers$.pipe(takeUntil(this.destroy$)).subscribe((headers) => {
      this.selectedType$
        .pipe(takeUntil(this.destroy$))
        .subscribe((selectedType) => {
          this.contentStateService.setContentCreateData(headers, selectedType);
          this.router.navigate(['/content-create']);
        });
    });
  }
}