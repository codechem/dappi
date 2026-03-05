import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { Store } from '@ngrx/store';
import { Observable, combineLatest, map, take } from 'rxjs';
import { selectIsAuthenticated, selectUser } from '../../state/auth/auth.selectors';
import * as AuthActions from '../../state/auth/auth.actions';

export const AdminGuard: CanActivateFn = ():
  | Observable<boolean | UrlTree>
  | Promise<boolean | UrlTree>
  | boolean
  | UrlTree => {
  const store = inject(Store);
  const router = inject(Router);

  store.dispatch(AuthActions.checkAuth());

  return combineLatest([store.select(selectIsAuthenticated), store.select(selectUser)]).pipe(
    take(1),
    map(([isAuthenticated, user]) => {
      if (!isAuthenticated) {
        return router.createUrlTree(['/auth']);
      }

      if (user?.roles.includes('Admin')) {
        return true;
      }

      return router.createUrlTree(['/home']);
    })
  );
};