import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from '../services/auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let client: HttpClient;
  let http: HttpTestingController;
  let auth: AuthService;
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting()] });
    client = TestBed.inject(HttpClient); http = TestBed.inject(HttpTestingController); auth = TestBed.inject(AuthService);
  });
  afterEach(() => http.verify());

  it('adds a bearer token to TaskFlow API requests', () => {
    auth.token.set('signed-token');
    client.get('http://localhost:5002/api/tasks').subscribe();
    const request = http.expectOne('http://localhost:5002/api/tasks');
    expect(request.request.headers.get('Authorization')).toBe('Bearer signed-token');
    request.flush([]);
  });

  it('leaves requests unchanged without a token and never leaks it to other origins', () => {
    client.get('http://localhost:5002/api/tasks').subscribe();
    const anonymous = http.expectOne('http://localhost:5002/api/tasks');
    expect(anonymous.request.headers.has('Authorization')).toBe(false); anonymous.flush([]);
    auth.token.set('signed-token');
    client.get('https://untrusted.example/data').subscribe();
    const external = http.expectOne('https://untrusted.example/data');
    expect(external.request.headers.has('Authorization')).toBe(false); external.flush({});
  });
});
