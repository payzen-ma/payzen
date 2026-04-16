import { Component, OnInit, OnDestroy, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { InputTextModule } from 'primeng/inputtext';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { TooltipModule } from 'primeng/tooltip';
import { ChartModule } from 'primeng/chart';
import { CompanyService } from '@app/core/services/company.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { DashboardService } from '@app/core/services/dashboard.service';
import { EmployeeService } from '@app/core/services/employee.service';
import { Company } from '@app/core/models/company.model';
import { Subject, takeUntil, forkJoin, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';

import { DialogModule } from 'primeng/dialog';
import { ClientFormComponent } from '../components/client-form/client-form.component';

@Component({
  selector: 'app-expert-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    TranslateModule,
    TableModule,
    ButtonModule,
    TagModule,
    InputTextModule,
    IconFieldModule,
    InputIconModule,
    TooltipModule,
    ChartModule,
    DialogModule,
    ClientFormComponent
  ],
  templateUrl: './expert-dashboard.html',
  styleUrl: './expert-dashboard.css'
})
export class ExpertDashboard implements OnInit, OnDestroy {
  private companyService = inject(CompanyService);
  private contextService = inject(CompanyContextService);
  private dashboardService = inject(DashboardService);
  private employeeService = inject(EmployeeService);
  private destroy$ = new Subject<void>();
  private readonly eligibleEmployeeStatuses = new Set(['active', 'on_leave']);
  private employeesCountRequestId = 0;

  // Signals
  readonly companies = signal<Company[]>([]);
  readonly isLoading = signal<boolean>(true);
  readonly searchQuery = signal<string>('');
  readonly pendingLeaves = signal<number>(0);
  readonly totalClients = signal<number>(0);
  readonly globalEmployeeCount = signal<number>(0);
  readonly employeesByCompany = signal<Record<string, number>>({});
  readonly employeesScopeLabel = signal<string>('Actifs + en conge');

  // Dialog state
  readonly isClientFormVisible = signal<boolean>(false);
  readonly clientFormMode = signal<'create' | 'edit'>('create');
  readonly selectedCompanyForEdit = signal<Company | undefined>(undefined);
  readonly selectedCompany = signal<Company | null>(null);
  readonly selectedCompanyId = computed(() => {
    const sc = this.selectedCompany();
    return sc ? Number(sc.id) : undefined;
  });

  // Computed
  readonly totalEmployees = computed(() =>
    this.companies().reduce((acc, curr) => acc + (curr.employeeCount || 0), 0)
  );
  readonly employeeKpiCount = computed(() => this.globalEmployeeCount());
  readonly employeesByClientChartData = computed(() => {
    const companies = this.companies();
    const byCompany = this.employeesByCompany();
    const labels = companies.map(c => c.legalName);
    const values = companies.map(c => byCompany[c.id] ?? 0);

    return {
      labels,
      datasets: [
        {
          label: 'Employes actifs + en conge',
          data: values,
          backgroundColor: '#3b82f6',
          borderRadius: 6,
          maxBarThickness: 42
        }
      ]
    };
  });
  readonly employeesByClientChartOptions = computed(() => ({
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: false
      }
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: {
          precision: 0
        }
      }
    }
  }));
  readonly clientsByCityChartData = computed(() => {
    const companies = this.companies();
    const cityCount = companies.reduce((acc, company) => {
      const city = (company.city || 'Non renseignee').trim();
      acc[city] = (acc[city] || 0) + 1;
      return acc;
    }, {} as Record<string, number>);

    const labels = Object.keys(cityCount);
    const data = labels.map(label => cityCount[label]);

    return {
      labels,
      datasets: [
        {
          data,
          backgroundColor: ['#2563eb', '#16a34a', '#f59e0b', '#7c3aed', '#ef4444', '#0ea5e9', '#14b8a6']
        }
      ]
    };
  });
  readonly clientsByCityChartOptions = computed(() => ({
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'bottom'
      }
    }
  }));
  readonly activeClientsCount = computed(() => this.companies().filter(c => c.isActive).length);
  readonly avgEmployeesPerClient = computed(() => {
    const clients = this.totalClients() || this.companies().length;
    const employees = this.employeeKpiCount();
    return clients > 0 ? Math.round((employees / clients) * 10) / 10 : 0;
  });
  readonly clientCoverageRate = computed(() => {
    const total = this.totalClients() || this.companies().length;
    const active = this.activeClientsCount();
    return total > 0 ? Math.round((active / total) * 100) : 0;
  });

  ngOnInit(): void {
    this.loadPortfolioDashboard();

    // Subscribe to context changes
    this.contextService.contextChanged$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.loadPortfolioDashboard();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadPortfolioDashboard(): void {
    this.loadClientCompanies();
    // Also load expert summary (total employees across managed clients)
    this.loadDashboardSummary();
  }

  loadClientCompanies(): void {
    this.isLoading.set(true);

    this.companyService.getManagedCompanies().subscribe({
      next: (companies) => {
        this.companies.set(companies);
        this.loadEligibleEmployeeCount(companies);

        // Try to fetch per-company employee counts if backend didn't provide them
        const missingCounts = companies.filter(c => !c.employeeCount || c.employeeCount === 0).map(c => c.id);
        if (missingCounts.length > 0) {
          const requests = missingCounts.map(id =>
            this.companyService.getCompanyEmployeeCount(id).pipe(
              map(count => ({ id, count }))
            )
          );

          forkJoin(requests).subscribe({
            next: (results: Array<{ id: string; count: number }>) => {
              const companiesArr = this.companies();
              results.forEach((r: { id: string; count: number }) => {
                const idx = companiesArr.findIndex(x => x.id === r.id);
                if (idx >= 0) {
                  companiesArr[idx].employeeCount = r.count;
                }
              });
              this.companies.set(companiesArr);
            },
            error: (err: any) => alert('Failed to load employee counts for some companies')
          });
        }
        // Do not auto-select a company so the cabinet-wide audit log is shown by default
        this.isLoading.set(false);
      },
      error: (err) => {
        alert('Failed to load managed companies');
        this.isLoading.set(false);
      }
    });
  }

  loadDashboardSummary(): void {
    this.dashboardService.getDashboardSummary().subscribe({
      next: (summary) => {
        this.totalClients.set(summary.totalCompanies);
      },
      error: (err) => {
        alert('Failed to load dashboard summary');
      }
    });
  }

  private loadEligibleEmployeeCount(companies: Company[]): void {
    const requestId = ++this.employeesCountRequestId;
    if (!companies.length) {
      this.globalEmployeeCount.set(0);
      this.employeesByCompany.set({});
      return;
    }

    const requests = companies.map(company =>
      this.employeeService.getEmployees({ companyId: company.id, limit: 1000 }).pipe(
        map(response => response.employees.filter(employee => this.eligibleEmployeeStatuses.has(employee.status || '')).length),
        catchError(() => of(0))
      )
    );

    forkJoin(requests).subscribe({
      next: counts => {
        if (requestId !== this.employeesCountRequestId) {
          return;
        }
        const byCompany = companies.reduce((acc, company, index) => {
          acc[company.id] = counts[index] ?? 0;
          return acc;
        }, {} as Record<string, number>);
        this.employeesByCompany.set(byCompany);
        const total = counts.reduce((sum, current) => sum + current, 0);
        this.globalEmployeeCount.set(total);
      },
      error: () => {
        if (requestId !== this.employeesCountRequestId) {
          return;
        }
        this.employeesByCompany.set({});
        this.globalEmployeeCount.set(0);
      }
    });
  }

  getEligibleEmployeeCount(companyId: string): number {
    return this.employeesByCompany()[companyId] ?? 0;
  }

  onSelectCompany(company: Company): void {
    // Set selected company for audit log view and switch context to client
    this.selectedCompany.set(company);
    this.contextService.switchToClientContext(company, true);
  }

  getMissingDocsCount(company: Company): number {
    return 0;
  }

  getLastPayrollStatus(company: Company): 'validated' | 'pending' | 'late' {
    return 'pending';
  }

  getSeverity(status: boolean): 'success' | 'danger' {
    return status ? 'success' : 'danger';
  }

  getStatusLabel(status: boolean): string {
    return status ? 'Active' : 'Inactive';
  }

  onSearch(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.searchQuery.set(target.value);
  }

  openCreateClient(): void {
    this.clientFormMode.set('create');
    this.selectedCompanyForEdit.set(undefined);
    this.isClientFormVisible.set(true);
  }

  openEditClient(company: Company): void {
    this.clientFormMode.set('edit');
    this.selectedCompanyForEdit.set(company);
    this.isClientFormVisible.set(true);
  }

  onClientSaved(): void {
    this.isClientFormVisible.set(false);
    this.loadClientCompanies(); // Refresh list
  }
}
