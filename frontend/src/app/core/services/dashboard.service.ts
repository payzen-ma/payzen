import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '@environments/environment';

// Employee Summary Response (from /api/employee/summary)
export interface EmployeeSummaryResponse {
  totalEmployees: number;
  activeEmployees: number;
  employees: EmployeeDashboardItem[];
}

export interface EmployeeDashboardItem {
  id: string;
  firstName: string;
  lastName: string;
  position: string;
  department: string;
  status: string; // 'active', 'on_leave', 'inactive'
  startDate: string;
  missingDocuments: number;
  contractType: string;
  manager?: string;
}

// Dashboard Summary Response (from /api/dashboard/summary) - Expert Mode
export interface DashboardSummaryResponse {
  totalCompanies: number;
  totalEmployees: number;
  accountingFirmsCount: number;
  avgEmployeesPerCompany: number;
  employeeDistribution: DistributionBucket[];
  recentCompanies: RecentCompany[];
  asOf: string;
}

export interface DistributionBucket {
  bucket: string;
  companiesCount: number;
  employeesCount: number;
  percentage: number;
}

export interface RecentCompany {
  id: number;
  companyName: string;
  countryName: string | null;
  cityName: string | null;
  employeesCount: number;
  createdAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  /**
   * Get employee summary for standard (client) dashboard
   * Calls: GET /api/employee/summary
   * Returns: Total employees, active employees, and employee list
   */
  getEmployeeSummary(): Observable<EmployeeSummaryResponse> {
    return this.http.get<EmployeeSummaryResponse>(`${this.apiUrl}/dashboard/employees`);
  }

  /**
   * Get dashboard summary for expert dashboard
   * Calls: GET /api/dashboard/summary
   * Returns: Global statistics including total companies, employees, distribution, etc.
   */
  getDashboardSummary(): Observable<DashboardSummaryResponse> {
    // Backend provides an expert summary at /api/dashboard/expert/summary
    const url = `${this.apiUrl}/dashboard/expert/summary`;

    return this.http.get<any>(url).pipe(
      map(raw => {
        console.log('[DashboardService] expert summary raw response:', raw);
        const totalCompanies = raw?.TotalClients ?? raw?.TotalCompanies ?? raw?.totalClients ?? raw?.totalCompanies ?? 0;
        const totalEmployees = raw?.TotalEmployees ?? raw?.totalEmployees ?? raw?.TotalEmployeesCount ?? raw?.totalEmployeesCount ?? 0;
        const avg = totalCompanies ? (totalEmployees / totalCompanies) : 0;

        const mapped: DashboardSummaryResponse = {
          totalCompanies,
          totalEmployees,
          accountingFirmsCount: 0,
          avgEmployeesPerCompany: avg,
          employeeDistribution: [],
          recentCompanies: [],
          asOf: raw?.AsOf ?? new Date().toISOString()
        };

        return mapped;
      })
    );
  }
}
