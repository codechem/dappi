import { createFeatureSelector, createSelector } from '@ngrx/store';
import { ContentState } from './content.state';

export const selectContentState = createFeatureSelector<ContentState>('content');

export const selectItems = createSelector(
  selectContentState,
  (state) => state.items
);

export const selectHeaders = createSelector(
  selectContentState,
  (state) => state.headers
);

export const selectSelectedType = createSelector(
  selectContentState,
  (state) => state.selectedType
);

export const selectLoading = createSelector(
  selectContentState,
  (state) => state.loading
);

export const selectError = createSelector(
  selectContentState,
  (state) => state.error
);

export const selectTotalItems = createSelector(
  selectContentState,
  (state) => state.totalItems
);

export const selectCurrentPage = createSelector(
  selectContentState,
  (state) => state.currentPage
);

export const selectItemsPerPage = createSelector(
  selectContentState,
  (state) => state.itemsPerPage
);

export const selectSearchText = createSelector(
  selectContentState,
  (state) => state.searchText
);

export const selectTotalPages = createSelector(
  selectTotalItems,
  selectItemsPerPage,
  (totalItems, itemsPerPage) => Math.ceil(totalItems / itemsPerPage)
);

export const selectPaginationArray = createSelector(
  selectCurrentPage,
  selectTotalPages,
  (currentPage, totalPages) => {
    const paginationArray = [];
    
    if (totalPages > 0) {
      paginationArray.push(1);
    }

    const pagesToShow = 5;
    const halfPagesToShow = Math.floor(pagesToShow / 2);

    let startPage = Math.max(2, currentPage - halfPagesToShow);
    let endPage = Math.min(totalPages - 1, currentPage + halfPagesToShow);

    if (startPage > 2) {
      paginationArray.push(-1);
    }

    for (let i = startPage; i <= endPage; i++) {
      if (!paginationArray.includes(i)) {
        paginationArray.push(i);
      }
    }

    if (endPage < totalPages - 1 && totalPages > 1) {
      paginationArray.push(-1); // -1 represents ellipsis
    }

    if (totalPages > 1 && !paginationArray.includes(totalPages)) {
      paginationArray.push(totalPages);
    }

    return paginationArray;
  }
);