import { Permission } from './permission.model';

// Role interface matching backend structure
export interface Role {
  id: string;
  name: string;
  description?: string;
  permissions: Permission[];
  isActive: boolean;
  createdAt: Date;
  updatedAt?: Date;
}

// Create role request
export interface CreateRoleRequest {
  name: string;
  description?: string;
  permissions: Permission[];
}

// Update role request
export interface UpdateRoleRequest {
  name?: string;
  description?: string;
  permissions?: Permission[];
  isActive?: boolean;
}
