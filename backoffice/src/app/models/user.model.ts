export interface User {
  id: number;
  employeeId?: number;
  email: string;
  firstName: string;
  lastName: string;
  username?: string;
  role: string;
  roleId?: number;
  companyId?: number;
  companyName?: string;
  status?: 'active' | 'inactive';
  createdAt?: string;
}

export interface UserCreateRequest {
  employeeId?: number;
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  roleId: number;
  companyId?: number;
  dateOfBirth?: string; // ISO date string e.g. 1990-01-01
}

export interface UserUpdateRequest {
  email?: string;
  firstName?: string;
  lastName?: string;
  roleId?: number;
  companyId?: number;
  status?: string;
}

export interface AssignRoleRequest {
  userId: number;
  roleId: number;
}
