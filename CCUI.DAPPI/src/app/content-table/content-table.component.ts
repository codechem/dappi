import { CommonModule } from '@angular/common';
import {
  Component,
  HostListener,
  Input,
  OnChanges,
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
import { ContentStateService } from '../content-manager/content-state.service';
import { DomSanitizer, SafeUrl } from '@angular/platform-browser';
import { HttpClient } from '@angular/common/http';

export interface TableHeader {
  key: string;
  label: string;
  type: 'text' | 'textarea' | 'file';
}

export interface ContentItem {
  id: string;
  [key: string]: any;
}

export interface PaginatedResponse {
  total: number;
  offset: number;
  limit: number;
  data: ContentItem[];
}

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
  ],
  templateUrl: './content-table.component.html',
  styleUrl: './content-table.component.scss',
})
export class ContentTableComponent implements OnInit, OnChanges {
  @Input() selectedType = 'Article';
  @Input() items: ContentItem[] = [];
  @Input() headers: TableHeader[] = [];
  Math = Math;

  searchText = '';
  isSearching = false;
  filteredItems: ContentItem[] = [];
  menuPosition = { top: 0, left: 0 };
  activeMenuItemId?: string = undefined;

  selectAll = false;
  selectedItems: Set<string> = new Set();

  currentPage = 1;
  itemsPerPage = 10;
  totalItems = 0;
  totalPages = 1;
  paginationArray: number[] = [];
  isLoading = false;

  constructor(
    private router: Router,
    private contentStateService: ContentStateService,
    private sanitizer: DomSanitizer,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    this.loadData();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['items']) {
      this.updateFilteredItems();
      this.selectedItems.clear();
      this.selectAll = false;
    }
  }

  loadData(): void {
    this.isLoading = true;
    const offset = (this.currentPage - 1) * this.itemsPerPage;

    const endpoint = `http://localhost:5101/api/${this.selectedType
      .toLowerCase()
      .replace(/\s+/g, '-')}`;

    this.http
      .get<PaginatedResponse>(endpoint, {
        params: {
          offset: offset.toString(),
          limit: 5, //this.itemsPerPage.toString(),
          SearchTerm: this.searchText || '',
        },
      })
      .subscribe({
        next: (response) => {
          this.items = response.data;
          this.totalItems = response.total;
          this.updateFilteredItems();
          this.calculatePagination();
          this.isLoading = false;
        },
        error: (error) => {
          console.error('Error fetching data:', error);
          this.isLoading = false;
        },
      });
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
    let endPage = Math.min(
      this.totalPages - 1,
      this.currentPage + halfPagesToShow
    );

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

    if (
      this.totalPages > 1 &&
      !this.paginationArray.includes(this.totalPages)
    ) {
      this.paginationArray.push(this.totalPages);
    }
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages || page === this.currentPage) {
      return;
    }

    this.currentPage = page;
    this.loadData();
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
      this.filteredItems.forEach((item) => this.selectedItems.add(item.id));
    }
  }

  toggleSelectItem(event: any, itemId: string): void {
    const checked = event.checked;

    if (checked) {
      this.selectedItems.add(itemId);
    } else {
      this.selectedItems.delete(itemId);
    }

    this.selectAll =
      this.filteredItems.length > 0 &&
      this.selectedItems.size === this.filteredItems.length;
  }

  deleteSelectedItems(): void {
    if (this.selectedItems.size === 0) return;

    const confirmDelete = window.confirm(
      `Are you sure you want to delete ${this.selectedItems.size} selected item(s)?`
    );

    if (confirmDelete) {
      const deletePromises: Promise<any>[] = [];

      this.selectedItems.forEach((itemId) => {
        const endpoint = `http://localhost:5101/api/${this.selectedType
          .toLowerCase()
          .replace(/\s+/g, '-')}/${itemId}`;

        const deletePromise = new Promise((resolve, reject) => {
          this.http.delete(endpoint).subscribe({
            next: (response) => resolve(response),
            error: (error) => reject(error),
          });
        });

        deletePromises.push(deletePromise);
      });

      Promise.allSettled(deletePromises).then((results) => {
        const successfulDeletes = results.filter(
          (result) => result.status === 'fulfilled'
        ).length;

        console.log(
          `Successfully deleted ${successfulDeletes} of ${this.selectedItems.size} items`
        );

        this.loadData();

        this.selectedItems.clear();
        this.selectAll = false;
      });
    }
  }

  getColumnWidth(header: any): number {
    switch (header.type) {
      case 'file':
        return 150;
      case 'date':
        return 180;
      case 'description':
        return 250;
      case 'title':
        return 200;
      default:
        return 150;
    }
  }

  convertToImage(byteArray: ArrayBuffer | string): SafeUrl {
    if (!byteArray) return '';

    if (typeof byteArray === 'string') {
      return this.sanitizer.bypassSecurityTrustUrl(byteArray);
    }

    const base64 = btoa(
      new Uint8Array(byteArray).reduce(
        (data, byte) => data + String.fromCharCode(byte),
        ''
      )
    );

    return this.sanitizer.bypassSecurityTrustUrl(
      `data:image/jpeg;base64,${base64}`
    );
  }

  getCellDisplay(item: ContentItem, header: TableHeader) {
    const value = item[header.key];

    if (header.type === 'file' && value) {
      return this.convertToImage(value);
    }

    return value;
  }

  navigateToCreate(): void {
    this.contentStateService.setContentCreateData(
      this.headers,
      this.selectedType
    );

    this.router.navigate(['/content-create']);
  }

  toggleMenu(item: ContentItem, event: MouseEvent): void {
    event.stopPropagation();

    if (this.activeMenuItemId === item.id) {
      this.activeMenuItemId = undefined;
    } else {
      const buttonRect = (
        event.currentTarget as HTMLElement
      ).getBoundingClientRect();

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
    this.contentStateService.setContentCreateData(
      this.headers,
      this.selectedType,
      item
    );
    this.router.navigate(['content-create']);
    this.closeMenu();
  }

  onDelete(item: ContentItem): void {
    console.log('Delete item:', item);
    const confirmDelete = window.confirm(`Are you sure you want to delete?`);
    if (confirmDelete) {
      const endpoint = `http://localhost:5101/api/${this.selectedType
        .toLowerCase()
        .replace(/\s+/g, '-')}/${item.id}`;

      this.http.delete(endpoint).subscribe({
        next: (response) => {
          console.log('Successfully deleted:', response);
          this.loadData();
        },
        error: (error) => {
          console.error('Error deleting record:', error);
        },
        complete: () => {
          this.closeMenu();
        },
      });
    }
    this.closeMenu();
  }

  toggleSearch(): void {
    this.isSearching = true;
  }

  closeSearch(): void {
    setTimeout(() => {
      if (!this.searchText) {
        this.isSearching = false;
      }
    }, 100);
  }

  clearSearch(): void {
    this.searchText = '';
    this.currentPage = 1;
    this.loadData();
    this.isSearching = false;
  }

  onSearchTextChange(newText: string): void {
    this.searchText = newText.trim();
    clearTimeout(this.searchDebounce);

    this.searchDebounce = setTimeout(() => {
      this.currentPage = 1;
      this.loadData();
    }, 300);
  }

  private searchDebounce: any;

  private updateFilteredItems(): void {
    this.filteredItems = [...this.items];
  }
}