import { Location , formatDate} from '@angular/common';
import { Component, Inject, LOCALE_ID, OnDestroy, OnInit } from '@angular/core';
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
import { Subscription, take } from 'rxjs';
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

  fileStates: {
    [fieldKey: string]: {
      selectedFile: File | null;
      previewUrl: string | null;
      uploadStatus: string;
      touched: boolean;
    };
  } = {};

  maxFileSize: number = 3 * 1024 * 1024;

  constructor(
    private fb: FormBuilder,
    private location: Location,
    private store: Store,
    private actions$: Actions,
    @Inject(LOCALE_ID)private locale: string
  ) {
    this.contentForm = this.fb.group({});
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  get selectedFile(): File | null {
    const firstFileField = this.fileFields[0];
    if (!firstFileField) return null;
    return this.fileStates[firstFileField.key]?.selectedFile ?? null;
  }

  get filePreviewUrl(): string | null {
    const firstFileField = this.fileFields[0];
    if (!firstFileField) return null;
    return this.fileStates[firstFileField.key]?.previewUrl ?? null;
  }

  get uploadStatus(): string {
    const firstFileField = this.fileFields[0];
    if (!firstFileField) return 'No file field';
    return this.fileStates[firstFileField.key]?.uploadStatus || 'Ready to upload';
  }

  get fileFieldTouched(): boolean {
    const firstFileField = this.fileFields[0];
    if (!firstFileField) return false;
    return this.fileStates[firstFileField?.key]?.touched || false;
  }

  getRelationDisplayValue(item: any): string {
    if (!item) return '';

    const displayKeys = [
      'name',
      'title',
      'label',
      'displayName',
      'value',
      'Name',
      'Title',
      'Label',
      'DisplayName',
      'Value',
    ];

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
      const field = this.contentFields.find((f) => f.key === key);
      if (!field) return;

      let value = itemData[key];

      switch (field.type) {
        case FieldType.relation:
        case FieldType.enum:
          if (value == null) {
            const relationId = itemData[`${key}Id`];
            value =
              relationId != null && field.relatedItems?.length
                ? field.relatedItems.find((item: any) => item.Id === relationId) || null
                : null;
          }
          break;

        case FieldType.collection:
          if (Array.isArray(value) && field.relatedItems?.length) {
            value = value
              .map((dataItem: any) =>
                field.relatedItems!.find((relatedItem: any) => relatedItem.Id === dataItem.Id)
              )
              .filter((item: any) => item !== undefined);
          }
          break;

        case FieldType.file:
          if (value) {
            this.initializeFileState(key);

            const fileUrl = typeof value === 'object' ? value.Url : value;
            if (fileUrl) {
              this.fileStates[key].previewUrl = fileUrl;
              this.fileStates[key].uploadStatus = 'Complete';
              value = fileUrl;
            }
          }
          break;

        case FieldType.date:
          if (value) {
            value = new Date(value);
          }
          break;

        default:
          break;
      }

      if (value !== undefined) {
        this.contentForm.get(key)?.setValue(value);
      }
    });
  }

  private initializeFileState(key: string): void {
    if (!this.fileStates[key]) {
      this.fileStates[key] = {
        selectedFile: null,
        previewUrl: null,
        uploadStatus: 'Ready to upload',
        touched: false,
      };
    }
  }

  compareObjects(o1: any, o2: any): boolean {
    if (!o1 || !o2) {
      return o1 === o2;
    }

    if (o1.Id && o2.Id) {
      return o1.Id === o2.Id;
    }

    return o1 === o2;
  }

  ngOnInit(): void {
    this.subscriptions.add(
      this.headers$.subscribe((fields) => {
        this.fields = fields;
        console.log(FieldType);
        this.contentFields = this.fields
          .filter((field) => field.type !== FieldType.id)
          .map((field) => {
            console.log(field);
            const contentField: ContentField = {
              key: field.key,
              label: field.label,
              type: field.type,
              placeholder: `Enter ${field.label}`,
              required: field.isRequired,
              validators: field.isRequired ? [Validators.required] : undefined,
              relatedTo: field.relatedTo,
              relatedItems: [],
            };

            if (
              field.type === FieldType.collection ||
              field.type === FieldType.relation ||
              field.type === FieldType.enum
            ) {
              contentField.multiple = field.type === FieldType.collection;
              if (field.relatedTo) {
                this.store.dispatch(
                  ContentActions.loadRelatedItems({
                    selectedType: field.relatedTo,
                  })
                );
              }
            }

            if (field.type === FieldType.role) {
              contentField.multiple = true;
            }

            return contentField;
          });

        this.relationFields = this.contentFields.filter(
          (field) =>
            field.type === FieldType.collection ||
            field.type === FieldType.relation ||
            field.type === FieldType.enum
        );

        this.fileFields = this.contentFields.filter((field) => field.type === FieldType.file);

        this.fileFields.forEach((field) => {
          if (!this.fileStates[field.key]) {
            this.fileStates[field.key] = {
              selectedFile: null,
              previewUrl: null,
              uploadStatus: 'Ready to upload',
              touched: false,
            };
          }
        });

        const checkboxFields = this.contentFields.filter(
          (field) => field.type === FieldType.checkbox
        );
        const nonCheckboxNonFileFields = this.contentFields.filter(
          (field) => field.type !== FieldType.file && field.type !== FieldType.checkbox
        );

        const halfNonCheckboxLength = Math.ceil(nonCheckboxNonFileFields.length / 2);
        const halfCheckboxLength = Math.ceil(checkboxFields.length / 2);

        this.leftColumnFields = nonCheckboxNonFileFields.slice(0, halfNonCheckboxLength);
        this.rightColumnFields = nonCheckboxNonFileFields.slice(halfNonCheckboxLength);
        this.leftColumnCheckboxFields = checkboxFields.slice(0, halfCheckboxLength);
        this.rightColumnCheckboxFields = checkboxFields.slice(halfCheckboxLength);

        this.buildForm();
      })
    );

    this.subscription.add(
      this.relatedItems$.subscribe((relatedItemsMap) => {
        if (relatedItemsMap) {
          const hadRelatedItems = this.contentFields.some(
            (field) => field.relatedItems && field.relatedItems.length > 0
          );

          this.contentFields = this.contentFields.map((field) => {
            if (
              (field.type === FieldType.collection ||
                field.type === FieldType.relation ||
                field.type === FieldType.enum) &&
              field.relatedTo &&
              relatedItemsMap[field.relatedTo]
            ) {
              return {
                ...field,
                relatedItems: relatedItemsMap[field.relatedTo]?.Data || [],
              };
            } else return field;
          });

          this.relationFields = this.contentFields.filter(
            (field) =>
              field.type === FieldType.collection ||
              field.type === FieldType.relation ||
              field.type === FieldType.enum
          );

          this.fileFields = this.contentFields.filter((field) => field.type === FieldType.file);

          this.fileFields.forEach((field) => {
            if (!this.fileStates[field.key]) {
              this.fileStates[field.key] = {
                selectedFile: null,
                previewUrl: null,
                uploadStatus: 'Ready to upload',
                touched: false,
              };
            }
          });

          const checkboxFields = this.contentFields.filter(
            (field) => field.type === FieldType.checkbox
          );
          const nonCheckboxNonFileFields = this.contentFields.filter(
            (field) => field.type !== FieldType.file && field.type !== FieldType.checkbox
          );

          const halfNonCheckboxLength = Math.ceil(nonCheckboxNonFileFields.length / 2);
          const halfCheckboxLength = Math.ceil(checkboxFields.length / 2);

          this.leftColumnFields = nonCheckboxNonFileFields.slice(0, halfNonCheckboxLength);
          this.rightColumnFields = nonCheckboxNonFileFields.slice(halfNonCheckboxLength);
          this.leftColumnCheckboxFields = checkboxFields.slice(0, halfCheckboxLength);
          this.rightColumnCheckboxFields = checkboxFields.slice(halfCheckboxLength);

          this.buildForm();
          if (this.currentItem !== undefined) {
            setTimeout(() => {
              this.populateFormWithData(this.currentItem);
            }, 0);
          }
        }
      })
    );
    this.subscriptions.add(this.selectedType$.subscribe((type) => (this.selectedType = type)));

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

      if (field.type === FieldType.checkbox) {
        group[field.key] = [false, validators];
      } else if (
        field.type === FieldType.collection ||
        field.type === FieldType.relation ||
        field.type === FieldType.enum ||
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
    if (!this.fileStates[fieldKey]) {
      this.fileStates[fieldKey] = {
        selectedFile: null,
        previewUrl: null,
        uploadStatus: 'Ready to upload',
        touched: false,
      };
    }

    this.fileStates[fieldKey].touched = true;

    if (event.target.files && event.target.files.length) {
      const file = event.target.files[0];

      if (file.size > this.maxFileSize) {
        this.fileStates[fieldKey].uploadStatus = 'File too large (max 3MB)';
        return;
      }

      this.fileStates[fieldKey].selectedFile = file;
      this.fileStates[fieldKey].uploadStatus = 'Ready to upload';

      if (file.type.startsWith('image/')) {
        const reader = new FileReader();
        reader.onload = () => {
          this.fileStates[fieldKey].previewUrl = reader.result as string;
        };
        reader.readAsDataURL(file);
      }

      this.contentForm.get(fieldKey)?.setValue('pending-upload');
    }
  }

  resetFile(): void {
    this.fileFields.forEach((field) => {
      if (this.fileStates[field.key]) {
        this.fileStates[field.key] = {
          selectedFile: null,
          previewUrl: null,
          uploadStatus: 'Ready to upload',
          touched: false,
        };
      }
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
      .every((field) => {
        const fileState = this.fileStates[field.key];
        return fileState && (fileState.selectedFile !== null || fileState.previewUrl !== null);
      });

    return isBasicFormValid && requiredFileFieldsValid;
  }

  resetForm(): void {
    this.contentForm.reset();
    this.resetFile();
  }

  onSubmit(): void {
    Object.keys(this.contentForm.controls).forEach((key) => {
      this.contentForm.get(key)?.markAsTouched();
    });
    this.fileFields.forEach((field) => {
      if (this.fileStates[field.key]) {
        this.fileStates[field.key].touched = true;
      }
    });

    if (this.isFormValid()) {
      const formData = {} as any;

      Object.keys(this.contentForm.value).forEach((key) => {
        const value = this.contentForm.value[key];
        const field = this.contentFields.find((f) => f.key === key);

        if (field?.type === FieldType.file) {
          return;
        } else if (field?.type === FieldType.relation || field?.type === FieldType.enum) {
          if (value) {
            if (!field.multiple && value.Id) {
              const relationKey = `${key}Id`;
              formData[relationKey] = value.Id;
            } else if (!field.multiple && this.isEnumLikeObject(value)) {
              const enumValue = this.extractEnumValue(value);
              if (enumValue !== null) {
                formData[key] = enumValue;
              } else {
                formData[key] = value;
              }
            } else if (field.multiple && Array.isArray(value)) {
              formData[key] = value.map((item: any) => (item.Id ? { Id: item.Id } : item));
            } else if (value.Id) {
              const relationKey = `${key}Id`;
              formData[relationKey] = value.Id;
            } else {
              formData[key] = value;
            }
          }
        } else if (field?.type === FieldType.collection && Array.isArray(value)) {
          formData[key] = value;
        } else if (field?.type === FieldType.role) {
          formData[key] = Array.isArray(value) ? value : value ? [value] : [];
        } else if (field?.type === FieldType.date && value) {
          if (value instanceof Date) {
            formData[key] = value.toISOString();
          } else {
            formData[key] = value;
          }
        } else if(field?.type === FieldType.dateonly && value){
          formData[key] = formatDate(value, "yyyy-MM-dd" , this.locale);
        }else {
          formData[key] = value;
        }
      });
      this.submitToBackend(formData);
    }
  }

  private isEnumLikeObject(obj: any): boolean {
    if (!obj || typeof obj !== 'object' || Array.isArray(obj) || obj.Id !== undefined) {
      return false;
    }

    const keys = Object.keys(obj);
    if (keys.length !== 1) {
      return false;
    }

    const value = obj[keys[0]];
    return typeof value === 'number';
  }

  private extractEnumValue(obj: any): number | null {
    if (!this.isEnumLikeObject(obj)) {
      return null;
    }

    const keys = Object.keys(obj);
    return obj[keys[0]];
  }

  private submitToBackend(formData: any): void {
    if (this.currentItem !== undefined) {
      this.store.dispatch(
        ContentActions.updateContent({
          id: this.currentItem.Id,
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

    this.subscriptions.add(
      this.actions$
        .pipe(ofType(ContentActions.createContentSuccess), take(1))
        .subscribe((action) => {
          const newId = action.id;
          if (newId && this.selectedFile) {
            this.uploadFileIfExists(newId);
          } else {
            this.finishSubmission();
          }
        })
    );
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
        })
      );

      this.subscription.add(
        this.actions$
          .pipe(ofType(ContentActions.uploadFileSuccess), take(1))
          .subscribe((action) => {
            this.finishSubmission();
          })
      );
    } else {
      this.finishSubmission();
    }
  }

  private finishSubmission(): void {
    this.store.dispatch(ContentActions.setCurrentItem({ currentItem: undefined }));
    this.resetForm();
    this.location.back();
  }
}
