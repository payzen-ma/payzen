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
type ExportKey = 'journal' | 'cnss' | 'cnssPdf' | 'ir' | 'irPdf';

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
  selectedMonth = signal<number>(new Date().getMonth() + 1);

  readonly yearOptions: SelectOption[] = this.buildYearOptions();
  readonly monthOptions: SelectOption[] = this.buildMonthOptions();

  // ── Loading states ──────────────────────────────────────────────────────
  loading = signal<Record<ExportKey, boolean>>({ journal: false, cnss: false, cnssPdf: false, ir: false, irPdf: false });
  preetabliLoading = signal<boolean>(false);

  // ── Messages d'erreur ───────────────────────────────────────────────────
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);
  preetabliResult = signal<CnssPreetabliParseResult | null>(null);
  selectedPreetabliFile = signal<File | null>(null);

  ngOnInit(): void { }

  // ── Actions ─────────────────────────────────────────────────────────────

  onExportJournal(): void {
    this.download('journal', `JournalPaie_${this.selectedYear()}_${String(this.selectedMonth()).padStart(2, '0')}.xlsx`);
  }

  onExportCnss(): void {
    this.download('cnss', `EtatCnss_${this.selectedYear()}_${String(this.selectedMonth()).padStart(2, '0')}.csv`);
  }

  onExportCnssPdf(): void {
    this.download('cnssPdf', `EtatCNSS_${this.selectedYear()}_${String(this.selectedMonth()).padStart(2, '0')}.pdf`);
  }

  onExportIr(): void {
    this.download('ir', `EtatIR_${this.selectedYear()}_${String(this.selectedMonth()).padStart(2, '0')}.csv`);
  }

  onExportIrPdf(): void {
    this.download('irPdf', `EtatIR_${this.selectedYear()}_${String(this.selectedMonth()).padStart(2, '0')}.pdf`);
  }

  onPreetabliFileSelected(event: Event): void {
    const target = event.target as HTMLInputElement | null;
    const file = target?.files?.item(0) ?? null;
    this.selectedPreetabliFile.set(file);
  }

  onParsePreetabli(): void {
    const file = this.selectedPreetabliFile();
    if (!file) {
      this.errorMessage.set('Veuillez sélectionner un fichier préétabli CNSS (.txt).');
      return;
    }

    this.clearMessages();
    this.preetabliLoading.set(true);
    this.exportService
      .parseCnssPreetabli(file)
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
      key === 'journal' ? this.exportService.downloadJournal(companyId, this.selectedYear(), this.selectedMonth()) :
        key === 'cnss' ? this.exportService.downloadCnss(companyId, this.selectedYear(), this.selectedMonth()) :
          key === 'cnssPdf' ? this.exportService.downloadCnssPdf(companyId, this.selectedYear(), this.selectedMonth()) :
            key === 'ir' ? this.exportService.downloadIr(companyId, this.selectedYear(), this.selectedMonth()) :
              this.exportService.downloadIrPdf(companyId, this.selectedYear(), this.selectedMonth());

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
