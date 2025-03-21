import { Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { of } from 'rxjs';
import {
  map,
  mergeMap,
  catchError,
  withLatestFrom,
  switchMap,
  filter,
} from 'rxjs/operators';
import { HttpClient } from '@angular/common/http';
import { select, Store } from '@ngrx/store';
import * as CollectionActions from './collection.actions';
import { ModelField } from '../../models/content.model';
import { selectSelectedType } from '../content/content.selectors';
import * as ContentActions from '../content/content.actions';

@Injectable()
export class CollectionEffects {
  loadCollectionTypes$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CollectionActions.loadCollectionTypes),
      mergeMap(() => {
        return this.http.get<string[]>('http://localhost:5101/api/models').pipe(
          map((collectionTypes) => {
            return CollectionActions.loadCollectionTypesSuccess({
              collectionTypes,
            });
          }),
          catchError((error) =>
            of(
              CollectionActions.loadCollectionTypesFailure({
                error: error.message,
              })
            )
          )
        );
      })
    )
  );

  setDefaultSelectedType$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CollectionActions.loadCollectionTypesSuccess),
      withLatestFrom(this.store.pipe(select(selectSelectedType))),
      filter(
        ([action, selectedType]) =>
          !selectedType && action.collectionTypes.length > 0
      ),
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
        const endpoint = `http://localhost:5101/api/models/fields/${action.modelType}`;
        return this.http.get<ModelField[]>(endpoint).pipe(
          map((fields) =>
            CollectionActions.loadFieldsSuccess({
              fields: [...fields],
            })
          ),
          catchError((error) =>
            of(CollectionActions.loadFieldsFailure({ error: error.message }))
          )
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

        return this.http
          .post<SaveResponse>(
            'http://localhost:5101/api/create-migrations-update-db',
            {}
          )
          .pipe(
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
                console.log(
                  'Backend is restarting. This is expected behavior.'
                );
                return of(
                  CollectionActions.saveContentSuccess({ restarting: true })
                );
              }
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
        return this.http.post('http://localhost:5101/api/models', payload).pipe(
          switchMap((response) =>
            this.http.get('http://localhost:5101/api/update-db-context').pipe(
              map(() =>
                CollectionActions.addCollectionTypeSuccess({
                  collectionType: action.collectionType,
                })
              ),
              catchError((error) => {
                console.error('Error updating DB context:', error);
                alert(
                  'Model created but failed to update DB context. Please try again.'
                );
                return of(
                  CollectionActions.addCollectionTypeFailure({ error })
                );
              })
            )
          ),
          catchError((error) => {
            console.error('Error creating model:', error);
            alert('Failed to create model. Please try again.');
            return of(CollectionActions.addCollectionTypeFailure({ error }));
          })
        );
      })
    )
  );

  addField$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CollectionActions.addField),
      withLatestFrom(this.store.pipe(select(selectSelectedType))),
      filter(([action, selectedType]) => !!selectedType),
      switchMap(([action, selectedType]) => {
        const payload = {
          fieldName: action.field.fieldName,
          fieldType: action.field.fieldType,
        };

        return this.http
          .put(`http://localhost:5101/api/models/${selectedType}`, payload)
          .pipe(
            map((response) =>
              CollectionActions.addFieldSuccess({
                field: payload,
              })
            ),
            catchError((error) =>
              of(CollectionActions.addFieldFailure({ error: error.message }))
            )
          );
      })
    )
  );

  constructor(
    private actions$: Actions,
    private http: HttpClient,
    private store: Store
  ) {}
}
