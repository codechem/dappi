import { Component, OnInit } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { ButtonComponent } from '../button/button.component';
import { MatDialog } from '@angular/material/dialog';
import { AddFieldDialogComponent } from '../add-field-dialog/add-field-dialog.component';
import {
  FieldItem,
  FieldsListComponent,
} from '../fields-list/fields-list.component';
import { CommonModule } from '@angular/common';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { HttpClient } from '@angular/common/http';
import { catchError, of } from 'rxjs';
import { MatSpinner } from '@angular/material/progress-spinner';
import { ModelField } from '../models/content.model';
import { Store } from '@ngrx/store';
import * as ContentActions from '../state/content/content.actions';
import * as CollectionActions from '../state/collection/collection.actions';
import {
  selectHeaders,
  selectSelectedType,
} from '../state/content/content.selectors';
import { selectFields } from '../state/collection/collection.selectors';

interface SaveResponse {
  success: boolean;
  restarting?: boolean;
  error?: any;
}

@Component({
  selector: 'app-builder',
  standalone: true,
  imports: [
    SidebarComponent,
    MatIconModule,
    MatButtonModule,
    ButtonComponent,
    FieldsListComponent,
    CommonModule,
    MatSpinner,
  ],
  templateUrl: './builder.component.html',
  styleUrl: './builder.component.scss',
})
export class BuilderComponent implements OnInit {
  disabled = true;
  fieldsData: FieldItem[] = [];
  selectedType = '';
  isSaving = false;

  isModalOpen: boolean = false;
  maxRetries: number = 15;
  currentRetry: number = 0;
  retryInterval: number = 1000;
  retryTimeout: any;
  serverRestarting: boolean = false;

  selectedType$ = this.store.select(selectSelectedType);
  fieldsData$ = this.store.select(selectFields);

  constructor(
    private dialog: MatDialog,
    private http: HttpClient,
    private store: Store
  ) {}

  async ngOnInit(): Promise<void> {
    await this.loadFieldsData();
    this.selectedType$.subscribe((selectedType) => {
      this.store.dispatch(
        CollectionActions.loadFields({ modelType: selectedType })
      );
    });

    this.fieldsData$.subscribe((fields) => {
      this.formatFields(fields);
    });
  }

  openAddFieldDialog(): void {
    const dialogRef = this.dialog.open(AddFieldDialogComponent, {
      width: '800px',
      panelClass: 'add-field-dialog-container',
      disableClose: true,
      data: { selectedType: this.selectedType },
    });

    dialogRef.afterClosed().subscribe(async (result) => {
      if (result) {
        await this.loadFieldsData();
        this.disabled = false;
      }
    });
  }

  private formatFields(fields: ModelField[]): void {
    if (fields && fields.length > 0) {
      this.fieldsData = fields.map((field) => {
        let fieldType = field.fieldType;
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
          case 'byte[]':
            type = 'Media';
            iconName = 'perm_media';
            break;
          default:
            type = fieldType;
            iconText = 'Aa';
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

  private async loadFieldsData(): Promise<boolean> {
    if (!this.selectedType) return Promise.resolve(false);
    return Promise.resolve(true);
    // this.isModalOpen = true;
    // this.isSaving = true;

    // try {

    //   if (fields && fields.length > 0) {
    //     this.fieldsData = fields.map((field) => {
    //       let fieldType = field.fieldType;
    //       let type: string;
    //       let iconText: string | undefined;
    //       let iconName: string | undefined;

    //       switch (fieldType) {
    //         case 'string':
    //           if (
    //             field.fieldName.toLowerCase().includes('url') ||
    //             field.fieldName.toLowerCase().includes('link')
    //           ) {
    //             type = 'Link';
    //             iconName = 'link';
    //           } else {
    //             type = 'Text';
    //             iconText = 'Aa';
    //           }
    //           break;
    //         case 'int':
    //         case 'double':
    //         case 'decimal':
    //           type = 'Number';
    //           iconText = '123';
    //           break;
    //         case 'DateTime':
    //           type = 'DateTime';
    //           iconName = 'event';
    //           break;
    //         case 'bool':
    //           type = 'Checkbox';
    //           iconName = 'check_box';
    //           break;
    //         case 'byte[]':
    //           type = 'Media';
    //           iconName = 'perm_media';
    //           break;
    //         default:
    //           type = fieldType;
    //           iconText = 'Aa';
    //       }

    //       return {
    //         name: field.fieldName,
    //         type: type,
    //         iconText: iconText,
    //         iconName: iconName,
    //       };
    //     });
    // })
  }

  private closeModal(): void {
    this.isSaving = false;
    this.isModalOpen = false;
    clearTimeout(this.retryTimeout);
  }

  saveContent(): void {
    this.isSaving = true;
    this.isModalOpen = true;

    this.http
      .post('http://localhost:5101/api/create-migrations-update-db', {})
      .pipe(
        catchError(async (error) => {
          if (error.status === 200) {
            console.log('Backend is restarting. This is expected behavior.');
            this.serverRestarting = true;
            await this.startPolling();
            return of({ success: true, restarting: true });
          }

          console.error('Error saving content:', error);
          this.closeModal();
          return of({ success: false, error: error });
        })
      )
      .subscribe({
        next: async (response: any) => {
          const typedResponse: SaveResponse =
            response.success !== undefined
              ? (response as SaveResponse)
              : { success: true };

          if (!typedResponse.success) {
            alert('Failed to save content. Please try again.');
            this.isSaving = false;
            this.closeModal();
          } else {
            if (typedResponse.restarting) {
              this.serverRestarting = true;
              this.showRestartNotification();
            } else {
              this.isSaving = false;
              this.disabled = true;
              this.startPolling();
            }
          }
        },
        error: (err) => {
          console.error('Unexpected error:', err);
          this.isSaving = false;
          this.closeModal();
        },
      });
  }

  private showRestartNotification(): void {
    alert(
      'Migrations applied. Application restarting... Please wait a moment before making additional changes.'
    );
  }

  async onCollectionTypeSelected(type: string): Promise<void> {
    this.selectedType = type;
    await this.loadFieldsData();
  }

  private async startPolling(): Promise<boolean> {
    this.currentRetry = 0;
    this.serverRestarting = true;
    return await this.loadFieldsData();
  }

  ngOnDestroy(): void {
    clearTimeout(this.retryTimeout);
  }
}