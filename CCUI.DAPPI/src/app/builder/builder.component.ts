import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { ButtonComponent } from '../button/button.component';
import { MatDialog } from '@angular/material/dialog';
import { AddFieldDialogComponent } from '../add-field-dialog/add-field-dialog.component';
import { FieldItem, FieldsListComponent } from '../fields-list/fields-list.component';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { filter, Subscription, take } from 'rxjs';
import { MatSpinner } from '@angular/material/progress-spinner';
import { ModelField, ModelResponse } from '../models/content.model';
import { select, Store } from '@ngrx/store';
import * as CollectionActions from '../state/collection/collection.actions';
import * as ContentActions from '../state/content/content.actions';
import { selectSelectedType } from '../state/content/content.selectors';
import {
  selectCollectionTypes,
  selectDraftCollectionTypes,
  selectFields,
  selectModelResponse,
  selectSaveError,
  selectServerRestarting,
} from '../state/collection/collection.selectors';
import {MatMenuModule} from '@angular/material/menu';
import { loadCollectionTypes } from '../state/collection/collection.actions';
import { DeleteColletionTypeDialogComponent } from '../delete-colletion-type-dialog/delete-colletion-type-dialog.component';
import { AddCollectionTypeDialogComponent } from '../add-collection-type-dialog/add-collection-type-dialog.component';
import { ConfigureActionsDialogComponent } from '../configure-actions-dialog/configure-actions-dialog.component';
@Component({
  selector: 'app-builder',
  standalone: true,
  imports: [
    SidebarComponent,
    MatIconModule,
    MatButtonModule,
    ButtonComponent,
    FieldsListComponent,
    MatSpinner,
    MatMenuModule
  ],
  templateUrl: './builder.component.html',
  styleUrl: './builder.component.scss',
})
export class BuilderComponent implements OnInit, OnDestroy {
  disabled = false;
  fieldsData: FieldItem[] = [];
  model: ModelResponse | null = null;
  selectedType = '';
  isSaving = false;

  isModalOpen: boolean = false;

  selectedType$ = this.store.select(selectSelectedType);
  fieldsData$ = this.store.select(selectFields);
  model$ = this.store.select(selectModelResponse);
  draftCollectionTypes: string[] = [];
  draftCollectionTypes$ = this.store.select(selectDraftCollectionTypes);

  collectionTypes: string[] = [];
  collectionTypes$ = this.store.select(selectCollectionTypes);

  private subscription: Subscription = new Subscription();

  constructor(
    private dialog: MatDialog,
    private store: Store
  ) {}

  async ngOnInit(): Promise<void> {
    this.store.dispatch(CollectionActions.loadDraftCollectionTypes());

    this.subscription.add(
      this.draftCollectionTypes$.subscribe((draftTypes) => {
        this.draftCollectionTypes = draftTypes;
        this.updateSaveButtonState();
      })
    );

    this.subscription.add(
      this.selectedType$.subscribe((selectedType) => {
        this.selectedType = selectedType;
        this.updateSaveButtonState();
      })
    );

    this.subscription.add(
      this.selectedType$.subscribe((selectedType) => {
        this.store.dispatch(CollectionActions.loadFields({ modelType: selectedType }));
      })
    );
    
    this.subscription.add(
      this.model$.subscribe(model => this.model = model)
    )
    
    this.subscription.add(
      this.fieldsData$.subscribe((Fields) => {
        this.formatFields(this.model?.Fields);
      })
    );

    this.subscription.add(
      this.store
        .pipe(
          select(selectServerRestarting),
          filter((restarting) => restarting),
          take(1)
        )
        .subscribe(() => {
          alert(
            'Migrations applied. Application restarting... Please wait a moment before making additional changes.'
          );

          location.reload();
        })
    );
    this.subscription.add(
      this.store
        .pipe(
          select(selectSaveError),
          filter((error) => !!error),
          take(1)
        )
        .subscribe(() => {
          alert('Failed to save content. Please try again.');
          this.closeModal();
        })
    );
    this.subscription.add(
      this.collectionTypes$.subscribe(types => 
        this.collectionTypes = types
      )
    )
  }

