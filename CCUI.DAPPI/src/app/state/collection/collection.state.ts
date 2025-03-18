import { FieldItem } from "../../fields-list/fields-list.component";
import { ModelField } from "../../models/content.model";

export interface CollectionState {
  collectionTypes: string[];
  loadingCollectionTypes: boolean;
  errorCollectionTypes: string | null;
  fields: ModelField[];
  loadingFields: boolean;
  errorFields: string | null;
  searchText: string;
}

export const initialCollectionState: CollectionState = {
  collectionTypes: [],
  loadingCollectionTypes: false,
  errorCollectionTypes: null,
  fields: [],
  loadingFields: false,
  errorFields: null,
  searchText: ''
};