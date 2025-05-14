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
  publishedCollectionTypes: string[];
  draftCollectionTypes: string[];
}

export const initialCollectionState: CollectionState = {
  publishedCollectionTypes: [],
  collectionTypes: [],
  loadingCollectionTypes: false,
  errorCollectionTypes: null,
  fields: [],
  loadingFields: false,
  isSaving: false,
  saveError: null,
  serverRestarting: false,
  draftCollectionTypes: [],
};
