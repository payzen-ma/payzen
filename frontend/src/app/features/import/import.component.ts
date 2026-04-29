import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { Component, ElementRef, OnInit, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { ImportService } from '@app/core/services/import.service';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';

interface ImportedAbsenceRow {
  row: number;
  matricule: string;
  employeeName: string;
  absenceDate: string;
  durationType: string;
  reason?: string;
  status: string;
}

interface ImportErrorRow {
  row: number;
  matricule?: string;
  message: string;
}

interface ImportResult {
  totalSheets: number;
  processedSheets: number;
  failedSheets: number;
  skippedSheets: number;
  sheets: ImportSheetRow[];
  importedAbsences: ImportedAbsenceRow[];
  errors: ImportErrorRow[];
  employeeChecks: EmployeeCheckRow[];
  autoCreatedEmployees: AutoCreatedEmployeeRow[];
}

interface ImportSheetRow {
  sheetName: string;
  sheetType: string;
  success: boolean;
  message: string;
  totalRows?: number;
  successCount?: number;
  errorCount?: number;
  createdDepartmentsCount?: number;
  createdJobPositionsCount?: number;
  addedEmployees?: AddedEmployeeRow[];
  errors?: ImportErrorRow[];
}

interface AddedEmployeeRow {
  row: number;
  firstName: string;
  lastName: string;
}

interface EmployeeCheckRow {
  row: number;
  matricule?: string;
  employeeName?: string;
  exists: boolean;
  isLastNameMatch: boolean;
  isFirstNameMatch: boolean;
  message: string;
}

interface AutoCreatedEmployeeRow {
  matricule: string;
  fullName: string;
  email: string;
}

@Component({
  selector: 'app-import',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    ToastModule,
    HttpClientModule
  ],
  providers: [MessageService],
  template: `
    <div class="p-6 w-full max-w-7xl mx-auto">
      <h1 class="text-3xl font-bold mb-6">modifications accumulées</h1>

      <div class="upload-card surface-card border-round shadow-1">
        <div
          class="upload-dropzone"
          [class.dragover]="dragOver"
          (dragover)="onDragOver($event)"
          (dragleave)="onDragLeave($event)"
          (drop)="onDrop($event)"
        >
          <div class="upload-icon">
            <i class="pi pi-table"></i>
          </div>

          <div class="upload-title">Glisser deposer votre fichier de modifications accumulées</div>
          <div class="upload-subtitle">
            ou <a class="browse-text" (click)="browseFile()">cliquez pour parcourir</a>
          </div>
          <div class="upload-format">Format accepté : XLSX</div>
          <input #fileInput type="file" accept=".xlsx" (change)="onFileChange($event)" hidden />
        </div>

        <div class="selected-file" *ngIf="selectedFile">
          Fichier sélectionné : {{ selectedFile.name }}
        </div>

        <div style="margin-top: 1rem;">
          <label class="flex items-center gap-2 text-sm">
            <input type="checkbox" [(ngModel)]="sendWelcomeEmail" />
            Envoyer automatiquement les emails de bienvenue et créer les comptes utilisateurs pour les nouveaux employés
          </label>
        </div>

        <div class="button-row">
          <button pButton type="button" icon="pi pi-download" [label]="isDownloadingTemplate ? 'Téléchargement...' : 'Télécharger template'" class="btn-template" [disabled]="isDownloadingTemplate" (click)="downloadTemplate()"></button>
          <button pButton type="button" icon="pi pi-times" label="Annuler" class="btn-cancel p-button-outlined" (click)="onCancel()"></button>
          <button pButton type="button" icon="pi pi-upload" [label]="isImporting ? 'Import en cours...' : 'Importer'" class="btn-import" [disabled]="!selectedFile || isImporting" (click)="uploadSelectedFile()"></button>
        </div>

        <section class="import-result" *ngIf="importResult">
          <h3 class="result-title">Résultat de l'import</h3>

          <div class="summary-grid">
            <article class="summary-card">
              <span class="summary-label">Employés ajoutés</span>
              <strong class="summary-value">{{ newEmployeesSheet?.successCount ?? 0 }}</strong>
            </article>
            <article class="summary-card">
              <span class="summary-label">Erreurs</span>
              <strong class="summary-value">{{ newEmployeesSheet?.errorCount ?? 0 }}</strong>
            </article>
            <article class="summary-card">
              <span class="summary-label">Départements créés</span>
              <strong class="summary-value">{{ newEmployeesSheet?.createdDepartmentsCount ?? 0 }}</strong>
            </article>
            <article class="summary-card">
              <span class="summary-label">Postes créés</span>
              <strong class="summary-value">{{ newEmployeesSheet?.createdJobPositionsCount ?? 0 }}</strong>
            </article>
          </div>

          <div class="sheet-tabs" *ngIf="visibleSheets.length">
            <button
              type="button"
              *ngFor="let sheet of visibleSheets"
              class="sheet-tab-btn"
              [class.active]="selectedSheetName === sheet.sheetName"
              (click)="selectSheet(sheet.sheetName)"
            >
              {{ sheet.sheetName }}
            </button>
          </div>

          <div class="sheets-stack" *ngIf="selectedSheet as sheet">
            <article class="sheet-card">
              <div class="sheet-header">
                <div>
                  <h4>{{ sheet.sheetName }}</h4>
                  <p>{{ sheet.message }}</p>
                </div>
                <span class="badge" [class.badge-success]="sheet.success" [class.badge-error]="!sheet.success">
                  {{ sheet.success ? 'Succès' : 'Échec' }}
                </span>
              </div>

              <div class="sheet-metrics">
                <div class="metric-item">
                  <span>Type</span>
                  <strong>{{ sheet.sheetType }}</strong>
                </div>
                <div class="metric-item">
                  <span>Ajoutés</span>
                  <strong>{{ sheet.successCount ?? 0 }}</strong>
                </div>
                <div class="metric-item">
                  <span>Erreurs</span>
                  <strong>{{ sheet.errorCount ?? 0 }}</strong>
                </div>
                <div class="metric-item" *ngIf="sheet.sheetType === 'nouveaux_employes'">
                  <span>Départements créés</span>
                  <strong>{{ sheet.createdDepartmentsCount ?? 0 }}</strong>
                </div>
                <div class="metric-item" *ngIf="sheet.sheetType === 'nouveaux_employes'">
                  <span>Postes créés</span>
                  <strong>{{ sheet.createdJobPositionsCount ?? 0 }}</strong>
                </div>
              </div>

              <div class="employees-table-wrap" *ngIf="sheet.addedEmployees?.length">
                <table class="employees-table">
                  <thead>
                    <tr>
                      <th>SALARIE</th>
                      <th>LIGNE</th>
                      <th>STATUT</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let emp of sheet.addedEmployees">
                      <td>{{ emp.firstName }} {{ emp.lastName }}</td>
                      <td>{{ emp.row }}</td>
                      <td><span class="status-chip status-ok">Ajouté</span></td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <div class="detail-block error-block" *ngIf="sheet.errors?.length">
                <h5>Détails des erreurs</h5>
                <ul>
                  <li *ngFor="let e of sheet.errors">
                    Ligne {{ e.row }} — {{ e.message }}
                  </li>
                </ul>
              </div>
            </article>
          </div>
        </section>
      </div>

      <p-toast></p-toast>
    </div>
  `,
  styles: [
    `
      :host ::ng-deep {
        .surface-card {
          padding: 1.75rem;
        }
      }

      .upload-card {
        background: #ffffff;
        border: 1px solid rgba(148, 163, 184, 0.2);
        border-radius: 24px;
      }

      .upload-dropzone {
        min-height: 260px;
        border: 2px dashed #cbd5e1;
        border-radius: 20px;
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
        gap: 0.5rem;
        text-align: center;
        padding: 2.25rem;
        background: #f8fafc;
        transition: background 0.2s ease, border-color 0.2s ease;
        cursor: pointer;
      }

      .upload-dropzone.dragover {
        background: #eff6ff;
        border-color: #60a5fa;
      }

      .upload-icon {
        width: 72px;
        height: 72px;
        border-radius: 9999px;
        background: #dbeafe;
        display: flex;
        align-items: center;
        justify-content: center;
      }

      .upload-icon .pi {
        font-size: 1.875rem;
        color: #1e3a8a;
      }

      .upload-title {
        font-size: 1.125rem;
        font-weight: 700;
        color: #0f172a;
      }

      .upload-subtitle {
        color: #2563eb;
        font-size: 0.95rem;
      }

      .browse-text {
        color: #2563eb;
        text-decoration: underline;
        cursor: pointer;
      }

      .upload-format {
        font-size: 0.95rem;
        color: #64748b;
      }

      .selected-file {
        margin-top: 1rem;
        font-size: 0.95rem;
        color: #334155;
      }

      .button-row {
        display: flex;
        justify-content: flex-end;
        gap: 0.75rem;
        margin-top: 1.5rem;
      }

      .btn-template,
      .btn-cancel,
      .btn-import {
        border-radius: 10px;
        font-weight: 600;
      }

      .btn-template {
        background: #eef2ff;
        color: #1e3a8a;
        border: 1px solid #c7d2fe;
      }

      .btn-import {
        background: #1d4ed8;
        border: 1px solid #1d4ed8;
        color: #ffffff;
      }

      .import-result {
        margin-top: 1.75rem;
        background: #f8fafc;
        border: 1px solid #e2e8f0;
        border-radius: 16px;
        padding: 1rem;
      }

      .result-title {
        font-size: 1.15rem;
        font-weight: 700;
        color: #0f172a;
        margin-bottom: 0.9rem;
      }

      .summary-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(170px, 1fr));
        gap: 0.75rem;
        margin-bottom: 1rem;
      }

      .summary-card {
        background: #ffffff;
        border: 1px solid #e2e8f0;
        border-radius: 12px;
        padding: 0.75rem 0.9rem;
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
      }

      .summary-label {
        color: #64748b;
        font-size: 0.82rem;
      }

      .summary-value {
        color: #0f172a;
        font-size: 1.25rem;
      }

      .sheets-stack {
        display: flex;
        flex-direction: column;
        gap: 0.85rem;
      }

      .sheet-tabs {
        display: flex;
        flex-wrap: wrap;
        gap: 0.5rem;
        margin-bottom: 0.9rem;
      }

      .sheet-tab-btn {
        background: #ffffff;
        color: #334155;
        border: 1px solid #cbd5e1;
        border-radius: 10px;
        padding: 0.45rem 0.75rem;
        font-size: 0.82rem;
        font-weight: 600;
        cursor: pointer;
      }

      .sheet-tab-btn.active {
        background: #dbeafe;
        color: #1e3a8a;
        border-color: #93c5fd;
      }

      .sheet-card {
        background: #ffffff;
        border: 1px solid #e2e8f0;
        border-radius: 14px;
        padding: 0.9rem;
      }

      .sheet-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        gap: 0.6rem;
      }

      .sheet-header h4 {
        margin: 0;
        font-size: 1rem;
        color: #0f172a;
      }

      .sheet-header p {
        margin: 0.2rem 0 0;
        font-size: 0.87rem;
        color: #64748b;
      }

      .badge {
        border-radius: 999px;
        font-size: 0.75rem;
        padding: 0.25rem 0.6rem;
        font-weight: 700;
      }

      .badge-success {
        background: #dcfce7;
        color: #166534;
      }

      .badge-error {
        background: #fee2e2;
        color: #991b1b;
      }

      .sheet-metrics {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(130px, 1fr));
        gap: 0.6rem;
        margin-top: 0.8rem;
      }

      .metric-item {
        background: #f8fafc;
        border: 1px solid #e2e8f0;
        border-radius: 10px;
        padding: 0.5rem 0.65rem;
      }

      .metric-item span {
        display: block;
        color: #64748b;
        font-size: 0.78rem;
      }

      .metric-item strong {
        color: #0f172a;
        font-size: 0.96rem;
      }

      .detail-block {
        margin-top: 0.8rem;
        background: #eff6ff;
        border: 1px solid #bfdbfe;
        border-radius: 10px;
        padding: 0.6rem 0.75rem;
      }

      .detail-block h5 {
        margin: 0 0 0.35rem 0;
        font-size: 0.88rem;
        color: #1e3a8a;
      }

      .detail-block ul {
        margin: 0;
        padding-left: 1rem;
      }

      .detail-block li {
        margin-bottom: 0.2rem;
        color: #1f2937;
      }

      .error-block {
        background: #fff7ed;
        border-color: #fdba74;
      }

      .employees-table-wrap {
        margin-top: 0.85rem;
        border: 1px solid #e2e8f0;
        border-radius: 12px;
        overflow: hidden;
      }

      .employees-table {
        width: 100%;
        border-collapse: collapse;
        background: #ffffff;
      }

      .employees-table th {
        text-align: left;
        font-size: 0.76rem;
        letter-spacing: 0.02em;
        color: #64748b;
        padding: 0.7rem 0.85rem;
        border-bottom: 1px solid #e2e8f0;
        background: #f8fafc;
      }

      .employees-table td {
        padding: 0.8rem 0.85rem;
        border-bottom: 1px solid #f1f5f9;
        color: #0f172a;
      }

      .status-chip {
        display: inline-block;
        padding: 0.2rem 0.55rem;
        border-radius: 999px;
        font-size: 0.72rem;
        font-weight: 700;
      }

      .status-ok {
        background: #dcfce7;
        color: #166534;
      }
    `
  ]
})
export class ImportComponent implements OnInit {
  @ViewChild('fileInput') fileInput?: ElementRef<HTMLInputElement>;
  private importService = inject(ImportService);
  private messageService = inject(MessageService);
  private readonly contextService = inject(CompanyContextService);

