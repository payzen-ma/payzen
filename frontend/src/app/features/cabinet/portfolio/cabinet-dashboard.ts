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
import { SkeletonModule } from 'primeng/skeleton';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { MultiSelectModule } from 'primeng/multiselect';
import { CardModule } from 'primeng/card';
import { FormsModule } from '@angular/forms';

import { CompanyService } from '@app/core/services/company.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { Company } from '@app/core/models/company.model';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-cabinet-dashboard',
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
    SkeletonModule,
    MessageModule,
    SelectModule,
    MultiSelectModule,
    CardModule,
    FormsModule
  ],
  templateUrl: './cabinet-dashboard.html',
  styleUrl: './cabinet-dashboard.css'
})
export class CabinetDashboard implements OnInit, OnDestroy {
  private companyService = inject(CompanyService);
  private contextService = inject(CompanyContextService);
  private destroy$ = new Subject<void>();

  // Signals
  readonly companies = signal<Company[]>([]);
  readonly isLoading = signal<boolean>(true);
  readonly error = signal<string | null>(null);
  
  // Filter Signals
  readonly globalFilter = signal<string>('');
  readonly statusFilter = signal<string | null>(null);
  readonly validationFilter = signal<string[]>([]);

  // KPI Signals
  readonly totalCompanies = computed(() => this.companies().length);
  readonly activeCompanies = computed(() => 
    this.companies().filter(c => !c.status || c.status === 'active').length
  );
  readonly syncIssues = signal<number>(0); // Placeholder for now

  // Options for filters
  readonly statusOptions = [
    { label: 'Active', value: 'active' },
    { label: 'Suspended', value: 'suspended' },
    { label: 'Pending', value: 'pending' }
  ];

  readonly validationOptions = [
    { label: 'Validated', value: 'validated' },
    { label: 'Pending', value: 'pending' },
    { label: 'Rejected', value: 'rejected' }
  ];

  ngOnInit(): void {
    this.loadPortfolio();

    // Subscribe to context changes
    this.contextService.contextChanged$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.loadPortfolio();
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadPortfolio(): void {
    this.isLoading.set(true);
    this.error.set(null);
    
    this.companyService.getManagedCompanies().subscribe({
      next: (companies) => {
        // Mocking some data fields that might be missing from the API for now
        const enrichedCompanies = companies.map(c => ({
          ...c,
          status: c.status || 'active',
          validationStatus: 'validated', // Mock
          lastSync: new Date(), // Mock
          lastActivity: new Date(), // Mock
          ownerName: 'John Doe' // Mock
        }));
        this.companies.set(enrichedCompanies);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load portfolio', err);
        this.error.set('Unable to load portfolio. Please try again later.');
        this.isLoading.set(false);
      }
    });
  }

  onRowClick(company: Company): void {
    if (company.id) {
      this.contextService.switchContext(company.id);
    }
  }

  clearFilters(table: any): void {
    this.globalFilter.set('');
    this.statusFilter.set(null);
    this.validationFilter.set([]);
    table.clear();
  }

  getSeverity(status: string): "success" | "info" | "warn" | "danger" | "secondary" | "contrast" | undefined {
    switch (status) {
      case 'active':
        return 'success';
      case 'suspended':
        return 'danger';
      case 'pending':
        return 'warn';
      default:
        return 'info';
    }
  }
  
  getValidationSeverity(status: string): "success" | "info" | "warn" | "danger" | "secondary" | "contrast" | undefined {
    switch (status) {
      case 'validated':
        return 'success';
      case 'pending':
        return 'warn';
      case 'rejected':
        return 'danger';
      default:
        return 'info';
    }
  }
}
