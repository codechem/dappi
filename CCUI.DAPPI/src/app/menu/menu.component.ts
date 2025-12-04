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

  updateDisabled:boolean= false;
  deleteDisabled:boolean = false;

  private subscription: Subscription = new Subscription();

  constructor(private store: Store) { }
  ngOnInit(): void {
    this.subscription.add(
      this.allowedCrudActions$.subscribe((allowedCrudActions) => {
        this.updateDisabled = !allowedCrudActions?.includes(CrudActions.Update);
        this.deleteDisabled = !allowedCrudActions?.includes(CrudActions.Delete);
      })
    )
  }

 

  onEdit(): void {
    this.edit.emit(this.item);
  }

  onDelete(): void {
    this.delete.emit(this.item);
  }
}
