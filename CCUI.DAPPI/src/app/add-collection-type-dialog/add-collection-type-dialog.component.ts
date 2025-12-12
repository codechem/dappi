import { Component, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import {
  FormsModule,
  Validators,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  FormArray,
} from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';

import { Store } from '@ngrx/store';
import * as CollectionActions from '../state/collection/collection.actions';
import { ModelValidators } from '../validators/model-validators';
import { selectCollectionTypes } from '../state/collection/collection.selectors';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { CrudActions, EnumKvp } from '../models/content.model';
import { parseEnum } from '../utils/utilFunctions';

@Component({
  selector: 'app-add-collection-type-dialog',
  standalone: true,
  imports: [
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    FormsModule,
    ReactiveFormsModule,
    MatCheckboxModule
  ],
  templateUrl: './add-collection-type-dialog.component.html',
  styleUrl: './add-collection-type-dialog.component.scss',
})
export class AddCollectionTypeDialogComponent {
  collectionForm: FormGroup;
  isSubmitting: boolean = false;
  collectionTypes$ = this.store.select(selectCollectionTypes);
  crudActions: EnumKvp[] = parseEnum(CrudActions);
  defaultCrudActions: CrudActions[] = [CrudActions.Get,
    CrudActions.GetOne,
    CrudActions.Create,
    CrudActions.Update,
    CrudActions.Delete];

  constructor(
    private dialogRef: MatDialogRef<AddCollectionTypeDialogComponent>,
    private fb: FormBuilder,
    private store: Store,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {
    this.collectionForm = this.fb.group({
      isAuditableEntity: [false, {}],
      crudActions: this.fb.array([]),
      displayName: [
        '',
        {
          validators: [
            Validators.required,
            Validators.maxLength(50),
            ModelValidators.pascalCase,
            ModelValidators.reservedKeyword,
          ],
          asyncValidators: [ModelValidators.collectionNameIsTaken(this.collectionTypes$)],
        },
      ],
    });
    this.setDefaultCrudActions();
  }

  get selectedCrudActions(): FormArray {
    return this.collectionForm.get('crudActions') as FormArray;
  }

  onCheckboxChange(event: any, value: number) {
    const checkArray: FormArray = this.selectedCrudActions;
    if (event.source._checked) {
      checkArray.push(this.fb.control(value));
    } else {
      const index = checkArray.controls.findIndex(control => control.value === value);
      checkArray.removeAt(index);
    }
  }

  setDefaultCrudActions() {
    this.defaultCrudActions.forEach(action => {
      this.selectedCrudActions.push(this.fb.control(action));
    });
  }

  onContinue(): void {
    if (!this.collectionForm.valid || this.isSubmitting) {
      this.collectionForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.store.dispatch(
      CollectionActions.addCollectionType({
        collectionType: this.collectionForm.value.displayName,
        isAuditableEntity: this.collectionForm.value.isAuditableEntity,
        crudActions: this.collectionForm.value.crudActions
      })
    );

    this.isSubmitting = false;
    this.dialogRef.close({
      success: true,
    });
  }

  onClose(): void {
    this.dialogRef.close();
  }

  get canSubmit(): boolean {
    return this.collectionForm.valid && !this.isSubmitting;
  }
}
