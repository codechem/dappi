import { Component, OnInit, PLATFORM_ID, Inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { LeftMenuComponent } from './left-menu/left-menu.component';
import { AuthComponent } from './auth/auth.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, LeftMenuComponent, AuthComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent implements OnInit {
  title = 'CCUI.DAPPI';
  isAuthenticated = false;

  constructor(@Inject(PLATFORM_ID) private platformId: Object) {}

  ngOnInit() {
    this.checkAuthentication();
  }

  checkAuthentication() {
    if (isPlatformBrowser(this.platformId)) {
      try {
        const token = localStorage.getItem('jwt_token');

        if (token) {
          try {
            // Simple expiration check
            const tokenParts = token.split('.');
            if (tokenParts.length !== 3) {
              this.isAuthenticated = false;
              return;
            }

            const tokenPayload = JSON.parse(atob(tokenParts[1]));

            if (!tokenPayload.exp) {
              this.isAuthenticated = true;
              return;
            }

            const expiration = tokenPayload.exp * 1000;
            const now = Date.now();

            this.isAuthenticated = expiration > now;

            if (!this.isAuthenticated) {
              localStorage.removeItem('jwt_token');
            }
          } catch (error) {
            localStorage.removeItem('jwt_token');
            this.isAuthenticated = false;
          }
        } else {
          this.isAuthenticated = false;
        }
      } catch (e) {
        this.isAuthenticated = false;
      }
    } else {
      console.log('Not running in browser environment, skipping authentication check');
    }
  }
}
