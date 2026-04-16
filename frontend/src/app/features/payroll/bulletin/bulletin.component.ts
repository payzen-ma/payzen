import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  PayrollDetail,
  PayrollFilters,
  PayrollResult,
  PayrollResultStatus
} from '@app/core/models/payroll.model';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { Employee, EmployeeService } from '@app/core/services/employee.service';
import { PayrollService } from '@app/core/services/payroll.service';
import { ToastService } from '@app/core/services/toast.service';
import { BadgeComponent } from '@app/shared/ui/badge/badge.component';
import { ButtonComponent } from '@app/shared/ui/button/button.component';
import { SelectComponent, SelectOption } from '@app/shared/ui/select/select.component';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';

@Component({
  selector: 'app-bulletin',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    SelectComponent,
    ButtonComponent,
    BadgeComponent,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    IconFieldModule,
    InputIconModule
  ],
  templateUrl: './bulletin.component.html',
})
export class BulletinComponent implements OnInit {
  private readonly payrollService = inject(PayrollService);
  private readonly employeeService = inject(EmployeeService);
  private readonly contextService = inject(CompanyContextService);
  private readonly translate = inject(TranslateService);
  private readonly toastService = inject(ToastService);

  // État
  readonly loading = signal(false);
  readonly calculating = signal(false);
  readonly approving = signal(false);
  readonly employees = signal<Employee[]>([]);
  readonly payrollResults = signal<PayrollResult[]>([]);

  // Modal détail bulletin
  readonly showDetailModal = signal(false);
  readonly detailLoading = signal(false);
  readonly selectedDetail = signal<PayrollDetail | null>(null);
  readonly showEditStatusModal = signal(false);
  readonly editingResult = signal<PayrollResult | null>(null);
  readonly selectedEditStatus = signal<PayrollResultStatus | null>(null);

  // Alert box (remplace alert())
  readonly showAlert = signal(false);
  readonly alertMessage = signal('');
  readonly alertType = signal<'success' | 'error' | 'warning' | 'info'>('info');

  // Confirm dialog (remplace confirm())
  readonly showConfirmDialog = signal(false);
  readonly confirmTitle = signal('');
  readonly confirmMessage = signal('');
  private confirmResolve: ((value: boolean) => void) | null = null;

  // Filtres
  readonly selectedMonth = signal<number>(new Date().getMonth() + 1);
  readonly selectedYear = signal<number>(new Date().getFullYear());
  readonly selectedHalf = signal<number | null>(null);
  readonly selectedEmployee = signal<number | null>(null);
  readonly statusFilter = signal<PayrollResultStatus | null>(null);
  readonly searchQuery = signal<string>('');

  // Options pour les selects
  readonly monthOptions = signal<SelectOption[]>([]);
  readonly yearOptions = signal<SelectOption[]>([]);
  readonly halfOptions = signal<SelectOption[]>([
    { label: 'Mois entier', value: null },
    { label: '1ère quinzaine', value: 1 },
    { label: '2ème quinzaine', value: 2 }
  ]);
  readonly employeeOptions = signal<SelectOption[]>([
    { label: '', value: null, disabled: true }
  ]);
  readonly statusOptions = signal<SelectOption[]>([]);
  readonly editStatusOptions = signal<SelectOption[]>([]);

  // Données filtrées
  readonly filteredResults = computed(() => {
    let results = this.payrollResults();

    // Filtre par statut (en normalisant les valeurs renvoyées par l'API)
    const currentStatusFilter = this.statusFilter();
    if (currentStatusFilter) {
      results = results.filter(r => this.normalizeStatus(r.status) === currentStatusFilter);
    }

    // Filtre par recherche (nom employé)
    const query = this.searchQuery().toLowerCase().trim();
    if (query) {
      results = results.filter(r =>
        r.employeeName.toLowerCase().includes(query)
      );
    }

    return results;
  });

  // Statistiques
  readonly stats = computed(() => {
    const results = this.filteredResults();
    return {
      total: results.length,
      success: results.filter(r => this.normalizeStatus(r.status) === PayrollResultStatus.SUCCESS).length,
      error: results.filter(r => this.normalizeStatus(r.status) === PayrollResultStatus.ERROR).length,
      pending: results.filter(r => this.normalizeStatus(r.status) === PayrollResultStatus.PENDING).length,
      totalBrut: results.reduce((sum, r) => sum + (r.totalBrut || 0), 0),
      // Keep KPI aligned with row display logic (totalNet2 has priority when provided)
      totalNet: results.reduce((sum, r) => sum + ((r.totalNet2 ?? r.totalNet) || 0), 0)
    };
  });

