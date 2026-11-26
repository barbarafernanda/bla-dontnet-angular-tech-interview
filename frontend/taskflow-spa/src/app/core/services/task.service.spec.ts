import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TaskWrite } from '../models/task.model';
import { TaskService } from './task.service';

describe('TaskService', () => {
  let service: TaskService;
  let http: HttpTestingController;
  const task: TaskWrite = { title: 'Review', description: null, status: 'Pending', dueDate: '2030-01-01T00:00:00Z' };
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(TaskService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('uses the correct routes and verbs for the complete CRUD lifecycle', () => {
    service.list().subscribe();
    const list = http.expectOne('http://localhost:5002/api/tasks');
    expect(list.request.method).toBe('GET'); list.flush([]);
    service.get('123').subscribe();
    const get = http.expectOne('http://localhost:5002/api/tasks/123');
    expect(get.request.method).toBe('GET'); get.flush({});
    service.create(task).subscribe();
    const create = http.expectOne('http://localhost:5002/api/tasks');
    expect(create.request.method).toBe('POST'); expect(create.request.body).toEqual(task); create.flush({});
    service.update('123', task).subscribe();
    const update = http.expectOne('http://localhost:5002/api/tasks/123');
    expect(update.request.method).toBe('PUT'); expect(update.request.body).toEqual(task); update.flush({});
    service.delete('123').subscribe();
    const remove = http.expectOne('http://localhost:5002/api/tasks/123');
    expect(remove.request.method).toBe('DELETE'); remove.flush(null, { status: 204, statusText: 'No Content' });
  });
});
