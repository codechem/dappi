import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-drawer',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  templateUrl: './drawer.component.html',
  styleUrl: './drawer.component.scss',
})
export class DrawerComponent {
  @Input() isOpen = false;
  @Input() title = '';
  @Input() width = '35%';
  @Output() closed = new EventEmitter<void>();
  
  objectKeys = Object.keys;
  
  close(): void {
    this.closed.emit();
  }
}