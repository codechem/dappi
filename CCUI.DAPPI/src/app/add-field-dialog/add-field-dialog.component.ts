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
import { ModelValidators } from '../validators/model-validators';

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

  availableModels: { label: string; value: string }[] = [];
  availableEnums: string[] = [];
  selectedEnum: string = '';

  selectedRelationTypeIndex: number | null = null;

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
      netType: 'DateOnly',
    },
    {
      icon: 'perm_media',
      label: 'Media',
      description: 'For uploading images or videos',
      value: 'media',
      netType: 'MediaInfo',
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
      description: 'For selecting from predefined options(Enumerations)',
      value: 'dropdown',
      netType: this.selectedEnum ?? 'Enum',
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
    {
      icon: 'leak_remove',
      label: 'Relation',
      description: 'Create relation between models',
      value: 'relation',
      netType: 'OneToOne',
    },
  ];
  private subscription: Subscription = new Subscription();

  relatedTo: string | undefined = '';

  fieldForm: FormGroup = this.fb.group({
    fieldName: ['', [Validators.required, Validators.maxLength(50)]],
    requiredField: [false],
    relatedModel: [''],
    relatedRelationName: [''],
  });
  selectedFieldTypeIndex: number | null = null;

  collectionTypes$ = this.store.select(selectCollectionTypes);

  constructor(
    private dialogRef: MatDialogRef<AddFieldDialogComponent>,
    private fb: FormBuilder,
    private enumsService: EnumsService,
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
          ],
          asyncValidators: [ModelValidators.fieldNameIsTaken(this.selectedTypeFields$)],
        },
      ],
      requiredField: [false],
      relatedModel: [''],
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
    this.enumsService.getEnums().subscribe((result: EnumsResponse) => {
      this.availableEnums = Object.keys(result);
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
    this.fieldForm.get('relatedModel')?.setValidators([Validators.required]);
    this.updateRelationTypes();
  }

  private updateRelationTypes(relatedModel?: string): void {
    const modelName = relatedModel || '/';
    this.relatedTo = relatedModel;
    this.relationTypes = [
      {
        label: 'Many-to-many',
        description: `Many ${this.selectedType}s can relate to many ${modelName}s`,
        value: 'many-to-many',
      },
      {
        label: 'One-to-many',
        description: `One ${this.selectedType} can relate to many ${modelName}s`,
        value: 'one-to-many',
      },
      {
        label: 'Many-to-one',
        description: `Many ${this.selectedType}s can relate to one ${modelName}`,
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
    let netType: string = '';

    switch (index) {
      case 0:
        netType = 'ManyToMany';
        break;
      case 1:
        netType = 'OneToMany';
        break;
      case 2:
        netType = 'ManyToOne';
        break;
      case 3:
        netType = 'OneToOne';
        break;
    }
    this.fieldTypes = [
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
        netType: 'DateOnly',
      },
      {
        icon: 'perm_media',
        label: 'Media',
        description: 'For uploading images or videos',
        value: 'media',
        netType: 'MediaInfo',
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
        netType: netType,
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
      {
        icon: 'leak_remove',
        label: 'Relation',
        description: 'Create relation between models',
        value: 'relation',
        netType: netType,
      },
    ];
  }

  selectFieldType(index: number): void {
    this.selectedFieldTypeIndex = index;

    if (index === 8) {
      this.fieldForm.get('relatedModel')?.setValidators([Validators.required]);
      this.fieldForm.get('relatedRelationName')?.setValidators([Validators.required]);
    } else {
      this.fieldForm.get('relatedModel')?.clearValidators();
      this.fieldForm.get('relatedRelationName')?.clearValidators();
    }

    ['relatedModel', 'relatedRelationName'].forEach((control) => {
      this.fieldForm.get(control)?.updateValueAndValidity();
    });
    this.selectedRelationTypeIndex = null;
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
      relatedTo: this.relatedTo,
      isRequired: this.fieldForm.value.requiredField,
      relatedRelationName: this.fieldForm.value.relatedRelationName,
    };

    if (this.selectedFieldTypeIndex === 5) {
      payload.fieldType = this.selectedEnum;
    }

    this.store.dispatch(CollectionActions.addField({ field: payload }));
    this.dialogRef.close();
  }

  onClose(): void {
    this.dialogRef.close();
  }

  get canSubmit(): boolean | undefined {
    if (this.selectedFieldTypeIndex === 8) {
      return this.selectedRelationTypeIndex !== null;
    } else {
      return this.fieldForm.get('fieldName')?.valid;
    }
  }
}
