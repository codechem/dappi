import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { EnumsResponse } from '../../models/enums-response.model';
import { BASE_API_URL } from '../../../Constants';

export interface CreateEnumRequest {
  name: string;
  values: { name: string; value: number }[];
}

export interface UpdateEnumRequest {
  values: { name: string; value: number }[];
}

@Injectable({ providedIn: 'root' })
export class EnumManagementService {
  constructor(private http: HttpClient) {}

  getAllEnums(): Observable<EnumsResponse> {
    return this.http
      .get<{ [enumName: string]: { [key: string]: number } }>(`${BASE_API_URL}enum-manager/getAll`)
      .pipe(
        map((raw) => {
          const result: EnumsResponse = {};
          for (const enumName in raw) {
            result[enumName] = Object.keys(raw[enumName]).map((name) => ({
              name,
              value: raw[enumName][name],
            }));
          }
          return result;
        })
      );
  }

  getEnum(enumName: string): Observable<{ [key: string]: number }> {
    return this.http.get<{ [key: string]: number }>(`${BASE_API_URL}enum-manager/${enumName}`);
  }

  createEnum(name: string, values: { name: string; value: number }[]): Observable<{ [key: string]: number }> {
    const request: CreateEnumRequest = { name, values };
    return this.http.post<{ [key: string]: number }>(`${BASE_API_URL}enum-manager`, request);
  }

  updateEnum(enumName: string, values: { name: string; value: number }[]): Observable<{ [key: string]: number }> {
    const request: UpdateEnumRequest = { values };
    return this.http.put<{ [key: string]: number }>(`${BASE_API_URL}enum-manager/${enumName}`, request);
  }

  deleteEnum(enumName: string): Observable<void> {
    return this.http.delete<void>(`${BASE_API_URL}enum-manager/${enumName}`);
  }
}