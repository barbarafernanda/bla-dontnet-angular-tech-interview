import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject, Injector } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { API_CONFIG } from '../api-config';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const injector = inject(Injector);
  const token = auth.token();
  const config = inject(API_CONFIG);
  const url = new URL(request.url, window.location.origin);
  const trusted = [config.auth, config.tasks].some(base => {
    const api = new URL(base, window.location.origin);
    return url.origin === api.origin && (url.pathname === '/api' || url.pathname.startsWith('/api/'));
  });
  const authenticatedRequest = token && trusted ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
  return next(authenticatedRequest).pipe(catchError((error: unknown) => {
    if (token && trusted && error instanceof HttpErrorResponse && error.status === 401) {
      auth.logout();
      void injector.get(Router).navigateByUrl('/login');
    }
    return throwError(() => error);
  }));
};
