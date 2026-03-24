import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { HttpResponse } from '@angular/common/http';

@Injectable({ providedIn: 'root' })
export class EventLogService {
  private baseUrl = 'http://localhost:5119';
  private apiUrl = `${this.baseUrl}/api/events`;

  constructor(private http: HttpClient) {}

  /**
   * Get events with optional filters and pagination
   */
  getEvents(filters?: {
    source?: 'company' | 'employee';
    eventName?: string;
    fromDate?: string;
    toDate?: string;
    companyId?: number;
    employeeId?: number;
    page?: number;
    pageSize?: number;
  }): Observable<any[]> {
    let params = new HttpParams();
    const f = filters ?? {} as any;
    if (f.source) params = params.set('source', f.source);
    if (f.eventName) params = params.set('eventName', f.eventName);
    if (f.fromDate) params = params.set('fromDate', f.fromDate);
    if (f.toDate) params = params.set('toDate', f.toDate);
    if (f.companyId != null) params = params.set('companyId', String(f.companyId));
    if (f.employeeId != null) params = params.set('employeeId', String(f.employeeId));
    if (f.page != null) params = params.set('page', String(f.page));
    if (f.pageSize != null) params = params.set('pageSize', String(f.pageSize));

    return this.http.get<any>(this.apiUrl, { params }).pipe(
      map(res => {
        if (Array.isArray(res)) return res;
        if (res == null) return [];
        // handle common lowercase keys
        if (Array.isArray(res.items)) return res.items;
        if (Array.isArray(res.data)) return res.data;
        if (Array.isArray(res.rows)) return res.rows;
        if (Array.isArray(res.result)) return res.result;
        if (Array.isArray(res.value)) return res.value;
        // handle PascalCase keys returned by some APIs
        if (Array.isArray(res.Items)) return res.Items;
        if (Array.isArray(res.Data)) return res.Data;
        if (Array.isArray(res.Rows)) return res.Rows;
        if (Array.isArray(res.Result)) return res.Result;
        if (Array.isArray(res.Value)) return res.Value;
        // sometimes payload is { Count: n, Items: [...] }
        if (res.Items && Array.isArray(res.Items)) return res.Items;

        return [];
      })
    );
  }

  /** Debug: return full HttpResponse so caller can inspect status/headers/body */
  getEventsRaw(filters?: {
    source?: 'company' | 'employee';
    eventName?: string;
    fromDate?: string;
    toDate?: string;
    companyId?: number;
    employeeId?: number;
    page?: number;
    pageSize?: number;
  }): Observable<HttpResponse<any>> {
    let params = new HttpParams();
    const f = filters ?? {} as any;
    if (f.source) params = params.set('source', f.source);
    if (f.eventName) params = params.set('eventName', f.eventName);
    if (f.fromDate) params = params.set('fromDate', f.fromDate);
    if (f.toDate) params = params.set('toDate', f.toDate);
    if (f.companyId != null) params = params.set('companyId', String(f.companyId));
    if (f.employeeId != null) params = params.set('employeeId', String(f.employeeId));
    if (f.page != null) params = params.set('page', String(f.page));
    if (f.pageSize != null) params = params.set('pageSize', String(f.pageSize));

    return this.http.get<any>(this.apiUrl, { params, observe: 'response' as const });
  }
}
