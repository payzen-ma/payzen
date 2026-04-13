import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { DashboardSummary, RecentCompany } from '../models/dashboard.model';
import { RevenueMetrics, UsageMetrics } from '../models/metrics.model';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);
  // Base API - adapt if you have environment variable
  private baseUrl = 'https://api-test.payzenhr.com/api';

  getSummary(): Observable<DashboardSummary> {
    return this.http.get<any>(`${this.baseUrl}/dashboard/summary`).pipe(
      map((resp) => this.transformSummary(resp))
    );
  }

  private transformSummary(resp: any): DashboardSummary {
    if (!resp) {
      return {
        totalCompanies: 0,
        totalEmployees: 0,
        accountingFirmsCount: 0,
        avgEmployeesPerCompany: 0,
        employeeDistribution: [],
        recentCompanies: [],
        asOf: undefined
      };
    }

    const employeeDistribution = (resp.EmployeeDistribution || resp.employeeDistribution || []).map((e: any) => ({
      bucket: e.Bucket ?? e.bucket,
      companiesCount: e.CompaniesCount ?? e.companiesCount ?? 0,
      employeesCount: e.EmployeesCount ?? e.employeesCount ?? 0,
      percentage: e.Percentage ?? e.percentage
    }));

    const recentCompanies = (resp.RecentCompanies || resp.recentCompanies || []).map((c: any) => ({
      id: c.Id ?? c.id,
      companyName: c.CompanyName ?? c.companyName,
      countryName: c.CountryName ?? c.countryName,
      cityName: c.CityName ?? c.cityName,
      employeesCount: c.EmployeesCount ?? c.employeesCount,
      createdAt: c.CreatedAt ?? c.createdAt,
      status: c.Status ?? c.status
    }));

    return {
      totalCompanies: resp.TotalCompanies ?? resp.totalCompanies ?? 0,
      totalEmployees: resp.TotalEmployees ?? resp.totalEmployees ?? 0,
      accountingFirmsCount: resp.AccountingFirmsCount ?? resp.accountingFirmsCount ?? 0,
      avgEmployeesPerCompany: resp.AvgEmployeesPerCompany ?? resp.avgEmployeesPerCompany ?? 0,
      employeeDistribution,
      recentCompanies,
      asOf: resp.AsOf ?? resp.asOf
    };
  }

  // Convenience composite endpoint that may return summary + recent lists
  getDashboardComposite(): Observable<any> {
    return this.http.get<any>(`${this.baseUrl}/dashboard`);
  }

  getRecentCompanies(limit = 5): Observable<RecentCompany[]> {
    const params = new HttpParams().set('limit', String(limit)).set('sort', 'createdAt:desc');
    return this.http.get<RecentCompany[]>(`${this.baseUrl}/companies`, { params });
  }

  // Usage metrics aggregated server-side
  getUsageMetrics(from?: string, to?: string, groupBy: 'day' | 'month' = 'day'): Observable<UsageMetrics> {
    let params = new HttpParams().set('groupBy', groupBy);
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<UsageMetrics>(`${this.baseUrl}/metrics/usage`, { params });
  }

  getRevenueMetrics(from?: string, to?: string, groupBy: 'day' | 'month' = 'day'): Observable<RevenueMetrics> {
    let params = new HttpParams().set('groupBy', groupBy);
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<RevenueMetrics>(`${this.baseUrl}/metrics/revenue`, { params });
  }
}
