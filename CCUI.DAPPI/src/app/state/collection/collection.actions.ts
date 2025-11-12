import { createAction, props } from '@ngrx/store';
import { ModelField } from '../../models/content.model';

export const loadPublishedCollectionTypes = createAction(
  '[Collection] Load Published Collection Types'
);

export const loadPublishedCollectionTypesSuccess = createAction(
  '[Collection] Load Published Collection Types Success',
  props<{ publishedCollectionTypes: string[] }>()
);

export const loadPublishedCollectionTypesFailure = createAction(
  '[Collection] Load Published Collection Types Failure',
  props<{ error: string }>()
);

export const loadDraftCollectionTypes = createAction('[Collection] Load Draft Collection Types');

export const loadDraftCollectionTypesSuccess = createAction(
  '[Collection] Load Draft Collection Types Success',
  props<{ draftCollectionTypes: string[] }>()
);

export const loadDraftCollectionTypesFailure = createAction(
  '[Collection] Load Draft Collection Types Failure',
  props<{ error: string }>()
);

export const loadCollectionTypes = createAction('[Collection] Load Collection Types');

export const loadCollectionTypesSuccess = createAction(
  '[Collection] Load Collection Types Success',
  props<{ collectionTypes: string[] }>()
);

export const loadCollectionTypesFailure = createAction(
  '[Collection] Load Collection Types Failure',
  props<{ error: string }>()
);

export const loadFields = createAction('[Collection] Load Fields', props<{ modelType: string }>());

export const loadFieldsSuccess = createAction(
  '[Collection] Load Fields Success',
  props<{ fields: ModelField[] }>()
);

export const loadFieldsFailure = createAction(
  '[Collection] Load Fields Failure',
  props<{ error: string }>()
);

export const addCollectionType = createAction(
  '[Collection] Add Collection Type',
  props<{ collectionType: string  , isAuditableEntity:boolean}>()
);

export const addCollectionTypeSuccess = createAction(
  '[Collection] Add Collection Type Success',
  props<{ collectionType: string }>()
);

export const addCollectionTypeFailure = createAction(
  '[Collection] Add Collection Type Failure',
  props<{ error: string }>()
);

export const addField = createAction('[Collection] Add Field', props<{ field: ModelField }>());

export const addFieldSuccess = createAction(
  '[Collection] Add Field Success',
  props<{ field: ModelField }>()
);

export const addFieldFailure = createAction(
  '[Collection] Add Field Failure',
  props<{ error: string }>()
);

export const saveContent = createAction('[Collection] Save Content');

export const saveContentSuccess = createAction(
  '[Collection] Save Content Success',
  props<{ restarting?: boolean }>()
);

export const saveContentFailure = createAction(
  '[Collection] Save Content Failure',
  props<{ error: any }>()
);

export const deleteCollectionType = createAction('[Collection] Delete Collection Type' , props<{modelName:string}>());

export const deleteCollectionTypeSuccess = createAction(
  '[Collection] Delete Collection Type Success',
  props<{message: string}>()
)

export const deleteCollectionTypeFailure = createAction(
  '[Collection] Delete Collection Type Failure',
  props<{ error: any }>()
);

export const collectionHasRelatedProperties = createAction('[Colleciton] Collection has related properties',
  props<{modelName: string}>()
)

export const collectionHasRelatedPropertiesSuccess = createAction('[Colleciton] Collection has related properties Success',
  props<{hasRelatedProperties: boolean}>()
)
export const collectionHasRelatedPropertiesFailure = createAction('[Colleciton] Collection has related properties Failure',
  props<{error: any}>()
)
