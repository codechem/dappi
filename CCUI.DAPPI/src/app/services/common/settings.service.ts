import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { BASE_API_URL } from '../../../Constants';

@Injectable({ providedIn: 'root' })
export class SettingsService {
  constructor(private http: HttpClient) {}

  private shortNameToLongNameMap: Record<string, string> = {
    ['aws-s3']: 'Amazon S3',
    ['local']: 'Local Storage'
  }

  getStorageSource(): Observable<string> {
    return this.http.get(`${BASE_API_URL}providers/storage`, { responseType: 'text' }).pipe(
      map((storageProviderName) => {
        return this.shortNameToLongNameMap[storageProviderName] ?? storageProviderName;
      })
    );
  }
}
