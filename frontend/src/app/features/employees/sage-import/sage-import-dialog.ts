import { CommonModule } from '@angular/common';
import { Component, EventEmitter, inject, Input, Output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  EmployeeService,
  SageImportCreatedItem,
  SageImportError,
  SageImportResult
} from '@app/core/services/employee.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { ProgressBarModule } from 'primeng/progressbar';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';

type ImportStep = 'upload' | 'importing' | 'results';

@Component({
  selector: 'app-sage-import-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DialogModule,
    ButtonModule,
    ToastModule,
    ProgressBarModule,
    TableModule,
    TagModule,
    TranslateModule
  ],
  providers: [MessageService],
  templateUrl: './sage-import-dialog.html',
  styleUrls: ['./sage-import-dialog.component.css']
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
  selectedMonth = signal<number | null>(null);
  selectedYear = signal<number | null>(new Date().getFullYear());
  isDragging = signal(false);
  isImporting = signal(false);
  importResult = signal<SageImportResult | null>(null);
  errorMessage = signal<string | null>(null);

  readonly months = Array.from({ length: 12 }, (_, i) => i + 1);
  readonly years = Array.from({ length: 11 }, (_, i) => new Date().getFullYear() - 5 + i);

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

    const month = this.selectedMonth();
    const year = this.selectedYear();

    this.employeeService.importFromSage(file, this.companyId, month ?? undefined, year ?? undefined, false).subscribe({
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

  analyzeFile(): void {
    const file = this.selectedFile();
    if (!file) return;

    this.isImporting.set(true);
    this.step.set('importing');
    this.errorMessage.set(null);

    const month = this.selectedMonth();
    const year = this.selectedYear();

    this.employeeService.importFromSage(file, this.companyId, month ?? undefined, year ?? undefined, true).subscribe({
      next: (result) => {
        this.importResult.set(result);
        this.isImporting.set(false);
        this.step.set('results');
        this.importComplete.emit(result);
      },
      error: (err) => {
        this.isImporting.set(false);
        this.step.set('upload');
        const msg = err?.error?.message || err?.error?.Message || 'Erreur lors de l\'analyse du fichier.';
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

  get updatedItems(): SageImportCreatedItem[] {
    return this.importResult()?.updated ?? [];
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
