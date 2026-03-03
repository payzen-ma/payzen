import { Injectable, inject, signal, WritableSignal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of, throwError } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { environment } from '@environments/environment';
import {
  Absence,
  AbsenceFilters,
  AbsencesResponse,
  CreateAbsenceRequest,
  UpdateAbsenceRequest,
  AbsenceStats,
  AbsenceDurationType,
  AbsenceStatus
} from '@app/core/models/absence.model';
import { CompanyContextService } from './companyContext.service';

// Backend DTO interface (après conversion camelCase par l'interceptor)
interface AbsenceReadDto {
  id: number;
  employeeId: number;
  employeeFirstName: string;
  employeeLastName: string;
  employeeFullName: string;
  absenceDate: string;
  absenceDateFormatted: string;
  durationType: number; // 1=FullDay, 2=HalfDay, 3=Hourly
  durationTypeDescription: string;
  isMorning: boolean | null;
  halfDayDescription: string | null;
  startTime: string | null;
  endTime: string | null;
  absenceType: string;
  reason: string | null;
  status: number; // 1=Submitted, 2=Approved, 3=Rejected, 4=Cancelled, 5=Expired
  statusDescription: string;
  createdAt: string;
  createdBy?: number;
  createdByName?: string;
  decisionAt?: string | null;
  decisionBy?: number | null;
  decisionByName?: string | null;
  decisionComment?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class AbsenceService {
  private readonly ABSENCE_URL = `${environment.apiUrl}/absences`;
  private contextService = inject(CompanyContextService);

  public readonly hoursList: string[] = Array.from({ length: 24 }, (_, i) =>
    `${i.toString().padStart(2, '0')}:00`
  );

  public newAbsence: WritableSignal<Partial<CreateAbsenceRequest>> = signal({});

  updateField(field: string, value: string) {
    this.newAbsence.update(prev => ({
      ...prev,
      [field]: value
    }));
  }

  constructor(private http: HttpClient) {}

  /**
   * Map backend DTO to frontend Absence model
   */
  private mapDtoToAbsence(dto: AbsenceReadDto): Absence {
    console.log('[AbsenceService] Mapping DTO:', dto);
    
    const durationTypeMap: Record<number, AbsenceDurationType> = {
      1: 'FullDay',
      2: 'HalfDay',
      3: 'Hourly'
    };

    // Map status number to string based on StatusDescription or number
    let status: AbsenceStatus | undefined;
    if (dto.status) {
      // Map by status number directly
      switch (dto.status) {
        case 1:
          status = 'Submitted';
          break;
        case 2:
          status = 'Approved';
          break;
        case 3:
          status = 'Rejected';
          break;
        case 4:
          status = 'Cancelled';
          break;
        case 5:
          status = 'Expired';
          break;
      }
    }
    // Fallback to StatusDescription if numeric mapping fails
    if (!status && dto.statusDescription) {
      const statusDesc = dto.statusDescription.toLowerCase();
      if (statusDesc === 'submitted') {
        status = 'Submitted';
      } else if (statusDesc === 'approved') {
        status = 'Approved';
      } else if (statusDesc === 'rejected') {
        status = 'Rejected';
      } else if (statusDesc === 'cancelled') {
        status = 'Cancelled';
      } else if (statusDesc === 'expired') {
        status = 'Expired';
      }
    }

    return {
      id: dto.id,
      employeeId: dto.employeeId,
      employeeName: dto.employeeFullName,
      absenceDate: dto.absenceDate,
      durationType: durationTypeMap[dto.durationType] || 'FullDay',
      isMorning: dto.isMorning ?? undefined,
      startTime: dto.startTime ?? undefined,
      endTime: dto.endTime ?? undefined,
      absenceType: dto.absenceType as any,
      reason: dto.reason ?? undefined,
      status,
      statusDescription: dto.statusDescription,
      createdAt: dto.createdAt,
      createdBy: dto.createdBy ?? 0,
      createdByName: dto.createdByName ?? undefined,
      decisionAt: dto.decisionAt ?? undefined,
      decisionBy: dto.decisionBy ?? undefined,
      decisionByName: dto.decisionByName ?? undefined,
      decisionComment: dto.decisionComment ?? undefined
    };
  }

  /**
   * Get all absences with optional filters (for HR)
   */
  getAbsences(filters?: AbsenceFilters): Observable<AbsencesResponse> {
    let params = new HttpParams();
    const companyId = this.contextService.companyId();
    
    if (companyId) {
      params = params.set('companyId', String(companyId));
    }

    if (filters) {
      if (filters.employeeId) params = params.set('employeeId', filters.employeeId.toString());
      if (filters.absenceType) params = params.set('absenceType', filters.absenceType);
      if (filters.durationType) params = params.set('durationType', filters.durationType);
      if (filters.status) params = params.set('status', filters.status);
      if (filters.startDate) params = params.set('startDate', filters.startDate);
      if (filters.endDate) params = params.set('endDate', filters.endDate);
      if (filters.page) params = params.set('page', filters.page.toString());
      if (filters.limit) params = params.set('limit', filters.limit.toString());
    }

    return this.http.get<AbsenceReadDto[]>(this.ABSENCE_URL, { params }).pipe(
      map(dtos => {
        const absences = dtos.map(dto => this.mapDtoToAbsence(dto));
        
        // Filter only approved absences for stats calculation
        const approvedAbsences = absences.filter(a => a.status === 'Approved');
        
        // Calculate stats from approved absences only
        const totalAbsences = approvedAbsences.length;
        const totalDays = approvedAbsences.reduce((acc, a) => {
          if (a.durationType === 'FullDay') return acc + 1;
          if (a.durationType === 'HalfDay') return acc + 0.5;
          if (a.durationType === 'Hourly' && a.startTime && a.endTime) {
            // Calculate hours and convert to days (8h = 1 day)
            const start = a.startTime.split(':');
            const end = a.endTime.split(':');
            const startMinutes = parseInt(start[0]) * 60 + parseInt(start[1]);
            const endMinutes = parseInt(end[0]) * 60 + parseInt(end[1]);
            const durationMinutes = endMinutes - startMinutes;
            const hours = durationMinutes / 60;
            return acc + (hours / 8); // 8h work day = 1 day
          }
          return acc;
        }, 0);

        return {
          absences,
          total: absences.length,
          stats: { totalAbsences, totalDays }
        };
      }),
      catchError(err => {
        console.error('Failed to fetch absences:', err);
        return throwError(() => err);
      })
    );
  }

  /**
   * Get absences for a specific employee
   */
  getEmployeeAbsences(employeeId: string): Observable<AbsencesResponse> {
    // Use the existing getAbsences method with employeeId filter
    return this.getAbsences({ employeeId: Number(employeeId) });
  }

  /**
   * Get a single absence by ID
   */
  getAbsenceById(id: number): Observable<Absence> {
    return this.http.get<AbsenceReadDto>(`${this.ABSENCE_URL}/${id}`).pipe(
      map(dto => this.mapDtoToAbsence(dto))
    );
  }

  /**
   * Create a new absence request
   */
  createAbsence(request: CreateAbsenceRequest): Observable<Absence> {
    const companyId = this.contextService.companyId();
    // Normalize date-only values: PrimeNG datepicker can emit Date objects,
    // but backend expects a DateOnly string (yyyy-MM-dd).
    const normalizeRequest: any = { ...request };
    const d = normalizeRequest.absenceDate;
    if (d instanceof Date) {
      const year = d.getFullYear();
      const month = String(d.getMonth() + 1).padStart(2, '0');
      const day = String(d.getDate()).padStart(2, '0');
      normalizeRequest.absenceDate = `${year}-${month}-${day}`;
    } else if (typeof d === 'string') {
      // Empty string -> remove to avoid sending invalid value
      if (d === '') {
        delete normalizeRequest.absenceDate;
      } else if (/^\d{2}\/\d{2}\/\d{4}$/.test(d)) {
        // Convert dd/MM/yyyy -> yyyy-MM-dd
        const [day, month, year] = d.split('/');
        normalizeRequest.absenceDate = `${year}-${month.padStart(2, '0')}-${day.padStart(2, '0')}`;
      } else if (/^\d{4}-\d{2}-\d{2}$/.test(d)) {
        // already in ISO date-only
        normalizeRequest.absenceDate = d;
      } else {
        // Try to parse and convert to yyyy-MM-dd
        const parsed = new Date(d);
        if (!isNaN(parsed.getTime())) {
          const year = parsed.getFullYear();
          const month = String(parsed.getMonth() + 1).padStart(2, '0');
          const day = String(parsed.getDate()).padStart(2, '0');
          normalizeRequest.absenceDate = `${year}-${month}-${day}`;
        }
      }
    }

    const durationEnumMap: Record<AbsenceDurationType, number> = {
      FullDay: 1,
      HalfDay: 2,
      Hourly: 3
    };

    const payload: any = {
      EmployeeId: normalizeRequest.employeeId,
      AbsenceDate: normalizeRequest.absenceDate,
      DurationType: durationEnumMap[normalizeRequest.durationType as AbsenceDurationType] ?? normalizeRequest.durationType
    };

    if (normalizeRequest.absenceType) {
      payload.AbsenceType = normalizeRequest.absenceType;
    }

    // Add optional fields - omit null/undefined values
    if (normalizeRequest.reason) {
      payload.Reason = normalizeRequest.reason;
    }
    if (normalizeRequest.durationType === 'HalfDay' && normalizeRequest.isMorning !== undefined) {
      payload.IsMorning = normalizeRequest.isMorning;
    }
    if (normalizeRequest.durationType === 'Hourly') {
      if (normalizeRequest.startTime) payload.StartTime = normalizeRequest.startTime;
      if (normalizeRequest.endTime) payload.EndTime = normalizeRequest.endTime;
    }
    if (companyId) {
      // Convert companyId to number for C# int type
      payload.CompanyId = typeof companyId === 'string' ? parseInt(companyId, 10) : companyId;
    }

    // Backend expects data wrapped in 'dto' field
    console.log('[AbsenceService] Creating absence with payload:', JSON.stringify(payload, null, 2));

    return this.http.post<Absence>(this.ABSENCE_URL, payload).pipe(
      catchError(err => {
        console.error('Create absence error:', err);
        console.error('Error details:', err?.error);
        if (err?.error?.errors) {
          console.error('Validation errors:', JSON.stringify(err.error.errors, null, 2));
          // Log each field error
          Object.keys(err.error.errors).forEach(key => {
            console.error(`  - Field '${key}':`, err.error.errors[key]);
          });
        }
        return throwError(() => err);
      })
    );
  }

  /**
   * Update an absence
   */
  updateAbsence(id: number, request: UpdateAbsenceRequest): Observable<Absence> {
    return this.http.patch<Absence>(`${this.ABSENCE_URL}/${id}`, request);
  }

  /**
   * Delete an absence
   */
  deleteAbsence(id: number): Observable<void> {
    return this.http.delete<void>(`${this.ABSENCE_URL}/${id}`);
  }

  /**
   * Approve an absence request (HR action)
   */
  approveAbsence(id: number): Observable<void> {
    return this.http.post<void>(`${this.ABSENCE_URL}/${id}/approve`, {});
  }

  /**
   * Reject an absence request (HR action)
   */
  rejectAbsence(id: number, reason?: string): Observable<void> {
    return this.http.post<void>(`${this.ABSENCE_URL}/${id}/reject`, { 
      Reason: reason || ''
    });
  }

  /**
   * Cancel an absence request (Employee action - changes status to Cancelled)
   */
  cancelAbsence(id: number): Observable<void> {
    return this.http.post<void>(`${this.ABSENCE_URL}/${id}/cancel`, {});
  }

  /**
   * Get absence statistics
   */
  getAbsenceStats(filters?: AbsenceFilters): Observable<AbsenceStats> {
    let params = new HttpParams();
    const companyId = this.contextService.companyId();
    
    if (companyId) {
      params = params.set('companyId', String(companyId));
    }

    if (filters?.employeeId) {
      params = params.set('employeeId', filters.employeeId.toString());
    }

    // Try the dedicated stats endpoint first. If it's not available (404),
    // fall back to fetching absences and computing stats client-side.
    return this.http.get<AbsenceStats>(`${this.ABSENCE_URL}/stats`, { params }).pipe(
      catchError(err => {
        if (err?.status === 404) {
          // Fallback: fetch absences and compute stats
          // Ask for a reasonably large page to include all recent absences
          const fallbackParams = params.set('limit', '1000');
          return this.http.get<AbsencesResponse>(this.ABSENCE_URL, { params: fallbackParams }).pipe(
            map(resp => {
              const absences = resp.absences || [];
              // Filter only approved absences for stats
              const approvedAbsences = absences.filter(a => a.status === 'Approved');
              const totalAbsences = approvedAbsences.length;
              // totalDays: sum full days + 0.5 for half days, else compute from start/end if hourly
              const totalDays = approvedAbsences.reduce((acc, a) => {
                if (a.durationType === 'FullDay') return acc + 1;
                if (a.durationType === 'HalfDay') return acc + 0.5;
                if (a.durationType === 'Hourly') return acc + 0; // hourly counted as 0 days (or compute hours if needed)
                return acc;
              }, 0);
              return { totalAbsences, totalDays } as AbsenceStats;
            })
          );
        }
        return throwError(() => err);
      })
    );
  }

  /**
   * Upload justification document
   */
  uploadJustification(absenceId: number, file: File): Observable<{ url: string }> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<{ url: string }>(`${this.ABSENCE_URL}/${absenceId}/upload`, formData);
  }
}
