import { Component, OnInit, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatDialogRef, MatDialogModule, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { EnumManagementService } from '../services/common/enum-management.service';

interface EnumDisplayData {
  name: string;
  values: { name: string; value: number }[];
  valueCount: number;
}

@Component({
  selector: 'app-edit-enum-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
  ],
  templateUrl: './edit-enum-dialog.component.html',
  styleUrl: './edit-enum-dialog.component.scss',
})
export class EditEnumDialogComponent implements OnInit {
  enumForm: FormGroup;

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<EditEnumDialogComponent>,
    private enumsService: EnumManagementService,
    private snackBar: MatSnackBar,
    @Inject(MAT_DIALOG_DATA) public data: EnumDisplayData
  ) {
    this.enumForm = this.fb.group({
      values: this.fb.array([]),
    });
  }

  ngOnInit(): void {
    this.loadEnumValues();
  }

  get values(): FormArray {
    return this.enumForm.get('values') as FormArray;
  }

  loadEnumValues(): void {
    const valuesArray = this.values;
    this.data.values.forEach((value) => {
      const valueGroup = this.fb.group({
        name: [value.name, [Validators.required, Validators.pattern(/^[A-Z][a-zA-Z0-9]*$/)]],
        value: [value.value, [Validators.required, Validators.min(0)]],
      });
      valuesArray.push(valueGroup);
    });
  }

  addValue(): void {
    const valueGroup = this.fb.group({
      name: ['', [Validators.required, Validators.pattern(/^[A-Z][a-zA-Z0-9]*$/)]],
      value: [this.getNextValue(), [Validators.required, Validators.min(0)]],
    });
    this.values.push(valueGroup);
  }

  removeValue(index: number): void {
    if (this.values.length > 1) {
      this.values.removeAt(index);
    }
  }

  getNextValue(): number {
    if (this.values.length === 0) {
      return 0;
    }
    const currentValues = this.values.controls.map(c => c.get('value')?.value || 0);
    const maxValue = Math.max(...currentValues);
    return maxValue + 1;
  }

  onSubmit(): void {
    if (!this.enumForm.valid) {
      this.enumForm.markAllAsTouched();
      return;
    }

    const formValue = this.enumForm.value;
    const names = formValue.values.map((v: any) => v.name);
    
    if (names.length !== new Set(names).size) {
      this.snackBar.open('Duplicate value names are not allowed', 'Close', { duration: 3000 });
      return;
    }

    const values = formValue.values.map((v: any) => v.value);
    if (values.length !== new Set(values).size) {
      this.snackBar.open('Duplicate numeric values are not allowed', 'Close', { duration: 3000 });
      return;
    }

    this.enumsService.updateEnum(this.data.name, formValue.values).subscribe({
      next: () => {
        this.dialogRef.close(true);
      },
      error: (error) => {
        console.error('Failed to update enum:', error);
        const errorMessage = error.error?.message || 'Failed to update enum';
        this.snackBar.open(errorMessage, 'Close', { duration: 5000 });
      },
    });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  getNameError(index: number): string {
    const control = this.values.at(index).get('name');
    if (control?.hasError('required')) {
      return 'Name is required';
    }
    if (control?.hasError('pattern')) {
      return 'Name must start with uppercase letter and contain only alphanumeric characters';
    }
    return '';
  }

  getValueError(index: number): string {
    const control = this.values.at(index).get('value');
    if (control?.hasError('required')) {
      return 'Value is required';
    }
    if (control?.hasError('min')) {
      return 'Value must be non-negative';
    }
    return '';
  }
}