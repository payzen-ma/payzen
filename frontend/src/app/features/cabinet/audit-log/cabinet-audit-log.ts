import { Component, OnInit, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { TableModule } from 'primeng/table';
import { TimelineModule } from 'primeng/timeline';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { SelectButtonModule } from 'primeng/selectbutton';
import { MultiSelectModule } from 'primeng/multiselect';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { TooltipModule } from 'primeng/tooltip';
import { SkeletonModule } from 'primeng/skeleton';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';

import { AuditLogService } from '@app/core/services/audit-log.service';
import { CompanyService } from '@app/core/services/company.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { AuditEventType, AuditLogDisplayItem } from '@app/core/models/audit-log.model';
import { Company } from '@app/core/models/company.model';

@Component({
  selector: 'app-cabinet-audit-log',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    TableModule,
    TimelineModule,
    ButtonModule,
    DatePickerModule,
    SelectModule,
    SelectButtonModule,
    MultiSelectModule,
    TagModule,
    DialogModule,
    TooltipModule,
    SkeletonModule,
    CardModule,
    InputTextModule,
    IconFieldModule,
    InputIconModule
  ],
  templateUrl: './cabinet-audit-log.html',
  styleUrl: './cabinet-audit-log.css'
})
export class CabinetAuditLogComponent implements OnInit {
  private auditLogService = inject(AuditLogService);
  private companyService = inject(CompanyService);
  public contextService = inject(CompanyContextService);

  // Signals
  readonly logs = signal<AuditLogDisplayItem[]>([]);
  readonly isLoading = signal<boolean>(true);
  readonly viewMode = signal<'timeline' | 'table'>('timeline');
  
  // Filters
  readonly dateRange = signal<Date[] | null>(null);
  readonly selectedEventTypes = signal<AuditEventType[]>([]);
  readonly selectedCompany = signal<Company | null>(null);
  readonly searchQuery = signal<string>('');

  // Options
  readonly eventTypes = Object.values(AuditEventType).map(type => ({ label: type, value: type }));
  readonly companies = signal<Company[]>([]);

  // Detail Modal
  readonly showDetailModal = signal<boolean>(false);
  readonly selectedLog = signal<AuditLogDisplayItem | null>(null);

  // Computed Filtered Logs
  readonly filteredLogs = computed(() => {
    let items = this.logs();
    const query = this.searchQuery().toLowerCase();
    const types = this.selectedEventTypes();
    const company = this.selectedCompany();
    const dates = this.dateRange();
    const role = this.contextService.role();

    // Relay Restriction: Filter logs if user is not Owner
    // Assuming Relay users only see logs for companies they have access to
    // For now, we'll simulate this by filtering out "Cabinet" level events if not Owner
    if (role !== 'Owner') {
       items = items.filter(log => log.entityType !== 'Cabinet');
    }

    if (query) {
      items = items.filter(log => 
        log.description.toLowerCase().includes(query) || 
        log.actor.name.toLowerCase().includes(query) ||
        log.entityName.toLowerCase().includes(query)
      );
    }

    if (types.length > 0) {
      items = items.filter(log => types.includes(log.eventType));
    }

    if (company) {
      // Assuming log has companyId or we filter by entityName matching company name
      // Ideally AuditLogDisplayItem should have companyId
      items = items.filter(log => log.entityName === company.legalName); 
    }

    if (dates && dates.length === 2 && dates[0] && dates[1]) {
      const start = dates[0];
      const end = dates[1];
      items = items.filter(log => {
        const logDate = new Date(log.timestamp);
        return logDate >= start && logDate <= end;
      });
    }

    return items;
  });

  ngOnInit(): void {
    this.loadData();
    this.loadCompanies();
  }

  loadData(): void {
    this.isLoading.set(true);
    
    this.auditLogService.getCabinetAuditLogs().subscribe({
      next: (logs) => {
        this.logs.set(logs);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load audit logs', err);
        this.isLoading.set(false);
      }
    });
  }

  loadCompanies(): void {
    this.companyService.getManagedCompanies().subscribe(companies => {
      this.companies.set(companies);
    });
  }

  viewDetails(log: AuditLogDisplayItem): void {
    this.selectedLog.set(log);
    this.showDetailModal.set(true);
  }

  exportLogs(): void {
    // Implement export logic (CSV/Excel)
    console.log('Exporting logs...');
  }

  getSeverity(severity: string): "success" | "info" | "warn" | "danger" | "secondary" | "contrast" | undefined {
    return severity as any;
  }
}
