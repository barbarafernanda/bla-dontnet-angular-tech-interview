export type TaskStatus = 'Pending' | 'InProgress' | 'Completed' | 'Cancelled';

export interface TaskWrite {
  title: string;
  description: string | null;
  status: TaskStatus;
  dueDate: string;
}

export interface TaskItem extends TaskWrite {
  id: string;
  userId: string;
  createdAt: string;
  updatedAt: string;
}
