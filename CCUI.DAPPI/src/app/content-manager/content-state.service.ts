import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { TableHeader } from '../models/content.model';

@Injectable({
  providedIn: 'root',
})
export class ContentStateService {
  private _headersSubject = new BehaviorSubject<TableHeader[]>([]);
  private _selectedTypeSubject = new BehaviorSubject<string>('Article');
  private _itemData = new BehaviorSubject<any>(undefined);

  headers$ = this._headersSubject.asObservable();
  selectedType$ = this._selectedTypeSubject.asObservable();

  setContentCreateData(
    headers: TableHeader[],
    selectedType: string,
    itemData: any = undefined
  ): void {
    this._headersSubject.next(headers);
    this._selectedTypeSubject.next(selectedType);
    this._itemData.next(itemData);
  }

  getHeaders(): TableHeader[] {
    return this._headersSubject.getValue();
  }

  getSelectedType(): string {
    return this._selectedTypeSubject.getValue();
  }

  getItemData(): any {
    return this._itemData;
  }

  clearContentCreateData(): void {
    this._headersSubject.next([]);
    this._selectedTypeSubject.next('Article');
    this._itemData.next(undefined);
  }
}
