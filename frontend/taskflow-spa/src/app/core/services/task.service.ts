import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_CONFIG } from '../api-config';
import { TaskItem, TaskWrite } from '../models/task.model';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly base = `${inject(API_CONFIG).tasks}/api/tasks`;
  list(): Observable<TaskItem[]> { return this.http.get<TaskItem[]>(this.base); }
  get(id: string): Observable<TaskItem> { return this.http.get<TaskItem>(`${this.base}/${encodeURIComponent(id)}`); }
  create(task: TaskWrite): Observable<TaskItem> { return this.http.post<TaskItem>(this.base, task); }
  update(id: string, task: TaskWrite): Observable<TaskItem> { return this.http.put<TaskItem>(`${this.base}/${encodeURIComponent(id)}`, task); }
  delete(id: string): Observable<void> { return this.http.delete<void>(`${this.base}/${encodeURIComponent(id)}`); }
}
