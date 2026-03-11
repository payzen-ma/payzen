import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';

import { PayrollExportService } from './payroll-export.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';

import { SelectComponent, SelectOption } from '@app/shared/ui/select/select.component';
import { ButtonComponent } from '@app/shared/ui/button/button.component';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

/** État de chargement par export */
type ExportKey = 'journal' | 'cnss' | 'ir';

@Component({
  selector: 'app-payroll-exports',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    SelectComponent,
    ButtonComponent
  ],
  templateUrl: './payroll-exports.component.html',
  styleUrls: ['./payroll-exports.component.css']
})
export class PayrollExportsComponent implements OnInit {
  private readonly exportService   = inject(PayrollExportService);
  private readonly contextService  = inject(CompanyContextService);
  private readonly translate       = inject(TranslateService);

  // ── Formulaire ─────────────────────────────────────────────────────────
  selectedYear  = signal<number>(new Date().getFullYear());
  selectedMonth = signal<number>(new Date().getMonth() + 1);

  readonly yearOptions: SelectOption[]  = this.buildYearOptions();
  readonly monthOptions: SelectOption[] = this.buildMonthOptions();

  // ── Loading states ──────────────────────────────────────────────────────
  loading = signal<Record<ExportKey, boolean>>({ journal: false, cnss: false, ir: false });

  // ── Messages d'erreur ───────────────────────────────────────────────────
  errorMessage = signal<string | null>(null);
  successMessage = signal<string | null>(null);

  ngOnInit(): void {}

  // ── Actions ─────────────────────────────────────────────────────────────

  onExportJournal(): void {
    this.download('journal', `JournalPaie_${this.selectedYear()}_${String(this.selectedMonth()).padStart(2, '0')}.csv`);
  }

  onExportCnss(): void {
    this.download('cnss', `EtatCnss_${this.selectedYear()}_${String(this.selectedMonth()).padStart(2, '0')}.csv`);
  }

  onExportIr(): void {
    this.download('ir', `EtatIR_${this.selectedYear()}_${String(this.selectedMonth()).padStart(2, '0')}.csv`);
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
      key === 'cnss'    ? this.exportService.downloadCnss(companyId, this.selectedYear(), this.selectedMonth()) :
                          this.exportService.downloadIr(companyId, this.selectedYear(), this.selectedMonth());

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
