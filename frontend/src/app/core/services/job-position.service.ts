import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { JobPosition, JobPositionCreateUpdateDto } from '../models/job-position.model';

@Injectable({
  providedIn: 'root'
})
export class JobPositionService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/job-positions`;

  getAll(): Observable<JobPosition[]> {
    return this.http.get<JobPosition[]>(this.apiUrl);
  }

  getById(id: number): Observable<JobPosition> {
    return this.http.get<JobPosition>(`${this.apiUrl}/${id}`);
  }

  getByCompany(companyId: number): Observable<JobPosition[]> {
    return this.http.get<JobPosition[]>(`${this.apiUrl}/by-company/${companyId}`);
  }

  getPredefined(): Observable<JobPosition[]> {
    return this.http.get<JobPosition[]>(`${this.apiUrl}/predefined`);
  }

  create(dto: JobPositionCreateUpdateDto): Observable<JobPosition> {
    return this.http.post<JobPosition>(this.apiUrl, dto);
  }

  update(id: number, dto: JobPositionCreateUpdateDto): Observable<JobPosition> {
    return this.http.put<JobPosition>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}

