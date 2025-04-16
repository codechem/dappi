import { Component, OnInit, PLATFORM_ID, Inject, OnDestroy } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { LeftMenuComponent } from './left-menu/left-menu.component';
import { AuthComponent } from './auth/auth.component';
import { Subscription } from 'rxjs';
import { AuthService } from './services/auth/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, LeftMenuComponent, AuthComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'CCUI.DAPPI';
  isAuthenticated = false;
  private authSub: Subscription | null = null;

  constructor(
    @Inject(PLATFORM_ID) private platformId: Object,
    private authService: AuthService,
  ) {}

  ngOnInit() {
    if (isPlatformBrowser(this.platformId)) {
      this.authService.checkAuthentication();
      this.authSub = this.authService.isAuthenticated$.subscribe((isAuth) => {
        this.isAuthenticated = isAuth;
      });
    }
  }

  ngOnDestroy() {
    if (this.authSub) {
      this.authSub.unsubscribe();
    }
  }
}
