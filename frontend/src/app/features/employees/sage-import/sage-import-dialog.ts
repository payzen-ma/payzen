import { Component, EventEmitter, Input, Output, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { ProgressBarModule } from 'primeng/progressbar';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { MessageService } from 'primeng/api';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import {
  EmployeeService,
  SageImportResult,
  SageImportCreatedItem,
  SageImportError
} from '@app/core/services/employee.service';

type ImportStep = 'upload' | 'importing' | 'results';

@Component({
  selector: 'app-sage-import-dialog',
  standalone: true,
  imports: [
    CommonModule,
    DialogModule,
    ButtonModule,
    ToastModule,
    ProgressBarModule,
    TableModule,
    TagModule,
    TranslateModule
  ],
  providers: [MessageService],
  templateUrl: './sage-import-dialog.html'
})
export class SageImportDialogComponent {
  @Input() visible = false;
  @Output() visibleChange = new EventEmitter<boolean>();
  @Input() companyId?: number;
  @Output() importComplete = new EventEmitter<SageImportResult>();

  private readonly employeeService = inject(EmployeeService);
  private readonly messageService = inject(MessageService);
  private readonly translate = inject(TranslateService);

  step = signal<ImportStep>('upload');
  selectedFile = signal<File | null>(null);
  isDragging = signal(false);
  isImporting = signal(false);
  importResult = signal<SageImportResult | null>(null);
  errorMessage = signal<string | null>(null);

  // ─── Drag & Drop ───────────────────────────────────────────────────────────

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
    if (!file.name.toLowerCase().endsWith('.csv')) {
      this.errorMessage.set('Seuls les fichiers CSV sont acceptés.');
      return;
    }
    this.selectedFile.set(file);
  }

  removeFile(): void {
    this.selectedFile.set(null);
    this.errorMessage.set(null);
  }

  // ─── Import ────────────────────────────────────────────────────────────────

  startImport(): void {
    const file = this.selectedFile();
    if (!file) return;

    this.isImporting.set(true);
    this.step.set('importing');
    this.errorMessage.set(null);

    this.employeeService.importFromSage(file, this.companyId).subscribe({
      next: (result) => {
        this.importResult.set(result);
        this.isImporting.set(false);
        this.step.set('results');
        this.importComplete.emit(result);
      },
      error: (err) => {
        this.isImporting.set(false);
        this.step.set('upload');
        const msg = err?.error?.message || err?.error?.Message || 'Erreur lors de l\'import Sage.';
        this.errorMessage.set(msg);
      }
    });
  }

  // ─── Dialog ────────────────────────────────────────────────────────────────

  close(): void {
    this.visible = false;
    this.visibleChange.emit(false);
    this.reset();
  }

  reset(): void {
    this.step.set('upload');
    this.selectedFile.set(null);
    this.isImporting.set(false);
    this.importResult.set(null);
    this.errorMessage.set(null);
  }

  // ─── Template helpers ──────────────────────────────────────────────────────

  get successCount(): number {
    return this.importResult()?.successCount ?? 0;
  }

  get failedCount(): number {
    return this.importResult()?.failedCount ?? 0;
  }

  get createdItems(): SageImportCreatedItem[] {
    return this.importResult()?.created ?? [];
  }

  get errorItems(): SageImportError[] {
    return this.importResult()?.errors ?? [];
  }

  downloadTemplate(): void {
    const header = 'Prenom;Nom;CIN;DateNaissance;Telephone;Email;CNSS;Salaire;DateEntree;Matricule;Genre';
    const example = 'Mohamed;Alami;AB123456;15/03/1990;0612345678;m.alami@example.com;123456789;8500;01/01/2024;1001;M';
    const content = `${header}\n${example}\n`;
    const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'modele_import_sage.csv';
    a.click();
    URL.revokeObjectURL(url);
  }
}
