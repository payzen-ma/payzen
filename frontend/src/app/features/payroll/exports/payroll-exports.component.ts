import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs/operators';

import { PayrollExportService } from './payroll-export.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';

import { SelectComponent, SelectOption } from '@app/shared/ui/select/select.component';
import { ButtonComponent } from '@app/shared/ui/button/button.component';
import { TranslateModule } from '@ngx-translate/core';

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
    this.download('journal', `JournalPaie_${this.selectedYear()}_${String(this.selectedMonth()).padStart(2, '0')}.xlsx`);
  }

  onExportCnss(): void {
    this.download('cnss', `EtatCnss_${this.selectedYear()}_${String(this.selectedMonth()).padStart(2, '0')}.csv`);
  }

  onExportIr(): void {
    this.download('ir', `EtatIR_${this.selectedYear()}_${String(this.selectedMonth()).padStart(2, '0')}.xlsx`);
  }

  // ── Private helpers ─────────────────────────────────────────────────────

  private download(key: ExportKey, filename: string): void {
    const companyId = Number(this.contextService.companyId());
    if (!companyId) {
      this.errorMessage.set('Aucune entreprise sélectionnée.');
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
          this.successMessage.set(`${filename} téléchargé avec succès.`);
        },
        error: (err) => {
          const msg = err?.status === 404
            ? 'Aucun bulletin validé trouvé pour cette période.'
            : `Erreur lors de l'export : ${err?.statusText ?? 'Erreur inconnue'}`;
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
    const names = [
      'Janvier', 'Février', 'Mars', 'Avril', 'Mai', 'Juin',
      'Juillet', 'Août', 'Septembre', 'Octobre', 'Novembre', 'Décembre'
    ];
    return names.map((label, index) => ({ value: index + 1, label }));
  }
}
