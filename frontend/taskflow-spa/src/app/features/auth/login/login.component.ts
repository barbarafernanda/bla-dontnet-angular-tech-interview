import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { errorMessage } from '../../../core/http-error';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login', imports: [FormsModule, RouterLink],
  template: `<section class="card narrow"><h1>Sign in</h1><p>Welcome back to TaskFlow.</p>
    <form #form="ngForm" (ngSubmit)="submit()">
      <label>Email <input type="email" name="email" [(ngModel)]="email" required email autocomplete="email"></label>
      <label>Password <input type="password" name="password" [(ngModel)]="password" required autocomplete="current-password"></label>
      @if (error()) { <p class="error" role="alert">{{ error() }}</p> }
      <div class="actions"><button type="submit" [disabled]="form.invalid || busy()">Sign in</button>
        <button type="button" class="secondary" (click)="useDemo()">Use demo credentials</button></div>
    </form><p>New here? <a routerLink="/register">Create an account</a>.</p></section>`
})
export class LoginComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly error = signal('');
  readonly busy = signal(false);
  email = '';
  password = '';
  useDemo(): void { this.email = 'demo@taskflow.dev'; this.password = 'Demo@1234!'; }
  submit(): void {
    this.error.set(''); this.busy.set(true);
    this.auth.login(this.email, this.password).subscribe({
      next: () => { this.busy.set(false); void this.router.navigateByUrl('/tasks'); },
      error: error => { this.busy.set(false); this.error.set(errorMessage(error)); }
    });
  }
}
