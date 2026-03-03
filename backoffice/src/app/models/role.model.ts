import { User } from './user.model';

export interface Role {
  id: number;
  name: string;
  description?: string;
  permissions: Permission[];
  userCount?: number;
  Users?: User[]; // optional, matches API PascalCase payload
  users?: User[]; // optional, also accept camelCase payloads
  createdAt?: string;
}

export interface Permission {
  id: number;
  name: string;
  description?: string;
  resource?: string;
  action?: string;
  createdAt?: string;
}

export interface RoleCreateRequest {
  name: string;
  description?: string;
  permissionIds: number[];
}

export interface RoleUpdateRequest {
  name: string;
  description?: string;
  permissionIds: number[];
}

export interface RoleListResponse {
  roles: Role[];
  totalCount: number;
}

