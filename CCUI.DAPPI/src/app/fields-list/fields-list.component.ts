import { Component, Input, Output, EventEmitter, ChangeDetectionStrategy } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { PillComponent } from '../pill/pill.component';

export interface FieldItem {
  id?: string | number;
  name: string;
  iconText?: string;
  iconName?: string;
  type: string;
}


@Component({
  selector: 'app-fields-list',
  standalone: true,
  imports: [MatIconModule, MatButtonModule, DragDropModule, PillComponent],
  templateUrl: './fields-list.component.html',
  styleUrls: ['./fields-list.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FieldsListComponent {
  @Input() fields: FieldItem[] = [];
  @Output() fieldsChange = new EventEmitter<FieldItem[]>();
  @Output() editField = new EventEmitter<FieldItem>();
  @Output() deleteField = new EventEmitter<FieldItem>();

  onEdit(field: FieldItem): void {
    this.editField.emit(field);
  }

  onDelete(field: FieldItem): void {
    this.deleteField.emit(field);
  }

  drop(event: CdkDragDrop<FieldItem[]>): void {
    if (event.previousIndex === event.currentIndex) {
      return;
    }

    moveItemInArray(this.fields, event.previousIndex, event.currentIndex);
    this.fieldsChange.emit([...this.fields]);
  }

  trackByField(index: number, field: FieldItem): string | number {
    return field.id || field.name;
  }
}
