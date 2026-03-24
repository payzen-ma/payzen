import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';

export interface AttendanceBreakDto {
  id: number;
  breakStart: string;
  breakEnd: string | null;
  breakType: string;
}

export interface StartBreakDto {
  attendanceId: number;
  breakStart: string;
  breakType: string;
}

export interface EndBreakDto {
  breakEnd: string;
}

export interface BreakStatistics {
  attendanceId: number;
  totalBreakMinutes: number;
  totalBreakTime: string;
  breakCount: number;
}

@Injectable({
  providedIn: 'root'
})
export class AttendanceBreakService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/employee-attendance-break`;

  /**
   * Start a new break
   */
  startBreak(dto: StartBreakDto): Observable<AttendanceBreakDto> {
    return this.http.post<AttendanceBreakDto>(`${this.baseUrl}/start`, dto);
  }

  /**
   * End an open break
   */
  endBreak(attendanceId: number, dto: EndBreakDto): Observable<AttendanceBreakDto> {
    return this.http.post<AttendanceBreakDto>(
      `${this.baseUrl}/end/${attendanceId}`,
      dto
    );
  }

  /**
   * Get a specific break by ID
   */
  getById(id: number): Observable<AttendanceBreakDto> {
    return this.http.get<AttendanceBreakDto>(`${this.baseUrl}/${id}`);
  }

  /**
   * Get all breaks for a specific attendance record
   */
  getByAttendance(attendanceId: number): Observable<AttendanceBreakDto[]> {
    return this.http.get<AttendanceBreakDto[]>(
      `${this.baseUrl}/attendance/${attendanceId}`
    );
  }

  /**
   * Update an existing break (admin/manual editing)
   */
  updateBreak(id: number, dto: Partial<AttendanceBreakDto>): Observable<AttendanceBreakDto> {
    return this.http.put<AttendanceBreakDto>(`${this.baseUrl}/${id}`, dto);
  }

  /**
   * Delete a break
   */
  deleteBreak(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /**
   * Get total break time statistics for an attendance record
   */
  getTotalBreakTime(attendanceId: number): Observable<BreakStatistics> {
    return this.http.get<BreakStatistics>(
      `${this.baseUrl}/attendance/${attendanceId}/total-break-time`
    );
  }
}
