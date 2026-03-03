import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { environment } from '@environments/environment';
import {
  Overtime,
  CreateOvertimeRequest,
  UpdateOvertimeRequest,
  OvertimeFilters,
  OvertimesResponse,
  OvertimeStats,
  OvertimeStatus,
  OvertimeType
} from '@app/core/models/overtime.model';
import { CompanyContextService } from './companyContext.service';

// Backend DTO interface (après conversion camelCase par l'interceptor)
interface OvertimeReadDto {
  id: number;
  employeeId: number;
  employeeFirstName: string;
  employeeLastName: string;
  employeeFullName: string;
  overtimeDate: string;
  startTime: string;
  endTime: string;
  totalHours: number;
  reason: string | null;
  status: number;
  statusDescription: string;
  createdAt: string;
  createdBy?: number;
  createdByName?: string;
  approvedAt?: string | null;
  approvedBy?: number | null;
  approvedByName?: string | null;
  approvalComment?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class OvertimeService {
  // Backend controller is mounted at /api/employee-overtimes
  private readonly OVERTIME_URL = `${environment.apiUrl}/employee-overtimes`;
  private readonly http = inject(HttpClient);
  private readonly companyContextService = inject(CompanyContextService);

  /**
   * Get overtimes with filters
   */
  getOvertimes(filters: OvertimeFilters = {}): Observable<OvertimesResponse> {
    const companyId = this.companyContextService.companyId();
    if (!companyId) {
      throw new Error('No company selected');
    }

    let params = new HttpParams().set('companyId', companyId.toString());

    if (filters.employeeId) {
      params = params.set('employeeId', filters.employeeId.toString());
    }
    if (filters.startDate) {
      params = params.set('startDate', filters.startDate);
    }
    if (filters.endDate) {
      params = params.set('endDate', filters.endDate);
    }
    if (filters.status !== undefined) {
      params = params.set('status', filters.status.toString());
    }
    if (filters.page) {
      params = params.set('page', filters.page.toString());
    }
    if (filters.pageSize) {
      params = params.set('pageSize', filters.pageSize.toString());
    }

    return this.http.get<any>(
      this.OVERTIME_URL,
      { params }
    ).pipe(
      map(response => {
        // API may return either a paged object { data: [...] } or a raw array
        if (Array.isArray(response)) {
          return {
            data: response.map((dto: OvertimeReadDto) => this.mapDtoToOvertime(dto)),
            total: response.length,
            page: 1,
            pageSize: response.length
          } as OvertimesResponse;
        }

        if (response && Array.isArray(response.data)) {
          return {
            data: response.data.map((dto: OvertimeReadDto) => this.mapDtoToOvertime(dto)),
            total: response.total ?? response.data.length,
            page: response.page ?? 1,
            pageSize: response.pageSize ?? response.data.length
          } as OvertimesResponse;
        }

        return { data: [], total: 0, page: 1, pageSize: 0 } as OvertimesResponse;
      })
    );
  }

  /**
   * Get overtime by ID
   */
  getOvertimeById(id: number): Observable<Overtime> {
    return this.http.get<OvertimeReadDto>(`${this.OVERTIME_URL}/${id}`)
      .pipe(map(dto => this.mapDtoToOvertime(dto)));
  }

  /**
   * Create a new overtime declaration
   */
  createOvertime(request: CreateOvertimeRequest): Observable<Overtime | null> {
    const companyId = this.companyContextService.companyId();
    if (!companyId) {
      throw new Error('No company selected');
    }

    const payload = request; // send raw DTO (backend example uses unwrapped JSON)
    return this.http.post<OvertimeReadDto | null>(this.OVERTIME_URL, payload).pipe(
      map(dto => {
        if (!dto) {
          console.warn('[OvertimeService] POST returned null DTO');
          return null;
        }
        return this.mapDtoToOvertime(dto);
      })
    );
  }

  /**
   * Update an existing overtime declaration
   */
  updateOvertime(id: number, request: UpdateOvertimeRequest): Observable<Overtime> {
    return this.http.put<OvertimeReadDto>(`${this.OVERTIME_URL}/${id}`, request)
      .pipe(map(dto => this.mapDtoToOvertime(dto)));
  }

  /**
   * Delete an overtime declaration
   */
  deleteOvertime(id: number): Observable<void> {
    return this.http.delete<void>(`${this.OVERTIME_URL}/${id}`);
  }

  /**
   * Cancel an overtime declaration
   */
  cancelOvertime(id: number): Observable<Overtime> {
    return this.http.put<OvertimeReadDto>(`${this.OVERTIME_URL}/${id}/cancel`, {})
      .pipe(map(dto => this.mapDtoToOvertime(dto)));
  }

  /**
   * Submit an overtime (move from Draft -> Submitted)
   */
  submitOvertime(id: number): Observable<Overtime> {
    return this.http.put<OvertimeReadDto>(`${this.OVERTIME_URL}/${id}/submit`, {})
      .pipe(map(dto => this.mapDtoToOvertime(dto)));
  }

  /**
   * Approve an overtime declaration (for managers/RH)
   */
  approveOvertime(id: number, comment?: string): Observable<Overtime> {
    // Backend expects EmployeeOvertimeApprovalDto { Status: Approved|Rejected, ManagerComment }
    const payload = { status: OvertimeStatus.Approved, managerComment: comment } as any;
    return this.http.put<OvertimeReadDto>(`${this.OVERTIME_URL}/${id}/approve`, payload)
      .pipe(map(dto => this.mapDtoToOvertime(dto)));
  }

  /**
   * Reject an overtime declaration (for managers/RH)
   */
  rejectOvertime(id: number, comment: string): Observable<Overtime> {
    // The backend uses the same /approve endpoint and a DTO with Status=Rejected
    const payload = { status: OvertimeStatus.Rejected, managerComment: comment } as any;
    return this.http.put<OvertimeReadDto>(`${this.OVERTIME_URL}/${id}/approve`, payload)
      .pipe(map(dto => this.mapDtoToOvertime(dto)));
  }

  /**
   * Get overtime statistics for an employee
   */
  getOvertimeStats(employeeId?: number): Observable<OvertimeStats> {
    const companyId = this.companyContextService.companyId();
    if (!companyId) {
      throw new Error('No company selected');
    }

    let params = new HttpParams().set('companyId', companyId.toString());
    if (employeeId) {
      params = params.set('employeeId', employeeId.toString());
    }

    return this.http.get<OvertimeStats>(`${this.OVERTIME_URL}/stats`, { params });
  }

  /**
   * Map backend DTO to frontend Overtime model
   */
  private mapDtoToOvertime(dto: any): Overtime {
    const get = (camel: string, pascal: string) => dto?.[camel] ?? dto?.[pascal];

    const id = get('id', 'Id');
    const employeeId = get('employeeId', 'EmployeeId');
    const employeeFullName = get('employeeFullName', 'EmployeeFullName') ?? get('employeeFirstName', 'EmployeeFirstName');
    const overtimeDate = get('overtimeDate', 'OvertimeDate');
    const overtimeTypeRaw = get('overtimeType', 'OvertimeType');
    const overtimeType = this.mapOvertimeType(overtimeTypeRaw);
    const startTime = get('startTime', 'StartTime');
    const endTime = get('endTime', 'EndTime');
    const totalHours = Number(get('totalHours', 'TotalHours')) || Number(get('durationInHours', 'DurationInHours')) || 0;
    const reason = get('reason', 'Reason') ?? undefined;
    const statusRaw = get('status', 'Status');
    const status = this.mapOvertimeStatus(statusRaw);
    const statusDescription = get('statusDescription', 'StatusDescription') ?? undefined;
    const createdAt = get('createdAt', 'CreatedAt');
    const overtimeTypeDescription = get('overtimeTypeDescription', 'OvertimeTypeDescription') ?? undefined;
    const durationInHours = Number(get('durationInHours', 'DurationInHours')) || undefined;
    const rateMultiplierApplied = Number(get('rateMultiplierApplied', 'RateMultiplierApplied')) || undefined;
    const isProcessedInPayroll = get('isProcessedInPayroll', 'IsProcessedInPayroll');
    const employeeComment = get('employeeComment', 'EmployeeComment') ?? undefined;

    return {
      id: id,
      employeeId: employeeId,
      employeeFirstName: undefined,
      employeeLastName: undefined,
      employeeFullName: employeeFullName,
      overtimeDate: overtimeDate,
      overtimeType: overtimeType as any,
      startTime: startTime,
      endTime: endTime,
        totalHours: totalHours,
        durationInHours: durationInHours,
        overtimeTypeDescription: overtimeTypeDescription,
        rateMultiplierApplied: rateMultiplierApplied,
        isProcessedInPayroll: isProcessedInPayroll,
        employeeComment: employeeComment,
      reason: reason,
      status: status as OvertimeStatus,
      statusDescription: statusDescription,
      createdAt: createdAt,
      createdBy: get('createdBy', 'CreatedBy'),
      createdByName: get('createdByName', 'CreatedByName'),
      approvedAt: get('approvedAt', 'ApprovedAt'),
      approvedBy: get('approvedBy', 'ApprovedBy'),
      approvedByName: get('approvedByName', 'ApprovedByName'),
      approvalComment: get('approvalComment', 'ApprovalComment')
    };
  }

  /**
   * Map overtime type from string or number to OvertimeType enum
   */
  private mapOvertimeType(value: any): OvertimeType | undefined {
    if (value == null) return undefined;
    
    // If it's already a number, return it
    if (typeof value === 'number') return value as OvertimeType;
    
    // If it's a string, map it to the enum value
    if (typeof value === 'string') {
      const typeMap: { [key: string]: OvertimeType } = {
        'None': OvertimeType.None,
        'Standard': OvertimeType.Standard,
        'PublicHoliday': OvertimeType.PublicHoliday,
        'WeeklyRest': OvertimeType.WeeklyRest,
        'Night': OvertimeType.Night
      };
      return typeMap[value] ?? OvertimeType.Standard;
    }
    
    // Try to convert to number as fallback
    const num = Number(value);
    return isNaN(num) ? OvertimeType.Standard : num as OvertimeType;
  }

  /**
   * Map overtime status from string or number to OvertimeStatus enum
   */
  private mapOvertimeStatus(value: any): OvertimeStatus {
    if (value == null) return OvertimeStatus.Draft;
    
    // If it's already a number, return it
    if (typeof value === 'number') return value as OvertimeStatus;
    
    // If it's a string, map it to the enum value
    if (typeof value === 'string') {
      const statusMap: { [key: string]: OvertimeStatus } = {
        'Draft': OvertimeStatus.Draft,
        'Submitted': OvertimeStatus.Submitted,
        'Approved': OvertimeStatus.Approved,
        'Rejected': OvertimeStatus.Rejected,
        'Cancelled': OvertimeStatus.Cancelled
      };
      return statusMap[value] ?? OvertimeStatus.Draft;
    }
    
    // Try to convert to number as fallback
    const num = Number(value);
    return isNaN(num) ? OvertimeStatus.Draft : num as OvertimeStatus;
  }
}
