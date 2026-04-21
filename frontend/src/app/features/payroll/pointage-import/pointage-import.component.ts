import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '@app/core/services/auth.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { environment } from '@environments/environment';
import { TranslateModule } from '@ngx-translate/core';

type ImportStep = 'upload' | 'importing' | 'results';
type PeriodMode = 'monthly' | 'bi_monthly';

@Component({
  selector: 'app-pointage-import',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './pointage-import.component.html',
  styleUrl: './pointage-import.component.scss'
})
export class PointageImportComponent {
  private readonly http = inject(HttpClient);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly contextService = inject(CompanyContextService);

  // Étape du flux
  readonly step = signal<ImportStep>('upload');

  // Fichier sélectionné
  readonly selectedFile = signal<File | null>(null);
  readonly isDragging = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isImporting = signal(false);

  // Période
  readonly selectedMonth = signal<number>(new Date().getMonth() + 1);
  readonly selectedYear = signal<number>(new Date().getFullYear());
  readonly periodMode = signal<PeriodMode>('monthly');
  readonly selectedBiHalf = signal<number>(1); // 1 = première quinzaine, 2 = deuxième quinzaine

  readonly months = Array.from({ length: 12 }, (_, i) => i + 1);
  readonly years = Array.from({ length: 6 }, (_, i) => new Date().getFullYear() - 2 + i);
  readonly monthsNames = [
    'Janvier', 'Février', 'Mars', 'Avril', 'Mai', 'Juin',
    'Juillet', 'Août', 'Septembre', 'Octobre', 'Novembre', 'Décembre'
  ];

  // Résumé (pour l'instant purement frontend, à brancher sur l'API plus tard)
  readonly totalSheets = signal<number>(0);
  readonly totalLines = signal<number>(0);
  readonly successLines = signal<number>(0);
  readonly errorLines = signal<number>(0);

  readonly canImport = computed(
    () => !!this.selectedFile() && !!this.selectedMonth() && !!this.selectedYear()
  );

  readonly importErrors = signal<{ row: number; matricule?: string | null; message: string }[]>([]);

  // Drag & Drop
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(true);
  }

  onDragLeave(): void {
    this.isDragging.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging.set(false);
    const files = event.dataTransfer?.files;
    if (files?.length) {
      this.handleFileSelection(files[0]);
    }
  }

  onFileInputChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      this.handleFileSelection(input.files[0]);
    }
  }

  private handleFileSelection(file: File): void {
    this.errorMessage.set(null);
    // On accepte CSV et XLSX pour pointer vers le fichier Sage
    const lower = file.name.toLowerCase();
    if (!lower.endsWith('.csv') && !lower.endsWith('.xlsx')) {
      this.errorMessage.set('Seuls les fichiers CSV ou XLSX sont acceptés.');
      return;
    }
    this.selectedFile.set(file);
  }

  removeFile(): void {
    this.selectedFile.set(null);
    this.errorMessage.set(null);
  }

  startImport(): void {
    if (!this.selectedFile()) {
      return;
    }

    this.errorMessage.set(null);
    this.isImporting.set(true);
    this.step.set('importing');

    const file = this.selectedFile()!;
    const formData = new FormData();
    formData.append('file', file, file.name);

    const params = new URLSearchParams();
    params.set('month', String(this.selectedMonth()));
    params.set('year', String(this.selectedYear()));
    params.set('mode', this.periodMode());
    if (this.periodMode() === 'bi_monthly') {
      params.set('half', String(this.selectedBiHalf()));
    }
    const contextCompanyId = this.contextService.companyId();
    const companyId = contextCompanyId ? parseInt(String(contextCompanyId)) : undefined;
    if (companyId) {
      params.set('companyId', companyId.toString());
    }

    let url = `${environment.apiUrl}/import/timesheets`;
    const qs = params.toString();
    if (qs) {
      url += `?${qs}`;
    }

    this.http.post<TimesheetImportResult>(url, formData).subscribe({
      next: (res) => {
        this.totalSheets.set(res.totalSheets ?? 0);
        this.totalLines.set(res.totalLines ?? 0);
        this.successLines.set(res.successCount ?? 0);
        this.errorLines.set(res.errorCount ?? 0);
        this.importErrors.set(res.errors ?? []);

        this.isImporting.set(false);
        this.step.set('results');
      },
      error: (err) => {
        const msg =
          err?.error?.Message ||
          err?.error?.message ||
          'Erreur lors de l\'import du fichier de pointage.';
        this.errorMessage.set(msg);
        this.isImporting.set(false);
        this.step.set('upload');
      }
    });
  }

  /**
   * Décoder un token JWT pour inspecter ses claims
   */
  private decodeJwtToken(token: string): any {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) {
        throw new Error('Token JWT invalide');
      }
      const payload = parts[1];
      const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
      return JSON.parse(decoded);
    } catch (e) {
      console.error('Erreur décodage JWT:', e);
      return null;
    }
  }

  // Permettre un nouvel import sans quitter la page
  newImport(): void {
    this.step.set('upload');
    this.selectedFile.set(null);
    this.isImporting.set(false);
    this.errorMessage.set(null);
    this.totalSheets.set(0);
    this.totalLines.set(0);
    this.successLines.set(0);
    this.errorLines.set(0);
    this.importErrors.set([]);
  }

  // Naviguer vers la liste des pointages
  goToTimesheetList(): void {
    this.router.navigate(['/app/payroll/pointages']);
  }
}

interface TimesheetImportResult {
  month: number;
  year: number;
  periodMode: string;
  half?: number;
  totalSheets: number;
  totalLines: number;
  successCount: number;
  errorCount: number;
  errors: { row: number; matricule?: string | null; message: string }[];
}
