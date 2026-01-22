import { Component, Inject, OnDestroy, OnInit } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { ErrorStateMatcher } from '@angular/material/core';
import {
  FormsModule,
  Validators,
  FormBuilder,
  FormGroup,
  FormControl,
  FormGroupDirective,
  NgForm,
  ReactiveFormsModule,
} from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from '@angular/common';
import * as CollectionActions from '../state/collection/collection.actions';
import { Store } from '@ngrx/store';
import { MatSelectModule } from '@angular/material/select';
import { selectCollectionTypes, selectFields } from '../state/collection/collection.selectors';
import { selectSelectedType } from '../state/content/content.selectors';
import { Subscription } from 'rxjs';
import { EnumsResponse } from '../models/enums-response.model';
import { EnumsService } from '../services/common/enums.service';
import { EnumManagementService } from '../services/common/enum-management.service';
import { ModelValidators } from '../validators/model-validators';
import { forkJoin } from 'rxjs';
import { Pluralizer } from '../utils/pluralizer'
import { FieldTypeEnum } from '../enums/fieldType';

interface FieldType {
  type: FieldTypeEnum;
  icon: string;
  label: string;
  description: string;
  value: string;
  netType: string;
}

export interface AddFieldDialogData {
  selectedType: string;
  editMode?: boolean;
  fieldName?: string;
  fieldType?: string;
  isRequired?: boolean;
  hasIndex?: boolean;
  regex?: string;
  noPastDates?: boolean;
  min?: number | null;
  max?: number | null;
}

class MinValueErrorStateMatcher implements ErrorStateMatcher {
  constructor(
    private form: FormGroup,
    private invalidErrorKey: string,
    private crossFieldErrorKey: string
  ) {}

  isErrorState(control: FormControl | null, form: FormGroupDirective | NgForm | null): boolean {
    const isSubmitted = !!form?.submitted;
    const show = isSubmitted || !!control && (control.touched || control.dirty);
    const controlInvalid = !!control && control.invalid && show;
    const minInvalid = !!control?.hasError(this.invalidErrorKey);
    const crossFieldInvalid = this.form.hasError(this.crossFieldErrorKey) && !minInvalid;

    return controlInvalid || (show && crossFieldInvalid);
  }
}

