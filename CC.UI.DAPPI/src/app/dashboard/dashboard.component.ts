import { Component } from '@angular/core';
import { StatsCardComponent } from '../stats-card/stats-card.component';
import { RecentContentTableComponent } from '../recent-content-table/recent-content-table.component';
import { MatIcon, MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [StatsCardComponent, RecentContentTableComponent, MatIconModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent {}
