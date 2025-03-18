import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard/dashboard.component';
import { BuilderComponent } from './builder/builder.component';
import { ContentManagerComponent } from './content-manager/content-manager.component';
import { NewEntryComponent } from './new-entry/new-entry.component';

export const routes: Routes = [
  { path: '', redirectTo: '/home', pathMatch: 'full' },
  { path: 'home', component: DashboardComponent },
  { path: 'builder', component: BuilderComponent },
  { path: 'content-manager', component: ContentManagerComponent },
  { path: 'content-create', component: NewEntryComponent },
];
