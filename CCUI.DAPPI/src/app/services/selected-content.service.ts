import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SelectedContentService {
  private selectedTypeSource = new BehaviorSubject<string>('');
  currentSelectedType = this.selectedTypeSource.asObservable();

  setSelectedType(type: string): void {
    this.selectedTypeSource.next(type);
  }
}