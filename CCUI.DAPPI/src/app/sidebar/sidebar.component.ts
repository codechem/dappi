import {
  Component,
  Input,
  OnInit,
  OnDestroy,
  ViewChild,
  ElementRef,
  AfterViewInit,
  Output,
  EventEmitter,
} from '@angular/core';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ButtonComponent } from '../button/button.component';
import {
  Subject,
  takeUntil,
  debounceTime,
  distinctUntilChanged,
  catchError,
  finalize,
  of,
} from 'rxjs';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { AddCollectionTypeDialogComponent } from '../add-collection-type-dialog/add-collection-type-dialog.component';
import { SelectedContentService } from '../services/selected-content.service';
import { Router } from '@angular/router';

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
  @Input() collectionTypes: string[] = [];
  @ViewChild('searchInput') searchInput!: ElementRef<HTMLInputElement>;
  @Output() collectionTypeSelected = new EventEmitter<string>();

  filteredCollectionTypes: string[] = [];
  isSearching = false;
  searchText = '';
  isLoading = false;
  selectedType: string = '';

  private destroy$ = new Subject<void>();
  private searchTextChanged = new Subject<string>();

  constructor(
    private http: HttpClient,
    private dialog: MatDialog,
    private router: Router,
    private selectedContentService: SelectedContentService
  ) {}

  ngOnInit(): void {
    this.fetchCollectionTypes();

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

  fetchCollectionTypes(): void {
    this.isLoading = true;

    this.http
      .get<string[]>('http://localhost:5101/api/models')
      .pipe(
        takeUntil(this.destroy$),
        catchError((error) => {
          console.error('Error fetching collection types:', error);
          return of([]);
        }),
        finalize(() => {
          this.isLoading = false;
        })
      )
      .subscribe((data) => {
        this.collectionTypes = data;
        this.filteredCollectionTypes = [...this.collectionTypes];

        this.selectedContentService.currentSelectedType
          .pipe(takeUntil(this.destroy$))
          .subscribe((selectedType) => {
            if (this.collectionTypes.includes(selectedType)) {
              this.selectedType = selectedType;
              this.collectionTypeSelected.emit(selectedType);
            } else {
              this.selectCollectionType(this.collectionTypes[0]);
            }
          });
      });
  }

  selectCollectionType(type: string): void {
    this.headerText === 'Builder'
      ? this.router.navigate(['/builder'])
      : this.router.navigate(['/content-manager']);
    this.selectedType = type;
    this.selectedContentService.setSelectedType(type);
    this.collectionTypeSelected.emit(type);
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
    this.filteredCollectionTypes = [...this.collectionTypes];
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

  retry(): void {
    this.fetchCollectionTypes();
  }

  openAddCollectionTypeDialog(): void {
    const dialogRef = this.dialog.open(AddCollectionTypeDialogComponent, {
      width: '450px',
      panelClass: 'dark-theme-dialog',
      disableClose: true,
      data: { selectedType: this.selectedType },
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result && result.success) {
        this.fetchCollectionTypes();
      }
    });
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