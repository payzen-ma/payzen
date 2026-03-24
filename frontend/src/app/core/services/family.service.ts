import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import { Spouse, Child } from '@app/core/models/employee.model';

@Injectable({
  providedIn: 'root'
})
export class FamilyService {
  private readonly API = `${environment.apiUrl}/employees`;
  private readonly http = inject(HttpClient);

  // Spouse
  getSpouse(employeeId: string | number): Observable<Spouse | null> {
    return this.http.get<Spouse | null>(`${this.API}/${employeeId}/spouse`);
  }

  createSpouse(employeeId: string | number, payload: Partial<Spouse>): Observable<Spouse> {
    return this.http.post<Spouse>(`${this.API}/${employeeId}/spouse`, payload);
  }

  updateSpouse(employeeId: string | number, payload: Partial<Spouse>): Observable<Spouse> {
    return this.http.put<Spouse>(`${this.API}/${employeeId}/spouse`, payload);
  }

  deleteSpouse(employeeId: string | number): Observable<void> {
    return this.http.delete<void>(`${this.API}/${employeeId}/spouse`);
  }

  // Children
  getChildren(employeeId: string | number): Observable<Child[]> {
    return this.http.get<Child[]>(`${this.API}/${employeeId}/children`);
  }

  createChild(employeeId: string | number, payload: Partial<Child>): Observable<Child> {
    return this.http.post<Child>(`${this.API}/${employeeId}/children`, payload);
  }

  updateChild(employeeId: string | number, childId: number | string, payload: Partial<Child>): Observable<Child> {
    return this.http.put<Child>(`${this.API}/${employeeId}/children/${childId}`, payload);
  }

  deleteChild(employeeId: string | number, childId: number | string): Observable<void> {
    return this.http.delete<void>(`${this.API}/${employeeId}/children/${childId}`);
  }
}
