import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

interface App {
  name: string;
  status: string;
  error: string | null;
  cmsUrl: string;
  repoUrl: string | null;
  dokployUrl: string | null;
}

// The API returns validation errors as plain text.
function errorText(e: unknown, fallback: string): string {
  return e instanceof HttpErrorResponse && typeof e.error === 'string' ? e.error : fallback;
}

@Component({
  selector: 'app-root',
  imports: [FormsModule],
  template: `
    <header>
      <h1>Dappi Portal</h1>
      <p>Create a Dappi CMS app on Dokploy from an empty GitLab repo.</p>
    </header>

    <form (ngSubmit)="create()">
      <h2>New app</h2>
      <label>
        Name
        <input name="name" [(ngModel)]="name" placeholder="my-app" required />
      </label>
      <label>
        Empty GitLab repository
        <input name="repoUrl" [(ngModel)]="repoUrl" placeholder="https://gitlab.example.com/team/my-app" required />
      </label>
      <label>
        Project access token
        <input name="token" type="password" [(ngModel)]="token" required />
        <small>Maintainer role, with the api and write_repository scopes.</small>
      </label>
      <button [disabled]="busy()">Create</button>
      @if (formError()) {
        <p class="error">{{ formError() }}</p>
      }
    </form>

    <section>
      <h2>Apps</h2>
      <table>
        <tr><th>Name</th><th>Status</th><th>Links</th><th></th></tr>
        @for (app of apps(); track app.name) {
          <tr>
            <td>{{ app.name }}</td>
            <td>
              <span class="status" [attr.data-status]="app.status">{{ app.status }}</span>
              @if (app.error) {
                <div class="error">{{ app.error }}</div>
              }
            </td>
            <td class="links">
              <a [href]="app.cmsUrl" target="_blank">CMS</a>
              @if (app.repoUrl) { <a [href]="app.repoUrl" target="_blank">GitLab</a> }
              @if (app.dokployUrl) { <a [href]="app.dokployUrl" target="_blank">Dokploy</a> }
            </td>
            <td><button class="danger" (click)="remove(app.name)" [disabled]="busy()">Delete</button></td>
          </tr>
        } @empty {
          <tr><td colspan="4" class="empty">No apps yet.</td></tr>
        }
      </table>
    </section>

    <p class="hint">CMS login: admin / Dappi&#64;123. Logs and redeploys are in Dokploy.</p>
  `,
})
export class AppComponent implements OnInit, OnDestroy {
  private readonly http = inject(HttpClient);
  private timer?: ReturnType<typeof setInterval>;

  readonly apps = signal<App[]>([]);
  readonly busy = signal(false);
  readonly formError = signal('');
  name = '';
  repoUrl = '';
  token = '';

  ngOnInit(): void {
    void this.load();
    this.timer = setInterval(() => void this.load(), 5000);
  }

  ngOnDestroy(): void {
    clearInterval(this.timer);
  }

  async load(): Promise<void> {
    this.apps.set(await firstValueFrom(this.http.get<App[]>('/api/apps')));
  }

  async create(): Promise<void> {
    this.busy.set(true);
    this.formError.set('');
    try {
      await firstValueFrom(this.http.post('/api/apps', { name: this.name, repoUrl: this.repoUrl, token: this.token }));
      this.name = this.repoUrl = this.token = '';
      await this.load();
    } catch (e) {
      this.formError.set(errorText(e, 'Could not create the app.'));
    } finally {
      this.busy.set(false);
    }
  }

  async remove(name: string): Promise<void> {
    if (!confirm(`Delete ${name}? Its Dokploy project and database are removed, the GitLab repo stays.`)) {
      return;
    }

    this.busy.set(true);
    try {
      await firstValueFrom(this.http.delete(`/api/apps/${name}`));
      await this.load();
    } catch (e) {
      alert(errorText(e, 'Could not delete the app.'));
    } finally {
      this.busy.set(false);
    }
  }
}
