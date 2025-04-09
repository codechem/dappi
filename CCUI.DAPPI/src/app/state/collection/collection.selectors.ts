import { createFeatureSelector, createSelector } from '@ngrx/store';
import { CollectionState } from './collection.state';

export const selectCollectionState = createFeatureSelector<CollectionState>('collection');

export const selectCollectionTypes = createSelector(selectCollectionState, (state) =>
  state ? state.collectionTypes : [],
);

export const selectIsLoadingCollectionTypes = createSelector(
  selectCollectionState,
  (state) => state.loadingCollectionTypes,
);

export const selectCollectionTypesError = createSelector(
  selectCollectionState,
  (state) => state.errorCollectionTypes,
);

export const selectFields = createSelector(selectCollectionState, (state) => state.fields);

export const selectIsLoadingFields = createSelector(
  selectCollectionState,
  (state) => state.loadingFields,
);

export const selectIsSaving = createSelector(selectCollectionState, (state) => state.isSaving);

export const selectServerRestarting = createSelector(
  selectCollectionState,
  (state) => state.serverRestarting,
);

export const selectSaveError = createSelector(selectCollectionState, (state) => state.saveError);
