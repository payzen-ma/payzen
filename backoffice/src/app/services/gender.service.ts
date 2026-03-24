import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Gender, CreateGenderRequest, UpdateGenderRequest } from '../models/gender.model';

@Injectable({ providedIn: 'root' })
export class GenderService {
  private baseUrl = 'http://localhost:5119';
  private apiUrl = `${this.baseUrl}/api/genders`;

  constructor(private http: HttpClient) {}

  private fromDto(dto: any): Gender {
    return {
      id: dto.Id,
      code: dto.Code,
      nameFr: dto.NameFr,
      nameAr: dto.NameAr,
      nameEn: dto.NameEn,
      isActive: dto.IsActive,
      createdAt: dto.CreatedAt,
    };
  }

  getAll(includeInactive = true): Observable<Gender[]> {
    const url = includeInactive ? `${this.apiUrl}?includeInactive=true` : this.apiUrl;
    return this.http.get<any[]>(url).pipe(
      map(list => (list || []).map(d => this.fromDto(d)))
    );
  }

  getById(id: number): Observable<Gender> {
    return this.http.get<any>(`${this.apiUrl}/${id}`).pipe(
      map(d => this.fromDto(d))
    );
  }

  create(payload: CreateGenderRequest): Observable<Gender> {
    const dto: any = {
      Code: payload.code,
      NameFr: payload.nameFr,
      NameAr: payload.nameAr,
      NameEn: payload.nameEn,
      IsActive: payload.isActive ?? true,
    };

    return this.http.post<any>(this.apiUrl, dto).pipe(
      map(d => this.fromDto(d))
    );
  }

  update(id: number, payload: UpdateGenderRequest): Observable<Gender> {
    const dto: any = {};
    if (payload.code !== undefined) dto.Code = payload.code;
    if (payload.nameFr !== undefined) dto.NameFr = payload.nameFr;
    if (payload.nameAr !== undefined) dto.NameAr = payload.nameAr;
    if (payload.nameEn !== undefined) dto.NameEn = payload.nameEn;
    if (payload.isActive !== undefined) dto.IsActive = payload.isActive;

    return this.http.put<any>(`${this.apiUrl}/${id}`, dto).pipe(
      map(d => this.fromDto(d))
    );
  }

  delete(id: number) {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
