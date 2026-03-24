import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { LeaveTypeLegalRule } from '@app/core/models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class LeaveTypeLegalRuleService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/leave-type-legal-rules`;

  // Get all legal rules
  getAll(): Observable<LeaveTypeLegalRule[]> {
    return this.http.get<LeaveTypeLegalRule[]>(this.baseUrl);
  }

  // Get legal rules by leave type
  getByLeaveType(leaveTypeId: number): Observable<LeaveTypeLegalRule[]> {
    return this.http.get<LeaveTypeLegalRule[]>(`${this.baseUrl}?leaveTypeId=${leaveTypeId}`);
  }

  // Get by ID
  getById(id: number): Observable<LeaveTypeLegalRule> {
    return this.http.get<LeaveTypeLegalRule>(`${this.baseUrl}/${id}`);
  }

  // Create new legal rule
  create(request: any): Observable<LeaveTypeLegalRule> {
    return this.http.post<LeaveTypeLegalRule>(this.baseUrl, request);
  }

  // Update legal rule
  update(id: number, request: any): Observable<LeaveTypeLegalRule> {
    return this.http.patch<LeaveTypeLegalRule>(`${this.baseUrl}/${id}`, request);
  }

  // Delete legal rule
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}