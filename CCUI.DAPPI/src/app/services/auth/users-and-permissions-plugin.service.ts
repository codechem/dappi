import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { BASE_API_URL } from '../../../Constants';
import { CompleteInvitationRequest, InviteUserRequest } from './users-management.service';

export interface UsersAndPermissionsRoleItem {
  id: string;
  name: string;
  isDefaultForAuthenticatedUser: boolean;
}

export interface UsersAndPermissionsUserItem {
  id: number;
  userName: string;
  email: string;
  emailConfirmed: boolean;
  acceptedInvitation?: boolean;
  roleId: number;
  roleName: string;
}

export interface UsersAndPermissionsPermissionItem {
  permissionName: string;
  description: string;
  selected: boolean;
}

export type UsersAndPermissionsRolePermissionsResponse = Record<
  string,
  UsersAndPermissionsPermissionItem[]
>;

@Injectable({ providedIn: 'root' })
export class UsersAndPermissionsPluginService {
  private readonly endpoint = 'usersandpermissions';

  constructor(private http: HttpClient) { }

  getAllRoles(): Observable<UsersAndPermissionsRoleItem[]> {
    return this.http.get<any[]>(`${BASE_API_URL}${this.endpoint}/roles`).pipe(
      map((roles) => (roles ?? []).map((role) => this.normalizeRole(role)))
    );
  }

  getAllUsers(): Observable<UsersAndPermissionsUserItem[]> {
    return this.http.get<any[]>(`${BASE_API_URL}${this.endpoint}/users`).pipe(
      map((users) => (users ?? []).map((user) => this.normalizeUser(user)))
    );
  }

  getRolePermissions(roleName: string): Observable<UsersAndPermissionsRolePermissionsResponse> {
    const params = new HttpParams().set('roleName', roleName);

    return this.http.get<Record<string, any[]>>(`${BASE_API_URL}${this.endpoint}`, { params }).pipe(
      map((response) =>
        Object.fromEntries(
          Object.entries(response ?? {}).map(([controller, permissions]) => [
            controller,
            (permissions ?? []).map((permission) => this.normalizePermission(permission)),
          ])
        )
      )
    );
  }

  inviteUser(data: InviteUserRequest): Observable<UsersAndPermissionsUserItem> {
    return this.http
      .post<any>(`${BASE_API_URL}${this.endpoint}`, data)
      .pipe(map((user) => this.normalizeUser(user)));
  }

  completeInvitation(data: CompleteInvitationRequest): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(
      `${BASE_API_URL}${this.endpoint}/complete-invitation`,
      data
    );
  }

  private normalizeRole(role: any): UsersAndPermissionsRoleItem {
    return {
      id: role?.id ?? role?.Id ?? '',
      name: role?.name ?? role?.Name ?? '',
      isDefaultForAuthenticatedUser:
        role?.isDefaultForAuthenticatedUser ?? role?.IsDefaultForAuthenticatedUser ?? false,
    };
  }

  private normalizePermission(permission: any): UsersAndPermissionsPermissionItem {
    return {
      permissionName: permission?.permissionName ?? permission?.PermissionName ?? '',
      description: permission?.description ?? permission?.Description ?? '',
      selected: permission?.selected ?? permission?.Selected ?? false,
    };
  }

  private normalizeUser(user: any): UsersAndPermissionsUserItem {
    return {
      id: user?.id ?? user?.Id ?? 0,
      userName: user?.userName ?? user?.UserName ?? '',
      email: user?.email ?? user?.Email ?? '',
      emailConfirmed: user?.emailConfirmed ?? user?.EmailConfirmed ?? false,
      acceptedInvitation: user?.acceptedInvitation ?? user?.AcceptedInvitation ?? false,
      roleId: user?.roleId ?? user?.RoleId ?? 0,
      roleName: user?.roleName ?? user?.RoleName ?? '',
    };
  }
}
