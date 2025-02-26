import { Component } from '@angular/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from '@angular/common';
import { DashboardComponent } from '../dashboard/dashboard.component';
import { Sidebar } from '../sidebar/sidebar.component';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-left-menu',
  standalone: true,
  imports: [
    MatTooltipModule,
    MatIconModule,
    CommonModule,
    DashboardComponent,
    Sidebar,
    RouterOutlet,
  ],
  templateUrl: './left-menu.component.html',
  styleUrl: './left-menu.component.scss',
})
export class LeftMenu {
  class = '';
  activeIcon = 'home';
  currentRoute = '';
  menuItems = [
    { icon: 'home', tooltip: 'Home' },
    { icon: 'web', tooltip: 'Builder' },
    { icon: 'article', tooltip: 'User' },
  ];
  currentComponent = '';
  currentIconColor = '';

  constructor(private router: Router) {
    this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        this.currentRoute = event.url.split('/')[1];
      }
    });
  }

  onMenuItemClick(icon: string) {
    this.activeIcon = icon;
    const url = icon === 'home' ? '/home' : '/builder';
    this.router.navigate(['/' + icon]);
  }

  isCurrentIcon(icon: string): boolean {
    return icon === this.activeIcon;
  }
}
