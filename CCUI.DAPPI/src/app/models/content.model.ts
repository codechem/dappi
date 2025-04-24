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

export enum FieldType {
  'text',
  'textarea',
  'file',
  'collection',
  'id',
  'relation',
  'number',
  'checkbox',
  'date',
  'select',
  'role',
}
export interface TableHeader {
  key: string;
  label: string;
  type: FieldType;
  isRequired: boolean;
  relatedTo?: string;
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
  isRequired?: boolean;
}
