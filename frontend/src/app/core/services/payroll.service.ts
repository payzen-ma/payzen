import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';
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
   * Normalise `half` reçu depuis des UI composants select (souvent 'null' en string).
   * Retourne `undefined` si on veut calculer un "mois entier" (donc ne pas envoyer `half` au backend).
   */
  private normalizeHalfParam(half: unknown): number | undefined {
    if (half === undefined || half === null) return undefined;
    if (typeof half === 'string') {
      const s = half.trim().toLowerCase();
      if (s === '' || s === 'null' || s === 'undefined') return undefined;
    }

    const n = typeof half === 'number' ? half : Number(half);
    if (Number.isNaN(n)) return undefined;
    return n;
  }

  /**
   * Calcule la paie pour tous les employés d'une période
   */
  calculatePayrollForAll(month: number, year: number, half?: number): Observable<any> {
    // Récupérer le companyId depuis le contexte
    const contextCompanyId = this.contextService.companyId();
    const companyId = contextCompanyId ? parseInt(contextCompanyId.toString()) : undefined;
    
    if (!companyId) {
      throw new Error('CompanyId est requis pour calculer la paie');
    }
    
    let params = new HttpParams()
      .set('companyId', companyId.toString())
      .set('month', month.toString())
      .set('year', year.toString());
    const normalizedHalf = this.normalizeHalfParam(half as unknown);
    if (normalizedHalf !== undefined) params = params.set('half', normalizedHalf.toString());

    return this.http.post(`${this.PAYROLL_URL}/calculate?useNativeEngine=true`, null, { params });
  }

  /**
   * Recalcule la paie pour un employé spécifique
   */
  calculatePayrollForEmployee(employeeId: number, month: number, year: number, half?: number): Observable<any> {
    let params = new HttpParams()
      .set('month', month.toString())
      .set('year', year.toString());
    const normalizedHalf = this.normalizeHalfParam(half as unknown);
    if (normalizedHalf !== undefined) params = params.set('half', normalizedHalf.toString());

    return this.http.post(`${this.PAYROLL_URL}/recalculate/${employeeId}?useNativeEngine=true`, null, { params });
  }

  /**
   * Récupère les résultats de paie avec filtres
   */
  getPayrollResults(filters: PayrollFilters): Observable<PayrollResultsResponse> {
    let params = new HttpParams();
    
    if (filters.month) params = params.set('month', filters.month.toString());
    if (filters.year) params = params.set('year', filters.year.toString());

    const normalizedHalf = this.normalizeHalfParam(filters.half as unknown);
    if (normalizedHalf !== undefined) params = params.set('half', normalizedHalf.toString());
    
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

  updatePayrollResultStatus(id: number, status: PayrollResultStatus): Observable<any> {
    const payload = { status };
    return this.http.patch(`${this.PAYROLL_URL}/results/${id}/status`, payload).pipe(
      catchError((error) => {
        if (error?.status === 404) {
          return this.http.patch(`${this.PAYROLL_URL}/${id}/status`, payload);
        }
        return throwError(() => error);
      })
    );
  }

  getCustomRules(companyId: number): Observable<Array<{
    id: number;
    title: string;
    description: string;
    dslSnippet: string;
    createdAt: string;
  }>> {
    const params = new HttpParams().set('companyId', companyId.toString());
    return this.http.get<Array<{
      id: number;
      title: string;
      description: string;
      dslSnippet: string;
      createdAt: string;
    }>>(`${this.PAYROLL_URL}/custom-rules`, { params });
  }

  previewCustomRule(payload: { title: string; description: string }): Observable<{ dslSnippet: string }> {
    return this.http.post<{ dslSnippet: string }>(`${this.PAYROLL_URL}/custom-rules/preview`, payload);
  }

  createCustomRule(companyId: number, payload: { title: string; description: string; dslSnippet: string }): Observable<{
    id: number;
    title: string;
    description: string;
    dslSnippet: string;
    createdAt: string;
  }> {
    const params = new HttpParams().set('companyId', companyId.toString());
    return this.http.post<{
      id: number;
      title: string;
      description: string;
      dslSnippet: string;
      createdAt: string;
    }>(`${this.PAYROLL_URL}/custom-rules`, payload, { params });
  }

  deleteCustomRule(id: number): Observable<any> {
    return this.http.delete(`${this.PAYROLL_URL}/custom-rules/${id}`);
  }

  /**
   * Télécharge la fiche de paie PDF pour un employé et une période.
   * GET: /api/payslip/employee/{employeeId}/period/{year}/{month}
   */
  downloadPayslip(
    employeeId: number,
    year: number,
    month: number,
    half?: number | null
  ): Observable<Blob> {
    let params = new HttpParams();
    const normalizedHalf = this.normalizeHalfParam(half as unknown);
    if (normalizedHalf !== undefined) params = params.set('half', normalizedHalf.toString());

    return this.http.get(
      `${environment.apiUrl}/payslip/employee/${employeeId}/period/${year}/${month}`,
      { responseType: 'blob', params }
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

  /**
   * Approuve (verrouille) tous les bulletins valides d'une période
   */
  approvePeriod(month: number, year: number, half?: number): Observable<any> {
    const contextCompanyId = this.contextService.companyId();
    const companyId = contextCompanyId ? parseInt(contextCompanyId.toString()) : undefined;
    
    if (!companyId) {
      throw new Error('CompanyId est requis pour approuver la paie');
    }
    
    let params = new HttpParams()
      .set('companyId', companyId.toString())
      .set('month', month.toString())
      .set('year', year.toString());
    const normalizedHalf = this.normalizeHalfParam(half as unknown);
    if (normalizedHalf !== undefined) params = params.set('half', normalizedHalf.toString());

    return this.http.post(`${this.PAYROLL_URL}/approve`, null, { params });
  }
}
