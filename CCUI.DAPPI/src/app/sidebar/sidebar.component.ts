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
import { HttpClient } from '@angular/common/http';
import { ButtonComponent } from '../button/button.component';
import { Subject, takeUntil, debounceTime, distinctUntilChanged } from 'rxjs';
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
  selectIsLoadingCollectionTypes,
} from '../state/collection/collection.selectors';
import { loadCollectionTypes } from '../state/collection/collection.actions';
import * as CollectionActions from '../state/collection/collection.actions';

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

  isSearching = false;
  searchText = '';
  isLoading = false;
  selectedType$ = this.store.select(selectSelectedType);
  collectionTypes$ = this.store.select(selectCollectionTypes);
  isLoadingCollectionTypes$ = this.store.select(selectIsLoadingCollectionTypes);
  collectionTypesError$ = this.store.select(selectCollectionTypesError);
  filteredCollectionTypes: string[] = [];

  private destroy$ = new Subject<void>();
  private searchTextChanged = new Subject<string>();

  constructor(
    private http: HttpClient,
    private dialog: MatDialog,
    private router: Router,
    private store: Store
  ) {}

  ngOnInit(): void {
    this.collectionTypes$.subscribe((types) => {
      if (types.length === 0) {
        this.store.dispatch(loadCollectionTypes());
      } else {
        this.filteredCollectionTypes = types;
      }
    });

    this.searchTextChanged
      .pipe(takeUntil(this.destroy$), debounceTime(300), distinctUntilChanged())
      .subscribe((text) => {
        this.filterCollectionTypes(text);
      });
  }

  ngAfterViewInit(): void {
    if (this.isSearching && this.searchInput) {
      this.searchInput.nativeElement.focus();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
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
    if (!this.searchText) {
      this.isSearching = false;
    }
  }

  clearSearch(): void {
    this.searchText = '';
    this.isSearching = false;
    this.collectionTypes$.subscribe((types) => {
      this.filteredCollectionTypes = [...types];
    });
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

    dialogRef.afterClosed().subscribe((result) => {
      if (result && result.success) {
        this.store.dispatch(loadCollectionTypes());
      }
    });
  }

  private filterCollectionTypes(text: string): void {
    if (!text) {
      this.collectionTypes$.subscribe((types) => {
        this.filteredCollectionTypes = [...types];
      });
      return;
    }
    this.collectionTypes$.subscribe((types) => {
      this.filteredCollectionTypes = types.filter((type) =>
        type.toLowerCase().includes(text.toLowerCase())
      );
    });
  }
}
