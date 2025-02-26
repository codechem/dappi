import { Component } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-add-field-dialog',
  standalone: true,
  imports: [MatDialogModule,
    MatDialogModule,
    MatFormFieldModule,
    MatCheckboxModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    FormsModule,
    CommonModule],
  templateUrl: './add-field-dialog.component.html',
  styleUrl: './add-field-dialog.component.scss',
})
export class AddFieldDialogComponent {
  fieldTypes = [
    { icon: 'Aa', label: 'Text', description: 'Explanation of what this type does' },
    { icon: '123', label: 'Number', description: 'Explanation of what this type does' },
    { icon: 'calendar_today', label: 'Date', description: 'Explanation of what this type does' },
    { icon: 'perm_media', label: 'Media', description: 'Explanation of what this type does' },
    { icon: 'link', label: 'Link', description: 'Explanation of what this type does' },
    { icon: 'Aa', label: 'Text', description: 'Explanation of what this type does' },
    { icon: '123', label: 'Number', description: 'Explanation of what this type does' },
    { icon: 'today', label: 'Date', description: 'Explanation of what this type does' }
  ];

  fieldName = '';
  requiredField = false;

  constructor(private dialogRef: MatDialogRef<AddFieldDialogComponent>) {}

  onAddField(): void {
    this.dialogRef.close();
  }

  onClose(): void {
    this.dialogRef.close();
  }
}
