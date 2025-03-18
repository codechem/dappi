import { CommonModule, Location } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
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
import { ContentItem, TableHeader } from '../models/content.model';
import { Store } from '@ngrx/store';
import {
  selectCurrentItem,
  selectHeaders,
  selectSelectedType,
} from '../state/content/content.selectors';
import * as ContentActions from '../state/content/content.actions';
import { Subscription } from 'rxjs';

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
export class NewRecordFormComponent implements OnInit, OnDestroy {
  private subscriptions: Subscription = new Subscription();

  fields: TableHeader[] = [];
  selectedType = '';

  headers$ = this.store.select(selectHeaders);
  selectedType$ = this.store.select(selectSelectedType);
  currentItem: ContentItem | undefined = undefined;

  private subscription: Subscription = new Subscription();
  currentItem$ = this.store.select(selectCurrentItem);

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
    private location: Location,
    private store: Store
  ) {
    this.contentForm = this.fb.group({});
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  populateFormWithData(itemData: any): void {
    Object.keys(this.contentForm.controls).forEach((key) => {
      if (itemData[key] !== undefined) {
        this.contentForm.get(key)?.setValue(itemData[key]);
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
    this.subscriptions.add(
      this.headers$.subscribe((fields) => {
        this.fields = fields;
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
      })
    );
    this.subscriptions.add(
      this.selectedType$.subscribe((type) => (this.selectedType = type))
    );

    this.subscription.add(
      this.currentItem$.subscribe((item) => {
        if (item !== undefined) {
          this.currentItem = item;
          this.populateFormWithData(item);
        }
      })
    );
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
    if (this.currentItem !== undefined) {
      this.store.dispatch(
        ContentActions.updateContent({
          id: this.currentItem.id,
          formData: formData,
          contentType: this.selectedType,
        })
      );
    } else {
      this.store.dispatch(
        ContentActions.createContent({
          formData: formData,
          contentType: this.selectedType,
        })
      );
    }
    this.store.dispatch(
      ContentActions.setCurrentItem({ currentItem: undefined })
    );
    this.resetForm();
    this.location.back();
    this.store.dispatch(
      ContentActions.loadContent({
        selectedType: this.selectedType ?? '',
        page: 1,
        limit: 10,
        searchText: '',
      })
    );
  }
}
