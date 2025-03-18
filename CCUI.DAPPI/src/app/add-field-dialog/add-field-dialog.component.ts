import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import {
  FormsModule,
  Validators,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
} from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from '@angular/common';
import * as CollectionActions from '../state/collection/collection.actions';
import { Store } from '@ngrx/store';

interface FieldType {
  icon: string;
  label: string;
  description: string;
  value: string;
  netType: string;
}

@Component({
  selector: 'app-add-field-dialog',
  standalone: true,
  imports: [
    MatDialogModule,
    MatFormFieldModule,
    MatCheckboxModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    FormsModule,
    ReactiveFormsModule,
    CommonModule,
  ],
  templateUrl: './add-field-dialog.component.html',
  styleUrl: './add-field-dialog.component.scss',
})
export class AddFieldDialogComponent {
  fieldTypes: FieldType[] = [
    {
      icon: 'Aa',
      label: 'Text',
      description: 'For single or multi-line text input',
      value: 'text',
      netType: 'string',
    },
    {
      icon: '123',
      label: 'Number',
      description: 'For numerical values and calculations',
      value: 'number',
      netType: 'int',
    },
    {
      icon: 'calendar_today',
      label: 'Date',
      description: 'For selecting dates and times',
      value: 'date',
      netType: 'DateTime',
    },
    {
      icon: 'perm_media',
      label: 'Media',
      description: 'For uploading images or videos',
      value: 'media',
      netType: 'byte[]',
    },
    {
      icon: 'link',
      label: 'Link',
      description: 'For website URLs or references',
      value: 'link',
      netType: 'string',
    },
    {
      icon: 'list',
      label: 'Dropdown',
      description: 'For selecting from predefined options',
      value: 'dropdown',
      netType: 'string',
    },
    {
      icon: 'check_box',
      label: 'Checkbox',
      description: 'For yes/no or true/false values',
      value: 'checkbox',
      netType: 'bool',
    },
    {
      icon: 'today',
      label: 'DateTime',
      description: 'For date and time values together',
      value: 'datetime',
      netType: 'DateTime',
    },
  ];

  fieldForm: FormGroup;
  selectedFieldTypeIndex: number | null = null;

  constructor(
    private dialogRef: MatDialogRef<AddFieldDialogComponent>,
    private fb: FormBuilder,
    @Inject(MAT_DIALOG_DATA) public data: { selectedType: string },
    private store: Store
  ) {
    this.fieldForm = this.fb.group({
      fieldName: ['', [Validators.required, Validators.maxLength(50)]],
      requiredField: [false],
    });
  }

  selectFieldType(index: number): void {
    this.selectedFieldTypeIndex = index;
  }

  onAddField(): void {
    if (!this.fieldForm.valid || this.selectedFieldTypeIndex === null) {
      this.fieldForm.markAllAsTouched();
      return;
    }

    const selectedFieldType = this.fieldTypes[this.selectedFieldTypeIndex];
    const payload = {
      fieldName: this.fieldForm.value.fieldName,
      fieldType: selectedFieldType.netType,
    };

    this.store.dispatch(CollectionActions.addField({ field: payload }));
    this.dialogRef.close();
  }

  onClose(): void {
    this.dialogRef.close();
  }

  get canSubmit(): boolean {
    return this.fieldForm.valid && this.selectedFieldTypeIndex !== null;
  }
}
