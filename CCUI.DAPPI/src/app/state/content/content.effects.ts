import { Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { of } from 'rxjs';
import { map, mergeMap, catchError, withLatestFrom } from 'rxjs/operators';
import { HttpClient } from '@angular/common/http';
import { Store } from '@ngrx/store';
import * as ContentActions from './content.actions';
import { ModelField, PaginatedResponse } from '../../models/content.model';
import {
  selectCurrentPage,
  selectItemsPerPage,
  selectSearchText,
  selectSelectedType,
} from './content.selectors';

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
          .get<PaginatedResponse>(endpoint, {
            params: {
              offset: ((action.page - 1) * action.limit).toString(),
              limit: action.limit.toString(),
              SearchTerm: action.searchText || '',
            },
          })

          .pipe(
            map((items) => ContentActions.loadContentSuccess({ items })),
            catchError((error) =>
              of(ContentActions.loadContentFailure({ error: error.message }))
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
        return this.http.get<ModelField[]>(endpoint).pipe(
          map((fields) => {
            const headers = fields.map((field) => ({
              key:
                field.fieldName.charAt(0).toLowerCase() +
                field.fieldName.slice(1),
              label: this.formatHeaderLabel(field.fieldName),
              type: this.mapFieldTypeToInputType(field.fieldType),
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
        this.store.select(selectCurrentPage),
        this.store.select(selectItemsPerPage),
        this.store.select(selectSearchText),
        this.store.select(selectSelectedType)
      ),
      map(([_, page, limit, searchText, selectedType]) =>
        ContentActions.loadContent({
          selectedType,
          page,
          limit,
          searchText,
        })
      )
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

  constructor(
    private actions$: Actions,
    private http: HttpClient,
    private store: Store
  ) {}
}
