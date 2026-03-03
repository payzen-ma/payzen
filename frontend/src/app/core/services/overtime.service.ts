import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
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
  private readonly OVERTIME_URL = `${environment.apiUrl}/overtimes`;
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

    return this.http.get<{ data: OvertimeReadDto[]; total: number; page: number; pageSize: number }>(
      this.OVERTIME_URL,
      { params }
    ).pipe(
      map(response => ({
        data: response.data.map(dto => this.mapDtoToOvertime(dto)),
        total: response.total,
        page: response.page,
        pageSize: response.pageSize
      }))
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
  createOvertime(request: CreateOvertimeRequest): Observable<Overtime> {
    const companyId = this.companyContextService.companyId();
    if (!companyId) {
      throw new Error('No company selected');
    }

    const payload = {
      ...request,
      companyId
    };

    return this.http.post<OvertimeReadDto>(this.OVERTIME_URL, payload)
      .pipe(map(dto => this.mapDtoToOvertime(dto)));
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
   * Approve an overtime declaration (for managers/RH)
   */
  approveOvertime(id: number, comment?: string): Observable<Overtime> {
    return this.http.put<OvertimeReadDto>(`${this.OVERTIME_URL}/${id}/approve`, { comment })
      .pipe(map(dto => this.mapDtoToOvertime(dto)));
  }

  /**
   * Reject an overtime declaration (for managers/RH)
   */
  rejectOvertime(id: number, comment: string): Observable<Overtime> {
    return this.http.put<OvertimeReadDto>(`${this.OVERTIME_URL}/${id}/reject`, { comment })
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
  private mapDtoToOvertime(dto: OvertimeReadDto): Overtime {
    return {
      id: dto.id,
      employeeId: dto.employeeId,
      employeeFirstName: dto.employeeFirstName,
      employeeLastName: dto.employeeLastName,
      employeeFullName: dto.employeeFullName,
      overtimeDate: dto.overtimeDate,
      overtimeType: (dto as any).overtimeType || OvertimeType.Hourly,
      startTime: dto.startTime,
      endTime: dto.endTime,
      totalHours: dto.totalHours,
      reason: dto.reason || undefined,
      status: dto.status as OvertimeStatus,
      statusDescription: dto.statusDescription,
      createdAt: dto.createdAt,
      createdBy: dto.createdBy,
      createdByName: dto.createdByName,
      approvedAt: dto.approvedAt,
      approvedBy: dto.approvedBy,
      approvedByName: dto.approvedByName,
      approvalComment: dto.approvalComment
    };
  }
}
