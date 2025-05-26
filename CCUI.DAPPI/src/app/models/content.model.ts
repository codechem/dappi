export interface ContentItem {
  Id: string;
  [key: string]: any;
}

export interface PaginatedResponse<T = ContentItem[]> {
  Total: number;
  Offset: number;
  Limit: number;
  Data: T;
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
  'enum',
}
export interface TableHeader {
  key: string;
  label: string;
  type: FieldType;
  isRequired: boolean;
  relatedTo?: string;
  isEnum?: boolean;
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
  relatedTo?: string;
  isRequired?: boolean;
  isEnum?: boolean;
}