  readonly currentCompanyName = this.contextService.companyName;

  ngOnInit(): void {
    this.refreshStatusOptions();
    this.translate.onLangChange?.subscribe(() => this.refreshStatusOptions());
    this.employeeOptions.set([{ label: this.translate.instant('payrollBulletin.loading'), value: null, disabled: true }]);
    this.initializeMonthsAndYears();
    this.loadEmployees();
    this.loadPayrollResults();
  }

  private refreshStatusOptions(): void {
    this.statusOptions.set([
      { label: this.translate.instant('payrollBulletin.allStatuses'), value: null },
      { label: this.translate.instant('payrollBulletin.statusSuccess'), value: PayrollResultStatus.SUCCESS },
      { label: this.translate.instant('payrollBulletin.statusError'), value: PayrollResultStatus.ERROR },
      { label: this.translate.instant('payrollBulletin.statusPending'), value: PayrollResultStatus.PENDING },
      { label: this.translate.instant('payrollBulletin.statusApproved') === 'payrollBulletin.statusApproved' ? 'Approuvée' : this.translate.instant('payrollBulletin.statusApproved'), value: PayrollResultStatus.APPROVED }
    ]);

    this.editStatusOptions.set([
      { label: this.translate.instant('payrollBulletin.statusSuccess'), value: PayrollResultStatus.SUCCESS },
      { label: this.translate.instant('payrollBulletin.statusError'), value: PayrollResultStatus.ERROR },
      { label: this.translate.instant('payrollBulletin.statusPending'), value: PayrollResultStatus.PENDING },
      { label: this.translate.instant('payrollBulletin.statusApproved') === 'payrollBulletin.statusApproved' ? 'Approuvée' : this.translate.instant('payrollBulletin.statusApproved'), value: PayrollResultStatus.APPROVED }
    ]);
  }

  private initializeMonthsAndYears(): void {
    // Mois
    const months = this.payrollService.getMonths();
    this.monthOptions.set(months.map(m => ({
      label: m.label,
      value: m.value
    })));

    // Années
    const years = this.payrollService.getYears();
    this.yearOptions.set(years.map(y => ({
      label: y.toString(),
      value: y
    })));
  }

  private loadEmployees(): void {
    const companyId = this.contextService.companyId();
    if (!companyId) {
      // Initialiser avec l'option par défaut même sans employés
      this.employeeOptions.set([
        { label: this.translate.instant('payrollBulletin.allEmployees'), value: null }
      ]);
      return;
    }

    this.employeeService.getEmployees({ companyId: companyId }).subscribe({
      next: (response) => {
        this.employees.set(response.employees || []);

        const options = [
          { label: this.translate.instant('payrollBulletin.allEmployees'), value: null },
          ...(response.employees || []).map(emp => ({
            label: `${emp.firstName} ${emp.lastName}`,
            value: parseInt(emp.id)
          }))
        ];

        this.employeeOptions.set(options);
      },
      error: (error) => {
        console.error('Erreur lors du chargement des employés:', error);
        // Initialiser avec l'option par défaut en cas d'erreur
        this.employeeOptions.set([
          { label: this.translate.instant('payrollBulletin.allEmployees'), value: null }
        ]);
        const errorMessage = this.extractErrorMessage(
          error,
          this.translate.instant('payrollBulletin.loadEmployeesError') || 'Impossible de charger les employés.'
        );
        this.toastService.error(errorMessage);
      }
    });
  }

  loadPayrollResults(): void {
    const companyId = this.contextService.companyId();
    const filters: PayrollFilters = {
      month: this.selectedMonth(),
      year: this.selectedYear(),
      companyId: companyId ? parseInt(companyId.toString()) : undefined,
      status: this.statusFilter() || undefined,
      half: this.selectedHalf() ?? undefined
    };

    this.loading.set(true);
    this.payrollService.getPayrollResults(filters).subscribe({
      next: (response) => {
        this.payrollResults.set(response.results);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Erreur lors du chargement des résultats:', error);
        this.loading.set(false);
        const errorMessage = this.extractErrorMessage(
          error,
          this.translate.instant('payrollBulletin.loadError') || 'Impossible de charger les résultats de paie.'
        );
        this.toastService.error(errorMessage);
      }
    });
  }