  selectedFile?: File;
  dragOver = false;
  importResult: ImportResult | null = null;
  sendWelcomeEmail = false;
  isImporting = false;
  isDownloadingTemplate = false;
  selectedSheetName: string | null = null;

  get visibleSheets(): ImportSheetRow[] {
    const sheets = this.importResult?.sheets ?? [];
    return sheets.filter(s => {
      const name = (s.sheetName || '').trim().toLowerCase();
      return name !== 'societe' && !name.startsWith('_');
    });
  }

  get newEmployeesSheet(): ImportSheetRow | undefined {
    return this.visibleSheets.find(s => s.sheetType === 'nouveaux_employes');
  }

  get selectedSheet(): ImportSheetRow | undefined {
    if (!this.visibleSheets.length) {
      return undefined;
    }
    if (!this.selectedSheetName) {
      return this.visibleSheets[0];
    }
    return this.visibleSheets.find(s => s.sheetName === this.selectedSheetName) ?? this.visibleSheets[0];
  }

  selectSheet(sheetName: string) {
    this.selectedSheetName = sheetName;
  }

  ngOnInit() {
    // Component initialization
  }

  browseFile() {
    this.fileInput?.nativeElement.click();
  }

  onFileChange(event: Event) {
    const target = event.target as HTMLInputElement;
    const file = target.files?.[0];
    if (file) {
      this.setSelectedFile(file);
    }
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
    this.dragOver = true;
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    this.dragOver = false;
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    this.dragOver = false;
    const file = event.dataTransfer?.files?.[0];
    if (file) {
      this.setSelectedFile(file);
    }
  }

