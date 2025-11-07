import { ContentTypeState } from "../enums/contentTypeState";

export interface RecentContent {
  Id: number;
  ModelName: string;
  Fields: Record<string, any>;
  ModifiedBy: string;
  ModifiedAt: string;
  State: ContentTypeState;
}
