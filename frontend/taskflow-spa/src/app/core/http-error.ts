import { HttpErrorResponse } from '@angular/common/http';

export function errorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const detail: unknown = error.error?.detail;
    if (typeof detail === 'string' && detail.trim()) return detail;
    return error.status === 0 ? 'The service is unavailable. Please try again.' : `Request failed (HTTP ${error.status}).`;
  }
  return 'An unexpected error occurred. Please try again.';
}
