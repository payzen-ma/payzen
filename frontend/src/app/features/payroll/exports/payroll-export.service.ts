import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';

export interface CnssPreetabliIssue {
  lineNumber: number;
  severity: string;
  message: string;
}

export interface CnssPreetabliHeader {
  natureRecordType: string;
  transferIdentifier: string;
  category: string;
  reservedZoneA00: string;
  globalHeaderRecordType: string;
  affiliateNumber: string;
  period: string;
  companyName: string;
  activity: string;
  address: string;
  city: string;
  postalCode: string;
  agencyCode: string;
  emissionDateRaw: string;
  exigibilityDateRaw: string;
}

export interface CnssPreetabliEmployeeRow {
  lineNumber: number;
  recordType: string;
  affiliateNumber: string;
  period: string;
  insuredNumber: string;
  fullName: string;
  childrenCount: number;
  familyAllowanceToPayCentimes: number;
  familyAllowanceToDeductCentimes: number;
  familyAllowanceNetToPayCentimes: number;
  familyAllowanceToPay: number;
  familyAllowanceToDeduct: number;
  familyAllowanceNetToPay: number;
  reservedZone: string;
}

export interface CnssPreetabliSummary {
  recordType: string;
  affiliateNumber: string;
  period: string;
  employeeCount: number;
  totalChildren: number;
  totalFamilyAllowanceToPay: number;
  totalFamilyAllowanceToDeduct: number;
  totalFamilyAllowanceNetToPay: number;
  totalInsuredNumbers: number;
  reservedZone: string;
}

export interface CnssPreetabliParseResult {
  header: CnssPreetabliHeader | null;
  employees: CnssPreetabliEmployeeRow[];
  summary: CnssPreetabliSummary | null;
  issues: CnssPreetabliIssue[];
}

/**
 * Service Angular pour les exports de paie marocaine.
 * Retourne des Blobs binaires (Excel / CSV) prêts à télécharger.
 */
@Injectable({
  providedIn: 'root'
})
export class PayrollExportService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/payroll/exports`;
  private readonly cnssBaseUrl = `${environment.apiUrl}/cnss/preetabli`;

  /**
   * Télécharge le Journal de Paie (Excel XLSX)
   */
  downloadJournal(companyId: number, year: number, month: number): Observable<Blob> {
    return this.http.get(
      `${this.baseUrl}/journal/${companyId}/${year}/${month}`,
      { responseType: 'blob' }
    );
  }

  /**
   * Télécharge l'État CNSS (CSV Damancom UTF-8 BOM)
   */
  downloadCnss(companyId: number, year: number, month: number): Observable<Blob> {
    return this.http.get(
      `${this.baseUrl}/cnss/${companyId}/${year}/${month}`,
      { responseType: 'blob' }
    );
  }

  /**
   * Télécharge l'État CNSS au format PDF (bordereau de déclaration CNSS)
   */
  downloadCnssPdf(companyId: number, year: number, month: number): Observable<Blob> {
    return this.http.get(
      `${this.baseUrl}/cnss-pdf/${companyId}/${year}/${month}`,
      { responseType: 'blob' }
    );
  }

  /**
   * Télécharge l'État IR (Excel XLSX)
   */
  downloadIr(companyId: number, year: number, month: number): Observable<Blob> {
    return this.http.get(
      `${this.baseUrl}/ir/${companyId}/${year}/${month}`,
      { responseType: 'blob' }
    );
  }

  /**
   * Télécharge l'État IR au format PDF
   */
  downloadIrPdf(companyId: number, year: number, month: number): Observable<Blob> {
    return this.http.get(
      `${this.baseUrl}/ir-pdf/${companyId}/${year}/${month}`,
      { responseType: 'blob' }
    );
  }

  parseCnssPreetabli(companyId: number, file: File): Observable<CnssPreetabliParseResult> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post<CnssPreetabliParseResult>(
      `${this.cnssBaseUrl}/parse?companyId=${encodeURIComponent(String(companyId))}`,
      formData
    );
  }

  getLatestCnssPreetabli(companyId: number, period?: string): Observable<CnssPreetabliParseResult> {
    const periodQuery = period ? `&period=${encodeURIComponent(period)}` : '';
    return this.http.get<CnssPreetabliParseResult>(
      `${this.cnssBaseUrl}/latest?companyId=${encodeURIComponent(String(companyId))}${periodQuery}`
    );
  }

  generateCnssBds(companyId: number, file: File): Observable<Blob> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    return this.http.post(
      `${this.cnssBaseUrl}/generate-bds?companyId=${encodeURIComponent(String(companyId))}`,
      formData,
      { responseType: 'blob' }
    );
  }

  /** Helper utilitaire : déclenche le téléchargement d'un Blob dans le navigateur */
  static triggerDownload(blob: Blob, filename: string): void {
    const url = window.URL.createObjectURL(blob);
    const a   = document.createElement('a');
    a.href     = url;
    a.download = filename;
    a.click();
    window.URL.revokeObjectURL(url);
  }
}
