import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTableDataSource } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router } from '@angular/router';
import { Store, select } from '@ngrx/store';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import * as ContentActions from '../state/content/content.actions';
import {
  selectContentTypeChanges,
  selectLoadingContentTypeChanges,
} from '../state/content/content.selectors';
import { RecentContent } from '../models/recent-content';

interface ContentTypeChange {
  id: number;
  modelName: string;
  fieldsCount: number;
  fieldTypes: string[];
  modifiedBy: string;
  additionalFields: number;
  modifiedAt: string;
  isPublished: boolean;
}

@Component({
  selector: 'app-recent-content-table',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule, MatTooltipModule],
  templateUrl: './recent-content-table.component.html',
  styleUrl: './recent-content-table.component.scss',
})
export class RecentContentTableComponent implements OnInit, OnDestroy {
  fieldIcons: { [key: string]: string } = {
    text: 'text_fields',
    number: '123',
    DateTime: 'calendar_today',
    media: 'perm_media',
    link: 'link',
    file: 'insert_drive_file',
    relation: 'link',
    collection: 'grid_view',
    string: 'text_fields',
    boolean: 'check_box',
    array: 'view_list',
    object: 'code',
    Guid: 'fingerprint',
    int: '123',
    MediaInfo: 'perm_media',
  };

  fieldTooltips: { [key: string]: string } = {
    text: 'Text Field',
    number: 'Number Field',
    DateTime: 'Date Field',
    media: 'Media Field',
    link: 'Link Field',
    file: 'File Field',
    relation: 'Relation Field',
    collection: 'Collection Field',
    string: 'String Field',
    boolean: 'Boolean Field',
    array: 'Array Field',
    object: 'Object Field',
    Guid: 'ID Field',
    int: 'Integer Field',
    MediaInfo: 'Media Field',
  };

  dataSource = new MatTableDataSource<ContentTypeChange>([]);
  loading = false;
  private destroy$ = new Subject<void>();

  constructor(
    private router: Router,
    private store: Store,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.store
      .pipe(select(selectLoadingContentTypeChanges), takeUntil(this.destroy$))
      .subscribe((loading) => {
        this.loading = loading;
        this.cdr.detectChanges();
      });

    this.store.dispatch(ContentActions.loadContentTypeChanges({ offset: 0, limit: 10 }));

    this.store
      .pipe(select(selectContentTypeChanges), takeUntil(this.destroy$))
      .subscribe((changes) => {
        if (changes && changes.Data)
          if (changes?.Data?.length > 0) {
            this.updateTableData(changes.Data);
          } else {
            this.dataSource.data = [];
            this.cdr.detectChanges();
          }
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  updateTableData(changesData: RecentContent[] | null): void {
    if (!changesData || changesData.length === 0) {
      console.warn('No content type changes found');
      this.dataSource.data = [];
      this.cdr.detectChanges();
      return;
    }

    const tableData: ContentTypeChange[] = changesData.map((change) => {
      const fieldsCount = change.Fields ? Object.keys(change.Fields).length : 0;

      const fieldTypes = change.Fields ? (Object.values(change.Fields) as string[]) : [];

      const modifiedDate = new Date(change.ModifiedAt);
      const now = new Date();
      const diffMs = now.getTime() - modifiedDate.getTime();
      const diffMinutes = Math.floor(diffMs / (1000 * 60));
      const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
      const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

      let lastEdited = '';
      if (diffMinutes < 5) {
        lastEdited = 'just now';
      } else if (diffHours < 1) {
        lastEdited = `${diffMinutes}m ago`;
      } else if (diffHours < 24) {
        lastEdited = `${diffHours}h ago`;
      } else if (diffDays === 1) {
        lastEdited = 'yesterday';
      } else if (diffDays < 7) {
        lastEdited = `${diffDays} days ago`;
      } else {
        lastEdited = `${Math.floor(diffDays / 7)} week${Math.floor(diffDays / 7) > 1 ? 's' : ''} ago`;
      }

      return {
        id: change.Id,
        modelName: change.ModelName,
        fieldsCount: fieldsCount,
        fieldTypes: fieldTypes.slice(0, 3),
        additionalFields: Math.max(0, fieldsCount - 3),
        modifiedBy: change.ModifiedBy,
        modifiedAt: lastEdited,
        isPublished: change.IsPublished,
      };
    });

    this.dataSource = new MatTableDataSource<ContentTypeChange>(tableData);
    this.cdr.detectChanges();
  }

  getFieldIcon(fieldType: string): string {
    return this.fieldIcons[fieldType] || 'help_outline';
  }

  getFieldTooltip(fieldType: string): string {
    return this.fieldTooltips[fieldType] || 'Unknown Field Type';
  }

  navigateToPathBuilder(itemName: string): void {
    this.store.dispatch(ContentActions.setContentType({ selectedType: itemName }));
    this.router.navigate(['/builder']);
  }

  editContentType(modelName: string): void {
    this.store.dispatch(ContentActions.setContentType({ selectedType: modelName }));
    this.router.navigate(['/builder']);
  }
}
