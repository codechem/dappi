import { ContentItem, PaginatedResponse, TableHeader } from '../../models/content.model';
import { RecentContent } from '../../models/recent-content';

export interface ContentState {
  items: PaginatedResponse | undefined;
  headers: TableHeader[];
  selectedType: string;
  loading: boolean;
  error: string | null;
  totalItems: number;
  itemsPerPage: number;
  currentItem: ContentItem | undefined;
  isSearching: boolean;
  relatedItems: PaginatedResponse | undefined;
  contentTypeChanges: Array<RecentContent> | null;
  loadingContentTypeChanges: boolean;
}

export const initialContentState: ContentState = {
  currentItem: undefined,
  items: undefined,
  headers: [],
  selectedType: '',
  loading: false,
  error: null,
  totalItems: 0,
  itemsPerPage: 10,
  isSearching: false,
  relatedItems: undefined,
  loadingContentTypeChanges: false,
  contentTypeChanges: null,
};
