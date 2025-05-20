import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { EMPTY, of } from 'rxjs';
import { map, mergeMap, catchError, withLatestFrom } from 'rxjs/operators';
import { HttpClient } from '@angular/common/http';
import { Store } from '@ngrx/store';
import { MatSnackBar } from '@angular/material/snack-bar';
import * as ContentActions from './content.actions';
import { selectItemsPerPage, selectSelectedType } from './content.selectors';
import { FieldType, ModelField, PaginatedResponse } from '../../models/content.model';
import { BASE_API_URL } from '../../../Constants';
import { MediaInfo } from '../../models/media-info.model';
import { RecentContent } from '../../models/recent-content';

@Injectable({
  providedIn: 'root',
})
export class ContentEffects {
  private actions$ = inject(Actions);
  private http = inject(HttpClient);
  private store = inject(Store);
  private snackBar = inject(MatSnackBar);

  loadContent$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ContentActions.loadContent),
      withLatestFrom(this.store.select(selectSelectedType)),
      mergeMap(([action, selectedType]) => {
        const endpoint = `${BASE_API_URL}${selectedType.toLowerCase().replace(/\s+/g, '-')}`;

        return this.http
          .get<any>(endpoint, {
            params: {
              offset: ((action.page - 1) * action.limit).toString(),
              limit: action.limit.toString(),
              SearchTerm: action.searchText || '',
            },
          })
          .pipe(
            map((response) =>
              ContentActions.loadContentSuccess({
                items: {
                  ...response,
                  data: response.Data,
                },
              })
            ),
            catchError((error) => {
              this.showErrorPopup(`Failed to load content: ${error.error}`);
              return of(ContentActions.loadContentFailure({ error: error.message }));
            })
          );
      })
    )
  );

  loadRelatedItems$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ContentActions.loadRelatedItems),
      mergeMap((action) => {
        const endpoint = `${BASE_API_URL}${action.selectedType.toLowerCase().replace(/\s+/g, '-')}`;
        return this.http.get<PaginatedResponse>(endpoint).pipe(
          map((response) =>
            ContentActions.loadRelatedItemsSuccess({
              relatedItems: {
                ...response,
                Data: response.Data,
              },
            })
          ),
          catchError((error) => {
            this.showErrorPopup(`Failed to load related items: ${error.error}`);
            return of(ContentActions.loadRelatedItemsFailure({ error: error.message }));
          })
        );
      })
    )
  );

  loadHeaders$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ContentActions.loadHeaders),
      mergeMap((action) => {
        if (!action.selectedType) {
          return EMPTY;
        }
        const endpoint = `${BASE_API_URL}models/fields/${action.selectedType}`;
        return this.http.get<ModelField[]>(endpoint).pipe(
          map((response) => {
            const headers = response.map((field) => {
              const fieldType = this.mapFieldTypeToInputType(field.fieldType);
              const isRelation = fieldType === FieldType.relation;

              return {
                key: field.fieldName,
                label: this.formatHeaderLabel(field.fieldName),
                type: fieldType,
                relatedTo: isRelation ? field.fieldType : this.getRelatedType(field.fieldType),
                isRequired: field.isRequired ?? false,
              };
            });
            return ContentActions.loadHeadersSuccess({ headers });
          }),
          catchError((error) => {
            this.showErrorPopup(`Failed to load headers: ${error.error}`);
            return of(ContentActions.loadHeadersFailure({ error: error.message }));
          })
        );
      })
    )
  );

  deleteContent$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ContentActions.deleteContent),
      mergeMap((action) => {
        const endpoint = `${BASE_API_URL}${action.contentType
          .toLowerCase()
          .replace(/\s+/g, '-')}/${action.id}`;

        return this.http.delete(endpoint).pipe(
          map(() => ContentActions.deleteContentSuccess({ id: action.id })),
          catchError((error) => {
            this.showErrorPopup(`Failed to delete content: ${error.error}`);
            return of(ContentActions.deleteContentFailure({ error: error.message }));
          })
        );
      })
    )
  );

  deleteMultipleContent$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ContentActions.deleteMultipleContent),
      mergeMap((action) => {
        const deletePromises = action.ids.map((id) => {
          const endpoint = `${BASE_API_URL}${action.contentType
            .toLowerCase()
            .replace(/\s+/g, '-')}/${id}`;

          return this.http.delete(endpoint).toPromise();
        });

        return Promise.all(deletePromises)
          .then(() => ContentActions.deleteMultipleContentSuccess({ ids: action.ids }))
          .catch((error) => {
            this.showErrorPopup(`Failed to delete multiple content: ${error.error}`);
            return ContentActions.deleteMultipleContentFailure({
              error: error.message,
            });
          });
      })
    )
  );

  reloadAfterDelete$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ContentActions.deleteContentSuccess, ContentActions.deleteMultipleContentSuccess),
      withLatestFrom(this.store.select(selectItemsPerPage), this.store.select(selectSelectedType)),
      map(([_, limit, selectedType]) =>
        ContentActions.loadContent({
          selectedType,
          page: 1,
          limit,
          searchText: '',
        })
      )
    )
  );

  createContent$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ContentActions.createContent),
      withLatestFrom(this.store.select(selectItemsPerPage)),
      mergeMap(([action, itemsPerPage]) => {
        const endpoint = `${BASE_API_URL}${action.contentType.toLowerCase().replace(/\s+/g, '-')}`;

        return this.http
          .post<{ Id: string }>(endpoint, action.formData, {
            headers: {
              // Don't set Content-Type here - Angular will set it automatically
              // with the correct boundary for multipart/form-data
            },
          })
          .pipe(
            map((response) => {
              this.store.dispatch(ContentActions.createContentSuccess({ id: response.Id }));
              return ContentActions.loadContent({
                selectedType: action.contentType,
                page: 1,
                limit: itemsPerPage,
                searchText: '',
              });
            }),
            catchError((error) => {
              this.showErrorPopup(`Failed to create content: ${error.error}`);
              return of(ContentActions.createContentFailure({ error: error.message }));
            })
          );
      })
    )
  );

  uploadFile$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ContentActions.uploadFile),
      mergeMap((action) => {
        const endpoint = `${BASE_API_URL}${action.contentType
          .toLowerCase()
          .replace(/\s+/g, '-')}/upload-file/${action.id}`;

        const formData = new FormData();
        formData.append('file', action.file);
        formData.append('fieldName', action.fieldName);

        return this.http.post<MediaInfo>(endpoint, formData).pipe(
          map((response: MediaInfo) => {
            this.store.dispatch(
              ContentActions.loadContent({
                selectedType: action.contentType,
                page: 1,
                limit: 10,
                searchText: '',
              })
            );

            return ContentActions.uploadFileSuccess({
              fileName: response.OriginalFileName,
              size: response.FileSize,
            });
          }),
          catchError((error) => {
            this.showErrorPopup(`Failed to upload file: ${error.error}`);
            return of(ContentActions.uploadFileFailure({ error: error.message }));
          })
        );
      })
    )
  );

  loadContentTypeChanges$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ContentActions.loadContentTypeChanges),
      mergeMap(() => {
        const endpoint = `${BASE_API_URL}content-type-changes`;

        return this.http.get<Array<RecentContent>>(endpoint).pipe(
          map((response) =>
            ContentActions.loadContentTypeChangesSuccess({
              changes: response,
            })
          ),
          catchError((error) => {
            this.showErrorPopup(`Failed to load content type changes: ${error.error}`);
            return of(ContentActions.loadContentTypeChangesFailure({ error: error.message }));
          })
        );
      })
    )
  );

  updateContent$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ContentActions.updateContent),
      withLatestFrom(this.store.select(selectItemsPerPage)),
      mergeMap(([action, itemsPerPage]) => {
        const endpoint = `${BASE_API_URL}${action.contentType
          .toLowerCase()
          .replace(/\s+/g, '-')}/${action.id}`;

        return this.http
          .put<any>(endpoint, action.formData, {
            headers: {
              // Don't set Content-Type here - Angular will set it automatically
            },
          })
          .pipe(
            map((response) => {
              this.store.dispatch(ContentActions.createContentSuccess({ id: response.Id }));
              return ContentActions.loadContent({
                selectedType: action.contentType,
                page: 1,
                limit: itemsPerPage,
                searchText: '',
              });
            }),
            catchError((error) => {
              this.showErrorPopup(`Failed to update content: ${error.error}`);
              return of(ContentActions.updateContentFailure({ error: error.message }));
            })
          );
      })
    )
  );

  private showErrorPopup(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      horizontalPosition: 'center',
      verticalPosition: 'bottom',
      panelClass: ['error-snackbar'],
    });
  }

  private getRelatedType(fieldType: string): string | undefined {
    return fieldType.includes('ICollection') ? fieldType.match(/<([^>]+)>/)?.[1] : undefined;
  }

  private formatHeaderLabel(key: string): string {
    return key
      .replace(/([A-Z])/g, ' $1')
      .replace(/_/g, ' ')
      .replace(/^./, (char) => char.toUpperCase())
      .trim();
  }

  private mapFieldTypeToInputType(fieldType: string): FieldType {
    const lowerFieldType = fieldType.toLowerCase();

    if (lowerFieldType.includes('userroles')) {
      return FieldType.role;
    }

    if (
      !lowerFieldType.includes('string') &&
      !lowerFieldType.includes('mediainfo') &&
      !lowerFieldType.includes('blob') &&
      !lowerFieldType.includes('icollection') &&
      !lowerFieldType.includes('guid') &&
      ![
        'int',
        'integer',
        'number',
        'float',
        'double',
        'decimal',
        'long',
        'short',
        'mediainfo',
        'boolean',
        'bool',
        'date',
        'datetime',
        'time',
        'char',
        'enum',
      ].includes(lowerFieldType)
    ) {
      return FieldType.relation;
    }

    // File types
    if (
      lowerFieldType.includes('mediainfo') ||
      lowerFieldType.includes('blob') ||
      lowerFieldType === 'binary'
    ) {
      return FieldType.file;
    }

    // Long text
    if (
      lowerFieldType === 'string' &&
      ['description', 'content', 'text', 'textarea'].some((keyword) =>
        lowerFieldType.includes(keyword)
      )
    ) {
      return FieldType.textarea;
    }

    // Collection
    if (lowerFieldType.includes('icollection')) {
      return FieldType.collection;
    }

    // IDs
    if (lowerFieldType.includes('guid') || lowerFieldType === 'uuid') {
      return FieldType.id;
    }

    // Boolean
    if (lowerFieldType === 'boolean' || lowerFieldType === 'bool') {
      return FieldType.checkbox;
    }

    // Date/Time
    if (['date', 'datetime', 'time'].includes(lowerFieldType)) {
      return FieldType.date;
    }

    // Enum
    if (lowerFieldType === 'enum') {
      return FieldType.select;
    }

    // Single character
    if (lowerFieldType === 'char') {
      return FieldType.text;
    }

    if (lowerFieldType === 'int') {
      return FieldType.number;
    }

    // Default
    return FieldType.text;
  }
}
