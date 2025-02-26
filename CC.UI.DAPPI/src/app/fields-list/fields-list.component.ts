import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

export interface FieldItem {
  name: string;          // e.g., 'Title', 'description', etc.
  iconText?: string;     // e.g., 'Aa' or '123'
  iconName?: string;     // e.g., 'link', 'image' (for Material icons)
  type: string;          // e.g., 'Text', 'Number', 'Media', 'Link', etc.
}

@Component({
  selector: 'app-fields-list',
  standalone: true,
  imports: [MatIconModule, MatButtonModule, CommonModule],
  templateUrl: './fields-list.component.html',
  styleUrl: './fields-list.component.scss'
})
export class FieldsListComponent {
  @Input() fields: FieldItem[] = [];

  onEdit(field: FieldItem): void {
    // Handle edit logic here, e.g., open a dialog
    console.log('Editing field:', field);
  }
}
