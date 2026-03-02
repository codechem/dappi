import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BASE_API_URL } from '../../../Constants';

export interface RoleItem {
  Id: string;
  Name: string;
  UserCount: number;
}

export interface RolesResponse {
  Total: number;
  Data: RoleItem[];
}

@Injectable({providedIn: 'root'})
export class RolesManagementService {
  constructor(private http: HttpClient) {}

  getRoles(searchTerm = ''): Observable<RolesResponse> {
    const params = new HttpParams().set('searchTerm', searchTerm);

    return this.http.get<RolesResponse>(`${BASE_API_URL}roles`, { params });
  }

  createRole(name: string): Observable<RoleItem> {
    return this.http.post<RoleItem>(`${BASE_API_URL}roles`, { name });
  }

  deleteRole(id: string): Observable<void> {
    return this.http.delete<void>(`${BASE_API_URL}roles/${id}`);
  }
}
