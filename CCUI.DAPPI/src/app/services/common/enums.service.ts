import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { EnumsResponse } from '../../models/enums-response.model';
import { BASE_API_URL } from '../../../Constants';

@Injectable({ providedIn: 'root' })
export class EnumsService {
  constructor(private http: HttpClient) {}

  getEnums(): Observable<EnumsResponse> {
    return this.http
      .get<{ [enumName: string]: { [key: string]: number } }>(`${BASE_API_URL}enums/getAll`)
      .pipe(
        map((raw) => {
          const result: EnumsResponse = {};
          for (const enumName in raw) {
            const items = raw[enumName];
            result[enumName] = Object.keys(items).map((name) => ({
              name,
              value: items[name],
            }));
          }
          return result;
        })
      );
  }
}
