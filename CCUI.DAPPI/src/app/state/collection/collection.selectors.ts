import { createFeatureSelector, createSelector } from '@ngrx/store';
import { CollectionState } from './collection.state';

export const selectCollectionState = createFeatureSelector<CollectionState>('collection');

export const selectCollectionTypes = createSelector(
  selectCollectionState,
  (state) => state ? state.collectionTypes : []
);

export const selectIsLoadingCollectionTypes = createSelector(
  selectCollectionState,
  (state) => state.loadingCollectionTypes
);

export const selectCollectionTypesError = createSelector(
  selectCollectionState,
  (state) => state.errorCollectionTypes
);

export const selectFields = createSelector(
  selectCollectionState,
  (state) => state.fields
);

// export const selectFieldsByModelType = (modelType: string) => createSelector(
//   selectCollectionState,
//   (state) => state.fields[modelType] || []
// );

export const selectIsLoadingFields = createSelector(
  selectCollectionState,
  (state) => state.loadingFields
);

export const selectFieldsError = createSelector(
  selectCollectionState,
  (state) => state.errorFields
);

export const selectSearchText = createSelector(
  selectCollectionState,
  (state) => state.searchText
);