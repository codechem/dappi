import { EnumItem } from './enum-item.model';

export interface EnumsResponse {
  [enumName: string]: EnumItem[];
}
