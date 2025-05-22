import { Component, ChangeDetectionStrategy } from '@angular/core';

import { StatsCardComponent } from '../stats-card/stats-card.component';
import { RecentContentTableComponent } from '../recent-content-table/recent-content-table.component';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss'],
  standalone: true,
  imports: [StatsCardComponent, RecentContentTableComponent, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardComponent {
  constructor() {}
}
