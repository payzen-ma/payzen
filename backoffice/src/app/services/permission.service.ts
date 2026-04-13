import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Permission } from '../models/role.model';

@Injectable({
  providedIn: 'root'
})
export class PermissionService {
  private baseUrl = 'https://api-test.payzenhr.com';
  private apiUrl = `${this.baseUrl}/api/permissions`;

  constructor(private http: HttpClient) { }

  /**
   * Create a new permission
   */
  createPermission(permission: Partial<Permission>): Observable<any> {
    return this.http.post(this.apiUrl, permission);
  }

  /**
   * Transform API response from PascalCase to camelCase
   */
  private transformPermission(data: any): Permission {
    return {
      id: data.Id || data.id,
      name: data.Name || data.name,
      description: data.Description || data.description,
      // If the DB doesn't have a Resource field, default to 'global'
      resource: data.Resource || data.resource || 'global',
      action: data.Action || data.action || undefined,
      createdAt: data.CreatedAt ?? data.createdAt ?? undefined
    };
  }

  /**
   * Get all permissions
   */
  getAllPermissions(): Observable<Permission[]> {
    return this.http.get<any[]>(this.apiUrl).pipe(
      map(permissions => permissions.map(p => this.transformPermission(p)))
    );
  }

  /**
   * Get permissions grouped by resource
   */
  getPermissionsByResource(): Observable<{ [resource: string]: Permission[] }> {
    return this.http.get<any>(`${this.apiUrl}/by-resource`).pipe(
      map(data => {
        const result: { [resource: string]: Permission[] } = {};
        // If the API returns an array (no by-resource support), group locally
        if (Array.isArray(data)) {
          data.map((p: any) => this.transformPermission(p)).forEach((perm: Permission) => {
            const key = perm.resource || 'global';
            if (!result[key]) result[key] = [];
            result[key].push(perm);
          });
          return result;
        }
        // Otherwise assume an object keyed by resource
        for (const resource in data) {
          result[resource] = data[resource].map((p: any) => this.transformPermission(p));
        }
        return result;
      })
    );
  }

  /**
   * Update an existing permission
   */
  updatePermission(id: number | string, permission: Partial<Permission>): Observable<any> {
    return this.http.put(`${this.apiUrl}/${id}`, permission);
  }

  /**
   * Delete a permission by id
   */
  deletePermission(id: number | string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
