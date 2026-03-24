import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '@environments/environment';

export interface AttendanceType {
  id: number;
  name: string;
  description?: string;
}

export interface AttendanceTypeLookupOption {
  id: number;
  label: string;
}

@Injectable({
  providedIn: 'root'
})
export class AttendanceTypeService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/attendance-type`;

  /**
   * Get all attendance types
   */
  getAll(): Observable<AttendanceType[]> {
    return this.http.get<AttendanceType[]>(this.baseUrl);
  }

  /**
   * Get attendance types as lookup options for dropdowns
   */
  getLookupOptions(): Observable<AttendanceTypeLookupOption[]> {
    return this.getAll().pipe(
      map(types => types.map(t => ({ id: t.id, label: t.name })))
    );
  }

  /**
   * Get a specific attendance type by ID
   */
  getById(id: number): Observable<AttendanceType> {
    return this.http.get<AttendanceType>(`${this.baseUrl}/${id}`);
  }
}
