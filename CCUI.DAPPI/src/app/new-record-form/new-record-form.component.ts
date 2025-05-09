import { Location } from '@angular/common';
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
import { ContentItem, FieldType, TableHeader } from '../models/content.model';
import { Store } from '@ngrx/store';
import {
  selectCurrentItem,
  selectHeaders,
  selectItems,
  selectRelatedItems,
  selectSelectedType,
} from '../state/content/content.selectors';
import * as ContentActions from '../state/content/content.actions';
import { map, Subscription, take } from 'rxjs';
import { MatOption } from '@angular/material/core';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatDatepickerToggle } from '@angular/material/datepicker';
import { Actions, ofType } from '@ngrx/effects';

interface ContentField {
  key: string;
  label: string;
  type: FieldType;
  placeholder?: string;
  required: boolean;
  validators?: any[];
  maxFileSize?: number;
  acceptedFileTypes?: string[];
  relatedItems?: any[];
  multiple?: boolean;
  relatedTo?: string;
}

@Component({
  selector: 'app-new-record-form',
  standalone: true,
  imports: [
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatButtonModule,
    FormsModule,
    ReactiveFormsModule,
    MatOption,
    MatSelectModule,
    MatCheckboxModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatDatepickerToggle,
  ],
  templateUrl: './new-record-form.component.html',
  styleUrls: ['./new-record-form.component.scss'],
})
export class NewRecordFormComponent implements OnInit, OnDestroy {
  private subscriptions: Subscription = new Subscription();

  fields: TableHeader[] = [];
  selectedType = '';
  fieldType = FieldType;
  headers$ = this.store.select(selectHeaders);
  selectedType$ = this.store.select(selectSelectedType);
  currentItem: ContentItem | undefined = undefined;
  relationFields: ContentField[] = [];
  contentTypes$ = this.store.select(selectItems);
  relatedItems$ = this.store.select(selectRelatedItems);

  private subscription: Subscription = new Subscription();
  currentItem$ = this.store.select(selectCurrentItem);

  contentForm: FormGroup;
  contentFields: ContentField[] = [];
  leftColumnFields: ContentField[] = [];
  rightColumnFields: ContentField[] = [];
  fileFields: ContentField[] = [];
  leftColumnCheckboxFields: ContentField[] = [];
  rightColumnCheckboxFields: ContentField[] = [];

  selectedFile: File | null = null;
  filePreviewUrl: string | null = null;
  uploadStatus: string = 'Ready to upload';
  fileFieldTouched: boolean = false;

  maxFileSize: number = 3 * 1024 * 1024;

