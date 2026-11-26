import { InjectionToken } from '@angular/core';

export interface ApiConfig { auth: string; tasks: string; }
export const API_CONFIG = new InjectionToken<ApiConfig>('TaskFlow API URLs', {
  providedIn: 'root',
  factory: () => ({ auth: 'http://localhost:5001', tasks: 'http://localhost:5002' }),
});
