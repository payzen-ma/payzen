import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { ContractType, ContractTypeCreateDto, ContractTypeUpdateDto } from '../models/contract-type.model';

@Injectable({
  providedIn: 'root'
})
export class ContractTypeService {
  private http = inject(HttpClient);
  private apiUrl = `${environment.apiUrl}/contract-types`;

  getAll(): Observable<ContractType[]> {
    return this.http.get<any[]>(this.apiUrl).pipe(
      map(items => (items || []).map(i => this.normalize(i)))
    );
  }

  getById(id: number): Observable<ContractType> {
    return this.http.get<any>(`${this.apiUrl}/${id}`).pipe(
      map(i => this.normalize(i))
    );
  }

  getByCompany(companyId: number): Observable<ContractType[]> {
    return this.http.get<any[]>(`${this.apiUrl}/by-company/${companyId}`).pipe(
      map(items => (items || []).map(i => this.normalize(i)))
    );
  }

  getPredefined(): Observable<ContractType[]> {
    return this.http.get<any[]>(`${this.apiUrl}/predefined`).pipe(
      map(items => (items || []).map(i => this.normalize(i)))
    );
  }

  create(dto: ContractTypeCreateDto): Observable<ContractType> {
    return this.http.post<ContractType>(this.apiUrl, dto);
  }

  update(id: number, dto: ContractTypeUpdateDto): Observable<ContractType> {
    return this.http.put<ContractType>(`${this.apiUrl}/${id}`, dto);
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  private normalize(item: any): ContractType {
    if (!item) return item;
    return {
      id: (item.Id ?? item.id ?? 0) as number,
      contractTypeName: (item.ContractTypeName ?? item.contractTypeName ?? '') as string,
      companyId: (item.CompanyId ?? item.companyId ?? 0) as number,
      companyName: (item.CompanyName ?? item.companyName) as string | undefined,
      legalContractTypeId: (item.LegalContractTypeId ?? item.legalContractTypeId ?? null) as number | null | undefined,
      stateEmploymentProgramId: (item.StateEmploymentProgramId ?? item.stateEmploymentProgramId ?? null) as number | null | undefined,
      createdAt: (item.CreatedAt ?? item.createdAt ?? '') as string
    };
  }
}
