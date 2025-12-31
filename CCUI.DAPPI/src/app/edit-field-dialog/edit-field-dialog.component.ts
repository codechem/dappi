import { Component, Inject, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from '@angular/common';
import { ModelValidators } from '../validators/model-validators';
import { Store } from '@ngrx/store';
import { selectFields } from '../state/collection/collection.selectors';
import { FieldTypeEnum } from '../enums/fieldType';

export interface EditFieldDialogData {
  fieldName: string;
  fieldType: string;
  isRequired: boolean;
  hasIndex: boolean;
  regex?: string;
  noPastDates?: boolean;
}

@Component({
  selector: 'app-edit-field-dialog',
  standalone: true,
  imports: [
    MatDialogModule,
    MatFormFieldModule,
    MatCheckboxModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    ReactiveFormsModule,
    CommonModule,
  ],
  templateUrl: './edit-field-dialog.component.html',
  styleUrl: './edit-field-dialog.component.scss',
})
export class EditFieldDialogComponent implements OnInit {
  editForm: FormGroup;
  selectedTypeFields$ = this.store.select(selectFields);
  isTextType = false;
  isDateType = false;

  constructor(
    private dialogRef: MatDialogRef<EditFieldDialogComponent>,
    private fb: FormBuilder,
    @Inject(MAT_DIALOG_DATA) public data: EditFieldDialogData,
    private store: Store
  ) {
    this.editForm = this.fb.group({
      fieldName: [
        data.fieldName,
        {
          validators: [
            Validators.required,
            Validators.maxLength(50),
            ModelValidators.pascalCase,
            ModelValidators.reservedKeyword,
            ModelValidators.collectionNameIsTaken,
          ],
          asyncValidators: [ModelValidators.fieldNameIsTaken(this.selectedTypeFields$, data.fieldName)],
        },
      ],
      isRequired: [data.isRequired],
      hasIndex: [data.hasIndex],
      regex: [data.regex || '', [ModelValidators.validRegex]],
      noPastDates: [data.noPastDates || false],
    });

    this.isTextType = data.fieldType.toLowerCase() === 'string';
    this.isDateType = data.fieldType === 'DateTime' || data.fieldType === 'DateOnly';
    
    console.log('Edit Dialog - Field Type:', data.fieldType, 'isDateType:', this.isDateType, 'noPastDates:', data.noPastDates);
  }

  ngOnInit(): void {}

  onSave(): void {
    if (!this.editForm.valid || !this.hasChanges()) {
      this.editForm.markAllAsTouched();
      return;
    }

    const result = {
      oldFieldName: this.data.fieldName,
      newFieldName: this.editForm.value.fieldName,
      isRequired: this.editForm.value.isRequired,
      hasIndex: this.editForm.value.hasIndex,
      regex: this.isTextType ? this.editForm.value.regex : undefined,
      noPastDates: this.isDateType ? this.editForm.value.noPastDates : undefined,
    };

    this.dialogRef.close(result);
  }

  onClose(): void {
    this.dialogRef.close();
  }

  hasChanges(): boolean {
    const formValue = this.editForm.value;
    return (
      formValue.fieldName !== this.data.fieldName ||
      formValue.isRequired !== this.data.isRequired ||
      formValue.hasIndex !== this.data.hasIndex ||
      (this.isTextType && formValue.regex !== (this.data.regex || '')) ||
      (this.isDateType && formValue.noPastDates !== (this.data.noPastDates || false))
    );
  }

  get canSubmit(): boolean {
    return this.editForm.valid && this.hasChanges();
  }
}
