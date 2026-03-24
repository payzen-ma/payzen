import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';
import { Role, Permission, RoleCreateRequest, RoleUpdateRequest } from '../models/role.model';

@Injectable({
  providedIn: 'root'
})
export class RoleService {
  private baseUrl = 'http://localhost:5119';
  private apiUrl = `${this.baseUrl}/api/roles`;

  constructor(private http: HttpClient) {}

  /**
   * Transform API response from PascalCase to camelCase
   */
  private transformRole(data: any): Role {
    const usersArray = data.Users ?? data.users ?? null;
    const userCountFromField = data.UserCount ?? data.userCount;
    const computedUserCount = Array.isArray(usersArray) ? usersArray.length : undefined;

    return {
      id: data.Id ?? data.id ?? data.RoleId,
      name: data.Name ?? data.name ?? data.RoleName,
      description: data.Description ?? data.description ?? '',
      permissions: data.Permissions ?? data.permissions ?? [],
      userCount: (typeof userCountFromField === 'number') ? userCountFromField
                : (typeof computedUserCount === 'number') ? computedUserCount
                : 0,
      createdAt: data.CreatedAt ?? data.createdAt ?? null
    };
  }

  /**
   * Get all roles
   */
  getAllRoles(): Observable<Role[]> {
    return this.http.get<any[]>(this.apiUrl).pipe(
      map(roles => roles.map(r => this.transformRole(r)))
    );
  }

  /**
   * Get role by ID
   */
  getRoleById(id: number): Observable<Role> {
    return this.http.get<any>(`${this.apiUrl}/${id}`).pipe(
      map(r => this.transformRole(r))
    );
  }

  /**
   * Create a new role
   */
  createRole(role: RoleCreateRequest): Observable<Role> {
    return this.http.post<any>(this.apiUrl, role).pipe(
      map(r => this.transformRole(r))
    );
  }

  /**
   * Update an existing role
   */
  updateRole(id: number, role: RoleUpdateRequest): Observable<Role> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, role).pipe(
      map(r => this.transformRole(r))
    );
  }

  /**
   * Delete a role
   */
  deleteRole(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get users with specific role
   */
  getUsersByRole(roleId: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${roleId}/users`);
  }

  /**
   * Assign permissions to a role using the roles-permissions endpoint.
   * Payload shape: { roleId: number, permissionIds: number[] }
   */
  assignPermissions(roleId: number, permissionIds: number[]): Observable<any> {
    // API expects RolePermissionsBulkAssignDto at POST /api/roles-permissions/bulk-assign
    const url = `${this.baseUrl}/api/roles-permissions/bulk-assign`;
    const payload: any = { RoleId: roleId, PermissionIds: permissionIds };
    console.debug('[RoleService] assignPermissions payload:', payload);
    return this.http.post<any>(url, payload);
  }

  /**
   * Get permissions assigned to a role
   */
  getRolePermissions(roleId: number): Observable<any[]> {
    const url = `${this.baseUrl}/api/roles-permissions/role/${roleId}`;
    return this.http.get<any[]>(url);
  }
}
