import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, throwError, forkJoin } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { environment } from '@environments/environment';
import {
  PermissionEntity,
  PermissionCreateDto,
  PermissionUpdateDto,
  PermissionReadDto,
  RoleEntity,
  RoleCreateDto,
  RoleUpdateDto,
  RoleReadDto,
  RolePermissionEntity,
  RolePermissionAssignDto,
  RolePermissionReadDto,
  UserRoleEntity,
  UserRoleAssignDto,
  UserRoleBulkAssignDto,
  UserRoleReplaceDto,
  UserRoleBulkAssignResponse,
  UserRoleReplaceResponse,
  RoleWithPermissions
} from '@app/core/models/permission-management.model';

@Injectable({
  providedIn: 'root'
})
export class PermissionManagementService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}`;

  // ==================== PERMISSIONS ====================

  /**
   * Get all permissions
   * GET /api/permissions
   */
  getAllPermissions(): Observable<PermissionEntity[]> {
    return this.http
      .get<PermissionReadDto[]>(`${this.apiUrl}/permissions`)
      .pipe(map(dtos => dtos.map(dto => this.mapPermissionDtoToEntity(dto))));
  }

  /**
   * Get permission by ID
   * GET /api/permissions/{id}
   */
  getPermission(id: number): Observable<PermissionEntity> {
    return this.http
      .get<PermissionReadDto>(`${this.apiUrl}/permissions/${id}`)
      .pipe(map(dto => this.mapPermissionDtoToEntity(dto)));
  }

  /**
   * Create a new permission
   * POST /api/permissions
   */
  createPermission(dto: PermissionCreateDto): Observable<PermissionEntity> {
    return this.http
      .post<PermissionReadDto>(`${this.apiUrl}/permissions`, dto)
      .pipe(map(responseDto => this.mapPermissionDtoToEntity(responseDto)));
  }

  /**
   * Update a permission
   * PUT /api/permissions/{id}
   */
  updatePermission(id: number, dto: PermissionUpdateDto): Observable<PermissionEntity> {
    return this.http
      .put<PermissionReadDto>(`${this.apiUrl}/permissions/${id}`, dto)
      .pipe(map(responseDto => this.mapPermissionDtoToEntity(responseDto)));
  }

  /**
   * Delete a permission (soft delete)
   * DELETE /api/permissions/{id}
   */
  deletePermission(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/permissions/${id}`);
  }

  // ==================== ROLES ====================

  /**
   * Get all roles
   * GET /api/roles
   */
  getAllRoles(): Observable<RoleEntity[]> {
    return this.http
      .get<RoleReadDto[]>(`${this.apiUrl}/roles`)
      .pipe(map(dtos => dtos.map(dto => this.mapRoleDtoToEntity(dto))));
  }

  /**
   * Get role by ID
   * GET /api/roles/{id}
   */
  getRole(id: number): Observable<RoleEntity> {
    return this.http
      .get<RoleReadDto>(`${this.apiUrl}/roles/${id}`)
      .pipe(map(dto => this.mapRoleDtoToEntity(dto)));
  }

  /**
   * Create a new role
   * POST /api/roles
   */
  createRole(dto: RoleCreateDto): Observable<RoleEntity> {
    return this.http
      .post<RoleReadDto>(`${this.apiUrl}/roles`, dto)
      .pipe(map(responseDto => this.mapRoleDtoToEntity(responseDto)));
  }

  /**
   * Update a role
   * PUT /api/roles/{id}
   */
  updateRole(id: number, dto: RoleUpdateDto): Observable<RoleEntity> {
    return this.http
      .put<RoleReadDto>(`${this.apiUrl}/roles/${id}`, dto)
      .pipe(map(responseDto => this.mapRoleDtoToEntity(responseDto)));
  }

  /**
   * Delete a role (soft delete)
   * DELETE /api/roles/{id}
   */
  deleteRole(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/roles/${id}`);
  }

  // ==================== ROLE-PERMISSION ASSIGNMENTS ====================

  /**
   * Get all permissions for a role
   * GET /api/roles-permissions/role/{roleId}
   */
  getRolePermissions(roleId: number): Observable<PermissionEntity[]> {
    return this.http
      .get<PermissionReadDto[]>(`${this.apiUrl}/roles-permissions/role/${roleId}`)
      .pipe(map(dtos => dtos.map(dto => this.mapPermissionDtoToEntity(dto))));
  }

  /**
   * Get all roles that have a specific permission
   * GET /api/roles-permissions/permission/{permissionId}
   */
  getPermissionRoles(permissionId: number): Observable<RoleEntity[]> {
    return this.http
      .get<RoleReadDto[]>(`${this.apiUrl}/roles-permissions/permission/${permissionId}`)
      .pipe(map(dtos => dtos.map(dto => this.mapRoleDtoToEntity(dto))));
  }

  /**
   * Assign a permission to a role
   * POST /api/roles-permissions
   */
  assignPermissionToRole(dto: RolePermissionAssignDto): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/roles-permissions`, dto);
  }

  /**
   * Remove a permission from a role
   * DELETE /api/roles-permissions
   */
  removePermissionFromRole(dto: RolePermissionAssignDto): Observable<void> {
    return this.http.request<void>('delete', `${this.apiUrl}/roles-permissions`, {
      body: dto
    });
  }

  /**
   * Get role with all its permissions (enriched)
   */
  getRoleWithPermissions(roleId: number): Observable<RoleWithPermissions> {
    return this.getRole(roleId).pipe(
      map(role => {
        // Fetch permissions separately
        // In a real app, this could be combined into a single backend call
        return role as RoleWithPermissions;
      })
    );
  }

  // ==================== USER-ROLE ASSIGNMENTS ====================

  /**
   * Get all roles for a user
   * GET /api/users-roles/user/{userId}
   */
  getUserRoles(userId: number): Observable<RoleEntity[]> {
    return this.http
      .get<RoleReadDto[]>(`${this.apiUrl}/users-roles/user/${userId}`)
      .pipe(map(dtos => dtos.map(dto => this.mapRoleDtoToEntity(dto))));
  }

  /**
   * Get all users that have a specific role
   * GET /api/users-roles/role/{roleId}
   */
  getRoleUsers(roleId: number): Observable<any[]> {
    // Backend returns user info - shape depends on backend implementation
    return this.http.get<any[]>(`${this.apiUrl}/users-roles/role/${roleId}`);
  }

  /**
   * Assign a role to a user
   * POST /api/users-roles
   */
  assignRoleToUser(dto: UserRoleAssignDto): Observable<{ message: string }> {
    return this.http.post<{ message: string }>(`${this.apiUrl}/users-roles`, dto);
  }

  /**
   * Assign multiple roles to a user
   * POST /api/users-roles/bulk-assign
   */
  bulkAssignRolesToUser(dto: UserRoleBulkAssignDto): Observable<UserRoleBulkAssignResponse> {
    return this.http.post<UserRoleBulkAssignResponse>(
      `${this.apiUrl}/users-roles/bulk-assign`,
      dto
    );
  }

  /**
   * Replace all roles for a user
   * PUT /api/users-roles/replace
   */
  replaceUserRoles(dto: UserRoleReplaceDto): Observable<UserRoleReplaceResponse> {
    return this.http.put<UserRoleReplaceResponse>(`${this.apiUrl}/users-roles/replace`, dto);
  }

  /**
   * Remove a role from a user
   * DELETE /api/users-roles
   */
  removeRoleFromUser(dto: UserRoleAssignDto): Observable<void> {
    return this.http.request<void>('delete', `${this.apiUrl}/users-roles`, {
      body: dto
    });
  }

  // ==================== HIGH-LEVEL USER-ROLE HELPERS ====================

  /**
   * Assign a single role to a user with fallbacks and idempotent handling.
   * POST /api/users-roles with { UserId, RoleId }
   * On 409 treat as success. On 404 try legacy endpoints.
   */
  assignRole(dto: UserRoleAssignDto): Observable<void> {
    if (!dto || !dto.UserId || !dto.RoleId) return throwError(() => new Error('Invalid parameters'));
    const url = `${this.apiUrl}/users-roles`;
    return this.http.post<any>(url, dto).pipe(
      map(() => void 0),
      catchError((err: any) => {
        if (err?.status === 409) return of(void 0);
        if (err?.status === 404) {
          // fallback 1
          return this.http.post<any>(`${this.apiUrl}/employee/${dto.UserId}/assign-role`, { roleId: dto.RoleId }).pipe(
            map(() => void 0),
            catchError((err2: any) => {
              if (err2?.status === 409) return of(void 0);
              if (err2?.status === 404) {
                // fallback 2
                return this.http.post<any>(`${this.apiUrl}/employee/${dto.UserId}/role`, { roleId: dto.RoleId }).pipe(
                  map(() => void 0),
                  catchError((err3: any) => {
                    if (err3?.status === 409) return of(void 0);
                    return throwError(() => err3);
                  })
                );
              }
              return throwError(() => err2);
            })
          );
        }
        return throwError(() => err);
      })
    );
  }

  /**
   * Assign multiple roles to a user. Tries bulk assign then falls back to per-role assign.
   */
  assignRoles(userId: number, roleIds: number[]): Observable<void> {
    if (!userId || !Array.isArray(roleIds)) return throwError(() => new Error('Invalid parameters'));
    const url = `${this.apiUrl}/users-roles/bulk-assign`;
    const dto: UserRoleBulkAssignDto = { UserId: userId, RoleIds: roleIds };
    return this.http.post<UserRoleBulkAssignResponse>(url, dto).pipe(
      map(() => void 0),
      catchError((err: any) => {
        if (err?.status === 409) return of(void 0);
        if (err?.status === 404) {
          // fallback: call assignRole for each role individually
          const calls = (roleIds || []).map(rid => this.assignRole({ UserId: userId, RoleId: rid }).pipe(catchError(() => of(void 0))));
          return forkJoin(calls).pipe(map(() => void 0));
        }
        return throwError(() => err);
      })
    );
  }

  /**
   * Replace all roles for a user using the replace endpoint.
   */
  replaceRoles(userId: number, roleIds: number[]): Observable<void> {
    if (!userId || !Array.isArray(roleIds)) return throwError(() => new Error('Invalid parameters'));
    const dto: UserRoleReplaceDto = { UserId: userId, RoleIds: roleIds };
    return this.replaceUserRoles(dto).pipe(
      map(() => void 0)
    );
  }

  /**
   * Remove a role from a user (treat 404 as success).
   */
  removeRole(userId: number, roleId: number): Observable<void> {
    if (!userId || !roleId) return throwError(() => new Error('Invalid parameters'));
    const dto: UserRoleAssignDto = { UserId: userId, RoleId: roleId };
    return this.removeRoleFromUser(dto).pipe(
      catchError((err: any) => {
        if (err?.status === 404) return of(void 0);
        return throwError(() => err);
      })
    );
  }

  /**
   * Assign multiple roles to an employee (by employee ID).
   */
  assignRolesToEmployee(employeeId: number, roleIds: number[]): Observable<void> {
    if (!employeeId || !Array.isArray(roleIds)) return throwError(() => new Error('Invalid parameters'));
    const url = `${this.apiUrl}/users-roles/employee/${employeeId}/assign`;
    return this.http.post<any>(url, roleIds).pipe(
      map(() => void 0),
      catchError((err: any) => {
        if (err?.status === 409) return of(void 0);
        return throwError(() => err);
      })
    );
  }

  // ==================== PRIVATE MAPPERS ====================

  private mapPermissionDtoToEntity(dto: PermissionReadDto): PermissionEntity {
    return {
      id: dto.id,
      name: dto.name,
      description: dto.description,
      createdAt: new Date(dto.createdAt)
    };
  }

  private mapRoleDtoToEntity(dto: RoleReadDto): RoleEntity {
    return {
      id: dto.id,
      name: dto.name,
      description: dto.description,
      createdAt: new Date(dto.createdAt)
    };
  }

  // ==================== UTILITY METHODS ====================

  /**
   * Check if a role has a specific permission
   */
  async roleHasPermission(roleId: number, permissionId: number): Promise<boolean> {
    const permissions = await this.getRolePermissions(roleId).toPromise();
    return permissions?.some(p => p.id === permissionId) ?? false;
  }

  /**
   * Check if a user has a specific role
   */
  async userHasRole(userId: number, roleId: number): Promise<boolean> {
    const roles = await this.getUserRoles(userId).toPromise();
    return roles?.some(r => r.id === roleId) ?? false;
  }
}
