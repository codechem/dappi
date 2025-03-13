import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
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
import { catchError, switchMap } from 'rxjs';
import { of } from 'rxjs';

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
    private http: HttpClient,
    private fb: FormBuilder,
    @Inject(MAT_DIALOG_DATA) public data: any
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

    const payload = {
      modelName: this.collectionForm.value.displayName
    };

    this.http
      .post(
        'http://localhost:5101/api/models',
        payload
      )
      .pipe(
        catchError((error) => {
          console.error('Error creating model:', error);
          alert('Failed to create model. Please try again.');
          this.isSubmitting = false;
          return of(null);
        }),
        switchMap((response) => {
          if (response === null) {
            return of(null);
          }
          
          return this.http.get('http://localhost:5101/api/update-db-context')
            .pipe(
              catchError((error) => {
                console.error('Error updating DB context:', error);
                alert('Model created but failed to update DB context. Please try again.');
                return of({ firstResponse: response, secondResponse: null });
              })
            );
        })
      )
      .subscribe((result) => {
        this.isSubmitting = false;
        if (result !== null) {
          this.dialogRef.close({
            success: true,
            response: result
          });
        }
      });
  }

  onClose(): void {
    this.dialogRef.close();
  }

  get canSubmit(): boolean {
    return this.collectionForm.valid && !this.isSubmitting;
  }
}