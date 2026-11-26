import { Component, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { errorMessage } from '../../../core/http-error';
import { TaskItem, TaskStatus, TaskWrite } from '../../../core/models/task.model';
import { TaskService } from '../../../core/services/task.service';

@Component({
  selector: 'app-task-form', imports: [FormsModule],
  template: `
    <section class="card">
      <h2>{{ task() ? 'Edit task' : 'New task' }}</h2>
      <form #form="ngForm" (ngSubmit)="submit()">
        <label>Title <input name="title" [(ngModel)]="title" required maxlength="120"></label>
        <label>Description <textarea name="description" [(ngModel)]="description" maxlength="8000"></textarea></label>
        <label>Status <select name="status" [(ngModel)]="status">
          <option value="Pending">Pending</option><option value="InProgress">In progress</option>
          <option value="Completed">Completed</option><option value="Cancelled">Cancelled</option>
        </select></label>
        <label>Due date <input name="dueDate" type="datetime-local" [(ngModel)]="dueDate" required></label>
        @if (error()) { <p class="error" role="alert">{{ error() }}</p> }
        <div class="actions"><button type="submit" [disabled]="form.invalid || busy()">Save</button>
          <button type="button" class="secondary" (click)="cancelled.emit()">Cancel</button></div>
      </form>
    </section>`
})
export class TaskFormComponent {
  private readonly service = inject(TaskService);
  readonly task = input<TaskItem | null>(null);
  readonly saved = output<TaskItem>();
  readonly cancelled = output<void>();
  readonly error = signal('');
  readonly busy = signal(false);
  title = '';
  description = '';
  status: TaskStatus = 'Pending';
  dueDate = '';

  constructor() {
    effect(() => {
      const task = this.task();
      this.title = task?.title ?? '';
      this.description = task?.description ?? '';
      this.status = task?.status ?? 'Pending';
      if (task) {
        const date = new Date(task.dueDate);
        this.dueDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
      } else this.dueDate = '';
    });
  }

  submit(): void {
    const date = new Date(this.dueDate);
    if (!this.title.trim() || Number.isNaN(date.getTime())) { this.error.set('Enter a title and a valid due date.'); return; }
    this.save({ title: this.title.trim(), description: this.description || null, status: this.status, dueDate: date.toISOString() });
  }
  save(value: TaskWrite): void {
    this.error.set(''); this.busy.set(true);
    const task = this.task();
    const request = task ? this.service.update(task.id, value) : this.service.create(value);
    request.subscribe({ next: item => { this.busy.set(false); this.saved.emit(item); }, error: error => { this.busy.set(false); this.error.set(errorMessage(error)); } });
  }
}
