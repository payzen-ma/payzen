import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '@environments/environment';
import { Observable } from 'rxjs';

export interface PayrollTaxSnapshotDto {
  month: number;
  year: number;
  brutMois: number;
  sniMois: number;
  cnssMois: number;
  amoMois: number;
  irMois: number;
  netMois: number;
  tauxIrMois: number;
  cumulBrut: number;
  cumulSni: number;
  cumulCnss: number;
  cumulAmo: number;
  cumulIr: number;
  cumulNet: number;
  tauxEffectif: number;
}

@Injectable({
  providedIn: 'root'
})
export class PayrollTaxService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/payroll`;

  getYearSummary(
    employeeId: number,
    companyId: number,
    year: number
  ): Observable<PayrollTaxSnapshotDto[]> {
    const params = new HttpParams()
      .set('year', year)
      .set('companyId', companyId);

    return this.http.get<PayrollTaxSnapshotDto[]>(
      `${this.baseUrl}/tax-snapshots/${employeeId}/tax-summary`,
      { params }
    );
  }
}