  async calculateForAll(): Promise<void> {
    const ok = await this.openConfirm(
      this.translate.instant('payrollBulletin.confirmGenerateTitle'),
      this.translate.instant('payrollBulletin.confirmGenerateMessage')
    );
    if (!ok) return;

    this.calculating.set(true);
    try {
      this.payrollService.calculatePayrollForAll(
        this.selectedMonth(),
        this.selectedYear(),
        this.selectedHalf() ?? undefined
      ).subscribe({
        next: (response) => {
          this.calculating.set(false);
          this.toastService.success(
            this.translate.instant('payrollBulletin.calculationSuccessFor', { month: response.month, year: response.year })
          );
          this.loadPayrollResults();
        },
        error: (error) => {
          console.error('Erreur lors du calcul:', error);
          this.calculating.set(false);
          const errorMessage = this.extractErrorMessage(
            error,
            this.translate.instant('payrollBulletin.calculationError')
          );
          this.toastService.error(errorMessage);
        }
      });
    } catch (error: any) {
      console.error('Erreur:', error);
      this.calculating.set(false);
      const errorMessage = this.extractErrorMessage(
        error,
        this.translate.instant('payrollBulletin.calculationError')
      );
      this.toastService.error(errorMessage);
    }
  }

  calculateForEmployee(): void {
    const employeeId = this.selectedEmployee();
    if (!employeeId) {
      this.toastService.warning(this.translate.instant('payrollBulletin.selectEmployeeRequired'));
      return;
    }

    this.calculating.set(true);
    this.payrollService.calculatePayrollForEmployee(
      employeeId,
      this.selectedMonth(),
      this.selectedYear(),
      this.selectedHalf() ?? undefined
    ).subscribe({
      next: (response) => {
        this.calculating.set(false);
        this.toastService.success(this.translate.instant('payrollBulletin.calculationSuccess'));
        this.loadPayrollResults();
      },
      error: (error) => {
        console.error('Erreur lors du calcul:', error);
        this.calculating.set(false);
        const errorMessage = this.extractErrorMessage(
          error,
          this.translate.instant('payrollBulletin.calculationError')
        );
        this.toastService.error(errorMessage);
      }
    });
  }

  onMonthChange(month: number): void {
    this.selectedMonth.set(month);
    this.loadPayrollResults();
  }

  onYearChange(year: number): void {
    this.selectedYear.set(year);
    this.loadPayrollResults();
  }

  onStatusFilterChange(status: PayrollResultStatus | null | string): void {
    // L'option "Tous les statuts" a une valeur null, qui arrive souvent sous forme de chaîne "null".
    if (status === null || status === undefined || status === '' || status === 'null') {
      this.statusFilter.set(null);
      return;
    }
    this.statusFilter.set(status as PayrollResultStatus);
  }

  onSearchChange(query: string): void {
    this.searchQuery.set(query);
  }

  /**
   * Normalise un statut retourné par l'API (SUCCESS, COMPLETED, Error, erreur, pending, etc.)
   * vers l'enum PayrollResultStatus, pour garder des compteurs, filtres et badges cohérents.
   *
   * Règle métier: tout ce qui n'est ni "erreur" ni "en attente" est considéré comme "succès".
   */
  private normalizeStatus(status: PayrollResultStatus | string | null | undefined): PayrollResultStatus | null {
    if (status === undefined || status === null) return null;
    const raw = status.toString().trim().toUpperCase();

    // Erreurs
    if (
      raw === PayrollResultStatus.ERROR ||
      raw === 'ERROR' ||
      raw === 'ERREUR' ||
      raw === 'FAILED' ||
      raw === 'FAIL'
    ) {
      return PayrollResultStatus.ERROR;
    }

    // En attente
    if (
      raw === PayrollResultStatus.PENDING ||
      raw === 'PENDING' ||
      raw === 'EN_ATTENTE' ||
      raw === 'EN ATTENTE'
    ) {
      return PayrollResultStatus.PENDING;
    }

    if (
      raw === PayrollResultStatus.APPROVED ||
      raw === 'APPROVED' || 
      raw === 'APPROUVÉE' ||
      raw === 'APPROUVEE'
    ) {
      return PayrollResultStatus.APPROVED;
    }

    // Par défaut, tout le reste est traité comme "succès"
    return PayrollResultStatus.SUCCESS;
  }

