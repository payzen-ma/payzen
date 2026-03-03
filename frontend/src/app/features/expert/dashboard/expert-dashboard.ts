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
import { CompanyService } from '@app/core/services/company.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { DashboardService } from '@app/core/services/dashboard.service';
import { Company } from '@app/core/models/company.model';
import { AuditLogComponent } from '../../../shared/components/audit-log/audit-log.component';
import { Subject, takeUntil, forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';

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
    AuditLogComponent,
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
  private destroy$ = new Subject<void>();

  // Signals
  readonly companies = signal<Company[]>([]);
  readonly isLoading = signal<boolean>(true);
  readonly searchQuery = signal<string>('');
  readonly pendingLeaves = signal<number>(0);
  readonly totalClients = signal<number>(0);
  readonly globalEmployeeCount = signal<number>(0);
  
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
        console.log('[ExpertDashboard] mapped companies:', companies);
        console.debug('[ExpertDashboard] mapped companies:', companies);

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
              console.debug('[ExpertDashboard] updated companies with counts:', this.companies());
            },
            error: (err: any) => console.warn('Failed fetching per-company counts', err)
          });
        }
        // Do not auto-select a company so the cabinet-wide audit log is shown by default
        console.debug('[ExpertDashboard] selectedCompany after load:', this.selectedCompany());
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load managed companies', err);
        this.isLoading.set(false);
      }
    });
  }

  loadDashboardSummary(): void {
    this.dashboardService.getDashboardSummary().subscribe({
      next: (summary) => {
        console.log('[ExpertDashboard] expert summary mapped:', summary);
        this.totalClients.set(summary.totalCompanies);
        this.globalEmployeeCount.set(summary.totalEmployees);
          console.log('[ExpertDashboard] globalEmployeeCount after set:', this.globalEmployeeCount(), 'computed totalEmployees():', this.totalEmployees());
      },
      error: (err) => {
        console.error('Failed to load dashboard summary', err);
      }
    });
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
