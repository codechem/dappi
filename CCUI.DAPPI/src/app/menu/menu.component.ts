import { Component, EventEmitter, Input, Output, ChangeDetectionStrategy, OnInit } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { Store } from '@ngrx/store';
import { selectAllowedCrudActions } from '../state/collection/collection.selectors';
import { CrudActions } from '../models/content.model';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-menu',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './menu.component.html',
  styleUrls: ['./menu.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MenuComponent implements OnInit {
  @Input() item: any;
  @Input() editText: string = 'Edit';
  @Input() deleteText: string = 'Delete';

  @Output() edit = new EventEmitter<any>();
  @Output() delete = new EventEmitter<any>();

  allowedCrudActions$ = this.store.select(selectAllowedCrudActions);
  allowedCrudActions: CrudActions[] | undefined = []
  crudActions = CrudActions;
  private subscription: Subscription = new Subscription();

  constructor(private store: Store) { }
  ngOnInit(): void {
    this.subscription.add(
      this.allowedCrudActions$.subscribe((allowedCrudActions) => (
        this.allowedCrudActions = allowedCrudActions
      ))
    )
  }

 

  onEdit(): void {
    this.edit.emit(this.item);
  }

  onDelete(): void {
    this.delete.emit(this.item);
  }
}
