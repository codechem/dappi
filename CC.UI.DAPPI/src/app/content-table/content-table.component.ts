import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { ButtonComponent } from '../button/button.component';
import { FormsModule } from '@angular/forms';
import { MatMenuModule } from '@angular/material/menu';

export interface ContentItem {
  id: number;
  title: string;
  description: string;
  status: 'Draft' | 'Published';
}

@Component({
  selector: 'app-content-table',
  standalone: true,
  imports: [MatCheckboxModule, MatIconModule, MatButtonModule, CommonModule, MatIconModule, ButtonComponent, FormsModule, MatMenuModule],
  templateUrl: './content-table.component.html',
  styleUrl: './content-table.component.scss'
})
export class ContentTableComponent {
  @Input() selectedType: string = 'Article';
  @Input() items: ContentItem[] = [];
  searchText: string = ''
  isSearching = false;

  onMenu(item: ContentItem) {
    console.log('Menu clicked for:', item);
  }

  toggleSearch() {
    this.isSearching = true;
  }

  closeSearch() {
    if (!this.searchText) {
      this.isSearching = false;
    }
  }

  clearSearch() {
    this.searchText = '';
    this.isSearching = false;
  }

  onSearchTextChange(newText: string): void {
    // this.filteredCollectionTypes = this.collectionTypes.filter((i) =>
    //   i.toLowerCase().includes(newText.toLowerCase())
    // );
  }
}
