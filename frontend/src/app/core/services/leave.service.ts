import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { LeaveType, LeaveTypeCreateDto, LeaveTypePatchDto, LeaveTypePolicy, LeaveTypePolicyCreateDto, LeaveTypePolicyPatchDto } from '../models';

@Injectable({
  providedIn: 'root'
})
export class LeaveService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/leave-types`;

  // Leave Types (company-scoped or global)
  getAll(companyId?: number): Observable<LeaveType[]> {
    const params = companyId ? `?companyId=${companyId}` : '';
    return this.http.get<any>(`${this.apiUrl}${params}`).pipe(
      map(res => {
        const list = Array.isArray(res) ? res : (res?.items || res?.data || []);
        return (list || []).map((item: any) => this.mapLeaveTypeFromDto(item));
      }),
    );
  }

  getById(id: number): Observable<LeaveType> {
    return this.http.get<any>(`${this.apiUrl}/${id}`).pipe(
      map(item => this.mapLeaveTypeFromDto(item))
    );
  }

  getByCompany(companyId: number): Observable<LeaveType[]> {
    return this.getAll(companyId);
  }

  // Get available leave types for creating policies (includes both global and company-specific)
  getAvailableForCompany(companyId: number): Observable<LeaveType[]> {
    return this.http.get<any>(`${this.apiUrl}?companyId=${companyId}&includeGlobal=true`).pipe(
      map(res => {
        const list = Array.isArray(res) ? res : (res?.items || res?.data || []);
        return (list || []).map((item: any) => this.mapLeaveTypeFromDto(item));
      })
    );
  }

  create(dto: LeaveTypeCreateDto): Observable<LeaveType> {
    return this.http.post<any>(this.apiUrl, dto).pipe(map(item => this.mapLeaveTypeFromDto(item)));
  }

  update(id: number, dto: LeaveTypePatchDto): Observable<LeaveType> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, dto).pipe(map(item => this.mapLeaveTypeFromDto(item)));
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  // Leave Type Policies
  private policiesUrl = `${environment.apiUrl}/leave-type-policies`;

  // Leave Type Legal Rules
  private legalRulesUrl = `${environment.apiUrl}/leave-type-legal-rules`;

  getLeaveTypeLegalRules(): Observable<any[]> {
    return this.http.get<any>(this.legalRulesUrl).pipe(
      map(res => Array.isArray(res) ? res : (res?.items || res?.data || []))
    );
  }

  getPoliciesByCompany(companyId?: number): Observable<LeaveTypePolicy[]> {
    const url = companyId ? `${this.policiesUrl}?companyId=${companyId}` : this.policiesUrl;
    return this.http.get<any>(url).pipe(
      map(res => {
        const list = Array.isArray(res) ? res : (res?.items || res?.data || []);
        return (list || []).map((i: any) => this.mapPolicyFromDto(i));
      })
    );
  }

  getPoliciesByLeaveType(leaveTypeId: number): Observable<LeaveTypePolicy[]> {
    return this.http.get<any>(`${this.policiesUrl}/by-leave-type/${leaveTypeId}`).pipe(map(res => {
      const list = Array.isArray(res) ? res : (res?.items || res?.data || []);
      return (list || []).map((i: any) => this.mapPolicyFromDto(i));
    }));
  }

  getPolicyById(id: number): Observable<LeaveTypePolicy> {
    return this.http.get<any>(`${this.policiesUrl}/${id}`).pipe(map(i => this.mapPolicyFromDto(i)));
  }

  createPolicy(dto: LeaveTypePolicyCreateDto): Observable<LeaveTypePolicy> {
    return this.http.post<any>(this.policiesUrl, dto).pipe(map(i => this.mapPolicyFromDto(i)));
  }

  updatePolicy(id: number, dto: LeaveTypePolicyPatchDto): Observable<LeaveTypePolicy> {
    return this.http.put<any>(`${this.policiesUrl}/${id}`, dto).pipe(map(i => this.mapPolicyFromDto(i)));
  }

  deletePolicy(id: number): Observable<void> {
    return this.http.delete<void>(`${this.policiesUrl}/${id}`);
  }

  // Mapping helpers to convert backend DTOs to frontend models
  private mapLeaveTypeFromDto(dto: any): LeaveType {
    if (!dto) return null as any;
    const get = (camel: string, pascal: string) => dto?.[camel] ?? dto?.[pascal];
    return {
      Id: get('id', 'Id'),
      LeaveCode: get('leaveCode', 'LeaveCode') || '',
      LeaveName: get('leaveName', 'LeaveName') || '',
      LeaveDescription: get('leaveDescription', 'LeaveDescription') || '',
      Scope: typeof get('scope', 'Scope') === 'number' ? get('scope', 'Scope') : (get('scope', 'Scope') === 'Global' ? 0 : 1),
      CompanyId: get('companyId', 'CompanyId') ?? null,
      CompanyName: get('companyName', 'CompanyName') || '',
      IsActive: Boolean(get('isActive', 'IsActive')),
      CreatedAt: get('createdAt', 'CreatedAt')
    } as LeaveType;
  }

  private mapPolicyFromDto(dto: any): LeaveTypePolicy {
    if (!dto) return null as any;
    const get = (camel: string, pascal: string) => dto?.[camel] ?? dto?.[pascal];
    const getBool = (camel: string, pascal: string) => Boolean(get(camel, pascal));
    const getNum = (camel: string, pascal: string, def: number = 0) => Number(get(camel, pascal)) || def;
    return {
      Id: get('id', 'Id'),
      CompanyId: get('companyId', 'CompanyId') ?? null,
      LeaveTypeId: get('leaveTypeId', 'LeaveTypeId'),
      IsEnabled: getBool('isEnabled', 'IsEnabled'),
      AccrualMethod: getNum('accrualMethod', 'AccrualMethod', 1),
      DaysPerMonthAdult: getNum('daysPerMonthAdult', 'DaysPerMonthAdult', 1.5),
      DaysPerMonthMinor: getNum('daysPerMonthMinor', 'DaysPerMonthMinor', 2),
      RequiresEligibility6Months: getBool('requiresEligibility6Months', 'RequiresEligibility6Months'),
      RequiresBalance: getBool('requiresBalance', 'RequiresBalance'),
      BonusDaysPerYearAfter5Years: getNum('bonusDaysPerYearAfter5Years', 'BonusDaysPerYearAfter5Years', 1.5),
      AnnualCapDays: getNum('annualCapDays', 'AnnualCapDays', 30),
      AllowCarryover: getBool('allowCarryover', 'AllowCarryover'),
      MaxCarryoverYears: getNum('maxCarryoverYears', 'MaxCarryoverYears', 2),
      MinConsecutiveDays: getNum('minConsecutiveDays', 'MinConsecutiveDays', 12),
      UseWorkingCalendar: getBool('useWorkingCalendar', 'UseWorkingCalendar')
    } as LeaveTypePolicy;
  }
}
