import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { EmployeeOvertime } from '../models/employee-overtime.model';

@Injectable({ providedIn: 'root' })
export class EmployeeOvertimeService {
  private apiUrl = 'https://api-test.payzenhr.com/api/employee-overtimes';

  constructor(private http: HttpClient) { }

  getAll(): Observable<EmployeeOvertime[]> {
    return this.http.get<EmployeeOvertime[]>(this.apiUrl);
  }

  getById(id: number): Observable<EmployeeOvertime> {
    return this.http.get<EmployeeOvertime>(`${this.apiUrl}/${id}`);
  }

  create(overtime: EmployeeOvertime): Observable<EmployeeOvertime> {
    return this.http.post<EmployeeOvertime>(this.apiUrl, overtime);
  }

  update(id: number, overtime: EmployeeOvertime): Observable<EmployeeOvertime> {
    return this.http.put<EmployeeOvertime>(`${this.apiUrl}/${id}`, overtime);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
