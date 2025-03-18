import { createReducer, on } from '@ngrx/store';
import * as ContentActions from './content.actions';
import { initialContentState } from './content.state';

export const contentReducer = createReducer(
  initialContentState,

  on(ContentActions.loadContent, (state) => ({
    ...state,
    loading: true,
    error: null,
  })),

  on(ContentActions.loadContentSuccess, (state, { items }) => ({
    ...state,
    items,
    loading: false,
  })),

  on(ContentActions.loadContentFailure, (state, { error }) => ({
    ...state,
    loading: false,
    error,
  })),

  on(ContentActions.setContentType, (state, { selectedType }) => ({
    ...state,
    selectedType,
    currentPage: 1,
  })),

  on(ContentActions.setHeaders, (state, { headers }) => ({
    ...state,
    headers,
  })),

  on(ContentActions.loadHeadersSuccess, (state, { headers }) => ({
    ...state,
    headers,
  })),

  on(ContentActions.setSearchText, (state, { searchText }) => ({
    ...state,
    searchText,
    currentPage: 1,
  })),

  on(ContentActions.setCurrentItem, (state, { currentItem }) => ({
    ...state,
    currentItem,
  })),

  on(ContentActions.setCurrentPage, (state, { page }) => ({
    ...state,
    currentPage: page,
  })),

  on(ContentActions.deleteContent, (state) => ({
    ...state,
    loading: true,
  })),

  on(ContentActions.deleteContentSuccess, (state) => ({
    ...state,
    loading: false,
  })),

  on(ContentActions.deleteContentFailure, (state, { error }) => ({
    ...state,
    loading: false,
    error,
  })),

  on(ContentActions.deleteMultipleContent, (state) => ({
    ...state,
    loading: true,
  })),

  on(ContentActions.deleteMultipleContentSuccess, (state) => ({
    ...state,
    selectedItems: new Set<string>(),
    loading: false,
  })),

  on(ContentActions.deleteMultipleContentFailure, (state, { error }) => ({
    ...state,
    loading: false,
    error,
  })),

  on(ContentActions.createContent, (state) => ({
    ...state,
    loading: true,
    error: null,
  })),

  on(ContentActions.createContentSuccess, (state) => ({
    ...state,
    loading: false,
  })),

  on(ContentActions.createContentFailure, (state, { error }) => ({
    ...state,
    loading: false,
    error,
  })),

  on(ContentActions.updateContent, (state) => ({
    ...state,
    loading: true,
    error: null,
  })),

  on(ContentActions.updateContentSuccess, (state) => ({
    ...state,
    loading: false,
  })),

  on(ContentActions.updateContentFailure, (state, { error }) => ({
    ...state,
    loading: false,
    error,
  }))
);