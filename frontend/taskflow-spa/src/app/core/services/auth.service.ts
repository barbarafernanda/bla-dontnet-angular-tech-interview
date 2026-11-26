import { HttpClient, HttpResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { API_CONFIG } from '../api-config';

export interface AuthToken { token: string; expiresAtUtc: string; }
export interface UserProfile { id: string; fullName: string; email: string; createdAt: string; }

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly base = `${inject(API_CONFIG).auth}/api/auth`;
  readonly token = signal<string | null>(null);

  login(email: string, password: string): Observable<AuthToken> {
    return this.http.post<AuthToken>(`${this.base}/login`, { email, password }).pipe(tap(result => this.token.set(result.token)));
  }
  register(fullName: string, email: string, password: string): Observable<UserProfile> {
    return this.http.post<UserProfile>(`${this.base}/register`, { fullName, email, password });
  }
  publicStatus(): Observable<HttpResponse<unknown>> { return this.http.get(`${this.base}/public-status`, { observe: 'response' }); }
  me(): Observable<HttpResponse<UserProfile>> { return this.http.get<UserProfile>(`${this.base}/me`, { observe: 'response' }); }
  logout(): void { this.token.set(null); }
}
