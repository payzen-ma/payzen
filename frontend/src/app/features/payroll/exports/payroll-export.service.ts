import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@environments/environment';

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
   * Télécharge l'État IR (Excel XLSX)
   */
  downloadIr(companyId: number, year: number, month: number): Observable<Blob> {
    return this.http.get(
      `${this.baseUrl}/ir/${companyId}/${year}/${month}`,
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
