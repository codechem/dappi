import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { DeleteColletionTypeDialogComponent } from '../delete-colletion-type-dialog/delete-colletion-type-dialog.component';
import { Store } from '@ngrx/store';
import { selectAllowedCrudActions, selectModelResponse } from '../state/collection/collection.selectors';
import { ConfigureModelRequest, CrudActions, EnumKvp, ModelResponse } from '../models/content.model';
import { Subscription } from 'rxjs';
import { selectSelectedType } from '../state/content/content.selectors';
import { parseEnum } from '../utils/utilFunctions';
import { FormArray, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { configureActions } from '../state/collection/collection.actions';

@Component({
  selector: 'app-configure-actions-dialog',
  imports: [
    MatDialogModule,
    MatButtonModule,
    CommonModule,
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    FormsModule,
    ReactiveFormsModule,
    MatCheckboxModule
  ],
  templateUrl: './configure-actions-dialog.component.html',
  styleUrl: './configure-actions-dialog.component.scss'
})
export class ConfigureActionsDialogComponent implements OnInit {

  configureActionsForm: FormGroup;

  allowedActions$ = this.store.select(selectAllowedCrudActions);
  allowedActions: CrudActions[] | undefined = [];

  selectedType$ = this.store.select(selectSelectedType);
  selectedtype: string = "";

  crudActions: EnumKvp[] = parseEnum(CrudActions);

  private subscription: Subscription = new Subscription();

  constructor(private dialogRef: MatDialogRef<ConfigureActionsDialogComponent>, private store: Store, private fb: FormBuilder,) {
    this.configureActionsForm = this.fb.group({
      crudActions: this.fb.array([])
    });
  }

  get selectedCrudActions(): FormArray {
    return this.configureActionsForm.get('crudActions') as FormArray;
  }

  setSelectedCrudActions() {
    this.allowedActions?.forEach(action => {
      this.selectedCrudActions.push(this.fb.control(action));
    });
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

  ngOnInit(): void {
    this.subscription.add(
      this.allowedActions$.subscribe((allowedActions) => {
        this.allowedActions = allowedActions;
      })
    );

    this.subscription.add(
      this.selectedType$.subscribe((selectedType) => {
        this.selectedtype = selectedType;
      })
    );

    this.setSelectedCrudActions();
  }

  onClose(): void {
    this.dialogRef.close();
  }
  onSubmit(): void {
    let request:ConfigureModelRequest = {
      crudActions: this.configureActionsForm.get("crudActions")?.value
    }
    this.store.dispatch(configureActions({model: this.selectedtype, request}));
    this.dialogRef.close();
  }
}