  getStatusBadgeVariant(status: PayrollResultStatus): 'success' | 'danger' | 'warning' | 'info' {
    const normalized = this.normalizeStatus(status);
    switch (normalized) {
      case PayrollResultStatus.SUCCESS:
        return 'success';
      case PayrollResultStatus.ERROR:
        return 'danger';
      case PayrollResultStatus.PENDING:
        return 'warning';
      case PayrollResultStatus.APPROVED:
        return 'success';
      default:
        return 'info';
    }
  }

  getStatusLabel(status: PayrollResultStatus): string {
    const normalized = this.normalizeStatus(status);
    switch (normalized) {
      case PayrollResultStatus.SUCCESS:
        return this.translate.instant('payrollBulletin.statusSuccess');
      case PayrollResultStatus.ERROR:
        return this.translate.instant('payrollBulletin.statusError');
      case PayrollResultStatus.PENDING:
        return this.translate.instant('payrollBulletin.statusPending');
      case PayrollResultStatus.APPROVED:
        const approvedLabel = this.translate.instant('payrollBulletin.statusApproved');
        return approvedLabel === 'payrollBulletin.statusApproved' ? 'Approuvée' : approvedLabel;
      default:
        return status;
    }
  }

  isApproved(status: PayrollResultStatus | string | null | undefined): boolean {
    return this.normalizeStatus(status) === PayrollResultStatus.APPROVED;
  }

  formatCurrency(amount: number | undefined | null): string {
    if (amount === undefined || amount === null) return '0.00 MAD';
    return new Intl.NumberFormat('fr-MA', {
      style: 'currency',
      currency: 'MAD'
    }).format(amount);
  }

  viewDetail(result: PayrollResult): void {
    this.selectedDetail.set(null);
    this.showDetailModal.set(true);
    this.detailLoading.set(true);
    this.payrollService.getPayrollDetail(result.id).subscribe({
      next: (detail) => {
        this.selectedDetail.set(detail);
        this.detailLoading.set(false);
      },
      error: (err) => {
        console.error('Erreur lors du chargement du détail:', err);
        this.detailLoading.set(false);
        this.showDetailModal.set(false);
        const errorMessage = this.extractErrorMessage(
          err,
          this.translate.instant('payrollBulletin.detailLoadError')
        );
        this.toastService.error(errorMessage);
      }
    });
  }

  closeDetailModal(): void {
    this.showDetailModal.set(false);
    this.selectedDetail.set(null);
  }

  /**
   * Extract a human-readable error message from various API error shapes
   * Supports: error.error.error, error.error.message, error.message, etc.
   */
  private extractErrorMessage(error: any, defaultMessage?: string): string {
    try {
      const candidates = [
        // Priorité 1: error.error.error ou error.error.message (format backend courant)
        error?.error?.error,
        error?.error?.message,
        error?.error?.Message,
        // Priorité 2: error.error.details ou error.error.Details
        error?.error?.details,
        error?.error?.Details,
        // Priorité 3: error.message, error.Message, error à la racine
        error?.message,
        error?.Message,
        // Priorité 4: statusText HTTP
        error?.statusText,
        // Fallback
        typeof error === 'string' ? error : null
      ];

      for (const candidate of candidates) {
        if (candidate !== null && candidate !== undefined && String(candidate).trim() !== '') {
          return String(candidate).trim();
        }
      }

      // Fallback basé sur le code HTTP
      if (error?.status) {
        const status = error.status;
        if (status === 400) return 'Requête invalide. Vérifiez vos données.';
        if (status === 401) return 'Authentification requise.';
        if (status === 403) return 'Accès refusé. Vérifiez vos permissions.';
        if (status === 404) return 'Ressource non trouvée.';
        if (status === 409) return 'Conflit. Veuillez réessayer.';
        if (status === 422) return 'Données invalides. Vérifiez votre saisie.';
        if (status === 429) return 'Trop de tentatives. Veuillez réessayer plus tard.';
        if (status === 500) return 'Erreur serveur. Veuillez réessayer.';
        if (status === 503) return 'Service temporairement indisponible.';
      }

      return defaultMessage || 'Une erreur est survenue. Veuillez réessayer.';
    } catch {
      return defaultMessage || 'Une erreur est survenue. Veuillez réessayer.';
    }
  }

