import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { EndpointDemoComponent } from './features/auth/endpoint-demo/endpoint-demo.component';
import { LoginComponent } from './features/auth/login/login.component';
import { RegisterComponent } from './features/auth/register/register.component';
import { TaskListComponent } from './features/tasks/task-list/task-list.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'tasks' },
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'api-demo', component: EndpointDemoComponent },
  { path: 'tasks', component: TaskListComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: 'tasks' },
];
