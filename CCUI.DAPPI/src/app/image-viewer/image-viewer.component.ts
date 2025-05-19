import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { SafeUrl } from '@angular/platform-browser';

@Component({
  selector: 'app-image-viewer',
  standalone: true,
  imports: [CommonModule, MatIconModule, MatButtonModule],
  templateUrl: './image-viewer.component.html',
  styleUrls: ['./image-viewer.component.scss'],
})
export class ImageViewerComponent {
  @Input() imageUrl: SafeUrl | string = '';
  @Input() title = 'Image Preview';
  @Output() closed = new EventEmitter<void>();

  zoomLevel = 1;

  zoomIn(): void {
    if (this.zoomLevel < 3) {
      this.zoomLevel += 0.25;
    }
  }

  zoomOut(): void {
    if (this.zoomLevel > 0.5) {
      this.zoomLevel -= 0.25;
    }
  }

  resetZoom(): void {
    this.zoomLevel = 1;
  }

  close(): void {
    this.closed.emit();
  }

  onOverlayClick(event: MouseEvent): void {
    // Only close if clicking directly on the overlay, not on its children
    if (event.target === event.currentTarget) {
      this.close();
    }
  }
}