  /** Affiche une alerte dans une box (CSS globaux .alert, .alert-success, etc.) */
  openAlert(message: string, type: 'success' | 'error' | 'warning' | 'info' = 'info'): void {
    this.alertMessage.set(message);
    this.alertType.set(type);
    this.showAlert.set(true);
  }

  closeAlert(): void {
    this.showAlert.set(false);
  }

  /** Ouvre la boîte de confirmation ; retourne une Promise résolue par true/false. */
  openConfirm(title: string, message: string): Promise<boolean> {
    this.confirmTitle.set(title);
    this.confirmMessage.set(message);
    this.showConfirmDialog.set(true);
    return new Promise<boolean>((resolve) => {
      this.confirmResolve = resolve;
    });
  }

  onConfirmYes(): void {
    this.showConfirmDialog.set(false);
    if (this.confirmResolve) {
      this.confirmResolve(true);
      this.confirmResolve = null;
    }
  }

  onConfirmNo(): void {
    this.showConfirmDialog.set(false);
    if (this.confirmResolve) {
      this.confirmResolve(false);
      this.confirmResolve = null;
    }
  }

  async deleteResult(result: PayrollResult): Promise<void> {
    const ok = await this.openConfirm(
      this.translate.instant('payrollBulletin.confirmDeleteTitle'),
      this.translate.instant('payrollBulletin.confirmDeleteMessage', { name: result.employeeName })
    );
    if (!ok) return;

    this.payrollService.deletePayrollResult(result.id).subscribe({
      next: () => {
        this.toastService.success(this.translate.instant('payrollBulletin.deleteSuccess'));
        this.loadPayrollResults();
      },
      error: (error) => {
        console.error('Erreur lors de la suppression:', error);
        const errorMessage = this.extractErrorMessage(
          error,
          this.translate.instant('payrollBulletin.deleteError')
        );
        this.toastService.error(errorMessage);
      }
    });
  }

  async editResult(result: PayrollResult): Promise<void> {
    this.editingResult.set(result);
    this.selectedEditStatus.set(this.normalizeStatus(result.status));
    this.showEditStatusModal.set(true);
  }

  closeEditStatusModal(): void {
    this.showEditStatusModal.set(false);
    this.editingResult.set(null);
    this.selectedEditStatus.set(null);
  }

  saveStatusChange(): void {
    const result = this.editingResult();
    const status = this.selectedEditStatus();

    if (!result || !status) {
      this.toastService.warning(this.translate.instant('payrollBulletin.selectStatusRequired'));
      return;
    }

    this.calculating.set(true);
    this.payrollService.updatePayrollResultStatus(result.id, status).subscribe({
      next: () => {
        this.calculating.set(false);
        this.closeEditStatusModal();
        this.toastService.success(
          this.translate.instant('payrollBulletin.editStatusSuccess', { name: result.employeeName })
        );
        this.loadPayrollResults();
      },
      error: (error) => {
        console.error('Erreur lors de la mise à jour du statut du bulletin:', error);
        this.calculating.set(false);
        const errorMessage = this.extractErrorMessage(
          error,
          this.translate.instant('payrollBulletin.editStatusError')
        );
        this.toastService.error(errorMessage);
      }
    });
  }

  async approvePeriod(): Promise<void> {
    const ok = await this.openConfirm(
      this.translate.instant('payrollBulletin.confirmApproveTitle') || 'Verrouiller la période',
      this.translate.instant('payrollBulletin.confirmApproveMessage') || 'Voulez-vous figer définitivement les bulletins de cette période ? Cette action interdira toute modification.'
    );
    if (!ok) return;

    this.approving.set(true);
    this.payrollService.approvePeriod(
      this.selectedMonth(),
      this.selectedYear(),
      this.selectedHalf() ?? undefined
    ).subscribe({
      next: () => {
        this.approving.set(false);
        this.toastService.success(this.translate.instant('payrollBulletin.approveSuccess') || 'La période a été verrouillée avec succès.');
        this.loadPayrollResults();
      },
      error: (error) => {
        console.error('Erreur lors de l\'approbation:', error);
        this.approving.set(false);
        const errorMessage = this.extractErrorMessage(
          error,
          this.translate.instant('payrollBulletin.approveError') || 'Impossible de verrouiller la période.'
        );
        this.toastService.error(errorMessage);
      }
    });
  }
}
