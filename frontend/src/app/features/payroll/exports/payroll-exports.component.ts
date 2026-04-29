import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { finalize } from 'rxjs/operators';

import { CompanyContextService } from '@app/core/services/companyContext.service';
import {
  CnssPreetabliParseResult,
  PayrollExportService
} from './payroll-export.service';

import { ButtonComponent } from '@app/shared/ui/button/button.component';
import { SelectComponent, SelectOption } from '@app/shared/ui/select/select.component';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

/** État de chargement par export */
type ExportKey = 'journal' | 'cnssPdf' | 'irPdf';

@Component({
  selector: 'app-payroll-exports',
  standalone: true,
  imports: [
    CommonModule,
    TranslateModule,
    SelectComponent,
    ButtonComponent
  ],
  templateUrl: './payroll-exports.component.html',
  styleUrl: './payroll-exports.component.css'
})
export class PayrollExportsComponent implements OnInit {
  private readonly exportService = inject(PayrollExportService);
  private readonly contextService = inject(CompanyContextService);
  private readonly translate = inject(TranslateService);

  // ── Formulaire ─────────────────────────────────────────────────────────
  selectedYear = signal<number>(new Date().getFullYear());
  journalMonthFrom = signal<number>(new Date().getMonth() + 1);
  journalMonthTo = signal<number>(new Date().getMonth() + 1);
  cnssPdfMonth = signal<number>(new Date().getMonth() + 1);
  irPdfMonth = signal<number>(new Date().getMonth() + 1);

  readonly yearOptions: SelectOption[] = this.buildYearOptions();
  readonly monthOptions: SelectOption[] = this.buildMonthOptions();

  // ── Loading states ──────────────────────────────────────────────────────
  loading = signal<Record<ExportKey, boolean>>({ journal: false, cnssPdf: false, irPdf: false });
  preetabliLoading = signal<boolean>(false);

  // ── Messages d'erreur ───────────────────────────────────────────────────
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  preetabliResult = signal<CnssPreetabliParseResult | null>(null);
  selectedPreetabliFile = signal<File | null>(null);

  ngOnInit(): void { }

  // ── Actions ─────────────────────────────────────────────────────────────

  onExportJournal(): void {
    if (this.journalMonthTo() < this.journalMonthFrom()) {
      this.errorMessage.set('Le mois "à" doit être supérieur ou égal au mois "de".');
      return;
    }
    this.download(
      'journal',
      `JournalPaie_${this.selectedYear()}_${String(this.journalMonthFrom()).padStart(2, '0')}_${String(this.journalMonthTo()).padStart(2, '0')}.csv`
    );
  }

  onExportCnssPdf(): void {
    this.download('cnssPdf', `EtatCNSS_${this.selectedYear()}_${String(this.cnssPdfMonth()).padStart(2, '0')}.pdf`);
  }

  onExportIrPdf(): void {
    this.download('irPdf', `EtatIR_${this.selectedYear()}_${String(this.irPdfMonth()).padStart(2, '0')}.pdf`);
  }

  onPreetabliFileSelected(event: Event): void {
    const target = event.target as HTMLInputElement | null;
    const file = target?.files?.item(0) ?? null;
    this.selectedPreetabliFile.set(file);
  }

  onParsePreetabli(): void {
    const companyId = Number(this.contextService.companyId());
    const file = this.selectedPreetabliFile();
    if (!companyId) {
      this.errorMessage.set(this.translate.instant('payrollExportsPage.messages.noCompany'));
      return;
    }
    if (!file) {
      this.errorMessage.set('Veuillez sélectionner un fichier préétabli CNSS (.txt).');
      return;
    }

    this.clearMessages();
    this.preetabliLoading.set(true);
    this.exportService
      .parseCnssPreetabli(companyId, file)
      .pipe(finalize(() => this.preetabliLoading.set(false)))
      .subscribe({
        next: (data) => {
          this.preetabliResult.set(data);
          this.successMessage.set('Le fichier préétabli CNSS a été analysé avec succès.');
        },
        error: (err) => {
          this.preetabliResult.set(null);
          this.errorMessage.set(err?.error?.message ?? 'Erreur lors de l\'analyse du préétabli CNSS.');
        }
      });
  }

