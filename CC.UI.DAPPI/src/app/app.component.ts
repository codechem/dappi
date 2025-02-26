import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Sidebar } from './sidebar/sidebar.component';
import { LeftMenu } from './left-menu/left-menu.component';
import { DashboardComponent } from './dashboard/dashboard.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, Sidebar, LeftMenu, DashboardComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  title = 'CC.UI.DAPPI';
}
