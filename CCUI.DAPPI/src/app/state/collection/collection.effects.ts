import { Injectable, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { EMPTY, of } from 'rxjs';
import {
  map,
  mergeMap,
  catchError,
  withLatestFrom,
  switchMap,
  filter,
  concatMap,
} from 'rxjs/operators';
import { HttpClient } from '@angular/common/http';
import { select, Store } from '@ngrx/store';
import { MatSnackBar } from '@angular/material/snack-bar';
import * as CollectionActions from './collection.actions';
import { selectSelectedType } from '../content/content.selectors';
import * as ContentActions from '../content/content.actions';
import { BASE_API_URL } from '../../../Constants';
import { ModelField, FieldType } from '../../models/content.model';

@Injectable()
export class CollectionEffects {
  private actions$ = inject(Actions);
  private http = inject(HttpClient);
  private store = inject(Store);
  private snackBar = inject(MatSnackBar);
  private enumsData: any = null;

  loadCollectionTypes$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CollectionActions.loadCollectionTypes),
      mergeMap(() => {
        return this.http.get<string[]>(`${BASE_API_URL}models`).pipe(
          map((collectionTypes) => {
            return CollectionActions.loadCollectionTypesSuccess({
              collectionTypes: collectionTypes,
            });
          }),
          catchError((error) => {
            this.showErrorPopup(`Failed to load collection types: ${error.error}`);
            return of(
              CollectionActions.loadCollectionTypesFailure({
                error: error.message,
              })
            );
          })
        );
      })
    )
  );

  loadDraftCollectionTypes$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CollectionActions.loadDraftCollectionTypes),
      mergeMap(() => {
        return this.http.get<string[]>(`${BASE_API_URL}content-type-changes/draft-models`).pipe(
          map((draftCollectionTypes) => {
            return CollectionActions.loadDraftCollectionTypesSuccess({
              draftCollectionTypes,
            });
          }),
          catchError((error) => {
            this.showErrorPopup(`Failed to load draft collection types: ${error.error}`);
            return of(
              CollectionActions.loadDraftCollectionTypesFailure({
                error: error.message,
              })
            );
          })
        );
      })
    )
  );

  loadPublishedCollectionTypes$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CollectionActions.loadPublishedCollectionTypes),
      mergeMap(() => {
        return this.http.get<string[]>(`${BASE_API_URL}content-type-changes/published-models`).pipe(
          map((publishedCollectionTypes) => {
            return CollectionActions.loadPublishedCollectionTypesSuccess({
              publishedCollectionTypes,
            });
          }),
          catchError((error) => {
            this.showErrorPopup(`Failed to load published collection types: ${error.error}`);
            return of(
              CollectionActions.loadPublishedCollectionTypesFailure({
                error: error.message,
              })
            );
          })
        );
      })
    )
  );

  setDefaultSelectedType$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CollectionActions.loadCollectionTypesSuccess),
      withLatestFrom(this.store.pipe(select(selectSelectedType))),
      filter(([action, selectedType]) => !selectedType && action.collectionTypes.length > 0),
      map(([action]) =>
        ContentActions.setContentType({
          selectedType: action.collectionTypes[0],
        })
      )
    )
  );

  loadFields$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CollectionActions.loadFields),
      mergeMap((action) => {
        if (!action.modelType) {
          return EMPTY;
        }

        const endpoint = `${BASE_API_URL}models/fields/${action.modelType}`;
        const enumsEndpoint = `${BASE_API_URL}enums/getAll`;

        const enumsRequest = this.enumsData
          ? of(this.enumsData)
          : this.http.get<any>(enumsEndpoint).pipe(
              map((data) => {
                this.enumsData = data;
                return data;
              })
            );

        return enumsRequest.pipe(
          mergeMap((enumsData) => {
            return this.http.get<ModelField[]>(endpoint).pipe(
              map((fields) => {
                const processedFields = fields.map((field) => {
                  const fieldType = this.mapFieldTypeToInputType(field.fieldType, enumsData);
                  return {
                    ...field,
                    isEnum: fieldType === FieldType.enum,
                  };
                });

                return CollectionActions.loadFieldsSuccess({
                  fields: [...processedFields],
                });
              }),
              catchError((error) => {
                this.showErrorPopup(`Failed to load fields: ${error.error}`);
                return of(CollectionActions.loadFieldsFailure({ error: error.message }));
              })
            );
          }),
          catchError((error) => {
            this.showErrorPopup(`Failed to load enums: ${error.error}`);
            return of(CollectionActions.loadFieldsFailure({ error: error.message }));
          })
        );
      })
    )
  );

  saveContent$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CollectionActions.saveContent),
      switchMap(() => {
        interface SaveResponse {
          success: boolean;
          restarting?: boolean;
          error?: any;
        }

        return this.http.post<SaveResponse>(`${BASE_API_URL}create-migrations-update-db`, {}).pipe(
          map((response: any) => {
            const typedResponse: SaveResponse =
              response && response.success !== undefined
                ? (response as SaveResponse)
                : { success: true };

            return CollectionActions.saveContentSuccess({
              restarting: !!typedResponse.restarting,
            });
          }),
          catchError((error) => {
            if (error.status === 200) {
              console.log('Backend is restarting. This is expected behavior.');
              return of(CollectionActions.saveContentSuccess({ restarting: true }));
            }
            this.showErrorPopup(`Failed to save content: ${error.error}`);
            console.error('Error saving content:', error);
            return of(CollectionActions.saveContentFailure({ error }));
          })
        );
      })
    )
  );

  addCollectionType$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CollectionActions.addCollectionType),
      switchMap((action) => {
        const payload = { modelName: action.collectionType };
        return this.http.post(`${BASE_API_URL}models`, payload).pipe(
          switchMap(() =>
            this.http.get(`${BASE_API_URL}update-db-context`).pipe(
              map(() =>
                CollectionActions.addCollectionTypeSuccess({
                  collectionType: action.collectionType,
                })
              ),
              catchError((error) => {
                console.error('Error updating DB context:', error);
                this.showErrorPopup(
                  `Model created but failed to update DB context: ${error.error}`
                );
                return of(CollectionActions.addCollectionTypeFailure({ error }));
              })
            )
          ),
          catchError((error) => {
            console.error('Error creating model:', error);
            this.showErrorPopup(`Failed to create model: ${error.error}`);
            return of(CollectionActions.addCollectionTypeFailure({ error }));
          })
        );
      })
    )
  );

  reloadCollectionTypesAfterAdd$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CollectionActions.addCollectionTypeSuccess),
      concatMap((action) => [
        CollectionActions.loadPublishedCollectionTypes(),
        CollectionActions.loadDraftCollectionTypes(),
        ContentActions.setContentType({
          selectedType: action.collectionType,
        }),
      ])
    )
  );

  addField$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CollectionActions.addField),
      withLatestFrom(this.store.pipe(select(selectSelectedType))),
      filter(([selectedType]) => !!selectedType),
      switchMap(([action, selectedType]) => {
        return this.http.put(`${BASE_API_URL}models/${selectedType}`, action.field).pipe(
          map(() =>
            CollectionActions.addFieldSuccess({
              field: action.field,
            })
          ),
          catchError((error) => {
            this.showErrorPopup(`Failed to add field: ${error.error}`);
            return of(CollectionActions.addFieldFailure({ error: error.message }));
          })
        );
      })
    )
  );

  reloadCollectionTypesAfterField$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CollectionActions.addFieldSuccess),
      concatMap(() => [
        CollectionActions.loadPublishedCollectionTypes(),
        CollectionActions.loadDraftCollectionTypes(),
      ])
    )
  );

  private mapFieldTypeToInputType(fieldType: string, enumsData?: any): FieldType {
    const lowerFieldType = fieldType.toLowerCase();

    if (enumsData && enumsData[fieldType]) {
      return FieldType.enum;
    }

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

  private showErrorPopup(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      horizontalPosition: 'center',
      verticalPosition: 'bottom',
      panelClass: ['error-snackbar'],
    });
  }
}
