import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BASE_API_URL } from '../../../Constants';

export interface usersAndPermissionsRoleItem {
  id: string;
  name: string;
  isDefaultForAuthenticatedUser: boolean;
}

export interface usersAndPermissionsPermissionItem {
  permissionName: string;
  description: string;
  selected: boolean;
}

export type usersAndPermissionsRolePermissionsResponse = Record<
  string,
  usersAndPermissionsPermissionItem[]
>;

@Injectable({ providedIn: 'root' })
export class UsersAndPermissionsPluginService {
  constructor(private http: HttpClient) {}

  getAllRoles(): Observable<usersAndPermissionsRoleItem[]> {
    return this.http.get<usersAndPermissionsRoleItem[]>(`${BASE_API_URL}usersandpermissions/roles`);
  }

  getRolePermissions(roleName: string): Observable<usersAndPermissionsRolePermissionsResponse> {
    const params = new HttpParams().set('roleName', roleName);

    return this.http.get<usersAndPermissionsRolePermissionsResponse>(
      `${BASE_API_URL}usersandpermissions`,
      { params }
    );
  }
}
