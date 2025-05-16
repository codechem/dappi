import { createFeatureSelector, createSelector } from '@ngrx/store';
import { ContentState } from './content.state';

export const selectContentState = createFeatureSelector<ContentState>('content');

export const selectItems = createSelector(selectContentState, (state) => state.items);
export const selectRelatedItems = createSelector(selectContentState, (state) => state.relatedItems);

export const selectHeaders = createSelector(selectContentState, (state) => state.headers);

export const selectSelectedType = createSelector(selectContentState, (state) => state.selectedType);

export const selectLoading = createSelector(selectContentState, (state) => state.loading);

export const selectError = createSelector(selectContentState, (state) => state.error);

export const selectTotalItems = createSelector(selectContentState, (state) => state.totalItems);

export const selectItemsPerPage = createSelector(selectContentState, (state) => state.itemsPerPage);

export const selectCurrentItem = createSelector(selectContentState, (state) => state.currentItem);

export const selectIsSearching = createSelector(selectContentState, (state) => state.isSearching);

export const selectContentTypeChanges = createSelector(
  selectContentState,
  (state) => state.contentTypeChanges,
);

export const selectLoadingContentTypeChanges = createSelector(
  selectContentState,
  (state) => state.loadingContentTypeChanges,
);
