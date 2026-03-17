import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BASE_API_URL } from '../../../Constants';

export interface InviteUserRequest {
  username: string;
  email: string;
  password?: string;
  roles: string[];
}

export interface CompleteInvitationRequest {
  token: string;
  oldPassword: string;
  newPassword: string;
}

export interface UserItem {
  Id: string;
  Name: string;
  Email: string;
  Roles: string[];
}

export interface PluginsStateResponse {
  services: Record<string, boolean>;
}

@Injectable({ providedIn: 'root' })
export class UsersManagementService {
  constructor(private http: HttpClient) {}

  getPluginsState(): Observable<PluginsStateResponse> {
    return this.http.get<PluginsStateResponse>(`${BASE_API_URL}plugins`);
  }

  inviteUser(data: InviteUserRequest): Observable<UserItem> {
    return this.http.post<UserItem>(`${BASE_API_URL}users`, data);
  }

  completeInvitation(data: CompleteInvitationRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${BASE_API_URL}users/complete-invitation`, data);
  }
}
