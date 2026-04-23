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
    <div class="p-6 max-w-3xl mx-auto">
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
          <button pButton type="button" label="Télécharger template" class="p-button-secondary" (click)="downloadTemplate()"></button>
          <button pButton type="button" label="Annuler" class="p-button-outlined" (click)="onCancel()"></button>
          <button pButton type="button" label="Importer" class="p-button-primary" [disabled]="!selectedFile" (click)="uploadSelectedFile()"></button>
        </div>

        <div class="import-result" *ngIf="importResult" style="margin-top:1.5rem;">
          <div class="p-3 border-round" style="background:#f1f5f9;">
            <div class="text-lg font-semibold mb-2">Résultat de l'import</div>
            <div class="grid grid-cols-1 md:grid-cols-3 gap-3 mb-4">
              <div class="p-3 border-round" style="background:#ffffff;">Total feuilles : {{ importResult.totalSheets }}</div>
              <div class="p-3 border-round" style="background:#ffffff;">Traitées : {{ importResult.processedSheets }}</div>
              <div class="p-3 border-round" style="background:#ffffff;">Échouées : {{ importResult.failedSheets }}</div>
            </div>

            <div *ngIf="importResult.sheets?.length" class="mb-4">
              <div class="font-semibold mb-2">
                Feuilles lues : {{ importResult.sheets.length }}
              </div>
              <div class="overflow-x-auto">
                <table class="w-full border-collapse text-sm" style="min-width:400px;">
                  <thead>
                    <tr class="text-left" style="border-bottom:1px solid #cbd5e1;">
                      <th class="py-2">Feuille</th>
                      <th class="py-2">Type</th>
                      <th class="py-2">Statut</th>
                      <th class="py-2">Message</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let sheet of importResult.sheets" style="border-bottom:1px solid #e2e8f0;">
                      <td class="py-2">{{ sheet.sheetName }}</td>
                      <td class="py-2">{{ sheet.sheetType }}</td>
                      <td class="py-2">{{ sheet.success ? 'Succès' : 'Échec' }}</td>
                      <td class="py-2">{{ sheet.message }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

            <div *ngIf="importResult.importedAbsences?.length" class="mb-4">
              <div class="font-semibold mb-2">Employés concernés</div>
              <div class="overflow-x-auto">
                <table class="w-full border-collapse text-sm" style="min-width:600px;">
                  <thead>
                    <tr class="text-left" style="border-bottom:1px solid #cbd5e1;">
                      <th class="py-2">Ligne</th>
                      <th class="py-2">Matricule</th>
                      <th class="py-2">Employé</th>
                      <th class="py-2">Date</th>
                      <th class="py-2">Durée</th>
                      <th class="py-2">Motif</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let row of importResult.importedAbsences" style="border-bottom:1px solid #e2e8f0;">
                      <td class="py-2">{{ row.row }}</td>
                      <td class="py-2">{{ row.matricule }}</td>
                      <td class="py-2">{{ row.employeeName }}</td>
                      <td class="py-2">{{ row.absenceDate }}</td>
                      <td class="py-2">{{ row.durationType }}</td>
                      <td class="py-2">{{ row.reason || '-' }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

            <div *ngIf="importResult.employeeChecks?.length" class="mb-4">
              <div class="font-semibold mb-2">Employés reconnus</div>
              <div class="overflow-x-auto">
                <table class="w-full border-collapse text-sm" style="min-width:700px;">
                  <thead>
                    <tr class="text-left" style="border-bottom:1px solid #cbd5e1;">
                      <th class="py-2">Ligne</th>
                      <th class="py-2">Matricule</th>
                      <th class="py-2">Employé (base)</th>
                      <th class="py-2">Existe</th>
                      <th class="py-2">Nom OK</th>
                      <th class="py-2">Prénom OK</th>
                      <th class="py-2">Message</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let check of importResult.employeeChecks" style="border-bottom:1px solid #e2e8f0;">
                      <td class="py-2">{{ check.row }}</td>
                      <td class="py-2">{{ check.matricule || '-' }}</td>
                      <td class="py-2">{{ check.employeeName || '-' }}</td>
                      <td class="py-2">{{ check.exists ? 'Oui' : 'Non' }}</td>
                      <td class="py-2">{{ check.isLastNameMatch ? 'Oui' : 'Non' }}</td>
                      <td class="py-2">{{ check.isFirstNameMatch ? 'Oui' : 'Non' }}</td>
                      <td class="py-2">{{ check.message }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

            <div *ngIf="importResult.autoCreatedEmployees?.length" class="mb-4">
              <div class="font-semibold mb-2">Employés créés automatiquement</div>
              <div class="overflow-x-auto">
                <table class="w-full border-collapse text-sm" style="min-width:500px;">
                  <thead>
                    <tr class="text-left" style="border-bottom:1px solid #cbd5e1;">
                      <th class="py-2">Matricule</th>
                      <th class="py-2">Nom complet</th>
                      <th class="py-2">Email</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr *ngFor="let emp of importResult.autoCreatedEmployees" style="border-bottom:1px solid #e2e8f0;">
                      <td class="py-2">{{ emp.matricule }}</td>
                      <td class="py-2">{{ emp.fullName }}</td>
                      <td class="py-2">{{ emp.email }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

            <div *ngIf="importResult.errors?.length">
              <div class="font-semibold mb-2">Erreurs détectées</div>
              <ul class="pl-5 list-disc">
                <li *ngFor="let error of importResult.errors">
                  Ligne {{ error.row }} - Matricule : {{ error.matricule || 'N/A' }} — {{ error.message }}
                </li>
              </ul>
            </div>
          </div>
        </div>
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
    if (!this.selectedFile) {
      console.warn('[Import] Aucun fichier sélectionné, arrêt.');
      return;
    }

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
          this.importResult = {
            totalSheets: result.totalSheets ?? 0,
            processedSheets: result.processedSheets ?? 0,
            failedSheets: result.failedSheets ?? 0,
            skippedSheets: result.skippedSheets ?? 0,
            sheets: result.sheets ?? [],
            importedAbsences: result.importedAbsences ?? [],
            errors: result.errors ?? [],
            employeeChecks: result.employeeChecks ?? [],
            autoCreatedEmployees: result.autoCreatedEmployees ?? []
          };
        }

        this.messageService.add({
          severity: 'success',
          summary: 'Succès',
          detail: 'Le fichier a été importé avec succès',
          life: 3000
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
      }
    );
  }

  downloadTemplate() {
    const contextCompanyId = this.contextService.companyId();
    const companyId = contextCompanyId ? parseInt(String(contextCompanyId), 10) : undefined;

    this.importService.downloadModuleTemplate(companyId).subscribe({
      next: (response) => {
        const contentDisposition = response.headers.get('content-disposition') ?? '';
        const fileNameMatch = /filename\*?=(?:UTF-8''|")?([^\";]+)/i.exec(contentDisposition);
        const decodedFileName = fileNameMatch?.[1]
          ? decodeURIComponent(fileNameMatch[1].replace(/"/g, '').trim())
          : 'template_import_nouveaux_employes.xlsx';

        const blob = response.body ?? new Blob();
        const objectUrl = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = objectUrl;
        link.download = decodedFileName;
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
      }
    });
  }

  onCancel() {
    this.selectedFile = undefined;
    if (this.fileInput) {
      this.fileInput.nativeElement.value = '';
    }
  }
}
