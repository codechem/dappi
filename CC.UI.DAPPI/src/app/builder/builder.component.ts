import { Component } from '@angular/core';
import { Sidebar } from '../sidebar/sidebar.component';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { ButtonComponent } from '../button/button.component';
import { MatDialog } from '@angular/material/dialog';
import { AddFieldDialogComponent } from '../add-field-dialog/add-field-dialog.component';
import { FieldItem, FieldsListComponent } from '../fields-list/fields-list.component';
import { CommonModule } from '@angular/common';

@Component({
    selector: 'app-builder',
    imports: [Sidebar, MatIconModule,
        MatButtonModule, ButtonComponent, FieldsListComponent, CommonModule],
    templateUrl: './builder.component.html',
    styleUrl: './builder.component.scss'
})
export class BuilderComponent {
  disabled: boolean = true;
  fieldsData: FieldItem[] = [
    { name: 'Title', iconText: 'Aa', type: 'Text' },
    { name: 'description', iconText: 'Aa', type: 'Text' },
    { name: 'Pagination', iconText: '123', type: 'Number' },
    { name: 'Cover', iconName: 'image', type: 'Media' },
    { name: 'author', iconName: 'link', type: 'Link' },
    { name: 'Published', iconText: 'Aa', type: 'Text' },
    { name: 'DateField', iconName: 'event', type: 'Date' },
  ];
  

  constructor(private dialog: MatDialog) { }


  openAddFieldDialog(): void {
    this.dialog.open(AddFieldDialogComponent, {
      width: '800px',
      panelClass: 'add-field-dialog-container',
      disableClose: true, // optional
    });
}
}
