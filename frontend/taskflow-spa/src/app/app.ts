import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/services/auth.service';

@Component({
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  selector: 'app-root',
  template: `<header><nav class="navigation"><a class="brand" routerLink="/">TaskFlow</a>
    <div class="nav-links"><a routerLink="/tasks" routerLinkActive="active">Tasks</a>
      <a routerLink="/api-demo" routerLinkActive="active">API demo</a>
      @if (auth.token()) { <button class="secondary" type="button" (click)="logout()">Sign out</button> }
      @else { <a routerLink="/login" routerLinkActive="active">Sign in</a> }</div>
  </nav></header><main><router-outlet /></main>`
})
export class App {
  readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  logout(): void { this.auth.logout(); void this.router.navigateByUrl('/login'); }
}
