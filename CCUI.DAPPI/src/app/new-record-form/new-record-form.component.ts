import { CommonModule, Location } from '@angular/common';
import { Component, Input, OnInit } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { HttpClient } from '@angular/common/http';
import { ContentStateService } from '../content-manager/content-state.service';
import { TableHeader } from '../models/content.model';

interface ContentField {
  key: string;
  label: string;
  type: 'text' | 'textarea' | 'file';
  placeholder?: string;
  required: boolean;
  validators?: any[];
  maxFileSize?: number;
  acceptedFileTypes?: string[];
}

@Component({
  selector: 'app-new-record-form',
  standalone: true,
  imports: [
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatButtonModule,
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
  ],
  templateUrl: './new-record-form.component.html',
  styleUrls: ['./new-record-form.component.scss'],
})
export class NewRecordFormComponent implements OnInit {
  @Input() selectedType = 'Article';
  @Input() fields: TableHeader[] = [];

  contentForm: FormGroup;
  contentFields: ContentField[] = [];
  leftColumnFields: ContentField[] = [];
  rightColumnFields: ContentField[] = [];
  fileFields: ContentField[] = [];

  selectedFile: File | null = null;
  filePreviewUrl: string | null = null;
  uploadStatus: string = 'Ready to upload';
  fileFieldTouched: boolean = false;

  maxFileSize: number = 3 * 1024 * 1024;

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private contentStateService: ContentStateService,
    private location: Location
  ) {
    this.contentForm = this.fb.group({});
  }

  populateFormWithData(itemData: any): void {
    Object.keys(this.contentForm.controls).forEach((key) => {
      if (itemData['_value'][key] !== undefined) {
        this.contentForm.get(key)?.setValue(itemData['_value'][key]);
      }
    });

    const fileField = this.fileFields[0]?.key;
    if (
      fileField &&
      itemData[fileField] &&
      typeof itemData[fileField] === 'string'
    ) {
      this.filePreviewUrl = itemData[fileField];
      this.uploadStatus = 'Complete';
    }
  }

  ngOnInit(): void {
    this.fields = this.contentStateService.getHeaders();
    this.selectedType = this.contentStateService.getSelectedType();
    const itemData = this.contentStateService.getItemData();

    this.contentFields = this.fields
      .filter((field) => field.key !== 'id')
      .map((field) => ({
        key: field.key,
        label: field.label,
        type: field.type,
        placeholder: `Enter ${field.label}`,
        required: true,
        validators: [Validators.required],
      }));

    this.fileFields = this.contentFields.filter(
      (field) => field.type === 'file'
    );

    const nonFileFields = this.contentFields.filter(
      (field) => field.type !== 'file'
    );
    const halfLength = Math.ceil(nonFileFields.length / 2);

    this.leftColumnFields = nonFileFields.slice(0, halfLength);
    this.rightColumnFields = nonFileFields.slice(halfLength);

    this.buildForm();

    if (itemData) {
      this.populateFormWithData(itemData);
    }
  }

  buildForm(): void {
    const group: any = {};

    this.contentFields.forEach((field) => {
      const validators = field.required
        ? [Validators.required, ...(field.validators || [])]
        : field.validators || [];
      group[field.key] = ['', validators];
    });

    this.contentForm = this.fb.group(group);
  }

  onFileChange(event: any, fieldKey: string): void {
    this.fileFieldTouched = true;

    if (event.target.files && event.target.files.length) {
      const file = event.target.files[0];

      if (file.size > this.maxFileSize) {
        this.uploadStatus = 'File too large (max 3MB)';
        return;
      }

      this.selectedFile = file;
      this.uploadStatus = 'Complete';

      if (file.type.startsWith('image/')) {
        const reader = new FileReader();
        reader.onload = () => {
          this.filePreviewUrl = reader.result as string;
        };
        reader.readAsDataURL(file);
      }

      this.contentForm.get(fieldKey)?.setValue(file);
    }
  }

  resetFile(): void {
    this.selectedFile = null;
    this.filePreviewUrl = null;
    this.uploadStatus = 'Ready to upload';

    this.fileFields.forEach((field) => {
      this.contentForm.get(field.key)?.setValue(null);
    });
  }

  getFileSize(file: File | null): string {
    if (!file) return '0 KB';

    if (file.size < 1024) {
      return `${file.size} B`;
    } else if (file.size < 1024 * 1024) {
      return `${(file.size / 1024).toFixed(0)} KB`;
    } else {
      return `${(file.size / (1024 * 1024)).toFixed(1)} MB`;
    }
  }

  getErrorMessage(fieldKey: string): string {
    const control = this.contentForm.get(fieldKey);

    if (!control || !control.errors) return '';

    if (control.errors['required']) {
      return 'This field is required';
    }

    if (control.errors['maxlength']) {
      const maxLength = control.errors['maxlength'].requiredLength;
      return `Maximum length is ${maxLength} characters`;
    }

    if (control.errors['pattern']) {
      return 'Invalid format';
    }

    return 'Invalid input';
  }

  isFormValid(): boolean {
    const isBasicFormValid = this.contentForm.valid;

    const requiredFileFieldsValid = this.fileFields
      .filter((field) => field.required)
      .every((field) => this.selectedFile !== null);

    return isBasicFormValid && requiredFileFieldsValid;
  }

  resetForm(): void {
    this.contentForm.reset();
    this.resetFile();
    this.fileFieldTouched = false;
  }

  onSubmit(): void {
    Object.keys(this.contentForm.controls).forEach((key) => {
      this.contentForm.get(key)?.markAsTouched();
    });
    this.fileFieldTouched = true;

    if (this.isFormValid()) {
      const formData = {
        id: crypto.randomUUID(),
        ...this.contentForm.value,
      };

      this.submitToBackend(formData);
    }
  }

  private submitToBackend(formData: any): void {
    const existingItem = this.contentStateService.getItemData();

    const endpoint = `http://localhost:5101/api/${this.selectedType
      .toLowerCase()
      .replace(/\s+/g, '-')}`;

    if (existingItem['_value']) {
      this.http
        .put(`${endpoint}/${existingItem['_value'].id}`, formData)
        .subscribe({
          next: (response) => {
            console.log('Successfully updated:', response);
            this.resetForm();
            this.location.back();
          },
          error: (error) => {
            console.error('Error updating record:', error);
            this.location.back();
          },
        });
    } else {
      this.http.post(endpoint, formData).subscribe({
        next: (response) => {
          console.log('Successfully created:', response);
          this.resetForm();
          this.location.back();
        },
        error: (error) => {
          console.error('Error creating record:', error);
          this.location.back();
        },
      });
    }
  }
}
