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
} from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from '@angular/common';
import { Store } from '@ngrx/store';
import * as CollectionActions from '../state/collection/collection.actions';

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
    CommonModule,
  ],
  templateUrl: './add-collection-type-dialog.component.html',
  styleUrl: './add-collection-type-dialog.component.scss',
})
export class AddCollectionTypeDialogComponent {
  collectionForm: FormGroup;
  isSubmitting: boolean = false;

  constructor(
    private dialogRef: MatDialogRef<AddCollectionTypeDialogComponent>,
    private fb: FormBuilder,
    private store: Store,
    @Inject(MAT_DIALOG_DATA) public data: any,
  ) {
    this.collectionForm = this.fb.group({
      displayName: ['', [Validators.required, Validators.maxLength(50)]],
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
      }),
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
