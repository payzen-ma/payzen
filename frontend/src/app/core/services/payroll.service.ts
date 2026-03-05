import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';
import { 
  PayrollResult, 
  PayrollResultsResponse, 
  PayrollStats, 
  PayrollDetail,
  CalculatePayrollRequest,
  PayrollFilters,
  PayrollResultStatus
} from '@app/core/models/payroll.model';
import { CompanyContextService } from './companyContext.service';

@Injectable({
  providedIn: 'root'
})
export class PayrollService {
  private readonly PAYROLL_URL = `${environment.apiUrl}/payroll`;
  private readonly contextService = inject(CompanyContextService);

  constructor(private http: HttpClient) {}

  /**
   * Calcule la paie pour tous les employés d'une période
   */
  calculatePayrollForAll(month: number, year: number): Observable<any> {
    // Récupérer le companyId depuis le contexte
    const contextCompanyId = this.contextService.companyId();
    const companyId = contextCompanyId ? parseInt(contextCompanyId.toString()) : undefined;
    
    if (!companyId) {
      throw new Error('CompanyId est requis pour calculer la paie');
    }
    
    const params = new HttpParams()
      .set('companyId', companyId.toString())
      .set('month', month.toString())
      .set('year', year.toString());
    
    return this.http.post(`${this.PAYROLL_URL}/calculate?useNativeEngine=true`, null, { params });
  }

  /**
   * Recalcule la paie pour un employé spécifique
   */
  calculatePayrollForEmployee(employeeId: number, month: number, year: number): Observable<any> {
    const params = new HttpParams()
      .set('month', month.toString())
      .set('year', year.toString());
    
    return this.http.post(`${this.PAYROLL_URL}/recalculate/${employeeId}?useNativeEngine=true`, null, { params });
  }

  /**
   * Récupère les résultats de paie avec filtres
   */
  getPayrollResults(filters: PayrollFilters): Observable<PayrollResultsResponse> {
    let params = new HttpParams();
    
    if (filters.month) params = params.set('month', filters.month.toString());
    if (filters.year) params = params.set('year', filters.year.toString());
    
    // Convert companyId to number if it's from context (string)
    const contextCompanyId = this.contextService.companyId();
    const companyId = filters.companyId ?? (contextCompanyId ? parseInt(contextCompanyId.toString()) : undefined);
    if (companyId) params = params.set('companyId', companyId.toString());
    
    if (filters.status) params = params.set('status', filters.status);
    
    return this.http.get<PayrollResultsResponse>(`${this.PAYROLL_URL}/results`, { params });
  }

  /**
   * Récupère le détail complet d'une fiche de paie
   */
  getPayrollDetail(id: number): Observable<PayrollDetail> {
    return this.http.get<PayrollDetail>(`${this.PAYROLL_URL}/results/${id}`);
  }

  /**
   * Récupère les statistiques de paie pour une période
   */
  getPayrollStats(month: number, year: number, companyId?: number): Observable<PayrollStats> {
    let params = new HttpParams()
      .set('month', month.toString())
      .set('year', year.toString());
    
    const contextCompanyId = this.contextService.companyId();
    const effectiveCompanyId = companyId ?? (contextCompanyId ? parseInt(contextCompanyId.toString()) : undefined);
    if (effectiveCompanyId) {
      params = params.set('companyId', effectiveCompanyId.toString());
    }
    
    return this.http.get<PayrollStats>(`${this.PAYROLL_URL}/stats`, { params });
  }

  /**
   * Supprime un résultat de paie
   */
  deletePayrollResult(id: number): Observable<any> {
    return this.http.delete(`${this.PAYROLL_URL}/results/${id}`);
  }

  /**
   * Télécharge la fiche de paie PDF pour un employé et une période.
   * GET: /api/payslip/employee/{employeeId}/period/{year}/{month}
   */
  downloadPayslip(employeeId: number, year: number, month: number): Observable<Blob> {
    return this.http.get(
      `${environment.apiUrl}/payslip/employee/${employeeId}/period/${year}/${month}`,
      { responseType: 'blob' }
    );
  }

  /**
   * Récupère les mois disponibles (helper)
   */
  getMonths(): { value: number; label: string }[] {
    return [
      { value: 1, label: 'Janvier' },
      { value: 2, label: 'Février' },
      { value: 3, label: 'Mars' },
      { value: 4, label: 'Avril' },
      { value: 5, label: 'Mai' },
      { value: 6, label: 'Juin' },
      { value: 7, label: 'Juillet' },
      { value: 8, label: 'Août' },
      { value: 9, label: 'Septembre' },
      { value: 10, label: 'Octobre' },
      { value: 11, label: 'Novembre' },
      { value: 12, label: 'Décembre' }
    ];
  }

  /**
   * Récupère les années disponibles (helper)
   */
  getYears(): number[] {
    const currentYear = new Date().getFullYear();
    const years: number[] = [];
    for (let i = currentYear - 2; i <= currentYear + 1; i++) {
      years.push(i);
    }
    return years;
  }
}
