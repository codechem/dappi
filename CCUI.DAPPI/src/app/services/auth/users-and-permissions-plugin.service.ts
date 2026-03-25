import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { BASE_API_URL } from '../../../Constants';

export interface UsersAndPermissionsRoleItem {
  id: string;
  name: string;
  isDefaultForAuthenticatedUser: boolean;
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

  constructor(private http: HttpClient) {}

  getAllRoles(): Observable<UsersAndPermissionsRoleItem[]> {
    return this.http.get<any[]>(`${BASE_API_URL}${this.endpoint}/roles`).pipe(
      map((roles) => (roles ?? []).map((role) => this.normalizeRole(role)))
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
}
