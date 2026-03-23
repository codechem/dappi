import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BASE_API_URL } from '../../../Constants';

export interface UsersAndPermissionsRoleItem {
  Id: string;
  Name: string;
  IsDefaultForAuthenticatedUser: boolean;
}

export interface UsersAndPermissionsPermissionItem {
  PermissionName: string;
  Description: string;
  Selected: boolean;
}

export type UsersAndPermissionsRolePermissionsResponse = Record<
  string,
  UsersAndPermissionsPermissionItem[]
>;

@Injectable({ providedIn: 'root' })
export class UsersAndPermissionsPluginService {
  constructor(private http: HttpClient) {}

  getAllRoles(): Observable<UsersAndPermissionsRoleItem[]> {
    return this.http.get<UsersAndPermissionsRoleItem[]>(`${BASE_API_URL}usersandpermissions/roles`);
  }

  getRolePermissions(roleName: string): Observable<UsersAndPermissionsRolePermissionsResponse> {
    const params = new HttpParams().set('roleName', roleName);

    return this.http.get<UsersAndPermissionsRolePermissionsResponse>(
      `${BASE_API_URL}usersandpermissions`,
      { params }
    );
  }
}
