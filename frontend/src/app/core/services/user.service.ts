import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { User } from '../models/user.model';
import { CompanyContextService } from './companyContext.service';

// Interface for the employee response that includes user/role info
interface EmployeeWithUserDto {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  companyName: string;
  roleName: string | null;
  statusName: string | null;
}

// Interface for available employees (without user accounts)
export interface AvailableEmployee {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  fullName: string;
}

@Injectable({
  providedIn: 'root'
})
export class UserService {
  private http = inject(HttpClient);
  private contextService = inject(CompanyContextService);
  private apiUrl = `${environment.apiUrl}/users`;
  private employeeApiUrl = `${environment.apiUrl}/employee`;
  private rolesApiUrl = `${environment.apiUrl}/roles`;

  /**
   * Fetch available roles from backend and exclude the internal Payzen admin role.
   */
  getRoles(): Observable<{ id: number; code: string; name: string }[]> {
    return this.http.get<any[]>(this.rolesApiUrl).pipe(
      map(items =>
        (items || [])
          .filter(i => {
            const code = (i.code ?? i.Code ?? '').toString().toLowerCase();
            // exclude the Payzen admin role by code
            return code !== 'admin_payzen' && code !== 'payzen_admin';
          })
          .map(i => ({ id: i.id ?? i.Id, code: i.code ?? i.Code, name: i.name ?? i.Name ?? i.displayName ?? String(i.id) }))
      )
    );
  }

  /**
   * Get employees who don't have a user account yet (available for invitation).
   * These are employees with roleName = null.
   */
  getAvailableEmployees(companyId: number): Observable<AvailableEmployee[]> {
    return this.http.get<any>(`${this.employeeApiUrl}/company/${companyId}`).pipe(
      map(resp => {
        const employees: EmployeeWithUserDto[] = this.normalizeEmployeeList(resp);
        return employees
          .filter(emp => emp.roleName === null || emp.roleName === undefined || emp.roleName === '')
          .map(emp => ({
            id: emp.id,
            firstName: emp.firstName,
            lastName: emp.lastName,
            email: emp.email,
            fullName: `${emp.firstName} ${emp.lastName}`.trim()
          }));
      })
    );
  }

  /**
   * Get users by company ID.
   * Uses the employee endpoint since users are linked via Employee.CompanyId.
   * Only returns employees who have an associated user account (with roleName).
   */
  getUsersByCompany(companyId: number): Observable<User[]> {
    return this.http.get<any>(`${this.employeeApiUrl}/company/${companyId}`).pipe(
      map(resp => {
        const employees: EmployeeWithUserDto[] = this.normalizeEmployeeList(resp);
        return employees
          .filter(emp => emp.roleName !== null && emp.roleName !== undefined)
          .map(emp => {
            const u = this.mapEmployeeToUser(emp);
            u.companyId = companyId?.toString();
            return u;
          });
      })
    );
  }

  /**
   * Normalize various backend shapes for the company employees endpoint.
   * Accepts either an array or an object with an `employees` property.
   */
  private normalizeEmployeeList(resp: any): EmployeeWithUserDto[] {
    if (!resp) return [];
    if (Array.isArray(resp)) return resp as EmployeeWithUserDto[];
    if (Array.isArray(resp.employees)) return resp.employees as EmployeeWithUserDto[];
    if (Array.isArray(resp.data)) return resp.data as EmployeeWithUserDto[];
    return [];
  }

  getUsers(companyId?: number): Observable<User[]> {
    // If no companyId provided, try to get from context, otherwise fetch all (admin?)
    const targetCompanyId = companyId || this.contextService.companyId();
    
    // Use the company-specific endpoint if we have a companyId
    if (targetCompanyId) {
      return this.getUsersByCompany(Number(targetCompanyId));
    }
    
    // Fallback to fetching all users (for admin scenarios)
    return this.http.get<any[]>(this.apiUrl).pipe(
      map(dtos => dtos.map(dto => this.mapDtoToUser(dto)))
    );
  }

  createUser(user: Partial<User>): Observable<User> {
    return this.http.post<any>(this.apiUrl, user).pipe(
      map(dto => this.mapDtoToUser(dto))
    );
  }

  updateUser(id: string, user: Partial<User>): Observable<User> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, user).pipe(
      map(dto => this.mapDtoToUser(dto))
    );
  }

  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  // Invite user (might be a specific endpoint or just create)
  inviteUser(email: string, role: string, companyId: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/invite`, { email, role, companyId });
  }

  /**
   * Assigns a role to a user via POST api/users-roles/
   * @param userId User ID
   * @param roleId Role ID
   */
  assignUserRole(userId: number, roleId: number): Observable<any> {
    const payload = { UserId: userId, RoleId: roleId };
    return this.http.post(`${environment.apiUrl}/users-roles`, payload);
  }

  /**
   * Get roles assigned to a specific user
   * Calls GET api/users-roles/user/{userId}
   */
  getUserRoles(userId: number): Observable<any[]> {
    return this.http.get<any[]>(`${environment.apiUrl}/users-roles/user/${userId}`);
  }

  /**
   * Get roles assigned to an employee (by employee ID)
   * Calls GET api/users-roles/employee/{employeeId}
   */
  getEmployeeRoles(employeeId: number): Observable<any[]> {
    return this.http.get<any[]>(`${environment.apiUrl}/users-roles/employee/${employeeId}`);
  }

  private mapDtoToUser(dto: any): User {
    return {
      id: dto.id?.toString(),
      email: dto.email,
      username: dto.username,
      firstName: dto.firstName || dto.employee?.firstName || '',
      lastName: dto.lastName || dto.employee?.lastName || '',
      role: dto.roles?.[0] || 'employee', // Simplified role mapping
      employee_id: dto.employeeId?.toString(),
      companyId: dto.companyId?.toString(),
      companyName: dto.companyName,
      isCabinetExpert: dto.isCabinetExpert
    } as User;
  }

  /**
   * Maps an employee DTO (from /api/employee/company/{id}) to a User object.
   * Used when fetching users via the employee endpoint.
   */
  private mapEmployeeToUser(emp: EmployeeWithUserDto): User {
    return {
      id: emp.id.toString(),
      email: emp.email,
      username: emp.email, // Use email as username fallback
      firstName: emp.firstName,
      lastName: emp.lastName,
      role: emp.roleName || 'employee',
      employee_id: emp.id.toString(),
      companyId: undefined, // Not directly available, but we know it's the requested company
      companyName: emp.companyName,
      isActive: emp.statusName === 'Active' || emp.statusName === 'Actif',
    } as User;
  }
}