@Component({
  selector: 'app-add-field-dialog',
  standalone: true,
  imports: [
    MatDialogModule,
    MatFormFieldModule,
    MatSelectModule,
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
export class AddFieldDialogComponent implements OnInit, OnDestroy {
  selectedType$ = this.store.select(selectSelectedType);
  selectedTypeFields$ = this.store.select(selectFields);
  selectedType = '';
  isEditMode = false;
  originalFieldName = '';

  selectedRelationTypeIndex: number | null = null;
  selectedRelationType = 'ManyToMany';
  relationTypeMap: Record<number, string> = {
    0: 'ManyToMany',
    1: 'OneToMany',
    2: 'ManyToOne',
    3: 'OneToOne'
  };

  availableModels: { label: string; value: string }[] = [];
  availableEnums: string[] = [];
  selectedEnum: string = '';

  minTextErrorStateMatcher!: ErrorStateMatcher;
  minNumberErrorStateMatcher!: ErrorStateMatcher;


  fieldTypes: FieldType[] = [
    {
      type: FieldTypeEnum.String,
      icon: 'Aa',
      label: 'Text',
      description: 'For single or multi-line text input',
      value: 'text',
      netType: 'string',
    },
    {
      type: FieldTypeEnum.Number,
      icon: '123',
      label: 'Number',
      description: 'For numerical values and calculations',
      value: 'number',
      netType: 'int',
    },
    {
      type: FieldTypeEnum.Date,
      icon: 'calendar_today',
      label: 'Date',
      description: 'For selecting dates',
      value: 'date',
      netType: 'DateOnly',
    },
    {
      type: FieldTypeEnum.Media,
      icon: 'perm_media',
      label: 'Media',
      description: 'For uploading images or videos',
      value: 'media',
      netType: 'MediaInfo',
    },
    {
      type: FieldTypeEnum.Link,
      icon: 'link',
      label: 'Link',
      description: 'For website URLs or references',
      value: 'link',
      netType: 'string',
    },
    {
      type: FieldTypeEnum.Dropdown,
      icon: 'list',
      label: 'Dropdown',
      description: 'For selecting from predefined options(Enumerations)',
      value: 'dropdown',
      netType: this.selectedEnum ?? 'Enum',
    },
    {
      type: FieldTypeEnum.Checkbox,
      icon: 'check_box',
      label: 'Checkbox',
      description: 'For yes/no or true/false values',
      value: 'checkbox',
      netType: 'bool',
    },
    {
      type: FieldTypeEnum.DateTime,
      icon: 'today',
      label: 'DateTime',
      description: 'For date and time values together',
      value: 'datetime',
      netType: 'DateTime',
    },
    {
      type: FieldTypeEnum.Relation,
      icon: 'leak_remove',
      label: 'Relation',
      description: 'Create relation between models',
      value: 'relation',
      netType: this.selectedRelationType,
    },
    {
      type: FieldTypeEnum.Float,
      icon: '123',
      label: 'Float Number',
      description: 'For float numerical values and calculations',
      value: 'float',
      netType: 'float',
    },
    {
      type: FieldTypeEnum.Double,
      icon: '123',
      label: 'Double Number',
      description: 'For double numerical values and calculations',
      value: 'double',
      netType: 'double',
    }
  ];
  private subscription: Subscription = new Subscription();

  relatedTo: string | undefined = '';

  fieldForm: FormGroup = this.fb.group({
    fieldName: ['', [Validators.required, Validators.maxLength(50)]],
    requiredField: [false],
    relatedModel: [''],
    relatedRelationName: [''],
    hasIndex: [false],
    noPastDates: [false],
    min: [null],
    max: [null],
  });

  selectedFieldTypeId: FieldTypeEnum | null = null;
  fieldTypeEnum = FieldTypeEnum;

  collectionTypes$ = this.store.select(selectCollectionTypes);

  constructor(
    private dialogRef: MatDialogRef<AddFieldDialogComponent>,
    private fb: FormBuilder,
    private enumsService: EnumsService,
    private enumManagementService: EnumManagementService,
    @Inject(MAT_DIALOG_DATA) public data: AddFieldDialogData,
    private store: Store
  ) {
    this.isEditMode = data.editMode || false;
    this.originalFieldName = data.fieldName || '';
    
    const asyncValidators = this.isEditMode
      ? [ModelValidators.fieldNameIsTaken(this.selectedTypeFields$, data.fieldName)]
      : [ModelValidators.fieldNameIsTaken(this.selectedTypeFields$)];
    
    this.fieldForm = this.fb.group({
      fieldName: [
        data.fieldName || '',
        {
          validators: [
            Validators.required,
            Validators.maxLength(50),
            ModelValidators.pascalCase,
            ModelValidators.reservedKeyword,
            ModelValidators.collectionNameIsTaken
          ],
          asyncValidators: asyncValidators,
        },
      ],
      requiredField: [data.isRequired || false],
      relatedModel: [''],
      relatedRelationName: [''],
      regex: [data.regex || '', [ModelValidators.validRegex]],
      hasIndex: [data.hasIndex || false],
      noPastDates: [data.noPastDates || false],
      min: [data.min ?? null, [ModelValidators.validMinValue, ModelValidators.validNumericInput]],
      max: [data.max ?? null, [ModelValidators.validMaxValue, ModelValidators.validNumericInput]]
    }, { validators: [ModelValidators.minMaxValueValidator] });

    this.minTextErrorStateMatcher = new MinValueErrorStateMatcher(
      this.fieldForm,
      'invalidMinTextValue',
      'minValueGreaterThanMaxValue'
    );
    this.minNumberErrorStateMatcher = new MinValueErrorStateMatcher(
      this.fieldForm,
      'invalidMinValue',
      'minValueGreaterThanMaxValue'
    );
    
    if (this.isEditMode && data.fieldType) {
      this.preselectFieldType(data.fieldType);
      this.fieldForm.get('relatedModel')?.clearValidators();
      this.fieldForm.get('relatedRelationName')?.clearValidators();
      this.fieldForm.get('relatedModel')?.updateValueAndValidity();
      this.fieldForm.get('relatedRelationName')?.updateValueAndValidity();
    }

    this.updateMinMaxValidators();
  }

  relationTypes: {
    label: string;
    description: string;
    value: string;
  }[] = [];

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }
  ngOnInit(): void {
    forkJoin({
      generatedEnums: this.enumsService.getEnums(),
      userCreatedEnums: this.enumManagementService.getAllEnums()
    }).subscribe({
      next: (result) => {
        const generated = Object.keys(result.generatedEnums || {});
        const userCreated = Object.keys(result.userCreatedEnums || {});
        const combined = [...generated, ...userCreated];
        this.availableEnums = [...new Set(combined)].sort();
      },
      error: (error) => {
        console.error('Failed to load enums:', error);
        this.enumsService.getEnums().subscribe({
          next: (result: EnumsResponse) => {
            this.availableEnums = Object.keys(result || {});
          },
          error: (err) => {
            console.error('Fallback enum loading failed:', err);
            this.availableEnums = [];
          }
        });
      }
    });
    this.fieldForm.get('relatedModel')?.valueChanges.subscribe((value) => {
      this.selectedEnum = value;
    });
    this.subscription.add(
      this.collectionTypes$.subscribe(
        (types) =>
        (this.availableModels = types
          .filter((m) => m != this.selectedType)
          .map((type) => ({
            label: type,
            value: type,
          })))
      )
    );

    this.subscription.add(
      this.fieldForm.get('relatedModel')?.valueChanges.subscribe((relatedModelValue) => {
        this.updateRelationTypes(relatedModelValue);
      })
    );

    this.subscription.add(
      this.selectedType$.subscribe((type) => {
        this.availableModels = this.availableModels
          .filter((m) => m.value != type)
          .map((type) => ({
            label: type.label,
            value: type.value,
          }));
        this.selectedType = type;
        this.updateRelationTypes();
      })
    );
    this.fieldForm.get('fieldName')?.addValidators(ModelValidators.fieldNameSameAsModel(this.selectedType));
    
    if (!this.isEditMode) {
      this.fieldForm.get('relatedModel')?.setValidators([Validators.required]);
    }
    
    this.updateRelationTypes();
  }

  private updateRelationTypes(relatedModel?: string): void {
    const modelName = relatedModel || '/';
    this.relatedTo = relatedModel;
    const pluralSelected = Pluralizer.pluralizeEN(this.selectedType);
    const pluralRelated = Pluralizer.pluralizeEN(modelName);

    this.relationTypes = [
      {
        label: 'Many-to-many',
        description: `Many ${pluralSelected} can relate to many ${pluralRelated}`,
        value: 'many-to-many',
      },
      {
        label: 'One-to-many',
        description: `One ${this.selectedType} can relate to many ${pluralRelated}`,
        value: 'one-to-many',
      },
      {
        label: 'Many-to-one',
        description: `Many ${pluralSelected} can relate to one ${modelName}`,
        value: 'many-to-one',
      },
      {
        label: 'One-to-one',
        description: `One ${this.selectedType} relates to one ${modelName}`,
        value: 'one-to-one',
      },
    ];
  }

  selectRelationType(index: number): void {
    this.selectedRelationTypeIndex = index;
    this.selectedRelationType = this.relationTypeMap[index];
  }

  selectFieldType(id: FieldTypeEnum): void {
    this.selectedFieldTypeId = id;

    if (id === this.fieldTypeEnum.Relation) {
      this.fieldForm.get('relatedModel')?.setValidators([Validators.required]);
      this.fieldForm.get('relatedRelationName')?.setValidators([Validators.required]);
    } else {
      this.fieldForm.get('relatedModel')?.clearValidators();
      this.fieldForm.get('relatedRelationName')?.clearValidators();
    }

    this.fieldForm.get('relatedModel')?.updateValueAndValidity();
    this.fieldForm.get('relatedRelationName')?.updateValueAndValidity();
    this.selectedRelationTypeIndex = null;
    this.updateMinMaxValidators();
  }

  onAddField(): void {
    if (!this.fieldForm.valid || this.selectedFieldTypeId === null) {
      this.fieldForm.markAllAsTouched();
      return;
    }

    if (this.isEditMode) {
      if (!this.hasChanges()) {
        return;
      }
      
      const result = {
        oldFieldName: this.originalFieldName,
        newFieldName: this.fieldForm.value.fieldName,
        isRequired: this.fieldForm.value.requiredField,
        hasIndex: this.fieldForm.value.hasIndex,
        regex: this.isTextType() ? this.fieldForm.value.regex : undefined,
        noPastDates: this.isDateType() ? this.fieldForm.value.noPastDates : undefined,
        min: (this.isTextType() || this.isNumericType()) ? this.fieldForm.value.min : null,
        max: (this.isTextType() || this.isNumericType()) ? this.fieldForm.value.max : null,
      };
      
      this.dialogRef.close(result);
      return;
    }

    const selectedFieldType = this.fieldTypes.find(fieldType => fieldType.type.toString() === this.selectedFieldTypeId?.toString());

    if (!selectedFieldType) {
      return;
    }

    if (selectedFieldType.type === this.fieldTypeEnum.Relation && this.selectedRelationTypeIndex !== null) 
    {
      selectedFieldType.netType = this.relationTypeMap[this.selectedRelationTypeIndex];
    }

    const payload = {
      fieldName: this.fieldForm.value.fieldName,
      fieldType: selectedFieldType.netType,
      relatedTo: this.relatedTo,
      isRequired: this.fieldForm.value.requiredField,
      relatedRelationName: this.fieldForm.value.relatedRelationName,
      regex: this.fieldForm.value.regex,
      hasIndex: this.fieldForm.value.hasIndex,
      noPastDates: this.fieldForm.value.noPastDates,
      ...this.buildMinMaxPayload(),
    };

    if (this.selectedFieldTypeId === this.fieldTypeEnum.Dropdown) {
      payload.fieldType = this.selectedEnum;
    }

    this.store.dispatch(CollectionActions.addField({ field: payload }));
    this.dialogRef.close();
  }

  onClose(): void {
    this.dialogRef.close();
  }

  get canSubmit(): boolean | undefined {
    if (this.isEditMode) {
      return this.fieldForm.valid && this.hasChanges();
    }
    
    if (this.selectedFieldTypeId === null) {
      return false;
    }
    
    if (this.selectedFieldTypeId === this.fieldTypeEnum.Relation) {
      return this.selectedRelationTypeIndex !== null && this.fieldForm.get('fieldName')?.valid;
    } else {
      return this.fieldForm.get('fieldName')?.valid;
    }
  }

  private preselectFieldType(fieldType: string): void {
    const typeMapping: Record<string, FieldTypeEnum> = {
      'string': FieldTypeEnum.String,
      'int': FieldTypeEnum.Number,
      'float': FieldTypeEnum.Float,
      'double': FieldTypeEnum.Double,
      'DateOnly': FieldTypeEnum.Date,
      'DateTime': FieldTypeEnum.DateTime,
      'bool': FieldTypeEnum.Checkbox,
      'MediaInfo': FieldTypeEnum.Media,
    };

    const mappedType = typeMapping[fieldType];
    if (mappedType !== undefined) {
      this.selectedFieldTypeId = mappedType;
    } else {
      if (['ManyToMany', 'OneToMany', 'ManyToOne', 'OneToOne'].includes(fieldType)) {
        this.selectedFieldTypeId = FieldTypeEnum.Relation;
      } else {
        this.selectedFieldTypeId = FieldTypeEnum.Dropdown;
        this.selectedEnum = fieldType;
      }
    }
  }

  private hasChanges(): boolean {
    if (!this.isEditMode) return true;
    
    const formValue = this.fieldForm.value;
    return (
      formValue.fieldName !== this.originalFieldName ||
      formValue.requiredField !== this.data.isRequired ||
      formValue.hasIndex !== this.data.hasIndex ||
      (this.isTextType() && formValue.regex !== (this.data.regex || '')) ||
      (this.isDateType() && formValue.noPastDates !== (this.data.noPastDates || false)) ||
      ((this.isTextType() || this.isNumericType()) && (formValue.min !== (this.data.min ?? null) || formValue.max !== (this.data.max ?? null)))
    );
  }

  private updateMinMaxValidators(): void {
    const minValueControl = this.fieldForm.get('min');
    const maxValueControl = this.fieldForm.get('max');

    if (this.isTextType()) {
      minValueControl?.setValidators([ModelValidators.validMinTextValue]);
      maxValueControl?.setValidators([ModelValidators.validMaxTextValue]);
    } else if (this.isNumericType()) {
      const numericValidators = this.selectedFieldTypeId === FieldTypeEnum.Number
        ? [ModelValidators.validNumericInput, ModelValidators.validWholeNumberInput]
        : [ModelValidators.validNumericInput];

      minValueControl?.setValidators([ModelValidators.validMinValue, ...numericValidators]);
      maxValueControl?.setValidators([ModelValidators.validMaxValue, ...numericValidators]);
    } else {
      minValueControl?.clearValidators();
      maxValueControl?.clearValidators();
    }

    minValueControl?.updateValueAndValidity();
    maxValueControl?.updateValueAndValidity();
  }

  private isTextType(): boolean {
    return this.selectedFieldTypeId === FieldTypeEnum.String;
  }

  private isDateType(): boolean {
    return this.selectedFieldTypeId === FieldTypeEnum.Date || this.selectedFieldTypeId === FieldTypeEnum.DateTime;
  }

  private isNumericType(): boolean {
    return this.selectedFieldTypeId === FieldTypeEnum.Number || 
           this.selectedFieldTypeId === FieldTypeEnum.Float || 
           this.selectedFieldTypeId === FieldTypeEnum.Double;
  }

  private isMinMaxApplicable(): boolean {
    return this.isTextType() || this.isNumericType();
  }

  private hasValue(value: unknown): boolean {
    return value !== null && value !== undefined && value !== '';
  }

  private buildMinMaxPayload(): { min?: number; max?: number } {
    if (!this.isMinMaxApplicable()) {
      return {};
    }

    const min = this.fieldForm.value.min;
    const max = this.fieldForm.value.max;
    const payload: { min?: number; max?: number } = {};

    if (this.hasValue(min)) {
      payload.min = min;
    }

    if (this.hasValue(max)) {
      payload.max = max;
    }

    return payload;
  }
}
