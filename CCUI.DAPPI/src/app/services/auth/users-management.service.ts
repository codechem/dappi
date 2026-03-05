import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BASE_API_URL } from '../../../Constants';

export interface InviteUserRequest {
  username: string;
  email: string;
  password: string;
  roles: string[];
}

export interface UserItem {
  Id: string;
  Name: string;
  Email: string;
  Roles: string[];
}

@Injectable({ providedIn: 'root' })
export class UsersManagementService {
  constructor(private http: HttpClient) {}

  inviteUser(data: InviteUserRequest): Observable<UserItem> {
    return this.http.post<UserItem>(`${BASE_API_URL}users`, data);
  }
}
