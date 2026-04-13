import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AuthService } from '@app/core/services/auth.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { Employee, EmployeeService } from '@app/core/services/employee.service';
import { PayrollService } from '@app/core/services/payroll.service';
import { ButtonComponent } from '@app/shared/ui/button/button.component';
import { SelectComponent, SelectOption } from '@app/shared/ui/select/select.component';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-payslip',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    SelectComponent,
    ButtonComponent
  ],
  templateUrl: './payslip.component.html'
})
export class PayslipComponent implements OnInit {
  private readonly payrollService = inject(PayrollService);
  private readonly employeeService = inject(EmployeeService);
  private readonly authService = inject(AuthService);
  private readonly contextService = inject(CompanyContextService);
  private readonly translate = inject(TranslateService);

  // ── État UI ──────────────────────────────────────────────────────────────
  readonly loading = signal(false);
  readonly generating = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  // ── Rôle courant ─────────────────────────────────────────────────────────
  /** Vrai si l'utilisateur connecté est un employé simple */
  readonly isSimpleEmployee = computed(() => this.authService.isEmployee());

  /** Vrai si l'utilisateur peut choisir un autre employé (RH, Admin, Manager, Cabinet…) */
  readonly canChooseEmployee = computed(() =>
    !this.authService.isEmployee()
  );

  // ── Sélections ───────────────────────────────────────────────────────────
  readonly selectedMonth = signal<number>(new Date().getMonth() + 1);
  readonly selectedYear = signal<number>(new Date().getFullYear());
  readonly selectedEmployee = signal<number | null>(null);
  // Vue d'affichage : 0 = mensuel, 1 = 1-15, 2 = 16-fin
  readonly selectedHalf = signal<number | null>(1);

  // ── Données ───────────────────────────────────────────────────────────────
  readonly employees = signal<Employee[]>([]);

  // ── Options selects ───────────────────────────────────────────────────────
  readonly monthOptions = signal<SelectOption[]>([]);
  readonly yearOptions = signal<SelectOption[]>([]);
  readonly employeeOptions = signal<SelectOption[]>([]);
  readonly halfOptions = signal<SelectOption[]>([
    { label: 'Mensuel', value: 0 },
    { label: '1-15', value: 1 },
    { label: '16-31', value: 2 }
  ]);

  // ── Nom de la société courante ────────────────────────────────────────────
  readonly currentCompanyName = this.contextService.companyName;

  // ── Infos employé courant (mode EMPLOYEE) ────────────────────────────
  /** ID issu du token JWT (peut être absent sur les sessions antérieures au correctif) */
  private readonly currentEmployeeId = computed<number | null>(() => {
    const user = this.authService.currentUser();
    if (!user?.employee_id) return null;
    return parseInt(user.employee_id.toString(), 10);
  });

  /** ID résolu (token ou API de secours) */
  readonly resolvedEmployeeId = signal<number | null>(null);

  readonly currentUserFullName = computed(() => {
    const user = this.authService.currentUser();
    if (!user) return '';
    return `${user.firstName} ${user.lastName}`.trim();
  });

  // ── Bouton générer actif? ─────────────────────────────────────────────────
  readonly canGenerate = computed(() => {
    // Pour un employé simple : mois et année ont toujours une valeur par défaut
    if (this.isSimpleEmployee()) {
      return true;
    }
    // Pour les autres rôles : un employé doit être sélectionné
    return this.selectedEmployee() !== null;
  });

  // ─────────────────────────────────────────────────────────────────────────
  ngOnInit(): void {
    this.initMonthOptions();
    this.initYearOptions();

    if (this.isSimpleEmployee()) {
      this.resolveCurrentEmployee();
    } else {
      this.loadEmployees();
    }
  }

  // ── Init options mois ─────────────────────────────────────────────────────
  private initMonthOptions(): void {
    const months = [
      'payslip.months.january', 'payslip.months.february',
      'payslip.months.march', 'payslip.months.april',
      'payslip.months.may', 'payslip.months.june',
      'payslip.months.july', 'payslip.months.august',
      'payslip.months.september', 'payslip.months.october',
      'payslip.months.november', 'payslip.months.december'
    ];
    this.monthOptions.set(
      months.map((key, i) => ({
        label: this.translate.instant(key),
        value: i + 1
      }))
    );
  }

