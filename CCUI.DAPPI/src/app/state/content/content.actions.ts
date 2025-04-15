import { createAction, props } from '@ngrx/store';
import { ContentItem, PaginatedResponse, TableHeader } from '../../models/content.model';

export const loadContent = createAction(
  '[Content] Load Content',
  props<{
    selectedType: string;
    page: number;
    limit: number;
    searchText: string;
  }>(),
);
export const loadRelatedItems = createAction(
  '[Content] Load Related Items',
  props<{
    selectedType: string;
  }>(),
);

export const loadContentSuccess = createAction(
  '[Content] Load Content Success',
  props<{
    items: PaginatedResponse;
  }>(),
);

export const loadRelatedItemsSuccess = createAction(
  '[Content] Load Related Items Success',
  props<{
    relatedItems: PaginatedResponse;
  }>(),
);

export const loadContentFailure = createAction(
  '[Content] Load Content Failure',
  props<{ error: string }>(),
);
export const loadRelatedItemsFailure = createAction(
  '[Content] Load Related Items Failure',
  props<{ error: string }>(),
);

export const loadHeaders = createAction(
  '[Content] Load Headers',
  props<{ selectedType: string }>(),
);

export const loadHeadersSuccess = createAction(
  '[Content] Load Headers Success',
  props<{ headers: TableHeader[] }>(),
);

export const loadHeadersFailure = createAction(
  '[Content] Load Headers Failure',
  props<{ error: string }>(),
);

export const setContentType = createAction(
  '[Content] Set Content Type',
  props<{ selectedType: string }>(),
);

export const setCurrentItem = createAction(
  '[Content] Set Current Item',
  props<{ currentItem: ContentItem | undefined }>(),
);

export const setIsSearching = createAction(
  '[Content] Set Is Searching',
  props<{ isSearching: boolean }>(),
);

export const setSearchText = createAction(
  '[Content] Set Search Text',
  props<{ searchText: string }>(),
);

export const deleteContent = createAction(
  '[Content] Delete Content',
  props<{ id: string; contentType: string }>(),
);

export const deleteContentSuccess = createAction(
  '[Content] Delete Content Success',
  props<{ id: string }>(),
);

export const deleteContentFailure = createAction(
  '[Content] Delete Content Failure',
  props<{ error: string }>(),
);

export const deleteMultipleContent = createAction(
  '[Content] Delete Multiple Content',
  props<{ ids: string[]; contentType: string }>(),
);

export const deleteMultipleContentSuccess = createAction(
  '[Content] Delete Multiple Content Success',
  props<{ ids: string[] }>(),
);

export const deleteMultipleContentFailure = createAction(
  '[Content] Delete Multiple Content Failure',
  props<{ error: string }>(),
);

export const createContent = createAction(
  '[Content] Create Content',
  props<{ formData: any; contentType: string }>(),
);

export const createContentSuccess = createAction('[Content] Create Content Success');

export const createContentFailure = createAction(
  '[Content] Create Content Failure',
  props<{ error: string }>(),
);

export const updateContent = createAction(
  '[Content] Update Content',
  props<{ id: string; formData: any; contentType: string }>(),
);

export const updateContentSuccess = createAction('[Content] Update Content Success');

export const updateContentFailure = createAction(
  '[Content] Update Content Failure',
  props<{ error: string }>(),
);