  openAddFieldDialog(): void {
    const dialogRef = this.dialog.open(AddFieldDialogComponent, {
      panelClass: 'add-field-dialog-container',
      disableClose: true,
      width: '67vw',
      maxWidth: '100vw',
      data: { selectedType: this.selectedType },
    });

    this.subscription.add(
      dialogRef.afterClosed().subscribe(async () => {
        this.disabled = false;
      })
    );
  }

  openAddCollectionTypeDialog(): void {
      const dialogRef = this.dialog.open(AddCollectionTypeDialogComponent, {
        width: '450px',
        panelClass: 'dark-theme-dialog',
        disableClose: true,
      });
  }

  private updateSaveButtonState(): void {
    this.disabled = this.draftCollectionTypes.length == 0;
  }

  private formatFields(fields: ModelField[] | undefined): void {
    if(!fields) return;
    if (fields && fields.length > 0) {
      this.fieldsData = fields.map((field) => {
        let fieldType = field.isEnum ? 'enum' : field.fieldType;
        let type: string;
        let iconText: string | undefined;
        let iconName: string | undefined;

        switch (fieldType) {
          case 'string':
            if (
              field.fieldName.toLowerCase().includes('url') ||
              field.fieldName.toLowerCase().includes('link')
            ) {
              type = 'Link';
              iconName = 'link';
            } else {
              type = 'Text';
              iconText = 'Aa';
            }
            break;
          case 'int':
            type = 'Number';
            iconText = '123';
            break;
          case 'double':
          case 'decimal':
          case 'float':
            type = 'Decimal';
            iconText = '1.75';
            break;
          case 'DateOnly':
            type = 'Date';
            iconName = 'calendar_today';
            break;
          case 'DateTime':
            type = 'DateTime';
            iconName = 'today';
            break;
          case 'bool':
            type = 'Checkbox';
            iconName = 'check_box';
            break;
          case 'MediaInfo':
            type = 'Media';
            iconName = 'perm_media';
            break;
          case 'Guid':
            type = 'Media';
            iconName = 'fingerprint';
            break;
          case 'enum':
            type = field.fieldType;
            iconName = 'list';
            break;
          default:
            type = fieldType;
            iconName = 'leak_remove';
        }

        return {
          name: field.fieldName,
          type: type,
          iconText: iconText,
          iconName: iconName,
        };
      });
    }
  }

  openDeleteCollectionTypeDialog(): void {
    if(!this.selectedType) return;
    const dialogRef = this.dialog.open(DeleteColletionTypeDialogComponent, {
      minWidth: 759,
      panelClass: 'dark-theme-dialog',
      disableClose: true,
      data: { selectedType: this.selectedType },
    });

    var newSelection = this.collectionTypes.filter(x => x !== this.selectedType)[0]; 
    this.subscription.add(
      dialogRef.afterClosed().subscribe(() => {
        this.store.dispatch(loadCollectionTypes());
        this.store.dispatch(ContentActions.setContentType({selectedType: newSelection ?? ""}));
        this.store.dispatch(CollectionActions.loadFields({modelType : newSelection}));
      })
    );
  }

  openConfigureActionsDialog():void{
    if(!this.selectedType) return;
    const dialogRef = this.dialog.open(ConfigureActionsDialogComponent, {
      width: '600px',
      panelClass: 'dark-theme-dialog',
      disableClose: true,
      data: { selectedType: this.selectedType },
    });
  }

  private closeModal(): void {
    this.isSaving = false;
    this.isModalOpen = false;
  }

  saveContent(): void {
    this.store.dispatch(CollectionActions.saveContent());
    this.isModalOpen = true;
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }
}
