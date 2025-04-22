import { createAction, props } from '@ngrx/store';
import { User } from './auth.state';

export const login = createAction('[Auth] Login', props<{ username: string; password: string }>());

export const loginSuccess = createAction(
  '[Auth] Login Success',
  props<{ user: User; token: string }>(),
);

export const loginFailure = createAction('[Auth] Login Failure', props<{ error: string }>());

export const register = createAction(
  '[Auth] Register',
  props<{ username: string; email: string; password: string }>(),
);

export const registerSuccess = createAction(
  '[Auth] Register Success',
  props<{ message: string }>(),
);

export const registerFailure = createAction('[Auth] Register Failure', props<{ error: string }>());

export const checkAuth = createAction('[Auth] Check Auth Status');

export const authStatusSuccess = createAction(
  '[Auth] Auth Status Success',
  props<{ user: User; token: string }>(),
);

export const authStatusFailure = createAction('[Auth] Auth Status Failure');

export const logout = createAction('[Auth] Logout');
export const logoutSuccess = createAction('[Auth] Logout Success');
