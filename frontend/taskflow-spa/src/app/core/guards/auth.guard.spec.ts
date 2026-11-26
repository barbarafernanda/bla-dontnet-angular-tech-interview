import { provideHttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { AuthService } from '../services/auth.service';
import { authGuard } from './auth.guard';

@Component({ selector: 'test-tasks-page', template: 'Tasks' }) class TasksPage {}
@Component({ selector: 'test-login-page', template: 'Login' }) class LoginPage {}

describe('authGuard', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideHttpClient(), provideRouter([
    { path: 'tasks', component: TasksPage, canActivate: [authGuard] },
    { path: 'login', component: LoginPage },
  ])] }));

  it('redirects an unauthenticated visitor to login', async () => {
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/tasks');
    expect(TestBed.inject(Router).url).toBe('/login');
  });

  it('allows an authenticated visitor to access tasks', async () => {
    TestBed.inject(AuthService).token.set('signed-token');
    const harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/tasks');
    expect(TestBed.inject(Router).url).toBe('/tasks');
  });
});
