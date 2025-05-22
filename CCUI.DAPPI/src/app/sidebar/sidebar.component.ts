import {
  Component,
  Input,
  OnInit,
  OnDestroy,
  ViewChild,
  ElementRef,
  AfterViewInit,
} from '@angular/core';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';
import { ButtonComponent } from '../button/button.component';
import { Subject, takeUntil, debounceTime, distinctUntilChanged, Subscription, filter } from 'rxjs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { AddCollectionTypeDialogComponent } from '../add-collection-type-dialog/add-collection-type-dialog.component';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import * as ContentActions from '../state/content/content.actions';
import { selectSelectedType } from '../state/content/content.selectors';
import {
  selectCollectionTypes,
  selectCollectionTypesError,
  selectPublishedCollectionTypes,
} from '../state/collection/collection.selectors';
import {
  loadCollectionTypes,
  loadPublishedCollectionTypes,
} from '../state/collection/collection.actions';
import { selectUser } from '../state/auth/auth.selectors';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    MatSidenavModule,
    MatListModule,
    CommonModule,
    MatIconModule,
    FormsModule,
    ButtonComponent,
    MatProgressSpinnerModule,
  ],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss',
})
export class SidebarComponent implements OnInit, OnDestroy, AfterViewInit {
  @Input() headerText: string = 'Builder';
  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;
  private subscriptions: Subscription = new Subscription();
  private isLoadingTypes = false;

  isSearching = false;
  isAdmin = false;

  selectedType$ = this.store.select(selectSelectedType);
  collectionTypes$ = this.store.select(selectCollectionTypes);
  collectionTypesError$ = this.store.select(selectCollectionTypesError);
  filteredCollectionTypes: string[] = [];
  collectionTypes: string[] = [];
  searchText = '';
  publishedCollectionTypes$ = this.store.select(selectPublishedCollectionTypes);
  publishedCollectionTypes: string[] = [];

  private destroy$ = new Subject<void>();
  private searchTextChanged = new Subject<string>();

  filteredTypes$ = this.collectionTypes$.pipe(
    takeUntil(this.destroy$),
    debounceTime(300),
    distinctUntilChanged(),
    filter((type) => !!type)
  );

  constructor(
    private dialog: MatDialog,
    private router: Router,
    private store: Store
  ) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.store
        .select(selectUser)
        .subscribe((user) => (this.isAdmin = user?.roles.includes('Admin') ?? false))
    );

    if (this.headerText === 'Builder') {
      if (!this.isLoadingTypes) {
        this.isLoadingTypes = true;
        this.store.dispatch(loadCollectionTypes());
      }

      this.subscriptions.add(
        this.collectionTypes$.subscribe((types) => {
          if (types.length > 0 || this.isLoadingTypes) {
            let updatedTypes = [...types];
            this.filteredCollectionTypes = updatedTypes;
            this.collectionTypes = updatedTypes;
            this.isLoadingTypes = false;
          }
        })
      );
    } else {
      if (!this.isLoadingTypes) {
        this.isLoadingTypes = true;
        this.store.dispatch(loadPublishedCollectionTypes());
      }

      this.subscriptions.add(
        this.publishedCollectionTypes$.subscribe((types) => {
          if (types.length > 0 || this.isLoadingTypes) {
            let updatedTypes = [...types];

            if (this.isAdmin && !updatedTypes.includes('Users')) {
              updatedTypes.push('Users');
            }

            this.filteredCollectionTypes = updatedTypes;
            this.collectionTypes = updatedTypes;
            this.isLoadingTypes = false;
          }
        })
      );
    }

    this.subscriptions.add(
      this.collectionTypesError$.subscribe((error) => {
        if (error) {
          this.isLoadingTypes = false;
        }
      })
    );

    this.subscriptions.add(
      this.searchTextChanged
        .pipe(takeUntil(this.destroy$), debounceTime(300), distinctUntilChanged())
        .subscribe((text) => {
          this.filterCollectionTypes(text);
        })
    );
  }

  ngAfterViewInit(): void {
    if (this.isSearching && this.searchInput) {
      this.searchInput.nativeElement.focus();
    }
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  selectCollectionType(type: string): void {
    this.store.dispatch(ContentActions.setContentType({ selectedType: type }));

    if (this.headerText === 'Builder') {
      this.router.navigate(['/builder']);
    } else {
      this.router.navigate(['/content-manager']);
    }
  }

  toggleSearch(): void {
    this.isSearching = true;

    setTimeout(() => {
      if (this.searchInput) {
        this.searchInput.nativeElement.focus();
      }
    });
  }

  closeSearch(): void {
    this.isSearching = false;
    this.searchTextChanged.next('');
    this.searchText = '';
  }

  clearSearch(): void {
    this.searchTextChanged.next('');
    this.searchText = '';
    this.isSearching = false;
  }

  onSearchTextChange(newText: string): void {
    this.searchTextChanged.next(newText);
  }

  trackByFn(index: number, item: string): string {
    return `${item}-${index}`;
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      this.clearSearch();
    }
  }

  openAddCollectionTypeDialog(): void {
    const dialogRef = this.dialog.open(AddCollectionTypeDialogComponent, {
      width: '450px',
      panelClass: 'dark-theme-dialog',
      disableClose: true,
    });
    this.subscriptions.add(
      dialogRef.afterClosed().subscribe(() => {
        this.store.dispatch(loadCollectionTypes());
      })
    );
  }

  private filterCollectionTypes(text: string): void {
    if (!text) {
      let types = [...this.collectionTypes];

      if (this.isAdmin && !types.includes('Users') && this.headerText !== 'Builder') {
        types.push('Users');
      }

      this.filteredCollectionTypes = types;
      return;
    }

    let filtered = this.collectionTypes.filter((type) =>
      type.toLowerCase().includes(text.toLowerCase())
    );

    if (
      this.isAdmin &&
      'users'.includes(text.toLowerCase()) &&
      !this.collectionTypes.includes('Users')
    ) {
      filtered.push('Users');
    }

    this.filteredCollectionTypes = filtered;
  }
}
