import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('registers using the public endpoint', () => {
    service.register('Alice', 'alice@example.test', 'password').subscribe(profile => expect(profile.email).toBe('alice@example.test'));
    const request = http.expectOne('http://localhost:5001/api/auth/register');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ fullName: 'Alice', email: 'alice@example.test', password: 'password' });
    request.flush({ id: '1', fullName: 'Alice', email: 'alice@example.test', createdAt: '2026-01-01T00:00:00Z' });
  });

  it('stores the returned token on successful login and clears it on logout', () => {
    service.login('alice@example.test', 'password').subscribe();
    const request = http.expectOne('http://localhost:5001/api/auth/login');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ email: 'alice@example.test', password: 'password' });
    request.flush({ token: 'signed-token', expiresAtUtc: '2099-01-01T00:00:00Z' });
    expect(service.token()).toBe('signed-token');
    service.logout();
    expect(service.token()).toBeNull();
  });

  it('surfaces login failures without setting a token', () => {
    let status = 0;
    service.login('alice@example.test', 'bad').subscribe({ error: error => status = error.status });
    http.expectOne('http://localhost:5001/api/auth/login').flush({ detail: 'Invalid credentials' }, { status: 401, statusText: 'Unauthorized' });
    expect(status).toBe(401);
    expect(service.token()).toBeNull();
  });
});
