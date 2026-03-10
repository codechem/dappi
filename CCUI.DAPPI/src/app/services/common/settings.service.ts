import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BASE_API_URL } from '../../../Constants';

export interface StorageSourceResponse {
  UsesS3: boolean;
  Source: string;
}

@Injectable({ providedIn: 'root' })
export class SettingsService {
  constructor(private http: HttpClient) {}

  getStorageSource(): Observable<StorageSourceResponse> {
    return this.http.get<StorageSourceResponse>(`${BASE_API_URL}models/storage-source`);
  }
}
