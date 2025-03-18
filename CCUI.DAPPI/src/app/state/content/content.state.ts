import { ContentItem, PaginatedResponse, TableHeader } from "../../models/content.model";

export interface ContentState {
  items: PaginatedResponse | undefined;
  headers: TableHeader[];
  selectedType: string;
  loading: boolean;
  error: string | null;
  totalItems: number;
  currentPage: number;
  itemsPerPage: number;
  searchText: string;
  selectedItems: Set<string>;
}

export const initialContentState: ContentState = {
  items: undefined,
  headers: [],
  selectedType: '',
  loading: false,
  error: null,
  totalItems: 0,
  currentPage: 1,
  itemsPerPage: 10,
  searchText: '',
  selectedItems: new Set<string>()
};