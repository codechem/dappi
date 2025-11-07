import { Component, Inject, OnInit } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Store } from '@ngrx/store';
import { collectionHasRelatedProperties, deleteCollectionType } from '../state/collection/collection.actions';
import { Observable, Subscription } from 'rxjs';
import { hasRelatedProperties } from '../state/collection/collection.selectors';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-delete-colletion-type-dialog',
  imports: [
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    CommonModule
  ],
  templateUrl: './delete-colletion-type-dialog.component.html',
  styleUrl: './delete-colletion-type-dialog.component.scss'
})

export class DeleteColletionTypeDialogComponent implements OnInit {

  hasRelatedProperties$: Observable<boolean>;
  hasRelatedProperties:boolean = false;
  selectedType:string = "";
  private subscription: Subscription = new Subscription();

  
  constructor
  (
    private dialogRef: MatDialogRef<DeleteColletionTypeDialogComponent>,
    @Inject(MAT_DIALOG_DATA) 
    data: any,
    private store:Store
  ) { 
    this.selectedType = data.selectedType;
    this.hasRelatedProperties$ = store.select(hasRelatedProperties);
  }

  async ngOnInit(): Promise<void>{
      this.store.dispatch(collectionHasRelatedProperties({ modelName: this.selectedType }));
  }

  onClose(): void {
    this.dialogRef.close();
  }

  onDelete():void{
    this.store.dispatch(deleteCollectionType({modelName:this.selectedType}));
    this.dialogRef.close({
      success: true,
    });
  }
}