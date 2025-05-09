import { CommonModule } from '@angular/common';
import {
  Component,
  HostListener,
  OnChanges,
  OnDestroy,
  OnInit,
  SimpleChanges,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';
import { MatMenuModule } from '@angular/material/menu';
import { Router } from '@angular/router';
import { MenuComponent } from '../menu/menu.component';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { Store } from '@ngrx/store';
import {
  selectHeaders,
  selectIsSearching,
  selectItems,
  selectItemsPerPage,
  selectLoading,
  selectSelectedType,
} from '../state/content/content.selectors';
import * as ContentActions from '../state/content/content.actions';
import { Subscription } from 'rxjs';
import { ContentItem, FieldType, TableHeader } from '../models/content.model';
import { DrawerComponent } from '../relation-drawer/drawer.component';
import { ImageViewerComponent } from '../image-viewer/image-viewer.component';

@Component({
  selector: 'app-content-table',
  standalone: true,
  imports: [
    MatCheckboxModule,
    MatIconModule,
    MatButtonModule,
    CommonModule,
    FormsModule,
    MatMenuModule,
    MenuComponent,
    DrawerComponent,
    ImageViewerComponent,
  ],
  templateUrl: './content-table.component.html',
  styleUrl: './content-table.component.scss',
})
export class ContentTableComponent implements OnInit, OnChanges, OnDestroy {
  private subscription: Subscription = new Subscription();
  fieldType = FieldType;
  Math = Math;
  selectedType: string | undefined = undefined;
  searchText: string | undefined = undefined;
  limit: number | undefined = undefined;
  items: ContentItem[] = [];

  objectKeys = Object.keys;

  isDrawerOpen = false;
  drawerTitle = '';
  drawerData: any = null;
  drawerType: FieldType | null = null;

  isImageViewerOpen = false;
  currentImageUrl: SafeUrl | string = '';
  currentImageTitle = '';

  selectedType$ = this.store.select(selectSelectedType);
  isSearching$ = this.store.select(selectIsSearching);
  itemsPerPage$ = this.store.select(selectItemsPerPage);
  items$ = this.store.select(selectItems);
  headers$ = this.store.select(selectHeaders);
  loading$ = this.store.select(selectLoading);

  isSearching = false;
  menuPosition = { top: 0, left: 0 };
  activeMenuItemId?: string = undefined;

  selectAll = false;
  selectedItems: Set<string> = new Set();

  currentPage = 1;
  itemsPerPage = 10;
  totalItems = 0;
  totalPages = 1;
  paginationArray: number[] = [];
  isLoading = true;

  constructor(
    private router: Router,
    private sanitizer: DomSanitizer,
    private store: Store,
  ) {}

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  onCellClick(item: ContentItem, header: TableHeader): void {
    if (header.type === FieldType.relation || header.type === FieldType.collection) {
      this.drawerTitle = header.label;
      this.drawerData = item[header.key];
      this.drawerType = header.type;
      this.isDrawerOpen = true;
    }
  }

  closeDrawer(): void {
    this.isDrawerOpen = false;
  }

  openImageViewer(imageUrl: SafeUrl | string, title: string): void {
    this.currentImageUrl = imageUrl;
    this.currentImageTitle = title;
    this.isImageViewerOpen = true;
    document.body.style.overflow = 'hidden';
  }

  closeImageViewer(): void {
    this.isImageViewerOpen = false;
    this.currentImageUrl = '';
    document.body.style.overflow = '';
  }

  getRelationDisplay(item: ContentItem, header: TableHeader): string {
    const value = item[header.key];

    if (!value) return 'None';

    if (header.type === FieldType.relation) {
      return value.title || value.name || 'View relation';
    }

    if (header.type === FieldType.collection) {
      return `${Array.isArray(value) ? value.length : 0} items`;
    }

    return String(value);
  }

  ngOnInit(): void {
    this.subscription.add(this.selectedType$.subscribe((type) => (this.selectedType = type)));

    this.subscription.add(
      this.isSearching$.subscribe((searching) => (this.isSearching = searching)),
    );

    this.subscription.add(this.itemsPerPage$.subscribe((limit) => (this.limit = limit)));
    this.subscription.add(
      this.items$.subscribe((items) => {
        this.items = items?.data ?? [];
        this.totalItems = items?.total ?? 0;
        this.calculatePagination();
      }),
    );
    this.subscription.add(this.loading$.subscribe((loading) => (this.isLoading = loading)));
  }

  openDrawer(item: ContentItem, header: TableHeader): void {
    if (header.type === FieldType.relation || header.type === FieldType.collection) {
      this.drawerTitle = header.label;
      this.drawerData = item[header.key];
      this.drawerType = header.type;
      this.isDrawerOpen = true;
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['items']) {
      this.selectedItems.clear();
      this.selectAll = false;
    }
  }

  calculatePagination(): void {
    this.totalPages = Math.ceil(this.totalItems / this.itemsPerPage);

    this.paginationArray = [];

    if (this.totalPages > 0) {
      this.paginationArray.push(1);
    }

    const pagesToShow = 5;
    const halfPagesToShow = Math.floor(pagesToShow / 2);

    let startPage = Math.max(2, this.currentPage - halfPagesToShow);
    let endPage = Math.min(this.totalPages - 1, this.currentPage + halfPagesToShow);

    if (startPage > 2) {
      this.paginationArray.push(-1);
    }

    for (let i = startPage; i <= endPage; i++) {
      if (!this.paginationArray.includes(i)) {
        this.paginationArray.push(i);
      }
    }

    if (endPage < this.totalPages - 1 && this.totalPages > 1) {
      this.paginationArray.push(-1); // -1 represents ellipsis
    }

    if (this.totalPages > 1 && !this.paginationArray.includes(this.totalPages)) {
      this.paginationArray.push(this.totalPages);
    }
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.currentPage) {
      return;
    }
    if (this.selectedType) {
      this.currentPage = page;
      this.store.dispatch(
        ContentActions.loadContent({
          selectedType: this.selectedType,
          page,
          limit: this.limit ?? 10,
          searchText: this.searchText ?? '',
        }),
      );
    }
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.goToPage(this.currentPage - 1);
    }
  }

  nextPage(): void {
    if (this.currentPage < this.totalPages) {
      this.goToPage(this.currentPage + 1);
    }
  }

  toggleSelectAll(event: any): void {
    const checked = event.checked;
    this.selectAll = checked;
    this.selectedItems.clear();

    if (checked) {
      this.items.forEach((item) => this.selectedItems.add(item.id));
    }
  }

  toggleSelectItem(event: any, itemId: string): void {
    const checked = event.checked;

    if (checked) {
      this.selectedItems.add(itemId);
    } else {
      this.selectedItems.delete(itemId);
    }

    this.selectAll = this.items.length > 0 && this.selectedItems.size === this.items.length;
  }

  deleteSelectedItems(): void {
    if (this.selectedItems.size === 0) return;

    const confirmDelete = window.confirm(
      `Are you sure you want to delete ${this.selectedItems.size} selected item(s)?`,
    );

    if (confirmDelete) {
      const ids = Array.from(this.selectedItems);
      this.store.dispatch(
        ContentActions.deleteMultipleContent({
          ids,
          contentType: this.selectedType ?? '',
        }),
      );

      this.store.dispatch(
        ContentActions.loadContent({
          selectedType: this.selectedType ?? '',
          page: this.currentPage,
          limit: this.limit ?? 10,
          searchText: this.searchText ?? '',
        }),
      );
      this.selectedItems.clear();
      this.selectAll = false;
    }
  }

  getFilteredHeaders(headers: TableHeader[]): TableHeader[] {
    if (!headers) return [];

    return headers.filter((header) => header.type !== FieldType.relation);
  }

  getColumnWidth(header: TableHeader): number {
    switch (header.type) {
      case FieldType.file:
        return 150;
      case FieldType.date:
        return 220;
      case FieldType.textarea:
        return 250;
      case FieldType.text:
        return 200;
      case FieldType.checkbox:
        return 80;
      default:
        return 150;
    }
  }

  getCellDisplay(item: ContentItem, header: TableHeader) {
    const value = item[header.key];

    if (value === null || value === undefined) {
      return '-';
    }

    switch (header.type) {
      case FieldType.file:
        return value.url;

      case FieldType.date:
        return this.formatDate(value);

      case FieldType.checkbox:
        return value;

      case FieldType.relation:
        return value.title || 'View relation';

      case FieldType.collection:
        return `${value.length || 0} items`;

      default:
        return value;
    }
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '-';

    try {
      const date = new Date(dateStr);

      if (isNaN(date.getTime())) {
        return dateStr;
      }

      return (
        date.toLocaleDateString('en-US', {
          year: 'numeric',
          month: 'short',
          day: 'numeric',
        }) +
        ' at ' +
        date.toLocaleTimeString('en-US', {
          hour: 'numeric',
          minute: '2-digit',
          hour12: true,
        })
      );
    } catch (error) {
      console.error('Error formatting date:', error);
      return dateStr;
    }
  }

  navigateToCreate(): void {
    this.router.navigate(['/content-create']);
  }

  toggleMenu(item: ContentItem, event: MouseEvent): void {
    event.stopPropagation();

    if (this.activeMenuItemId === item.id) {
      this.activeMenuItemId = undefined;
    } else {
      const buttonRect = (event.currentTarget as HTMLElement).getBoundingClientRect();

      const menuWidth = 150;

      const rightEdge = buttonRect.right + window.scrollX;
      const windowWidth = window.innerWidth;
      const positionLeft =
        rightEdge - menuWidth > windowWidth
          ? windowWidth - menuWidth - 12
          : buttonRect.right - menuWidth + window.scrollX;

      this.menuPosition = {
        top: buttonRect.bottom + window.scrollY + 4,
        left: positionLeft,
      };

      this.activeMenuItemId = item.id;
    }
  }

  getSelectedItem(): ContentItem | null {
    return this.items.find((item) => item.id === this.activeMenuItemId) || null;
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    if (this.activeMenuItemId !== null) {
      this.closeMenu();
    }
  }

  closeMenu(): void {
    this.activeMenuItemId = undefined;
  }

  onEdit(item: ContentItem): void {
    this.router.navigate(['content-create']);
    this.store.dispatch(ContentActions.setCurrentItem({ currentItem: item }));
    this.closeMenu();
  }

  onDelete(item: ContentItem): void {
    const confirmDelete = window.confirm(`Are you sure you want to delete?`);
    if (confirmDelete) {
      this.store.dispatch(
        ContentActions.deleteContent({
          id: item.id,
          contentType: this.selectedType ?? '',
        }),
      );
    }
    this.closeMenu();
  }

  toggleSearch(): void {
    this.store.dispatch(ContentActions.setIsSearching({ isSearching: true }));
  }

  clearSearch(): void {
    this.store.dispatch(ContentActions.setSearchText({ searchText: '' }));
    this.searchText = '';
    this.currentPage = 1;
    this.store.dispatch(ContentActions.setIsSearching({ isSearching: false }));
    this.store.dispatch(
      ContentActions.loadContent({
        selectedType: this.selectedType ?? '',
        page: 1,
        limit: this.itemsPerPage,
        searchText: '',
      }),
    );
  }

  onSearchTextChange(newText: string): void {
    this.store.dispatch(ContentActions.setSearchText({ searchText: newText }));
    this.store.dispatch(
      ContentActions.loadContent({
        selectedType: this.selectedType ?? '',
        page: 1,
        limit: this.itemsPerPage,
        searchText: newText,
      }),
    );
  }
}
