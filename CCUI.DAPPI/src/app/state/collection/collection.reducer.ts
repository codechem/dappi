import { createReducer, on } from '@ngrx/store';
import * as CollectionActions from './collection.actions';
import { initialCollectionState } from './collection.state';

export const collectionReducer = createReducer(
  initialCollectionState,

  on(CollectionActions.loadCollectionTypes, (state) => ({
    ...state,
    loadingCollectionTypes: true,
    errorCollectionTypes: null,
  })),

  on(
    CollectionActions.loadCollectionTypesSuccess,
    (state, { collectionTypes }) => ({
      ...state,
      collectionTypes,
      filteredCollectionTypes: collectionTypes,
      loadingCollectionTypes: false,
    })
  ),

  on(CollectionActions.loadCollectionTypesFailure, (state, { error }) => ({
    ...state,
    loadingCollectionTypes: false,
    errorCollectionTypes: error,
  })),

  on(CollectionActions.loadFields, (state) => ({
    ...state,
    loadingFields: true,
    errorFields: null,
  })),

  on(CollectionActions.loadFieldsSuccess, (state, { fields }) => ({
    ...state,
    fields: [...fields],
    loadingFields: false,
  })),

  on(CollectionActions.loadFieldsFailure, (state, { error }) => ({
    ...state,
    loadingFields: false,
    errorFields: error,
  })),

  on(
    CollectionActions.addCollectionTypeSuccess,
    (state, { collectionType }) => {
      const updatedTypes = [...state.collectionTypes, collectionType].sort();
      return {
        ...state,
        collectionTypes: updatedTypes,
      };
    }
  ),

  on(CollectionActions.addFieldSuccess, (state, { field }) => {
    return {
      ...state,
      fields: [...state.fields, field],
    };
  }),
  on(CollectionActions.saveContent, (state) => ({
    ...state,
    isSaving: true,
    saveError: null,
    serverRestarting: false,
  })),

  on(CollectionActions.saveContentSuccess, (state, { restarting }) => ({
    ...state,
    isSaving: !restarting,
    disabled: !restarting,
    serverRestarting: !!restarting,
  })),

  on(CollectionActions.saveContentFailure, (state, { error }) => ({
    ...state,
    isSaving: false,
    saveError: error,
  }))
);
