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
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';

import { Store } from '@ngrx/store';
import * as CollectionActions from '../state/collection/collection.actions';

function validCSharpClassNameValidator(control: AbstractControl): ValidationErrors | null {
  const value = control.value;

  if (!value) return null;

  if (!/^[A-Za-z_]/.test(value)) {
    return { invalidStart: true };
  }

  if (!/^[A-Za-z0-9_]+$/.test(value)) {
    return { invalidCharacters: true };
  }

  const csharpKeywords = [
    'abstract',
    'as',
    'base',
    'bool',
    'break',
    'byte',
    'case',
    'catch',
    'char',
    'checked',
    'class',
    'const',
    'continue',
    'decimal',
    'default',
    'delegate',
    'do',
    'double',
    'else',
    'enum',
    'event',
    'explicit',
    'extern',
    'false',
    'finally',
    'fixed',
    'float',
    'for',
    'foreach',
    'goto',
    'if',
    'implicit',
    'in',
    'int',
    'interface',
    'internal',
    'is',
    'lock',
    'long',
    'namespace',
    'new',
    'null',
    'object',
    'operator',
    'out',
    'override',
    'params',
    'private',
    'protected',
    'public',
    'readonly',
    'ref',
    'return',
    'sbyte',
    'sealed',
    'short',
    'sizeof',
    'stackalloc',
    'static',
    'string',
    'struct',
    'switch',
    'this',
    'throw',
    'true',
    'try',
    'typeof',
    'uint',
    'ulong',
    'unchecked',
    'unsafe',
    'ushort',
    'using',
    'virtual',
    'void',
    'volatile',
    'while',
  ];

  if (csharpKeywords.includes(value.toLowerCase())) {
    return { reservedKeyword: true };
  }

  return null;
}

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
      displayName: [
        '',
        [Validators.required, Validators.maxLength(50), validCSharpClassNameValidator],
      ],
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
