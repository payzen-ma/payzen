import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Department, DepartmentCreateUpdateDto } from '../models/department.model';

@Injectable({
  providedIn: 'root'
})
export class DepartmentService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/departements`;

  getAll(): Observable<Department[]> {
    return this.http.get<Department[]>(this.apiUrl);
  }

  getById(id: number): Observable<Department> {
    return this.http.get<Department>(`${this.apiUrl}/${id}`);
  }

  getByCompany(companyId: number): Observable<Department[]> {
    return this.http.get<Department[]>(`${this.apiUrl}/company/${companyId}`);
  }

  create(dto: DepartmentCreateUpdateDto): Observable<Department> {
    return this.http.post<Department>(this.apiUrl, dto);
  }

  update(id: number, dto: DepartmentCreateUpdateDto): Observable<Department> {
    return this.http.put<Department>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}

