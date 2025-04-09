import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StoreModule } from '@ngrx/store';
import { EffectsModule } from '@ngrx/effects';
import { contentReducer } from './content/content.reducer';
import { ContentEffects } from './content/content.effects';
import { collectionReducer } from './collection/collection.reducer';
import { CollectionEffects } from './collection/collection.effects';

@NgModule({
  declarations: [],
  imports: [
    CommonModule,
    StoreModule.forFeature('content', contentReducer),
    StoreModule.forFeature('collection', collectionReducer),
    EffectsModule.forFeature([ContentEffects, CollectionEffects]),
  ],
})
export class ContentModule {}
