import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { EmployeeDashboardData } from './employee-dashboard.model';

@Injectable({
  providedIn: 'root'
})
export class EmployeeDashboardService {
  private http = inject(HttpClient);
  // Using environment.apiUrl which points to your .NET Backend
  private apiUrl = `${environment.apiUrl}/dashboard/employee`;

  /**
   * Fetches the employee dashboard data directly from the C# Backend API.
   */
  getDashboardData(employeeId?: number): Observable<EmployeeDashboardData> {
    // Backend: GET /api/dashboard/employee (userId is taken from JWT claim "uid")
    return this.http.get<EmployeeDashboardData>(`${this.apiUrl}`);
  }
}
