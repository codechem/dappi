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
  combineLatest,
  map,
  take,
  Observable,
} from 'rxjs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { AddCollectionTypeDialogComponent } from '../add-collection-type-dialog/add-collection-type-dialog.component';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import * as ContentActions from '../state/content/content.actions';
import * as CollectionActons from '../state/collection/collection.actions';
import { selectSelectedType } from '../state/content/content.selectors';
import {
  selectCollectionTypes,
  selectCollectionTypesError,
  selectModelResponse,
  selectPublishedCollectionTypes,
} from '../state/collection/collection.selectors';
import {
  loadCollectionTypes,
  loadPublishedCollectionTypes,
} from '../state/collection/collection.actions';
import { selectUser } from '../state/auth/auth.selectors';
import { ModelResponse } from '../models/content.model';

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
  collectionTypes:string[] = [];
  collectionTypesError$ = this.store.select(selectCollectionTypesError);
  publishedCollectionTypes$ = this.store.select(selectPublishedCollectionTypes);

  filteredCollectionTypes: string[] = [];
  searchText = '';
  selectedType = '';

  responseModel$ = this.store.select(selectModelResponse);
  responseModel: ModelResponse | null = null;

  private destroy$ = new Subject<void>();
  private searchTextChanged = new Subject<string>();

  private baseCollectionTypes$: Observable<string[]> | null = null;

  constructor(
    private dialog: MatDialog,
    private router: Router,
    private store: Store
  ) {}

  ngOnInit(): void {
    const typesObservable$ =
      this.headerText === 'Builder' ? this.collectionTypes$ : this.publishedCollectionTypes$;

    this.baseCollectionTypes$ = combineLatest([
      typesObservable$,
      this.store.select(selectUser),
    ]).pipe(
      map(([types, user]) => {
        const isAdmin = user?.roles.includes('Admin') ?? false;
        this.isAdmin = isAdmin;

        const uniqueTypes = new Set([...types]);
        let resultTypes = Array.from(uniqueTypes);

        if (this.headerText !== 'Builder') {
          if (isAdmin && !uniqueTypes.has('Users')) {
            resultTypes.push('Users');
          }
        }

        return resultTypes;
      }),
      takeUntil(this.destroy$)
    );

    if (this.headerText === 'Builder') {
      this.store.dispatch(loadCollectionTypes());
    } else {
      this.store.dispatch(loadPublishedCollectionTypes());
    }

    this.subscriptions.add(
      this.baseCollectionTypes$.subscribe((types) => {
        this.filteredCollectionTypes = this.applySearchFilter(types, this.searchText);
      })
    );

    this.subscriptions.add(
      this.searchTextChanged
        .pipe(takeUntil(this.destroy$), debounceTime(300), distinctUntilChanged())
        .subscribe((searchText) => {
          if (this.baseCollectionTypes$) {
            this.baseCollectionTypes$.pipe(take(1)).subscribe((types) => {
              this.filteredCollectionTypes = this.applySearchFilter(types, searchText);
            });
          }
        })
    );

    this.subscriptions.add(
      this.collectionTypesError$.subscribe((error) => {
        if (error) {
          this.isLoadingTypes = false;
        }
      })
    );

    this.subscriptions.add(
      this.collectionTypes$.subscribe(types => 
        this.collectionTypes = types
      )
    )

    this.subscriptions.add(
      this.responseModel$.subscribe((modelResponse) => {
        this.responseModel = modelResponse;
      })
    )

  }

  ngAfterViewInit(): void {
    if (this.isSearching && this.searchInput) {
      this.searchInput.nativeElement.focus();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.subscriptions.unsubscribe();
  }

  selectCollectionType(type: string): void {
    this.store.dispatch(ContentActions.setContentType({ selectedType: type }));
    this.store.dispatch(CollectionActons.loadFields({modelType:type}));
    
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
    this.onSearchTextChange('');
    this.searchText = '';
  }

  clearSearch(): void {
    this.onSearchTextChange('');
    this.searchText = '';
    this.isSearching = false;
  }

  onSearchTextChange(newText: string): void {
    this.searchText = newText;
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
        if (this.headerText === 'Builder') {
          this.store.dispatch(loadCollectionTypes());
        } else {
          this.store.dispatch(loadPublishedCollectionTypes());
        }
      })
    );
  }

  private applySearchFilter(types: string[], searchText: string): string[] {
    if (!searchText.trim()) {
      return [...types];
    }

    const lowerSearchText = searchText.toLowerCase();
    return types.filter((type) => type.toLowerCase().includes(lowerSearchText));
  }
}
