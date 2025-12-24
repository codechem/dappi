import { Component, Inject, OnDestroy, OnInit } from '@angular/core';
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
      type: FieldTypeEnum.Date,
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
      type: FieldTypeEnum.Number,
      icon: '123',
      label: 'Decimal Number',
      description: 'For decimal numerical values and calculations',
      value: 'float',
      netType: 'float',
    },
  ];
  private subscription: Subscription = new Subscription();

  relatedTo: string | undefined = '';

  fieldForm: FormGroup = this.fb.group({
    fieldName: ['', [Validators.required, Validators.maxLength(50)]],
    requiredField: [false],
    relatedModel: [''],
    relatedRelationName: [''],
    hasIndex: [false],
  });

  selectedFieldTypeId: FieldTypeEnum | null = null;
  fieldTypeEnum = FieldTypeEnum;

  collectionTypes$ = this.store.select(selectCollectionTypes);

  constructor(
    private dialogRef: MatDialogRef<AddFieldDialogComponent>,
    private fb: FormBuilder,
    private enumsService: EnumsService,
    private enumManagementService: EnumManagementService,
    @Inject(MAT_DIALOG_DATA) public data: { selectedType: string },
    private store: Store
  ) {
    this.fieldForm = this.fb.group({
      fieldName: [
        '',
        {
          validators: [
            Validators.required,
            Validators.maxLength(50),
            ModelValidators.pascalCase,
            ModelValidators.reservedKeyword,
            ModelValidators.collectionNameIsTaken
          ],
          asyncValidators: [ModelValidators.fieldNameIsTaken(this.selectedTypeFields$)],
        },
      ],
      requiredField: [false],
      relatedModel: [''],
      relatedRelationName: [''],
      regex: ['', [ModelValidators.validRegex]],
      hasIndex: [false]
    });
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
    this.fieldForm.get('relatedModel')?.setValidators([Validators.required]);
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
  }

  onAddField(): void {
    if (!this.fieldForm.valid || this.selectedFieldTypeId === null) {
      this.fieldForm.markAllAsTouched();
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
    if (this.selectedFieldTypeId === this.fieldTypeEnum.Relation) {
      return this.selectedRelationTypeIndex !== null;
    } else {
      return this.fieldForm.get('fieldName')?.valid;
    }
  }
}
