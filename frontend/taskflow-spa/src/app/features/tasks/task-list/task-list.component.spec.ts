import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { TaskItem } from '../../../core/models/task.model';
import { TaskService } from '../../../core/services/task.service';
import { TaskListComponent } from './task-list.component';

const item: TaskItem = { id: '1', userId: 'owner', title: 'Review', description: null, status: 'Pending', dueDate: '2030-01-01T00:00:00Z', createdAt: '2026-01-01T00:00:00Z', updatedAt: '2026-01-01T00:00:00Z' };

describe('TaskListComponent', () => {
  it('loads and renders tasks, and calls delete then reloads', () => {
    const tasks = { list: vi.fn().mockReturnValue(of([item])), delete: vi.fn().mockReturnValue(of(undefined)) };
    TestBed.configureTestingModule({ imports: [TaskListComponent], providers: [{ provide: TaskService, useValue: tasks }] });
    const fixture = TestBed.createComponent(TaskListComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Review');
    fixture.componentInstance.delete('1');
    expect(tasks.delete).toHaveBeenCalledWith('1');
    expect(tasks.list).toHaveBeenCalledTimes(2);
  });
});
