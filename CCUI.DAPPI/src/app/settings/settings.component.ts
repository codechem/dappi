import { Component } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { DataStorageComponent } from './data-storage/data-storage.component';
import { UsersComponent } from './users/users.component';
import { RolesComponent } from './roles/roles.component';

type SettingsTab = 'storage' | 'users' | 'roles';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    MatIconModule,
    DataStorageComponent,
    UsersComponent,
    RolesComponent,
  ],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
})
export class SettingsComponent {
  activeTab: SettingsTab = 'storage';

  selectTab(tab: SettingsTab): void {
    this.activeTab = tab;
  }
}