  // ── Init options année ────────────────────────────────────────────────────
  private initYearOptions(): void {
    const currentYear = new Date().getFullYear();
    const years: SelectOption[] = [];
    for (let y = currentYear; y >= currentYear - 10; y--) {
      years.push({ label: y.toString(), value: y });
    }
    this.yearOptions.set(years);
  }
  // ── Résolution ID employé courant (mode EMPLOYEE) ──────────────────────────────
  /**
   * 1ère source : employee_id dans le token JWT (sessions après le correctif backend)
   * Fallback : GET /api/employee/me (sessions mises en cache avant le correctif)
   */
  private resolveCurrentEmployee(): void {
    const fromToken = this.currentEmployeeId();
    if (fromToken !== null) {
      this.resolvedEmployeeId.set(fromToken);
      return;
    }

    // Fallback : appel API dédié
    this.loading.set(true);
    this.employeeService.getMyEmployee().subscribe({
      next: (data) => {
        this.resolvedEmployeeId.set(data.employeeId);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
  // ── Chargement des employés (mode non-EMPLOYEE) ───────────────────────────
  private loadEmployees(): void {
    this.loading.set(true);
    this.employeeOptions.set([
      { label: this.translate.instant('payslip.loadingEmployees'), value: null, disabled: true }
    ]);

    const companyId = this.contextService.companyId();
    const filters = companyId ? { companyId } : {};

    this.employeeService.getEmployees(filters).subscribe({
      next: (response) => {
        this.employees.set(response.employees);
        this.employeeOptions.set(
          response.employees.map(emp => ({
            label: `${emp.firstName} ${emp.lastName}`,
            value: parseInt(emp.id, 10)
          }))
        );
        this.loading.set(false);
      },
      error: () => {
        this.employeeOptions.set([]);
        this.loading.set(false);
      }
    });
  }

  // ── Génération fiche de paie ──────────────────────────────────────────────
  generatePayslip(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const employeeId = this.isSimpleEmployee()
      ? this.resolvedEmployeeId()
      : this.selectedEmployee();

    if (!employeeId) {
      this.errorMessage.set(
        this.isSimpleEmployee()
          ? this.translate.instant('payslip.error.noEmployeeId')
          : this.translate.instant('payslip.error.noEmployee')
      );
      return;
    }

    this.generating.set(true);

    this.payrollService
      .downloadPayslip(
        employeeId,
        this.selectedYear(),
        this.selectedMonth(),
        this.selectedHalf()
      )
      .subscribe({
        next: (blob: Blob) => {
          this.generating.set(false);

          // Créer une URL blob et ouvrir le PDF dans un nouvel onglet
          const url = URL.createObjectURL(blob);
          const link = document.createElement('a');
          link.href = url;

          const month = this.selectedMonth().toString().padStart(2, '0');
          const year = this.selectedYear();
          const periodSuffix =
            this.selectedHalf() === 0
              ? 'mensuel'
              : this.selectedHalf() === 1
                ? 'demi_1_15'
                : 'demi_16_fin';

          if (this.isSimpleEmployee()) {
            const name = this.currentUserFullName().replace(/\s+/g, '_');
            link.download = `Fiche_Paie_${name}_${month}_${year}_${periodSuffix}.pdf`;
          } else {
            const selected = this.employees().find(
              e => parseInt(e.id, 10) === this.selectedEmployee()
            );
            const name = selected
              ? `${selected.firstName}_${selected.lastName}`.replace(/\s+/g, '_')
              : 'Employe';
            link.download = `Fiche_Paie_${name}_${month}_${year}_${periodSuffix}.pdf`;
          }

          document.body.appendChild(link);
          link.click();
          document.body.removeChild(link);
          URL.revokeObjectURL(url);

          this.successMessage.set(this.translate.instant('payslip.success.generated'));
        },
        error: (err) => {
          this.generating.set(false);
          if (err.status === 404) {
            if (this.isSimpleEmployee()) {
              this.errorMessage.set(this.translate.instant('payslip.error.notFoundEMP'));
              return;
            } else {
              this.errorMessage.set(this.translate.instant('payslip.error.notFound'));
            }
          } else if (err.status === 400) {
            this.errorMessage.set(this.translate.instant('payslip.error.payrollError'));
          } else {
            this.errorMessage.set(this.translate.instant('payslip.error.generic'));
          }
        }
      });
  }

  // ── Helpers ───────────────────────────────────────────────────────────────
  onMonthChange(value: number | null): void {
    if (value !== null) this.selectedMonth.set(value);
  }

  onYearChange(value: number | null): void {
    if (value !== null) this.selectedYear.set(value);
  }

  /** Nom du mois sélectionné (pour l'affichage récapitulatif) */
  readonly selectedMonthLabel = computed(() => {
    const opts = this.monthOptions();
    const m = this.selectedMonth();
    return opts.find(o => o.value === m)?.label ?? '';
  });

  /** Libellé de la période bimensuelle (1-15 / 16-fin) */
  readonly selectedHalfLabel = computed(() => {
    const h = this.selectedHalf();
    if (h === 0) return 'Mensuel';
    if (h === 1) return '1-15';
    if (h === 2) return '16-31';
    return '';
  });
}
