import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { errorMessage } from '../../../core/http-error';
import { TaskItem } from '../../../core/models/task.model';
import { TaskService } from '../../../core/services/task.service';
import { TaskFormComponent } from '../task-form/task-form.component';

@Component({
  selector: 'app-task-list', imports: [DatePipe, TaskFormComponent],
  template: `
    <div class="section-heading"><div><h1>Your tasks</h1><p>Plan and track your work.</p></div>
      <button type="button" (click)="editing.set(null); showForm.set(true)">New task</button></div>
    @if (error()) { <p class="error" role="alert">{{ error() }}</p> }
    @if (showForm()) { <app-task-form [task]="editing()" (saved)="onSaved()" (cancelled)="showForm.set(false)" /> }
    @if (loading()) { <p>Loading tasks...</p> }
    @else if (tasks().length === 0) { <p class="card">No tasks yet.</p> }
    @for (task of tasks(); track task.id) {
      <article class="card"><div class="section-heading"><h2>{{ task.title }}</h2><span class="badge">{{ task.status }}</span></div>
        @if (task.description) { <p>{{ task.description }}</p> }
        <p>Due: {{ task.dueDate | date:'medium' }}</p>
        <div class="actions"><button type="button" class="secondary" (click)="editing.set(task); showForm.set(true)">Edit</button>
          <button type="button" class="danger" (click)="delete(task.id)">Delete</button></div>
      </article>
    }`
})
export class TaskListComponent implements OnInit {
  private readonly service = inject(TaskService);
  readonly tasks = signal<TaskItem[]>([]);
  readonly editing = signal<TaskItem | null>(null);
  readonly showForm = signal(false);
  readonly loading = signal(false);
  readonly error = signal('');
  ngOnInit(): void { this.load(); }
  load(): void {
    this.loading.set(true); this.error.set('');
    this.service.list().subscribe({ next: tasks => { this.tasks.set(tasks); this.loading.set(false); }, error: error => { this.loading.set(false); this.error.set(errorMessage(error)); } });
  }
  delete(id: string): void {
    this.error.set('');
    this.service.delete(id).subscribe({ next: () => this.load(), error: error => this.error.set(errorMessage(error)) });
  }
  onSaved(): void { this.showForm.set(false); this.load(); }
}
