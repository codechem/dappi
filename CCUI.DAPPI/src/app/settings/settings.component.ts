import { Component, OnDestroy, OnInit } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { Subscription } from 'rxjs';
import { UsersComponent } from './users/users.component';
import { RolesComponent } from './roles/roles.component';
import { SettingsService } from '../services/common/settings.service';

type SettingsTab = 'users' | 'roles';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    MatIconModule,
    UsersComponent,
    RolesComponent,
  ],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
})
export class SettingsComponent implements OnInit, OnDestroy {
  private subscription = new Subscription();
  activeTab: SettingsTab = 'users';
  storageSource = 'Unknown';
  usesS3 = false;

  constructor(private settingsService: SettingsService) {}

  ngOnInit(): void {
    this.subscription.add(
      this.settingsService.getStorageSource().subscribe({
        next: (response) => {
          this.usesS3 = response.UsesS3;
          this.storageSource = response.Source;
        },
        error: () => {
          this.usesS3 = false;
          this.storageSource = 'Unknown';
        },
      })
    );
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  selectTab(tab: SettingsTab): void {
    this.activeTab = tab;
  }
}
