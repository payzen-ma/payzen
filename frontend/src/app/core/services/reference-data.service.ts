import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Gender, MaritalStatus, EducationLevel } from '../models/reference-data.model';

@Injectable({
  providedIn: 'root'
})
export class ReferenceDataService {
  private http = inject(HttpClient);
  private apiUrl = environment.apiUrl;

  // ==================== GENDER ====================
  getGenders(): Observable<Gender[]> {
    return this.http.get<Gender[]>(`${this.apiUrl}/genders`);
  }

  getGenderById(id: number): Observable<Gender> {
    return this.http.get<Gender>(`${this.apiUrl}/genders/${id}`);
  }

  createGender(data: { GenderName: string }): Observable<Gender> {
    return this.http.post<Gender>(`${this.apiUrl}/genders`, data);
  }

  updateGender(id: number, data: { GenderName: string }): Observable<Gender> {
    return this.http.put<Gender>(`${this.apiUrl}/genders/${id}`, data);
  }

  deleteGender(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/genders/${id}`);
  }

  // ==================== MARITAL STATUS ====================
  getMaritalStatuses(): Observable<MaritalStatus[]> {
    return this.http.get<MaritalStatus[]>(`${this.apiUrl}/marital-statuses`);
  }

  getMaritalStatusById(id: number): Observable<MaritalStatus> {
    return this.http.get<MaritalStatus>(`${this.apiUrl}/marital-statuses/${id}`);
  }

  createMaritalStatus(data: { MaritalStatusName: string }): Observable<MaritalStatus> {
    return this.http.post<MaritalStatus>(`${this.apiUrl}/marital-statuses`, data);
  }

  updateMaritalStatus(id: number, data: { MaritalStatusName: string }): Observable<MaritalStatus> {
    return this.http.put<MaritalStatus>(`${this.apiUrl}/marital-statuses/${id}`, data);
  }

  deleteMaritalStatus(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/marital-statuses/${id}`);
  }

  // ==================== EDUCATION LEVEL ====================
  getEducationLevels(): Observable<EducationLevel[]> {
    return this.http.get<EducationLevel[]>(`${this.apiUrl}/education-levels`);
  }

  getEducationLevelById(id: number): Observable<EducationLevel> {
    return this.http.get<EducationLevel>(`${this.apiUrl}/education-levels/${id}`);
  }

  createEducationLevel(data: { EducationLevelName: string }): Observable<EducationLevel> {
    return this.http.post<EducationLevel>(`${this.apiUrl}/education-levels`, data);
  }

  updateEducationLevel(id: number, data: { EducationLevelName: string }): Observable<EducationLevel> {
    return this.http.put<EducationLevel>(`${this.apiUrl}/education-levels/${id}`, data);
  }

  deleteEducationLevel(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/education-levels/${id}`);
  }
}
