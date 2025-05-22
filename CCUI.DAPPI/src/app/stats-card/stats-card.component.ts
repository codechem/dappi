import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  Input,
  OnDestroy,
  OnInit,
} from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { AddCollectionTypeDialogComponent } from '../add-collection-type-dialog/add-collection-type-dialog.component';
import { Subscription } from 'rxjs';
import * as CollectionActions from '../state/collection/collection.actions';
import { Store } from '@ngrx/store';
import {
  selectCollectionTypes,
  selectPublishedCollectionTypes,
} from '../state/collection/collection.selectors';

@Component({
  selector: 'app-stats-card',
  imports: [MatIconModule],
  templateUrl: './stats-card.component.html',
  styleUrl: './stats-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StatsCardComponent implements OnDestroy, OnInit {
  @Input() icon!: string;
  @Input() value!: string;
  @Input() title!: string;

  constructor(
    private router: Router,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef,
    private store: Store
  ) {}

  numberOfCollectionTypes: number = 0;
  numberOfPublishedCollectionTypes: number = 0;
  private subscription: Subscription = new Subscription();

  models$ = this.store.select(selectCollectionTypes);
  publishedModels$ = this.store.select(selectPublishedCollectionTypes);

  ngOnInit(): void {
    this.store.dispatch(CollectionActions.loadCollectionTypes());
    this.store.dispatch(CollectionActions.loadPublishedCollectionTypes());
    this.subscription.add(
      this.models$.subscribe((items) => {
        this.numberOfCollectionTypes = items.length;
        this.cdr.markForCheck();
      })
    );

    this.subscription.add(
      this.publishedModels$.subscribe((items) => {
        this.numberOfPublishedCollectionTypes = items.length;
        this.cdr.markForCheck();
      })
    );
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  openPopup(): void {
    this.router.navigate(['/builder']).then(() => {
      const dialogRef = this.dialog.open(AddCollectionTypeDialogComponent, {
        width: '450px',
        panelClass: 'dark-theme-dialog',
        disableClose: true,
      });
    });
  }
}
