import { Component, OnInit, OnDestroy } from '@angular/core';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { CommonModule } from '@angular/common';
import { NavigationEnd, Router, RouterModule } from '@angular/router';
import { Subject, Subscription } from 'rxjs';
import { filter, takeUntil } from 'rxjs/operators';

interface MenuItem {
  icon: string;
  tooltip: string;
  route: string;
  id: string;
}

@Component({
  selector: 'app-left-menu',
  standalone: true,
  imports: [CommonModule, MatTooltipModule, MatIconModule, RouterModule],
  templateUrl: './left-menu.component.html',
  styleUrls: ['./left-menu.component.scss'],
})
export class LeftMenuComponent implements OnInit, OnDestroy {
  private subscriptions: Subscription = new Subscription();

  activeIcon = 'home';

  menuItems: MenuItem[] = [
    { icon: 'home', tooltip: 'Home', route: '/home', id: 'home' },
    { icon: 'web', tooltip: 'Builder', route: '/builder', id: 'builder' },
    {
      icon: 'article',
      tooltip: 'Content Manager',
      route: '/content-manager',
      id: 'content-manager',
    },
  ];

  private destroy$ = new Subject<void>();

  constructor(private router: Router) {}

  ngOnInit(): void {
    this.subscriptions.add(
      this.router.events
        .pipe(
          filter((event) => event instanceof NavigationEnd),
          takeUntil(this.destroy$)
        )
        .subscribe((event: any) => {
          const currentPath = event.url.split('/')[1] || 'home';
          this.updateActiveIcon(currentPath);
        })
    );

    const initialPath = this.router.url.split('/')[1] || 'home';
    this.updateActiveIcon(initialPath);
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }

  onMenuItemClick(id: string): void {
    this.activeIcon = id;
  }

  isCurrentIcon(icon: string): boolean {
    return icon === this.activeIcon;
  }

  private updateActiveIcon(path: string): void {
    const menuItem = this.menuItems.find(
      (item) => item.route === `/${path}` || item.id === path
    );

    if (menuItem) {
      this.activeIcon = menuItem.id;
    }
  }
}
