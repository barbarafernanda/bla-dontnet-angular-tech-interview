import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { errorMessage } from '../../../core/http-error';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register', imports: [FormsModule, RouterLink],
  template: `<section class="card narrow"><h1>Create an account</h1>
    <form #form="ngForm" (ngSubmit)="submit()">
      <label>Full name <input name="fullName" [(ngModel)]="fullName" required maxlength="150" autocomplete="name"></label>
      <label>Email <input name="email" type="email" [(ngModel)]="email" required email maxlength="255" autocomplete="email"></label>
      <label>Password <input name="password" type="password" [(ngModel)]="password" required autocomplete="new-password"></label>
      @if (error()) { <p class="error" role="alert">{{ error() }}</p> }
      <button type="submit" [disabled]="form.invalid || busy()">Register</button>
    </form><p>Already have an account? <a routerLink="/login">Sign in</a>.</p></section>`
})
export class RegisterComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly error = signal('');
  readonly busy = signal(false);
  fullName = '';
  email = '';
  password = '';
  submit(): void {
    this.error.set(''); this.busy.set(true);
    this.auth.register(this.fullName, this.email, this.password).subscribe({
      next: () => { this.busy.set(false); void this.router.navigateByUrl('/login'); },
      error: error => { this.busy.set(false); this.error.set(errorMessage(error)); }
    });
  }
}
