import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, of, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { AssignRoleRequest, User, UserCreateRequest, UserUpdateRequest } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private baseUrl = 'https://api-test.payzenhr.com';
  private apiUrl = `${this.baseUrl}/api/employee`;
  // Users-Roles controller base
  private usersRolesUrl = `${this.baseUrl}/api/users-roles`;

  constructor(private http: HttpClient) { }

  /**
   * Transform API response from PascalCase to camelCase
   */
  private transformUser(data: any): User {
    return {
      id: data.Id || data.id,
      employeeId: data.EmployeeId || data.employeeId,
      email: data.Email || data.email,
      firstName: data.FirstName || data.firstName,
      lastName: data.LastName || data.lastName,
      username: data.Username || data.username,
      role: data.RoleNames || data.role,
      roleId: data.RoleId || data.roleId,
      companyId: data.CompanyId || data.companyId,
      companyName: data.CompanyName || data.companyName,
      status: data.Status || data.status || 'active',
      createdAt: data.CreatedAt || data.createdAt
    };
  }

  /**
   * Get all users
   */
  getAllUsers(): Observable<User[]> {
    return this.http.get<any[]>(this.apiUrl).pipe(
      map(users => users.map(u => this.transformUser(u)))
    );
  }

  /**
   * Get user by ID
   */
  getUserById(id: number): Observable<User> {
    return this.http.get<any>(`${this.apiUrl}/${id}`).pipe(
      map(u => this.transformUser(u))
    );
  }

  /**
   * Create a new user
   */
  createUser(user: UserCreateRequest): Observable<User> {
    const payload: any = {
      EmployeeId: user.employeeId,
      Email: user.email,
      FirstName: user.firstName,
      LastName: user.lastName,
      Password: user.password,
      RoleId: user.roleId,
      CompanyId: user.companyId,
      DateOfBirth: (user as any).dateOfBirth
    };

    return this.http.post<any>(this.apiUrl, payload).pipe(
      map(u => this.transformUser(u))
    );
  }

  /**
   * Update an existing user
   */
  updateUser(id: number, user: UserUpdateRequest): Observable<User> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, user).pipe(
      map(u => this.transformUser(u))
    );
  }

  /**
   * Delete a user
   */
  deleteUser(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Assign role to user
   */
  assignRole(request: AssignRoleRequest): Observable<void> {
    // Prefer the UsersRolesController endpoint
    const payload = { UserId: request.userId, RoleId: request.roleId };
    return this.http.post<void>(`${this.usersRolesUrl}`, payload).pipe(
      catchError(err => {
        // If conflict (already assigned), treat as success (idempotent)
        if (err && err.status === 409) {
          return of(void 0);
        }
        // fallback to legacy employee endpoints if controller not available
        if (err && err.status === 404) {
          return this.http.post<void>(`${this.apiUrl}/${request.userId}/assign-role`, { roleId: request.roleId }).pipe(
            catchError(err2 => {
              if (err2 && err2.status === 409) {
                return of(void 0);
              }
              if (err2 && err2.status === 404) {
                return this.http.post<void>(`${this.apiUrl}/${request.userId}/role`, { roleId: request.roleId });
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
   * Get roles assigned to a user
   */
  getUserRoles(userId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.usersRolesUrl}/user/${userId}`);
  }

  /**
   * Assign multiple roles to a user (bulk)
   * Tries sensible endpoints and payload shapes.
   */
  assignRoles(userId: number, roleIds: number[]): Observable<any> {
    const url = `${this.usersRolesUrl}/bulk-assign`;
    const payload = { UserId: userId, RoleIds: roleIds };

    return this.http.post<any>(url, payload).pipe(
      catchError(err => {
        // Try alternative route spellings
        const altUrls = [
          `${this.usersRolesUrl}/bulkassign`,
          `${this.usersRolesUrl}/bulkAssign`,
          `${this.usersRolesUrl}/assign-roles`,
          `${this.usersRolesUrl}/roles/bulk-assign`
        ];
        let attempt$: Observable<any> | null = null;
        for (const u of altUrls) {
          if (!attempt$) {
            attempt$ = this.http.post<any>(u, payload).pipe(catchError(e => { return throwError(() => e); }));
          }
        }
        return attempt$ ?? throwError(() => err);
      })
    );
  }

  /**
   * Replace all roles for a user (PUT /api/users-roles/replace)
   */
  replaceRoles(userId: number, roleIds: number[]): Observable<any> {
    const url = `${this.usersRolesUrl}/replace`;
    const payload = { UserId: userId, RoleIds: roleIds };
    return this.http.put<any>(url, payload);
  }

  /**
   * Remove a role from a user (DELETE with body)
   */
  removeRole(userId: number, roleId: number): Observable<any> {
    const url = `${this.usersRolesUrl}`;
    const payload = { UserId: userId, RoleId: roleId };
    return this.http.request('delete', url, { body: payload });
  }

  /**
   * Change user status
   */
  changeStatus(id: number, status: 'active' | 'inactive'): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/status`, { status });
  }
}
