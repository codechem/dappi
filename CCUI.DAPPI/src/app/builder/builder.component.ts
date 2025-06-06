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
import { ModelField } from '../models/content.model';
import { select, Store } from '@ngrx/store';
import * as CollectionActions from '../state/collection/collection.actions';
import { selectSelectedType } from '../state/content/content.selectors';
import {
  selectDraftCollectionTypes,
  selectFields,
  selectSaveError,
  selectServerRestarting,
} from '../state/collection/collection.selectors';

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
  ],
  templateUrl: './builder.component.html',
  styleUrl: './builder.component.scss',
})
export class BuilderComponent implements OnInit, OnDestroy {
  disabled = false;
  fieldsData: FieldItem[] = [];
  selectedType = '';
  isSaving = false;

  isModalOpen: boolean = false;

  selectedType$ = this.store.select(selectSelectedType);
  fieldsData$ = this.store.select(selectFields);

  draftCollectionTypes: string[] = [];
  draftCollectionTypes$ = this.store.select(selectDraftCollectionTypes);

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
      this.fieldsData$.subscribe((fields) => {
        this.formatFields(fields);
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
  }

  openAddFieldDialog(): void {
    const dialogRef = this.dialog.open(AddFieldDialogComponent, {
      width: '67vw',
      maxWidth: '100vw',
      panelClass: 'add-field-dialog-container',
      disableClose: true,
      data: { selectedType: this.selectedType },
    });

    this.subscription.add(
      dialogRef.afterClosed().subscribe(async () => {
        this.disabled = false;
      })
    );
  }

  private updateSaveButtonState(): void {
    this.disabled = !this.selectedType || !this.draftCollectionTypes.includes(this.selectedType);
  }

  private formatFields(fields: ModelField[]): void {
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
          case 'double':
          case 'decimal':
            type = 'Number';
            iconText = '123';
            break;
          case 'DateTime':
            type = 'DateTime';
            iconName = 'event';
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
