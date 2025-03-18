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
import {
  Subject,
  takeUntil,
  debounceTime,
  distinctUntilChanged,
  Subscription,
} from 'rxjs';
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
  private subscriptions: Subscription = new Subscription();

  isSearching = false;

  selectedType$ = this.store.select(selectSelectedType);
  collectionTypes$ = this.store.select(selectCollectionTypes);
  collectionTypesError$ = this.store.select(selectCollectionTypesError);
  filteredCollectionTypes: string[] = [];
  collectionTypes: string[] = [];
  searchText = '';

  private destroy$ = new Subject<void>();
  private searchTextChanged = new Subject<string>();

  constructor(
    private dialog: MatDialog,
    private router: Router,
    private store: Store
  ) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.collectionTypes$.subscribe((types) => {
        if (types.length === 0) {
          this.store.dispatch(loadCollectionTypes());
        } else {
          this.filteredCollectionTypes = types;
          this.collectionTypes = types;
        }
      })
    );
    this.subscriptions.add(
      this.searchTextChanged
        .pipe(
          takeUntil(this.destroy$),
          debounceTime(300),
          distinctUntilChanged()
        )
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
      this.filteredCollectionTypes = [...this.collectionTypes];
      return;
    }
    this.filteredCollectionTypes = this.collectionTypes.filter((type) =>
      type.toLowerCase().includes(text.toLowerCase())
    );
  }
}
