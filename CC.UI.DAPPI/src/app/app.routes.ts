import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard/dashboard.component';
import { Sidebar } from './sidebar/sidebar.component';
import { BuilderComponent } from './builder/builder.component';
import { ContentManagerComponent } from './content-manager/content-manager.component';

export const routes: Routes = [
  { path: '', redirectTo: '/home', pathMatch: 'full' },
  { path: 'home', component: DashboardComponent },
  { path: 'builder', component: BuilderComponent },
  { path: 'content-manager', component: ContentManagerComponent },
];