  onLoadLatestPreetabli(): void {
    const companyId = Number(this.contextService.companyId());
    if (!companyId) {
      this.errorMessage.set(this.translate.instant('payrollExportsPage.messages.noCompany'));
      return;
    }

    const period = `${this.selectedYear()}${String(this.cnssPdfMonth()).padStart(2, '0')}`;
    this.clearMessages();
    this.preetabliLoading.set(true);
    this.exportService
      .getLatestCnssPreetabli(companyId, period)
      .pipe(finalize(() => this.preetabliLoading.set(false)))
      .subscribe({
        next: (data) => {
          this.preetabliResult.set(data);
          this.successMessage.set('Dernier préétabli chargé depuis l\'historique.');
        },
        error: (err) => {
          this.preetabliResult.set(null);
          this.errorMessage.set(err?.error?.message ?? 'Aucun préétabli sauvegardé pour cette période.');
        }
      });
  }

  onGenerateBds(): void {
    const companyId = Number(this.contextService.companyId());
    const file = this.selectedPreetabliFile();
    if (!companyId) {
      this.errorMessage.set(this.translate.instant('payrollExportsPage.messages.noCompany'));
      return;
    }
    if (!file) {
      this.errorMessage.set('Veuillez sélectionner un fichier préétabli CNSS (.txt).');
      return;
    }

    this.clearMessages();
    this.preetabliLoading.set(true);
    this.exportService
      .generateCnssBds(companyId, file)
      .pipe(finalize(() => this.preetabliLoading.set(false)))
      .subscribe({
        next: (blob) => {
          PayrollExportService.triggerDownload(
            blob,
            `DS_CNSS_${this.selectedYear()}_${String(this.cnssPdfMonth()).padStart(2, '0')}.txt`
          );
          this.successMessage.set('Fichier e-BDS généré avec succès.');
        },
        error: (err) => {
          this.errorMessage.set(err?.error?.message ?? 'Erreur lors de la génération e-BDS.');
        }
      });
  }

  // ── Private helpers ─────────────────────────────────────────────────────

  private download(key: ExportKey, filename: string): void {
    const companyId = Number(this.contextService.companyId());
    if (!companyId) {
      this.errorMessage.set(this.translate.instant('payrollExportsPage.messages.noCompany'));
      return;
    }

    this.clearMessages();
    this.setLoading(key, true);

    const call$ =
      key === 'journal'
        ? this.exportService.downloadJournal(companyId, this.selectedYear(), this.journalMonthFrom(), this.journalMonthTo())
        : key === 'cnssPdf'
          ? this.exportService.downloadCnssPdf(companyId, this.selectedYear(), this.cnssPdfMonth())
          : this.exportService.downloadIrPdf(companyId, this.selectedYear(), this.irPdfMonth());

    call$.pipe(finalize(() => this.setLoading(key, false)))
      .subscribe({
        next: (blob) => {
          PayrollExportService.triggerDownload(blob, filename);
          this.successMessage.set(this.translate.instant('payrollExportsPage.messages.downloadSuccess', { filename }));
        },
        error: (err) => {
          const msg = err?.status === 404
            ? this.translate.instant('payrollExportsPage.messages.noBulletins')
            : this.translate.instant('payrollExportsPage.messages.errorGeneric', {
              error: err?.statusText ?? this.translate.instant('payrollExportsPage.messages.errorUnknown')
            });
          this.errorMessage.set(msg);
        }
      });
  }

  private setLoading(key: ExportKey, value: boolean): void {
    this.loading.update(prev => ({ ...prev, [key]: value }));
  }

  private clearMessages(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);
  }

  get selectedPreetabliFileName(): string {
    return this.selectedPreetabliFile()?.name ?? 'Aucun fichier choisi';
  }

  private buildYearOptions(): SelectOption[] {
    const current = new Date().getFullYear();
    const options: SelectOption[] = [];
    for (let y = current; y >= current - 4; y--) {
      options.push({ value: y, label: String(y) });
    }
    return options;
  }

  private buildMonthOptions(): SelectOption[] {
    const monthKeys = [
      'payslip.months.january', 'payslip.months.february', 'payslip.months.march',
      'payslip.months.april', 'payslip.months.may', 'payslip.months.june',
      'payslip.months.july', 'payslip.months.august', 'payslip.months.september',
      'payslip.months.october', 'payslip.months.november', 'payslip.months.december'
    ];
    return monthKeys.map((key, index) => ({
      value: index + 1,
      label: this.translate.instant(key)
    }));
  }
}
