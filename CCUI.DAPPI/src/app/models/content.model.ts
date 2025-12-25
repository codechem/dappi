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
  'dateonly',
  'select',
  'role',
  'enum',
}

export enum CrudActions {
  Get = 0,
  GetOne = 1,
  GetAll = 2,
  Create = 3,
  Update = 4,
  Patch = 5,
  Delete = 6
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
  regex?: string;
  minLength?: number;
  maxLength?: number;
}

export interface ModelResponse {
  Fields: ModelField[],
  AllowedActions: CrudActions[]
}

export interface EnumKvp {
  value: number,
  label:string
}

export interface ConfigureModelRequest {
  crudActions: CrudActions[]
}