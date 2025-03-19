import { ModelField } from '../../models/content.model';

export interface CollectionState {
  collectionTypes: string[];
  loadingCollectionTypes: boolean;
  errorCollectionTypes: string | null;
  fields: ModelField[];
  loadingFields: boolean;
  isSaving: boolean;
  saveError: any;
  serverRestarting: boolean;
}

export const initialCollectionState: CollectionState = {
  collectionTypes: [],
  loadingCollectionTypes: false,
  errorCollectionTypes: null,
  fields: [],
  loadingFields: false,
  isSaving: false,
  saveError: null,
  serverRestarting: false,
};