  constructor(
    private fb: FormBuilder,
    private location: Location,
    private store: Store,
    private actions$: Actions,
  ) {
    this.contentForm = this.fb.group({});
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  getRelationDisplayValue(item: any, field: ContentField): string {
    if (!item) return '';

    const displayKeys = ['name', 'title', 'label', 'displayName', 'value'];

    for (const key of displayKeys) {
      if (item[key] !== undefined && item[key] !== null) {
        return String(item[key]);
      }
    }

    if (item.id) return item.id;

    return JSON.stringify(item);
  }

  populateFormWithData(itemData: any): void {
    Object.keys(this.contentForm.controls).forEach((key) => {
      if (itemData[key] !== undefined) {
        const field = this.contentFields.find((f) => f.key === key);
        if (field && field.type === FieldType.date && itemData[key]) {
          const dateValue = new Date(itemData[key]);
          this.contentForm.get(key)?.setValue(dateValue);
        } else {
          this.contentForm.get(key)?.setValue(itemData[key]);
        }
      }
    });

    const fileField = this.fileFields[0]?.key;
    if (fileField && itemData[fileField] && typeof itemData[fileField] === 'string') {
      this.filePreviewUrl = itemData[fileField];
      this.uploadStatus = 'Complete';
    }
  }

  ngOnInit(): void {
    this.subscriptions.add(
      this.headers$.subscribe((fields) => {
        this.fields = fields;
        this.contentFields = this.fields
          .filter((field) => field.type !== FieldType.id)
          .map((field) => {
            const contentField: ContentField = {
              key: field.key,
              label: field.label,
              type: field.type,
              placeholder: `Enter ${field.label}`,
              required: field.isRequired,
              validators: field.isRequired ? [Validators.required] : undefined,
              relatedTo: field.relatedTo,
            };

            if (field.type === FieldType.collection || field.type === FieldType.relation) {
              contentField.multiple = field.type === FieldType.collection;
              if (field.relatedTo) {
                this.store.dispatch(
                  ContentActions.loadRelatedItems({
                    selectedType: field.relatedTo,
                  }),
                );
              }
            }

            if (field.type === FieldType.role) {
              contentField.multiple = true;
            }

            return contentField;
          });

        this.relationFields = this.contentFields.filter(
          (field) => field.type === FieldType.collection || field.type === FieldType.relation,
        );

        this.fileFields = this.contentFields.filter((field) => field.type === FieldType.file);

        const checkboxFields = this.contentFields.filter(
          (field) => field.type === FieldType.checkbox,
        );
        const nonCheckboxNonFileFields = this.contentFields.filter(
          (field) => field.type !== FieldType.file && field.type !== FieldType.checkbox,
        );

        const halfNonCheckboxLength = Math.ceil(nonCheckboxNonFileFields.length / 2);
        const halfCheckboxLength = Math.ceil(checkboxFields.length / 2);

        this.leftColumnFields = nonCheckboxNonFileFields.slice(0, halfNonCheckboxLength);
        this.rightColumnFields = nonCheckboxNonFileFields.slice(halfNonCheckboxLength);
        this.leftColumnCheckboxFields = checkboxFields.slice(0, halfCheckboxLength);
        this.rightColumnCheckboxFields = checkboxFields.slice(halfCheckboxLength);

        this.buildForm();
      }),
    );

    this.subscription.add(
      this.relatedItems$.subscribe((items) => {
        this.contentFields = this.contentFields.map((item) => {
          if (
            item.type === FieldType.collection ||
            item.type === FieldType.relation ||
            item.type === FieldType.select
          ) {
            return {
              ...item,
              relatedItems: items?.data,
            };
          } else return item;
        });

        this.relationFields = this.contentFields.filter(
          (field) => field.type === FieldType.collection || field.type === FieldType.relation,
        );

        this.fileFields = this.contentFields.filter((field) => field.type === FieldType.file);

        const checkboxFields = this.contentFields.filter(
          (field) => field.type === FieldType.checkbox,
        );
        const nonCheckboxNonFileFields = this.contentFields.filter(
          (field) => field.type !== FieldType.file && field.type !== FieldType.checkbox,
        );

        const halfNonCheckboxLength = Math.ceil(nonCheckboxNonFileFields.length / 2);
        const halfCheckboxLength = Math.ceil(checkboxFields.length / 2);

        this.leftColumnFields = nonCheckboxNonFileFields.slice(0, halfNonCheckboxLength);
        this.rightColumnFields = nonCheckboxNonFileFields.slice(halfNonCheckboxLength);
        this.leftColumnCheckboxFields = checkboxFields.slice(0, halfCheckboxLength);
        this.rightColumnCheckboxFields = checkboxFields.slice(halfCheckboxLength);

        this.buildForm();
      }),
    );
    this.subscriptions.add(this.selectedType$.subscribe((type) => (this.selectedType = type)));

    this.subscription.add(
      this.currentItem$.subscribe((item) => {
        if (item !== undefined) {
          this.currentItem = item;
          this.populateFormWithData(item);
        }
      }),
    );
  }

  buildForm(): void {
    const group: any = {};

    this.contentFields.forEach((field) => {
      const validators = field.required
        ? [Validators.required, ...(field.validators || [])]
        : field.validators || [];

      if (field.type === FieldType.checkbox) {
        group[field.key] = [false, validators];
      } else if (
        field.type === FieldType.collection ||
        field.type === FieldType.relation ||
        field.type === FieldType.role
      ) {
        group[field.key] = [field.multiple ? [] : null, validators];
      } else if (field.type === FieldType.number) {
        const numberValidators = [...validators];
        numberValidators.push(Validators.pattern('^[0-9]+(\\.[0-9]+)?$'));
        group[field.key] = [0, numberValidators];
      } else if (field.type === FieldType.date) {
        group[field.key] = [null, validators];
      } else {
        group[field.key] = ['', validators];
      }
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
      this.uploadStatus = 'Ready to upload';

      if (file.type.startsWith('image/')) {
        const reader = new FileReader();
        reader.onload = () => {
          this.filePreviewUrl = reader.result as string;
        };
        reader.readAsDataURL(file);
      }

      this.contentForm.get(fieldKey)?.setValue('pending-upload');
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
      const formData = Object.keys(this.contentForm.value).reduce((acc: any, key) => {
        const value = this.contentForm.value[key];
        const field = this.contentFields.find((f) => f.key === key);

        if (field?.type === FieldType.file) {
          return acc;
        } else if (field?.type === FieldType.relation && value && value.id) {
          const relationKey = this.fields.find(
            (f) => f.key.includes(field.key) && f.type === FieldType.id,
          )?.key;
          if (relationKey) acc[relationKey] = value.id;
        } else if (field?.type === FieldType.role) {
          acc[key] = Array.isArray(value) ? value : value ? [value] : [];
        } else if (field?.type === FieldType.date && value) {
          if (value instanceof Date) {
            acc[key] = value.toISOString();
          } else {
            acc[key] = value;
          }
        } else if (typeof value === 'object' && value !== null && !Array.isArray(value)) {
          acc[key] = [{ ...value }];
        } else {
          acc[key] = value;
        }

        return acc;
      }, {});

      this.submitToBackend(formData);
    }
  }

  private submitToBackend(formData: any): void {
    if (this.currentItem === undefined) {
      this.store.dispatch(
        ContentActions.createContent({
          formData: formData,
          contentType: this.selectedType,
        }),
      );

      this.subscriptions.add(
        this.actions$
          .pipe(ofType(ContentActions.createContentSuccess), take(1))
          .subscribe((action) => {
            const newId = action.id;
            if (newId && this.selectedFile) {
              this.uploadFileIfExists(newId);
            }
          }),
      );
    }
  }

  private uploadFileIfExists(recordId: string): void {
    if (this.selectedFile && this.fileFields.length > 0) {
      const fieldKey = this.fileFields[0].key;

      this.store.dispatch(
        ContentActions.uploadFile({
          id: recordId,
          file: this.selectedFile,
          fieldName: fieldKey,
          contentType: this.selectedType,
        }),
      );

      this.subscription.add(
        this.actions$
          .pipe(ofType(ContentActions.uploadFileSuccess), take(1))
          .subscribe((action) => {
            this.finishSubmission();
          }),
      );
    } else {
      this.subscription.add(
        this.actions$
          .pipe(ofType(ContentActions.createContentSuccess), take(1))
          .subscribe((action) => {
            this.finishSubmission();
          }),
      );
    }
  }

  private finishSubmission(): void {
    this.store.dispatch(ContentActions.setCurrentItem({ currentItem: undefined }));
    this.resetForm();
    this.location.back();
  }
}
