export interface ContentItem {
  id: string;
  [key: string]: any;
}

export interface PaginatedResponse {
  total: number;
  offset: number;
  limit: number;
  data: ContentItem[];
}

export interface TableHeader {
  key: string;
  label: string;
  type: 'text' | 'textarea' | 'file' | 'collection' | 'id';
  relatedTo?: string;
}

export interface DataResponse {
  $id: string;
  $values: any[];
}

export interface ApiResponse<T> {
  total: number;
  offset: number;
  limit: number;
  data: T[];
}

export interface ModelField {
  fieldName: string;
  fieldType: string;
}

export interface PaginatedResponse {
  total: number;
  offset: number;
  limit: number;
  data: ContentItem[];
}
export interface ModelField {
  fieldName: string;
  fieldType: string;
  relatedTo?: string;
  isRequired?: string;
}
