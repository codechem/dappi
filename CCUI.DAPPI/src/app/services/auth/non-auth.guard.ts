import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { Store } from '@ngrx/store';
import { Observable, map, take } from 'rxjs';
import { selectIsAuthenticated } from '../../state/auth/auth.selectors';
import * as AuthActions from '../../state/auth/auth.actions';

export const NonAuthGuard: CanActivateFn = ():
  | Observable<boolean | UrlTree>
  | Promise<boolean | UrlTree>
  | boolean
  | UrlTree => {
  const store = inject(Store);
  const router = inject(Router);

  store.dispatch(AuthActions.checkAuth());

  return store.select(selectIsAuthenticated).pipe(
    take(1),
    map((isAuthenticated) => {
      if (!isAuthenticated) {
        return true;
      } else {
        return router.createUrlTree(['/home']);
      }
    })
  );
};
