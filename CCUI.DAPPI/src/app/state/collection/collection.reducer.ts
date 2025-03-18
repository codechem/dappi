import { createReducer, on } from '@ngrx/store';
import * as CollectionActions from './collection.actions';
import { initialCollectionState } from './collection.state';

export const collectionReducer = createReducer(
  initialCollectionState,
  
  // Collection Types
  on(CollectionActions.loadCollectionTypes, (state) => ({
    ...state,
    loadingCollectionTypes: true,
    errorCollectionTypes: null
  })),
  
  on(CollectionActions.loadCollectionTypesSuccess, (state, { collectionTypes }) => ({
    ...state,
    collectionTypes,
    filteredCollectionTypes: collectionTypes,
    loadingCollectionTypes: false
  })),
  
  on(CollectionActions.loadCollectionTypesFailure, (state, { error }) => ({
    ...state,
    loadingCollectionTypes: false,
    errorCollectionTypes: error
  })),
  
  // Fields
  on(CollectionActions.loadFields, (state) => ({
    ...state,
    loadingFields: true,
    errorFields: null
  })),
  
  on(CollectionActions.loadFieldsSuccess, (state, { fields }) => ({
    ...state,
    fields: [
      ...fields,
    ],
    loadingFields: false
  })),
  
  on(CollectionActions.loadFieldsFailure, (state, { error }) => ({
    ...state,
    loadingFields: false,
    errorFields: error
  })),
  
  // Add Collection Type
  on(CollectionActions.addCollectionTypeSuccess, (state, { collectionType }) => {
    const updatedTypes = [...state.collectionTypes, collectionType].sort();
    return {
      ...state,
      collectionTypes: updatedTypes,
    };
  }),
  
  // // Add Field
  // on(CollectionActions.addFieldSuccess, (state, { modelType, field }) => {
  //   const currentFields = state.fields[modelType] || [];
  //   const updatedFields = [...currentFields, field];
    
  //   return {
  //     ...state,
  //     fields: {
  //       ...state.fields,
  //       [modelType]: updatedFields
  //     }
  //   };
  // })
);