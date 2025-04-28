import { Injectable } from '@angular/core';
import { Observable, catchError, throwError } from 'rxjs';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
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
  username: string;
  roles: string[];
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly API_URL = `${BASE_API_URL}Auth`;

  constructor(private http: HttpClient) {}

  login(username: string, password: string): Observable<AuthResponse> {
    const loginRequest: LoginRequest = { username, password };
    return this.http
      .post<AuthResponse>(`${this.API_URL}/login`, loginRequest)
      .pipe(catchError(this.handleError));
  }

  register(username: string, email: string, password: string): Observable<AuthResponse> {
    const registerRequest: RegisterRequest = { username, email, password };
    return this.http
      .post<AuthResponse>(`${this.API_URL}/register`, registerRequest)
      .pipe(catchError(this.handleError));
  }

  private handleError(error: HttpErrorResponse): Observable<never> {
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
          errorMessage = validationErrors.map((e) => e.description).join(' ');
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
