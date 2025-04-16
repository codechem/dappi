import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, catchError, map, tap, throwError } from 'rxjs';
import { isPlatformBrowser } from '@angular/common';
import { Inject, PLATFORM_ID } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';
import { BASE_API_URL } from '../../../Constants';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  user: {
    id: string;
    username: string;
    email: string;
  };
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private isAuthenticatedSubject = new BehaviorSubject<boolean>(false);
  public isAuthenticated$ = this.isAuthenticatedSubject.asObservable();

  private userSubject = new BehaviorSubject<any>(null);
  public user$ = this.userSubject.asObservable();
  private readonly API_URL = `${BASE_API_URL}Auth`;

  constructor(
    @Inject(PLATFORM_ID) private platformId: Object,
    private http: HttpClient,
    private router: Router,
  ) {
    if (isPlatformBrowser(this.platformId)) {
      this.checkAuthentication();
    }
  }

  login(username: string, password: string): Observable<AuthResponse> {
    const loginRequest: LoginRequest = { username, password };

    return this.http.post<AuthResponse>(`${this.API_URL}/login`, loginRequest).pipe(
      tap((response) => {
        this.handleAuthResponse(response);
      }),
      catchError(this.handleError),
    );
  }

  register(username: string, email: string, password: string): Observable<AuthResponse> {
    const registerRequest: RegisterRequest = { username, email, password };

    return this.http
      .post<AuthResponse>(`${this.API_URL}/register`, registerRequest)
      .pipe(catchError(this.handleError));
  }

  logout(): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem('jwt_token');
      localStorage.removeItem('user_data');
      this.isAuthenticatedSubject.next(false);
      this.userSubject.next(null);
      this.router.navigate(['/auth']);
    }
  }

  checkAuthentication(): void {
    const isValid = this.isTokenValid();

    this.isAuthenticatedSubject.next(isValid);

    if (!isValid) {
      localStorage.removeItem('jwt_token');
      localStorage.removeItem('user_data');
      this.userSubject.next(null);
    } else if (isPlatformBrowser(this.platformId)) {
      try {
        const userData = localStorage.getItem('user_data');
        if (userData) {
          const parsedData = JSON.parse(userData);
          this.userSubject.next(parsedData);
        }
      } catch (e) {}
    }
  }

  getToken(): string | null {
    if (isPlatformBrowser(this.platformId)) {
      return localStorage.getItem('jwt_token');
    }
    return null;
  }

  saveToken(token: string): void {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem('jwt_token', token);
    }
  }

  public isTokenValid(): boolean {
    if (!isPlatformBrowser(this.platformId)) return false;

    try {
      const token = localStorage.getItem('jwt_token');
      if (!token) return false;

      const tokenParts = token.split('.');
      if (tokenParts.length !== 3) return false;

      const tokenPayload = JSON.parse(atob(tokenParts[1]));
      if (!tokenPayload.exp) return true; // No expiration means valid

      const expiration = tokenPayload.exp * 1000;
      const now = Date.now();
      const isValid = expiration > now;

      return isValid;
    } catch (error) {
      return false;
    }
  }

  private handleAuthResponse(response: AuthResponse): void {
    if (response && response.token) {
      localStorage.setItem('jwt_token', response.token);
      localStorage.setItem('user_data', JSON.stringify(response.user));

      this.isAuthenticatedSubject.next(true);

      this.userSubject.next(response.user);
    }
  }

  private handleError(error: HttpErrorResponse) {
    let errorMessage = 'An unknown error occurred!';

    if (error.error instanceof ErrorEvent) {
      errorMessage = `Error: ${error.error.message}`;
    } else {
      if (error.status === 401) {
        errorMessage = 'Invalid username or password';
      } else if (error.status === 400) {
        if (error.error && typeof error.error === 'object') {
          const validationErrors = [];
          for (const key in error.error) {
            if (error.error.hasOwnProperty(key)) {
              validationErrors.push(error.error[key]);
            }
          }
          errorMessage = validationErrors.join('. ');
        } else {
          errorMessage = error.error?.message || 'Bad request';
        }
      } else {
        errorMessage = `Error Code: ${error.status}, Message: ${error.error?.message || error.message}`;
      }
    }

    return throwError(() => new Error(errorMessage));
  }
}
