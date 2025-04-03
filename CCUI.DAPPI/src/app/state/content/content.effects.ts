import { Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { of } from 'rxjs';
import { map, mergeMap, catchError, withLatestFrom } from 'rxjs/operators';
import { HttpClient } from '@angular/common/http';
import { Store } from '@ngrx/store';
import * as ContentActions from './content.actions';
import { selectItemsPerPage, selectSelectedType } from './content.selectors';
import { DataResponse } from '../../models/content.model';

@Injectable()
export class ContentEffects {
  loadContent$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ContentActions.loadContent),
      withLatestFrom(this.store.select(selectSelectedType)),
      mergeMap(([action, selectedType]) => {
        const endpoint = `http://localhost:5101/api/${selectedType
          .toLowerCase()
          .replace(/\s+/g, '-')}`;

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
                  data: response.data.$values,
                },
              })
            ),
            catchError((error) =>
              of(ContentActions.loadContentFailure({ error: error.message }))
            )
          );
      })
    )
  );

  loadRelatedItems$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ContentActions.loadRelatedItems),
      mergeMap((action) => {
        const endpoint = `http://localhost:5101/api/${action.selectedType
          .toLowerCase()
          .replace(/\s+/g, '-')}`;

        return this.http.get<any>(endpoint).pipe(
          map((response) =>
            ContentActions.loadRelatedItemsSuccess({
              relatedItems: {
                ...response,
                data: response.data.$values,
              },
            })
          ),
          catchError((error) =>
            of(ContentActions.loadRelatedItemsFailure({ error: error.message }))
          )
        );
      })
    )
  );

  loadHeaders$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ContentActions.loadHeaders),
      mergeMap((action) => {
        const endpoint = `http://localhost:5101/api/models/fields/${action.selectedType}`;
        return this.http.get<DataResponse>(endpoint).pipe(
          map((response) => {
            const headers = response.$values.map((field) => ({
              key:
                field.fieldName.charAt(0).toLowerCase() +
                field.fieldName.slice(1),
              label: this.formatHeaderLabel(field.fieldName),
              type: this.mapFieldTypeToInputType(field.fieldType),
              relatedTo: field.fieldType.includes('ICollection')
                ? field?.fieldType?.match(/<([^>]+)>/)?.[1]
                : undefined,
              isRequired: field.isRequired ?? false,
            }));
            return ContentActions.loadHeadersSuccess({ headers });
          }),
          catchError((error) =>
            of(ContentActions.loadHeadersFailure({ error: error.message }))
          )
        );
      })
    )
  );

  deleteContent$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ContentActions.deleteContent),
      mergeMap((action) => {
        const endpoint = `http://localhost:5101/api/${action.contentType
          .toLowerCase()
          .replace(/\s+/g, '-')}/${action.id}`;

        return this.http.delete(endpoint).pipe(
          map(() => ContentActions.deleteContentSuccess({ id: action.id })),
          catchError((error) =>
            of(ContentActions.deleteContentFailure({ error: error.message }))
          )
        );
      })
    )
  );

  deleteMultipleContent$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ContentActions.deleteMultipleContent),
      mergeMap((action) => {
        const deletePromises = action.ids.map((id) => {
          const endpoint = `http://localhost:5101/api/${action.contentType
            .toLowerCase()
            .replace(/\s+/g, '-')}/${id}`;

          return this.http.delete(endpoint).toPromise();
        });

        return Promise.all(deletePromises)
          .then(() =>
            ContentActions.deleteMultipleContentSuccess({ ids: action.ids })
          )
          .catch((error) =>
            ContentActions.deleteMultipleContentFailure({
              error: error.message,
            })
          );
      })
    )
  );

  reloadAfterDelete$ = createEffect(() =>
    this.actions$.pipe(
      ofType(
        ContentActions.deleteContentSuccess,
        ContentActions.deleteMultipleContentSuccess
      ),
      withLatestFrom(
        this.store.select(selectItemsPerPage),
        this.store.select(selectSelectedType)
      ),
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
        const endpoint = `http://localhost:5101/api/${action.contentType
          .toLowerCase()
          .replace(/\s+/g, '-')}`;

        return this.http
          .post(endpoint, action.formData, {
            headers: {
              // Don't set Content-Type here - Angular will set it automatically
              // with the correct boundary for multipart/form-data
            },
          })
          .pipe(
            map((response) => {
              this.store.dispatch(ContentActions.createContentSuccess());
              return ContentActions.loadContent({
                selectedType: action.contentType,
                page: 1,
                limit: itemsPerPage,
                searchText: '',
              });
            }),
            catchError((error) =>
              of(ContentActions.createContentFailure({ error: error.message }))
            )
          );
      })
    )
  );

  updateContent$ = createEffect(() =>
    this.actions$.pipe(
      ofType(ContentActions.updateContent),
      withLatestFrom(this.store.select(selectItemsPerPage)),
      mergeMap(([action, itemsPerPage]) => {
        const endpoint = `http://localhost:5101/api/${action.contentType
          .toLowerCase()
          .replace(/\s+/g, '-')}/${action.id}`;

        return this.http
          .put(endpoint, action.formData, {
            headers: {
              // Don't set Content-Type here - Angular will set it automatically
            },
          })
          .pipe(
            map((response) => {
              this.store.dispatch(ContentActions.createContentSuccess());
              return ContentActions.loadContent({
                selectedType: action.contentType,
                page: 1,
                limit: itemsPerPage,
                searchText: '',
              });
            }),
            catchError((error) =>
              of(ContentActions.updateContentFailure({ error: error.message }))
            )
          );
      })
    )
  );

  private formatHeaderLabel(key: string): string {
    return key
      .replace(/([A-Z])/g, ' $1')
      .replace(/_/g, ' ')
      .replace(/^./, (char) => char.toUpperCase())
      .trim();
  }

  private mapFieldTypeToInputType(
    fieldType: string
  ): 'text' | 'textarea' | 'file' | 'collection' | 'id' {
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

    if (lowerFieldType.includes('icollection')) {
      return 'collection';
    }
    if (lowerFieldType.includes('Guid')) return 'id';

    return 'text';
  }

  constructor(
    private actions$: Actions,
    private http: HttpClient,
    private store: Store
  ) {}
}
