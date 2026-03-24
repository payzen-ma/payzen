import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PayrollService } from '@app/core/services/payroll.service';
import { EmployeeService } from '@app/core/services/employee.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { 
  PayrollResult, 
  PayrollResultStatus, 
  PayrollFilters,
  PayrollDetail 
} from '@app/core/models/payroll.model';
import { Employee } from '@app/core/services/employee.service';
import { SelectComponent, SelectOption } from '@app/shared/ui/select/select.component';
import { ButtonComponent } from '@app/shared/ui/button/button.component';
import { BadgeComponent } from '@app/shared/ui/badge/badge.component';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

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
    DialogModule
  ],
  templateUrl: './bulletin.component.html',
  styleUrls: ['./bulletin.component.css']
})
export class BulletinComponent implements OnInit {
  private readonly payrollService = inject(PayrollService);
  private readonly employeeService = inject(EmployeeService);
  private readonly contextService = inject(CompanyContextService);
  private readonly translate = inject(TranslateService);

  // État
  readonly loading = signal(false);
  readonly calculating = signal(false);
  readonly employees = signal<Employee[]>([]);
  readonly payrollResults = signal<PayrollResult[]>([]);

  // Modal détail bulletin
  readonly showDetailModal = signal(false);
  readonly detailLoading = signal(false);
  readonly selectedDetail = signal<PayrollDetail | null>(null);

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
      totalNet: results.reduce((sum, r) => sum + (r.totalNet || 0), 0)
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
      { label: this.translate.instant('payrollBulletin.statusPending'), value: PayrollResultStatus.PENDING }
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
          console.log('Calcul terminé:', response);
          this.calculating.set(false);
          this.openAlert(this.translate.instant('payrollBulletin.calculationSuccessFor', { month: response.month, year: response.year }), 'success');
          this.loadPayrollResults();
        },
        error: (error) => {
          console.error('Erreur lors du calcul:', error);
          this.calculating.set(false);
          const errorMessage = error.error?.error || error.message || this.translate.instant('payrollBulletin.calculationError');
          this.openAlert(errorMessage, 'error');
        }
      });
    } catch (error: any) {
      console.error('Erreur:', error);
      this.calculating.set(false);
      this.openAlert(error?.message || this.translate.instant('payrollBulletin.calculationError'), 'error');
    }
  }

  calculateForEmployee(): void {
    const employeeId = this.selectedEmployee();
    if (!employeeId) {
      this.openAlert(this.translate.instant('payrollBulletin.selectEmployeeRequired'), 'warning');
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
        console.log('Calcul terminé:', response);
        this.calculating.set(false);
        this.openAlert(this.translate.instant('payrollBulletin.calculationSuccess'), 'success');
        this.loadPayrollResults();
      },
      error: (error) => {
        console.error('Erreur lors du calcul:', error);
        this.calculating.set(false);
        this.openAlert(this.translate.instant('payrollBulletin.calculationError'), 'error');
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
      default:
        return status;
    }
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
        this.openAlert(this.translate.instant('payrollBulletin.detailLoadError'), 'error');
      }
    });
  }

  closeDetailModal(): void {
    this.showDetailModal.set(false);
    this.selectedDetail.set(null);
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
        this.openAlert(this.translate.instant('payrollBulletin.deleteSuccess'), 'success');
        this.loadPayrollResults();
      },
      error: (error) => {
        console.error('Erreur lors de la suppression:', error);
        this.openAlert(this.translate.instant('payrollBulletin.deleteError'), 'error');
      }
    });
  }
}
