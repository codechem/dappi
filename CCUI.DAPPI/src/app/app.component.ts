import { Component, OnInit, PLATFORM_ID, Inject } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { RouterOutlet } from '@angular/router';
import { LeftMenuComponent } from './left-menu/left-menu.component';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { checkAuth } from './state/auth/auth.actions';
import { selectIsAuthenticated } from './state/auth/auth.selectors';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, LeftMenuComponent, CommonModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent implements OnInit {
  title = 'CCUI.DAPPI';
  isAuthenticated$: Observable<boolean>;

  constructor(
    @Inject(PLATFORM_ID) private platformId: object,
    private store: Store
  ) {
    this.isAuthenticated$ = this.store.select(selectIsAuthenticated);
  }

  ngOnInit() {
    if (isPlatformBrowser(this.platformId)) {
      this.store.dispatch(checkAuth());
    }
  }
}
