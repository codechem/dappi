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
import { ModelField } from '../content-manager/content-manager.component';
import { MatSpinner } from '@angular/material/progress-spinner';

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

  constructor(private dialog: MatDialog, private http: HttpClient) {}

  async ngOnInit(): Promise<void> {
    await this.loadFieldsData();
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

  private async loadFieldsData(): Promise<boolean> {
    if (!this.selectedType) return Promise.resolve(false);

    this.isModalOpen = true;
    this.isSaving = true;

    if (this.retryTimeout) {
      clearTimeout(this.retryTimeout);
    }

    if (this.currentRetry >= this.maxRetries) {
      this.closeModal();
      alert(
        'Server restart is taking longer than expected. Please refresh the page manually.'
      );
      return false;
    }

    this.currentRetry++;
    console.log(`Polling attempt ${this.currentRetry} of ${this.maxRetries}`);

    try {
      const fields = await this.http
        .get<ModelField[]>(
          `http://localhost:5101/api/models/fields/${this.selectedType}`
        )
        .pipe(
          catchError((error) => {
            console.error('Error fetching fields:', error);

            this.retryTimeout = setTimeout(() => {
              this.loadFieldsData();
            }, this.retryInterval);
            return of([]);
          })
        )
        .toPromise();

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

        this.closeModal();
        this.currentRetry = 0;
        return true;
      } else {
        this.retryTimeout = setTimeout(() => {
          this.loadFieldsData();
        }, this.retryInterval);
        return false;
      }
    } catch (error) {
      console.error('Error during HTTP request:', error);

      this.retryTimeout = setTimeout(() => {
        this.loadFieldsData();
      }, this.retryInterval);
      return false;
    }
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