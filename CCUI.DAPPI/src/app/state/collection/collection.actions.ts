import { createAction, props } from '@ngrx/store';
import { ConfigureModelRequest, ModelField, ModelResponse } from '../../models/content.model';

export interface UpdateFieldPayload {
  oldFieldName: string;
  newFieldName: string;
  fieldType: string;
  isRequired: boolean;
  hasIndex: boolean;
  regex?: string;
  noPastDates?: boolean;
  min?: number | null;
  max?: number | null;
}

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
  props<{ modelResponse:ModelResponse }>()
);

export const loadFieldsFailure = createAction(
  '[Collection] Load Fields Failure',
  props<{ error: string }>()
);

export const addCollectionType = createAction(
  '[Collection] Add Collection Type',
  props<{ collectionType: string  , isAuditableEntity:boolean, crudActions:number[]}>()
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

export const updateField = createAction(
  '[Collection] Update Field',
  props<{ payload: UpdateFieldPayload }>()
);

export const updateFieldSuccess = createAction(
  '[Collection] Update Field Success',
  props<{
    oldFieldName: string;
    newFieldName: string;
  }>()
);

export const updateFieldFailure = createAction(
  '[Collection] Update Field Failure',
  props<{ error: string }>()
);

export const deleteField = createAction(
  '[Collection] Delete Field',
  props<{ modelName: string; fieldName: string }>()
);

export const deleteFieldSuccess = createAction(
  '[Collection] Delete Field Success',
  props<{ fieldName: string }>()
);

export const deleteFieldFailure = createAction(
  '[Collection] Delete Field Failure',
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

export const configureActions = createAction('[Collection] Configure actions' , props<{model:string, request:ConfigureModelRequest}>());

export const configureActionsSuccess = createAction('[Collection] Configure actions Success' , props<{message:string}>());

export const configureActionsFailure = createAction('[Collection] Configure actions Failure' , props<{error:any}>());