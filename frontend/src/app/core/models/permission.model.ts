// Permission types based on backend RBAC
// Synced with backend /api/permissions
export type Permission =
  // User permissions
  | 'READ_USERS'
  | 'VIEW_USERS'
  | 'CREATE_USERS'
  | 'EDIT_USERS'
  | 'DELETE_USERS'
  // Role permissions
  | 'READ_ROLES'
  | 'VIEW_ROLE'
  | 'CREATE_ROLE'
  | 'EDIT_ROLE'
  | 'DELETE_ROLE'
  | 'ASSIGN_ROLES'
  | 'REVOKE_ROLES'
  // Permission permissions
  | 'READ_PERMISSIONS'
  | 'MANAGE_PERMISSIONS'
  // Company permissions
  | 'READ_COMPANIES'
  | 'VIEW_COMPANY'
  | 'CREATE_COMPANY'
  | 'EDIT_COMPANY'
  | 'DELETE_COMPANY'
  | 'VIEW_MANAGED_COMPANIES'
  | 'VIEW_CABINET_EXPERTS'
  | 'MANAGE_COMPANY_HIERARCHY'
  // Employee permissions
  | 'READ_EMPLOYEES'
  | 'VIEW_EMPLOYEE'
  | 'CREATE_EMPLOYEE'
  | 'EDIT_EMPLOYEE'
  | 'DELETE_EMPLOYEE'
  | 'VIEW_COMPANY_EMPLOYEES'
  | 'VIEW_SUBORDINATES'
  | 'MANAGE_EMPLOYEE_MANAGER'
  // Payroll permissions (future)
  | 'READ_PAYROLL'
  | 'CREATE_PAYROLL'
  | 'EDIT_PAYROLL'
  | 'VALIDATE_PAYROLL'
  | 'DELETE_PAYROLL';

// Permission check helper type
export interface PermissionCheck {
  hasPermission: boolean;
  message?: string;
}
