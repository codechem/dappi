import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-pill',
  imports: [CommonModule],
  templateUrl: './pill.component.html',
  styleUrl: './pill.component.scss',
})
export class PillComponent {
  @Input() text: string | undefined = '';
}
