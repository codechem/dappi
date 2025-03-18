import {
  Component,
  EventEmitter,
  Input,
  Output,
  ChangeDetectionStrategy,
} from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-menu',
  standalone: true,
  imports: [MatIconModule, CommonModule],
  templateUrl: './menu.component.html',
  styleUrls: ['./menu.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MenuComponent {
  @Input() item: any;
  @Input() editText: string = 'Edit';
  @Input() deleteText: string = 'Delete';

  @Output() edit = new EventEmitter<any>();
  @Output() delete = new EventEmitter<any>();

  onEdit(): void {
    this.edit.emit(this.item);
  }

  onDelete(): void {
    this.delete.emit(this.item);
  }
}