  setSelectedFile(file: File) {
    if (!this.validateFile(file)) {
      return;
    }

    this.selectedFile = file;
  }

  validateFile(file: File): boolean {
    const valid = /\.xlsx$/i.test(file.name);
    if (!valid) {
      this.messageService.add({
        severity: 'error',
        summary: 'Erreur',
        detail: 'Veuillez sélectionner un fichier Excel (.xlsx)',
        life: 3000
      });
    }
    return valid;
  }

  uploadSelectedFile() {
    console.log('[Import] Click sur Importer');
    if (!this.selectedFile || this.isImporting) {
      console.warn('[Import] Aucun fichier sélectionné, arrêt.');
      return;
    }
    this.isImporting = true;

    const now = new Date();
    const contextCompanyId = this.contextService.companyId();
    const companyId = contextCompanyId ? parseInt(String(contextCompanyId), 10) : undefined;
    console.log('[Import] Contexte companyId:', contextCompanyId, '->', companyId);
    console.log('[Import] Fichier sélectionné:', {
      name: this.selectedFile.name,
      size: this.selectedFile.size,
      type: this.selectedFile.type
    });
    console.log('[Import] Paramètres envoyés:', {
      month: now.getMonth() + 1,
      year: now.getFullYear(),
      mode: 'monthly',
      companyId,
      sendWelcomeEmail: this.sendWelcomeEmail
    });

    this.importService.uploadModuleFile(this.selectedFile, {
      month: now.getMonth() + 1,
      year: now.getFullYear(),
      mode: 'monthly',
      companyId,
      sendWelcomeEmail: this.sendWelcomeEmail
    }).subscribe(
      (response: any) => {
        console.log('[Import] Réponse brute API:', response);
        const result = response?.data ?? response;
        console.log('[Import] Résultat normalisé:', result);
        if (result) {
          const sheets = (result.sheets ?? []).map((s: any) => ({
            ...s,
            addedEmployees: (s.addedEmployees ?? []).map((a: any) => ({
              row: a.row,
              firstName: a.firstName,
              lastName: a.lastName
            })),
            errors: (s.errors ?? []).map((e: any) => ({
              row: e.row,
              message: e.message
            }))
          }));
          this.importResult = {
            totalSheets: result.totalSheets ?? 0,
            processedSheets: result.processedSheets ?? 0,
            failedSheets: result.failedSheets ?? 0,
            skippedSheets: result.skippedSheets ?? 0,
            sheets,
            importedAbsences: result.importedAbsences ?? [],
            errors: result.errors ?? [],
            employeeChecks: result.employeeChecks ?? [],
            autoCreatedEmployees: result.autoCreatedEmployees ?? []
          };
          this.selectedSheetName = this.visibleSheets[0]?.sheetName ?? null;
        }

        const totalAdded = this.visibleSheets.reduce((sum, s) => sum + (s.successCount ?? 0), 0);
        const totalErrors = this.visibleSheets.reduce((sum, s) => sum + (s.errorCount ?? 0), 0);
        const successDetail = `Import terminé. Employés ajoutés: ${totalAdded}. Erreurs: ${totalErrors}.`;
        this.messageService.add({
          severity: totalErrors > 0 ? 'warn' : 'success',
          summary: totalErrors > 0 ? 'Import terminé avec erreurs' : 'Import réussi',
          detail: successDetail,
          life: 5000
        });

        const createdEmployeesCount = this.importResult?.autoCreatedEmployees?.length ?? 0;
        if (createdEmployeesCount > 0) {
          this.messageService.add({
            severity: 'info',
            summary: 'Nouveaux employés ajoutés',
            detail: `${createdEmployeesCount} employé(s) ont été créés automatiquement.`,
            life: 5000
          });
        }
        this.onCancel();
        this.isImporting = false;
      },
      (error: any) => {
        console.error('[Import] Erreur upload:', error);
        console.error('[Import] Détail erreur API:', error?.error);
        const detail =
          error?.error?.message ||
          error?.error?.Message ||
          error?.error?.errors?.join(', ') ||
          error?.message ||
          error?.statusText ||
          'Une erreur s\'est produite lors de l\'importation';
        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail,
          life: 5000
        });
        this.isImporting = false;
      }
    );
  }

  downloadTemplate() {
    if (this.isDownloadingTemplate) {
      return;
    }
    this.isDownloadingTemplate = true;
    const contextCompanyId = this.contextService.companyId();
    const companyId = contextCompanyId ? parseInt(String(contextCompanyId), 10) : undefined;

    this.importService.downloadModuleTemplate(companyId).subscribe({
      next: (response) => {
        // Récupérer le nom du fichier depuis le header Content-Disposition
        const contentDisposition = response.headers.get('content-disposition') ?? '';
        console.log('Content-Disposition:', contentDisposition); // Debug

        let decodedFileName: string | undefined;

        if (contentDisposition) {
          // Pattern pour RFC 5987 (filename*=UTF-8''...)
          let fileNameMatch = /filename\*=UTF-8''([^\s;]+)/i.exec(contentDisposition);
          if (fileNameMatch?.[1]) {
            decodedFileName = decodeURIComponent(fileNameMatch[1]);
          } else {
            // Pattern pour filename standard
            fileNameMatch = /filename="?([^";\n]+)"?/i.exec(contentDisposition);
            if (fileNameMatch?.[1]) {
              decodedFileName = fileNameMatch[1].trim();
            }
          }
        }

        const blob = response.body ?? new Blob();
        const objectUrl = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = objectUrl;

        if (decodedFileName) {
          link.download = decodedFileName;
        }

        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        URL.revokeObjectURL(objectUrl);

        this.messageService.add({
          severity: 'success',
          summary: 'Template téléchargé',
          detail: 'Le fichier template a été généré et téléchargé.',
          life: 3000
        });
        this.isDownloadingTemplate = false;
      },
      error: (error: any) => {
        const detail =
          error?.error?.message ||
          error?.error?.Message ||
          error?.message ||
          'Impossible de télécharger le template';

        this.messageService.add({
          severity: 'error',
          summary: 'Erreur',
          detail,
          life: 5000
        });
        this.isDownloadingTemplate = false;
      }
    });
  }

  onCancel() {
    this.selectedFile = undefined;
    this.selectedSheetName = null;
    if (this.fileInput) {
      this.fileInput.nativeElement.value = '';
    }
  }
}
