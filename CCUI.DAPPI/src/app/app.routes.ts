import { Routes } from '@angular/router';
import { DashboardComponent } from './dashboard/dashboard.component';
import { BuilderComponent } from './builder/builder.component';
import { ContentManagerComponent } from './content-manager/content-manager.component';
import { NewEntryComponent } from './new-entry/new-entry.component';
import { SchemaImporterComponent } from './schema-importer/schema-importer.component';
import { AuthComponent } from './auth/auth.component';
import { AuthGuard } from './services/auth/auth.guard';
import { NonAuthGuard } from './services/auth/non-auth.guard';

export const routes: Routes = [
  { path: '', redirectTo: '/home', pathMatch: 'full' },
  { path: 'home', component: DashboardComponent, canActivate: [AuthGuard] },
  { path: 'builder', component: BuilderComponent, canActivate: [AuthGuard] },
  { path: 'content-manager', component: ContentManagerComponent, canActivate: [AuthGuard] },
  { path: 'content-create', component: NewEntryComponent, canActivate: [AuthGuard] },
  { path: 'schema-importer', component: SchemaImporterComponent, canActivate: [AuthGuard] },
  { path: 'auth', component: AuthComponent, canActivate: [NonAuthGuard] },
  { path: '**', redirectTo: '/home' },
];
