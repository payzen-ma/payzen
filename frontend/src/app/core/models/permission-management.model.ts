/**
 * Permission Management Models
 * Interfaces for managing permissions, roles, and user-role assignments
 * Aligned with backend /api/permissions, /api/roles, /api/roles-permissions, /api/users-roles
 */

import { Permission } from './permission.model';

// ==================== PERMISSION MODELS ====================

/**
 * Permission entity
 */
export interface PermissionEntity {
  id: number;
  name: Permission | string;
  description: string;
  createdAt: Date;
  updatedAt?: Date;
  createdBy?: number;
  updatedBy?: number;
}

/**
 * DTO for creating a permission
 */
export interface PermissionCreateDto {
  Name: string;
  Description: string;
}

/**
 * DTO for updating a permission
 */
export interface PermissionUpdateDto {
  Description?: string;
  // Name is typically not updatable
}

/**
 * DTO received from API
 */
export interface PermissionReadDto {
  id: number;
  name: string;
  description: string;
  createdAt: string;
}

// ==================== ROLE MODELS ====================

/**
 * Role entity
 */
export interface RoleEntity {
  id: number;
  name: string;
  description: string;
  createdAt: Date;
  updatedAt?: Date;
  createdBy?: number;
  updatedBy?: number;
  // Enriched field
  permissions?: PermissionEntity[];
}

/**
 * DTO for creating a role
 */
export interface RoleCreateDto {
  Name: string;
  Description: string;
}

/**
 * DTO for updating a role
 */
export interface RoleUpdateDto {
  Name?: string;
  Description?: string;
}

/**
 * DTO received from API
 */
export interface RoleReadDto {
  id: number;
  name: string;
  description: string;
  createdAt: string;
}

// ==================== ROLE-PERMISSION ASSIGNMENT ====================

/**
 * Role-Permission assignment entity
 */
export interface RolePermissionEntity {
  id: number;
  roleId: number;
  permissionId: number;
  roleName?: string;           // Enriched
  permissionName?: string;     // Enriched
  permissionDescription?: string;  // Enriched
  createdAt: Date;
}

/**
 * DTO for assigning permission to role
 */
export interface RolePermissionAssignDto {
  RoleId: number;
  PermissionId: number;
}

/**
 * DTO received from API (enriched)
 */
export interface RolePermissionReadDto {
  id: number;
  roleId: number;
  permissionId: number;
  roleName: string;
  permissionName: string;
  permissionDescription: string;
  createdAt: string;
}

// ==================== USER-ROLE ASSIGNMENT ====================

/**
 * User-Role assignment entity
 */
export interface UserRoleEntity {
  id: number;
  userId: number;
  roleId: number;
  userName?: string;      // Enriched
  roleName?: string;      // Enriched
  roleDescription?: string;  // Enriched
  createdAt: Date;
}

/**
 * DTO for assigning role to user
 */
export interface UserRoleAssignDto {
  UserId: number;
  RoleId: number;
}

/**
 * DTO for bulk assigning roles to user
 */
export interface UserRoleBulkAssignDto {
  UserId: number;
  RoleIds: number[];
}

/**
 * DTO for replacing all user roles
 */
export interface UserRoleReplaceDto {
  UserId: number;
  RoleIds: number[];
}

/**
 * Response from bulk assign operation
 */
export interface UserRoleBulkAssignResponse {
  message: string;
  assigned: number;
  reactivated: number;
  skipped: number;
}

/**
 * Response from replace operation
 */
export interface UserRoleReplaceResponse {
  message: string;
  removed: number;
  assigned: number;
  reactivated: number;
}

// ==================== UI DISPLAY MODELS ====================

/**
 * Permission with selection state (for UI checkboxes)
 */
export interface PermissionWithSelection extends PermissionEntity {
  selected: boolean;
}

/**
 * Role with permissions for display
 */
export interface RoleWithPermissions extends RoleEntity {
  permissionIds: number[];
  permissionNames: string[];
}

/**
 * User simplified for role assignment
 */
export interface UserForRoleAssignment {
  id: number;
  username: string;
  email: string;
  firstName?: string;
  lastName?: string;
  roles: RoleEntity[];
}
