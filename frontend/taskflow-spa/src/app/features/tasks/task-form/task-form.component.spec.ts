import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { TaskItem, TaskWrite } from '../../../core/models/task.model';
import { TaskService } from '../../../core/services/task.service';
import { TaskFormComponent } from './task-form.component';

const value: TaskWrite = { title: 'Review', description: null, status: 'Pending', dueDate: '2030-01-01T00:00:00Z' };
const item: TaskItem = { ...value, id: '1', userId: 'owner', createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01T00:00:00Z' };

describe('TaskFormComponent', () => {
  it('creates a new task and emits the result', () => {
    const service = { create: vi.fn().mockReturnValue(of(item)), update: vi.fn() };
    TestBed.configureTestingModule({ imports: [TaskFormComponent], providers: [{ provide: TaskService, useValue: service }] });
    const fixture = TestBed.createComponent(TaskFormComponent);
    const saved = vi.fn(); fixture.componentInstance.saved.subscribe(saved);
    fixture.componentInstance.save(value);
    expect(service.create).toHaveBeenCalledWith(value);
    expect(saved).toHaveBeenCalledWith(item);
  });

  it('updates an existing task and emits the result', () => {
    const service = { create: vi.fn(), update: vi.fn().mockReturnValue(of(item)) };
    TestBed.configureTestingModule({ imports: [TaskFormComponent], providers: [{ provide: TaskService, useValue: service }] });
    const fixture = TestBed.createComponent(TaskFormComponent);
    fixture.componentRef.setInput('task', item);
    const saved = vi.fn(); fixture.componentInstance.saved.subscribe(saved);
    fixture.componentInstance.save(value);
    expect(service.update).toHaveBeenCalledWith('1', value);
    expect(saved).toHaveBeenCalledWith(item);
  });
});
