import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  OnInit,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { SkeletonModule } from 'primeng/skeleton';
import { MessageModule } from 'primeng/message';
import { ProgressBarModule } from 'primeng/progressbar';
import { TooltipModule } from 'primeng/tooltip';
import { TagModule } from 'primeng/tag';

import {
  PayrollTaxService,
  PayrollTaxSnapshotDto
} from '../../services/payroll-tax.service';

export const MONTH_ABBR_FR = [
  '', 'Jan', 'Fév', 'Mar', 'Avr', 'Mai', 'Jun',
  'Jul', 'Aoû', 'Sep', 'Oct', 'Nov', 'Déc'
];

export const MONTH_NAMES_FR = [
  '', 'Janvier', 'Février', 'Mars', 'Avril', 'Mai', 'Juin',
  'Juillet', 'Août', 'Septembre', 'Octobre', 'Novembre', 'Décembre'
];

@Component({
  selector: 'app-tax-dashboard',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    FormsModule,
    SelectModule,
    SkeletonModule,
    MessageModule,
    ProgressBarModule,
    TooltipModule,
    TagModule
  ],
  templateUrl: './tax-dashboard.component.html',
  styleUrl: './tax-dashboard.component.css'
})
export class TaxDashboardComponent implements OnInit {
  private readonly taxService = inject(PayrollTaxService);

  readonly employeeId   = input.required<number>();
  readonly companyId    = input.required<number>();
  readonly employeeName = input.required<string>();

  readonly selectedYear  = signal(new Date().getFullYear());
  readonly selectedMonth = signal<number | null>(null);
  readonly snapshots     = signal<PayrollTaxSnapshotDto[]>([]);
  readonly isLoading     = signal(false);

  readonly months = Array.from({ length: 12 }, (_, i) => i + 1);

  readonly yearOptions = Array.from({ length: 6 }, (_, i) => {
    const y = new Date().getFullYear() + 1 - i;
    return { label: String(y), value: y };
  });

  readonly selectedSnap = computed(() =>
    this.snapshots().find(s => s.month === this.selectedMonth()) ?? null
  );

  readonly prevSnap = computed(() => {
    const m = this.selectedMonth();
    if (!m || m <= 1) return null;
    return this.snapshots().find(s => s.month === m - 1) ?? null;
  });

  readonly isExonere = computed(() => {
    const snap = this.selectedSnap();
    return snap ? snap.cumulIr === 0 : this.snapshots().every(s => s.cumulIr === 0);
  });

  readonly hasAnySnapshot = computed(() => this.snapshots().length > 0);

  readonly irProgressValue = computed(() => {
    const snap = this.selectedSnap();
    if (!snap || snap.cumulSni === 0) return 0;
    return Math.min(100, (snap.cumulIr / snap.cumulSni) * 100);
  });

  readonly irProgressSeverity = computed((): string => {
    const snap = this.selectedSnap();
    if (!snap || snap.cumulIr === 0) return 'success';
    if (snap.tauxEffectif <= 0.10) return 'warn';
    return 'danger';
  });

  readonly monthAbbr = MONTH_ABBR_FR;
  readonly monthNames = MONTH_NAMES_FR;

  constructor() {
    effect(() => {
      const year = this.selectedYear();
      void this.loadSnapshots(year);
    });
  }

  ngOnInit(): void {
    // L'effect dans le constructeur déclenche le premier chargement
  }

  private async loadSnapshots(year: number): Promise<void> {
    this.isLoading.set(true);
    this.selectedMonth.set(null);
    this.snapshots.set([]);

    this.taxService
      .getYearSummary(this.employeeId(), this.companyId(), year)
      .subscribe({
        next: (data) => {
          this.snapshots.set(data);
          this.isLoading.set(false);
          this.autoSelectMonth(data, year);
        },
        error: () => {
          this.snapshots.set([]);
          this.isLoading.set(false);
        }
      });
  }

  private autoSelectMonth(data: PayrollTaxSnapshotDto[], year: number): void {
    if (data.length === 0) return;

    const currentMonth = new Date().getMonth() + 1;
    const currentYear  = new Date().getFullYear();

    // Pour l'année courante : préférer le mois courant s'il est calculé,
    // sinon prendre le dernier mois calculé disponible.
    if (year === currentYear) {
      const hasCurrentMonth = data.some(s => s.month === currentMonth);
      if (hasCurrentMonth) {
        this.selectedMonth.set(currentMonth);
        return;
      }
    }

    // Pour les années passées (ou si le mois courant n'est pas encore calculé) :
    // sélectionner le mois le plus récent calculé.
    const lastSnap = data.reduce((prev, curr) => curr.month > prev.month ? curr : prev);
    this.selectedMonth.set(lastSnap.month);
  }

  getMonthSnap(month: number): PayrollTaxSnapshotDto | undefined {
    return this.snapshots().find(s => s.month === month);
  }

  getMonthColor(month: number): 'surface' | 'success' | 'warning' | 'primary' {
    const snap = this.getMonthSnap(month);
    if (!snap) return 'surface';
    if (snap.irMois === 0) return 'success';
    if (this.toDisplayPercent(snap.tauxIrMois) <= 10) return 'warning';
    return 'primary';
  }

  isMonthCalculated(month: number): boolean {
    return !!this.getMonthSnap(month);
  }

  selectMonth(month: number): void {
    if (!this.isMonthCalculated(month)) return;
    this.selectedMonth.set(month);
  }

  onYearChange(year: number): void {
    this.selectedYear.set(year);
  }

  formatAmount(value: number): string {
    return value.toLocaleString('fr-MA', { minimumFractionDigits: 2 }) + ' DH';
  }

  formatPercent(value: number): string {
    return this.toDisplayPercent(value).toFixed(2) + ' %';
  }

  private toDisplayPercent(value: number): number {
    // Compat: certains champs arrivent en ratio (0.37), d'autres déjà en % (31.77).
    return Math.abs(value) <= 1 ? value * 100 : value;
  }

  formatIrMonth(month: number): string {
    const snap = this.getMonthSnap(month);
    if (!snap) return '— DH';
    return snap.irMois.toLocaleString('fr-MA', { minimumFractionDigits: 0 }) + ' DH';
  }

  getMonthAriaLabel(month: number): string {
    const snap = this.getMonthSnap(month);
    const name = MONTH_NAMES_FR[month].toLowerCase();
    const year = this.selectedYear();
    if (!snap) return `Mois ${name} ${year} — non calculé`;
    return `Mois ${name} ${year} — IR : ${this.formatIrMonth(month)}`;
  }

  getStatusLabel(snap: PayrollTaxSnapshotDto | null): string {
    if (!snap) return '';
    return snap.irMois === 0 ? 'Exonéré' : 'Imposable';
  }

  getStatusSeverity(snap: PayrollTaxSnapshotDto | null): 'success' | 'warn' {
    if (!snap || snap.irMois === 0) return 'success';
    return 'warn';
  }

  getGlobalStatusSeverity(): 'success' | 'warn' {
    const totalIr = this.snapshots().reduce((sum, s) => sum + s.cumulIr, 0);
    return totalIr === 0 ? 'success' : 'warn';
  }

  getGlobalStatusLabel(): string {
    const totalIr = this.snapshots().reduce((sum, s) => sum + s.cumulIr, 0);
    return totalIr === 0 ? 'Exonéré' : 'Imposable';
  }

  irProgressColor(): string {
    const sev = this.irProgressSeverity();
    if (sev === 'success') return 'var(--success)';
    if (sev === 'warning') return 'var(--warning)';
    return 'var(--danger)';
  }
}
