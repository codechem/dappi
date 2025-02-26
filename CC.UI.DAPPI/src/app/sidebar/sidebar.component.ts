import { Component, Input, NgModule } from '@angular/core';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { FormsModule } from '@angular/forms';
import { ButtonComponent } from '../button/button.component';

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
  ],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.scss',
})
export class Sidebar {
  @Input() headerText: string = 'Builder';

  collectionTypes = [
    'Type Name1',
    'Type Name2',
    'Type Name3',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
    'Type Name',
  ];
  filteredCollectionTypes: Array<string> = this.collectionTypes;
  isSearching = false;
  searchText = '';

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
    this.filteredCollectionTypes = this.collectionTypes;
  }

  onSearchTextChange(newText: string): void {
    this.filteredCollectionTypes = this.collectionTypes.filter((i) =>
      i.toLowerCase().includes(newText.toLowerCase())
    );
  }
}
