import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, forkJoin, of, switchMap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { WorkingCalendar, CreateWorkingCalendarRequest, UpdateWorkingCalendarRequest } from '../models/working-calendar.model';

// Mapping from day names (English) to DayOfWeek numbers (0-6, where 0=Sunday)
const DAY_NAME_TO_DAYOFWEEK: Record<string, number> = {
  'sunday': 0,
  'monday': 1,
  'tuesday': 2,
  'wednesday': 3,
  'thursday': 4,
  'friday': 5,
  'saturday': 6
};

interface WorkingCalendarSyncPayload {
  companyId: number;
  selectedDays: string[]; // ['monday', 'tuesday', ...]
  standardHoursPerDay: number;
}

@Injectable({
  providedIn: 'root'
})
export class WorkingCalendarService {
  private readonly apiUrl = `${environment.apiUrl}/working-calendar`;
  private readonly http = inject(HttpClient);

  /**
   * Get all working calendars
   */
  getAll(): Observable<WorkingCalendar[]> {
    return this.http.get<WorkingCalendar[]>(this.apiUrl);
  }

  /**
   * Get working calendar by ID
   */
  getById(id: number): Observable<WorkingCalendar> {
    return this.http.get<WorkingCalendar>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get working calendars by company ID
   */
  getByCompanyId(companyId: number): Observable<WorkingCalendar[]> {
    return this.http.get<WorkingCalendar[]>(`${this.apiUrl}/company/${companyId}`);
  }

  /**
   * Create a new working calendar entry
   */
  create(request: CreateWorkingCalendarRequest): Observable<WorkingCalendar> {
    return this.http.post<any>(this.apiUrl, request).pipe(
      map(response => response.value || response)
    );
  }

  /**
   * Update an existing working calendar entry
   */
  update(id: number, request: UpdateWorkingCalendarRequest): Observable<WorkingCalendar> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, request).pipe(
      map(response => {
        if (!response) {
          return this.getById(id);
        }
        return response.value || response;
      })
    );
  }

  /**
   * Delete a working calendar entry
   */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Synchronize working days with their specific times for all days of the week
   * @param companyId The company ID
   * @param calendars Array of calendar entries with per-day times
   * @returns Observable of all created/updated working calendar entries
   */
  syncWorkingDaysWithTimes(companyId: number, calendars: any[]): Observable<any[]> {
    console.log('[WorkingCalendarService] syncWorkingDaysWithTimes:', { companyId, calendars });

    if (!calendars || calendars.length === 0) {
      console.warn('[WorkingCalendarService] No calendars to sync');
      return of([]);
    }

    // Create update requests for each calendar entry
    const requests: Observable<WorkingCalendar>[] = calendars.map(cal => {
      if (cal.id) {
        // Update existing entry
        const updateRequest: UpdateWorkingCalendarRequest = {
          isWorkingDay: cal.isWorkingDay,
          startTime: cal.isWorkingDay ? cal.startTime : undefined,
          endTime: cal.isWorkingDay ? cal.endTime : undefined
        };
        console.log(`[WorkingCalendarService] Updating calendar ID ${cal.id}:`, updateRequest);
        return this.update(cal.id, updateRequest);
      } else {
        // Create new entry
        const createRequest: CreateWorkingCalendarRequest = {
          companyId: companyId,
          dayOfWeek: cal.dayOfWeek,
          isWorkingDay: cal.isWorkingDay,
          startTime: cal.isWorkingDay ? cal.startTime : undefined,
          endTime: cal.isWorkingDay ? cal.endTime : undefined
        };
        console.log(`[WorkingCalendarService] Creating calendar for day ${cal.dayOfWeek}:`, createRequest);
        return this.create(createRequest);
      }
    });

    return forkJoin(requests.length > 0 ? requests : [of({} as WorkingCalendar)]);
  }

  /**
   * Synchronize working days for a company.
   * Creates/updates working calendar entries based on selected days and standard hours.
   * @param companyId The company ID
   * @param selectedDays Array of day names (e.g., ['monday', 'tuesday', ...])
   * @param standardHoursPerDay Standard hours per day (e.g., 8)
   * @returns Observable of all created/updated working calendar entries
   */
  syncWorkingDays(companyId: number, selectedDays: string[], standardHoursPerDay: number): Observable<WorkingCalendar[]> {
    console.log('[WorkingCalendarService] syncWorkingDays:', { companyId, selectedDays, standardHoursPerDay });

    // If no days selected, return empty array
    if (!selectedDays || selectedDays.length === 0) {
      console.warn('[WorkingCalendarService] No working days selected');
      return of([]);
    }

    // Get existing working calendars for this company, then sync
    return this.getByCompanyId(companyId).pipe(
      switchMap(existingCalendars => {
        console.log('[WorkingCalendarService] Existing calendars:', existingCalendars);
        
        // Calculate start and end times based on standard hours (e.g., 9:00 - 17:00 for 8 hours)
        const startTime = new Date();
        startTime.setHours(9, 0, 0, 0);
        const endTime = new Date();
        endTime.setHours(9 + standardHoursPerDay, 0, 0, 0);
        
        const startTimeSpan = this.dateToTimeSpan(startTime);
        const endTimeSpan = this.dateToTimeSpan(endTime);

        console.log('[WorkingCalendarService] Calculated times:', { startTimeSpan, endTimeSpan });

        // Create requests for all 7 days of the week
        const requests: Observable<WorkingCalendar>[] = [];
        for (let dayOfWeek = 0; dayOfWeek < 7; dayOfWeek++) {
          const dayName = this.dayOfWeekToName(dayOfWeek);
          const isWorkingDay = selectedDays.some(d => d.toLowerCase() === dayName.toLowerCase());

          const existing = existingCalendars.find(wc => wc.dayOfWeek === dayOfWeek);

          if (existing) {
            // Update existing entry
            const updateRequest: UpdateWorkingCalendarRequest = {
              isWorkingDay: isWorkingDay,
              startTime: isWorkingDay ? startTimeSpan : undefined,
              endTime: isWorkingDay ? endTimeSpan : undefined
            };
            console.log(`[WorkingCalendarService] Updating day ${dayName} (${dayOfWeek}):`, updateRequest);
            requests.push(this.update(existing.id, updateRequest));
          } else {
            // Create new entry
            const createRequest: CreateWorkingCalendarRequest = {
              companyId: companyId,
              dayOfWeek: dayOfWeek,
              isWorkingDay: isWorkingDay,
              startTime: isWorkingDay ? startTimeSpan : undefined,
              endTime: isWorkingDay ? endTimeSpan : undefined
            };
            console.log(`[WorkingCalendarService] Creating day ${dayName} (${dayOfWeek}):`, createRequest);
            requests.push(this.create(createRequest));
          }
        }

        // Return observable that waits for all requests
        return forkJoin(requests.length > 0 ? requests : [of({} as WorkingCalendar)]);
      })
    );
  }

  /**
   * Convert a Date to TimeSpan string format (HH:mm:ss)
   */
  private dateToTimeSpan(date: Date): string {
    const hours = String(date.getHours()).padStart(2, '0');
    const minutes = String(date.getMinutes()).padStart(2, '0');
    const seconds = String(date.getSeconds()).padStart(2, '0');
    return `${hours}:${minutes}:${seconds}`;
  }

  /**
   * Convert DayOfWeek number to name
   */
  private dayOfWeekToName(dayOfWeek: number): string {
    const names = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday'];
    return names[dayOfWeek];
  }
}
