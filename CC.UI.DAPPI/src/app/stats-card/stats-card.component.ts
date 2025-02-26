import { Component, Input } from '@angular/core';
import { MatCard } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-stats-card',
  standalone: true,
  imports: [MatIconModule, MatCard],
  templateUrl: './stats-card.component.html',
  styleUrl: './stats-card.component.scss',
})
export class StatsCardComponent {
  @Input() icon!: string;
  @Input() value!: string;
  @Input() title!: string;
}
