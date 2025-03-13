import { Router } from '@angular/router';
import { Component, OnInit, OnDestroy } from '@angular/core';
import {
  ContentItem,
  ContentTableComponent,
  TableHeader,
} from '../content-table/content-table.component';
import { MatIconModule } from '@angular/material/icon';
import { ButtonComponent } from '../button/button.component';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { HttpClient } from '@angular/common/http';
import { Subject, takeUntil, finalize, catchError, of } from 'rxjs';
import { MatSpinner } from '@angular/material/progress-spinner';
import { ContentStateService } from './content-state.service';

interface ApiResponse<T> {
  total: number;
  offset: number;
  limit: number;
  data: T[];
}

export interface ModelField {
  fieldName: string;
  fieldType: string;
}

@Component({
  selector: 'app-content-manager',
  standalone: true,
  imports: [
    SidebarComponent,
    ContentTableComponent,
    MatIconModule,
    MatButtonModule,
    ButtonComponent,
    CommonModule,
    MatSpinner,
  ],
  templateUrl: './content-manager.component.html',
  styleUrl: './content-manager.component.scss',
})
export class ContentManagerComponent implements OnInit, OnDestroy {
  disabled = false;
  selectedType = '';
  itemsData: ContentItem[] = [];
  tableHeaders: TableHeader[] = [];
  isLoading = false;
  private destroy$ = new Subject<void>();

  constructor(
    private router: Router,
    private http: HttpClient,
    private contentStateService: ContentStateService
  ) {}

  ngOnInit(): void {}

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onCollectionTypeSelected(type: string): void {
    this.selectedType = type;
    this.loadContentItems(type);
  }

  navigateToCreate(): void {
    this.contentStateService.setContentCreateData(
      this.tableHeaders,
      this.selectedType
    );
    this.router.navigate(['/content-create']);
  }

  private loadContentItems(type: string): void {
    this.isLoading = true;
    this.itemsData = [];
    this.tableHeaders = [];

    const fieldsEndpoint = `http://localhost:5101/api/models/fields/${type}`;

    this.http
      .get<ModelField[]>(fieldsEndpoint)
      .pipe(
        takeUntil(this.destroy$),
        catchError((error) => {
          console.error(`Error fetching ${type} fields:`, error);
          return of([]);
        })
      )
      .subscribe((fields) => {
        const dataEndpoint = `http://localhost:5101/api/${type}`;

        this.http
          .get<ApiResponse<any>>(dataEndpoint)
          .pipe(
            takeUntil(this.destroy$),
            catchError((error) => {
              console.error(`Error fetching ${type} data:`, error);
              return of({ total: 0, offset: 0, limit: 0, data: [] });
            }),
            finalize(() => {
              this.isLoading = false;
              this.updateSaveButtonState();
            })
          )
          .subscribe((response) => {
            if (fields.length > 0) {
              this.tableHeaders = fields.map((field) => ({
                key:
                  field.fieldName.charAt(0).toLowerCase() +
                  field.fieldName.slice(1),
                label: this.formatHeaderLabel(field.fieldName),
                type: this.mapFieldTypeToInputType(field.fieldType),
              }));
            }
            if (response.data.length > 0) {
              this.itemsData = response.data.map((item: any) => {
                return {
                  ...item,
                  id: item.id || this.generateTemporaryId(),
                };
              });
            }
          });
      });
  }

  private mapFieldTypeToInputType(
    fieldType: string
  ): 'text' | 'textarea' | 'file' {
    const lowerFieldType = fieldType.toLowerCase();

    if (lowerFieldType.includes('byte[]') || lowerFieldType.includes('blob')) {
      return 'file';
    }

    if (
      lowerFieldType === 'string' &&
      ['description', 'content', 'text', 'textarea'].some((keyword) =>
        lowerFieldType.includes(keyword)
      )
    ) {
      return 'textarea';
    }

    return 'text';
  }
  private formatHeaderLabel(key: string): string {
    return key
      .replace(/([A-Z])/g, ' $1')
      .replace(/_/g, ' ')
      .replace(/^./, (char) => char.toUpperCase())
      .trim();
  }

  private detectHeaderType(value: any): 'text' | 'textarea' | 'file' {
    if (
      typeof value === 'string' &&
      ['blob', 'longblob', 'varbinary'].some((type) =>
        value.toLowerCase().includes(type)
      )
    ) {
      return 'file';
    }
    if (
      typeof value === 'string' &&
      ['text', 'varchar', 'char'].some((type) =>
        value.toLowerCase().includes(type)
      )
    ) {
      return 'text';
    }
    if (
      typeof value === 'string' &&
      ['textarea'].includes(value.toLowerCase())
    ) {
      return 'textarea';
    }
    return 'text';
  }

  private generateTemporaryId(): string {
    return Math.random().toString(36).substr(2, 9);
  }

  private updateSaveButtonState(): void {
    this.disabled = this.itemsData.length === 0;
  }
}