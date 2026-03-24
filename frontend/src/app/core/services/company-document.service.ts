import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CompanyDocumentDto, CompanyDocumentUpdatePayload } from '../models/company.model';

@Injectable({ providedIn: 'root' })
export class CompanyDocumentService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/companydocuments`;

  /** GET /api/companydocuments */
  getAll(): Observable<CompanyDocumentDto[]> {
    return this.http.get<CompanyDocumentDto[]>(this.apiUrl);
  }

  /** GET /api/companydocuments/{id} */
  getById(id: number): Observable<CompanyDocumentDto> {
    return this.http.get<CompanyDocumentDto>(`${this.apiUrl}/${id}`);
  }

  /** GET /api/companydocuments/company/{companyId} */
  getByCompany(companyId: number): Observable<CompanyDocumentDto[]> {
    return this.http.get<CompanyDocumentDto[]>(`${this.apiUrl}/company/${companyId}`);
  }

  /** POST /api/companydocuments/upload  (multipart/form-data) */
  upload(formData: FormData): Observable<CompanyDocumentDto> {
    return this.http.post<CompanyDocumentDto>(`${this.apiUrl}/upload`, formData);
  }

  /** PUT /api/companydocuments/{id} */
  update(id: number, payload: CompanyDocumentUpdatePayload): Observable<CompanyDocumentDto> {
    return this.http.put<CompanyDocumentDto>(`${this.apiUrl}/${id}`, payload);
  }

  /** DELETE /api/companydocuments/{id}  (soft delete) */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /** GET /api/companydocuments/{id}/download */
  download(id: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/download`, { responseType: 'blob' });
  }
}
