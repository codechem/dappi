import { Component } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { UsersComponent } from './users/users.component';
import { RolesComponent } from './roles/roles.component';

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
export class SettingsComponent {
  activeTab: SettingsTab = 'users';

  selectTab(tab: SettingsTab): void {
    this.activeTab = tab;
  }
}
