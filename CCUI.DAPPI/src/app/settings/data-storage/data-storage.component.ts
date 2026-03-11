import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Subscription } from 'rxjs';
import { SettingsService } from '../../services/common/settings.service';

@Component({
  selector: 'app-data-storage',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule],
  templateUrl: './data-storage.component.html',
  styleUrl: './data-storage.component.scss',
})
export class DataStorageComponent implements OnInit, OnDestroy {
  private subscription = new Subscription();

  isLoading = false;
  error = '';
  storageSource: string = '';

  constructor(private settingsService: SettingsService) {}

  ngOnInit(): void {
    this.loadStorageSource();
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  private loadStorageSource(): void {
    this.isLoading = true;
    this.error = '';

    this.subscription.add(
      this.settingsService.getStorageSource().subscribe({
        next: (response) => {
          this.storageSource = response;
          this.isLoading = false;
        },
        error: (error) => {
          const apiMessage = error?.error?.message || error?.error?.title;
          this.error = apiMessage || 'Failed to load storage source.';
          this.isLoading = false;
        },
      })
    );
  }
}
