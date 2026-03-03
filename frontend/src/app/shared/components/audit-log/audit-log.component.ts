import { Component, Input, OnInit, OnChanges, SimpleChanges, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TimelineModule } from 'primeng/timeline';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { MultiSelectModule } from 'primeng/multiselect';
import { SkeletonModule } from 'primeng/skeleton';
import { TooltipModule } from 'primeng/tooltip';
import { ChipModule } from 'primeng/chip';
import { AuditLogService } from '@app/core/services/audit-log.service';
import { CompanyService } from '@app/core/services/company.service';
import { 
  AuditLogDisplayItem, 
  AuditLogFilter, 
  AuditEventType
} from '@app/core/models/audit-log.model';

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    TimelineModule,
    CardModule,
    TagModule,
    ButtonModule,
    InputTextModule,
    IconFieldModule,
    InputIconModule,
    MultiSelectModule,
    SkeletonModule,
    TooltipModule,
    ChipModule
  ],
  templateUrl: './audit-log.component.html',
  styleUrl: './audit-log.component.css'
})
export class AuditLogComponent implements OnInit, OnChanges {
  private auditLogService = inject(AuditLogService);
  private companyService = inject(CompanyService);
  private translate = inject(TranslateService);

  // Inputs
  @Input() companyId?: number;
  @Input() employeeId?: number;
  @Input() maxItems?: number;

  // Signals
  readonly auditLogs = signal<AuditLogDisplayItem[]>([]);
  readonly isLoading = signal(false);
  readonly hasError = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly searchQuery = signal('');
  readonly selectedEventTypes = signal<AuditEventType[]>([]);
  readonly startDate = signal<string | null>(null);
  readonly endDate = signal<string | null>(null);
  readonly filtersExpanded = signal(false);

  // Computed
  readonly filteredLogs = computed(() => {
    let logs = this.auditLogs();
    const query = this.searchQuery().toLowerCase();
    const eventTypes = this.selectedEventTypes();
    const startDateStr = this.startDate();
    const endDateStr = this.endDate();

    // Text search
    if (query) {
      logs = logs.filter(log =>
        this.getLocalizedDescription(log).toLowerCase().includes(query) ||
        this.getLocalizedFieldName(log).toLowerCase().includes(query) ||
        log.entityName.toLowerCase().includes(query) ||
        log.actor.name.toLowerCase().includes(query)
      );
    }

    // Event type filter
    if (eventTypes.length > 0) {
      logs = logs.filter(log => eventTypes.includes(log.eventType));
    }

    // Date range filter
    if (startDateStr || endDateStr) {
      logs = logs.filter(log => {
        const logDate = new Date(log.timestamp);
        const start = startDateStr ? new Date(startDateStr) : null;
        const end = endDateStr ? new Date(endDateStr) : null;
        
        if (start && end) {
          return logDate >= start && logDate <= end;
        } else if (start) {
          return logDate >= start;
        } else if (end) {
          return logDate <= end;
        }
        return true;
      });
    }

    // Limit items if maxItems is set
    if (this.maxItems) {
      logs = logs.slice(0, this.maxItems);
    }

    return logs;
  });

  readonly hasActiveFilters = computed(() => {
    return this.searchQuery().length > 0 || 
           this.selectedEventTypes().length > 0 || 
           this.startDate() !== null || 
           this.endDate() !== null;
  });

  readonly resultCount = computed(() => this.filteredLogs().length);
  readonly totalCount = computed(() => this.auditLogs().length);

  // Event type options for filter dropdown
  readonly eventTypeOptions = [
    { labelKey: 'audit.eventTypes.COMPANY_CREATED', value: AuditEventType.COMPANY_CREATED },
    { labelKey: 'audit.eventTypes.COMPANY_UPDATED', value: AuditEventType.COMPANY_UPDATED },
    { labelKey: 'audit.eventTypes.COMPANY_DELETED', value: AuditEventType.COMPANY_DELETED },
    { labelKey: 'audit.eventTypes.EMPLOYEE_CREATED', value: AuditEventType.EMPLOYEE_CREATED },
    { labelKey: 'audit.eventTypes.EMPLOYEE_UPDATED', value: AuditEventType.EMPLOYEE_UPDATED },
    { labelKey: 'audit.eventTypes.USER_ROLE_ASSIGNED', value: AuditEventType.ROLE_ASSIGNED },
    { labelKey: 'audit.eventTypes.USER_ROLE_REVOKED', value: AuditEventType.ROLE_REVOKED }
  ];

  getEventTypeLabel(type: AuditEventType): string {
    return this.getLocalizedEventType(type);
  }

  getLocalizedEventType(type: AuditEventType): string {
    const key = `audit.eventTypes.${type}`;
    const translated = this.translate.instant(key);
    return translated !== key ? translated : type;
  }

  getLocalizedFieldName(log: AuditLogDisplayItem): string {
    const fieldName = String(log.details?.['fieldName'] ?? '').trim();
    if (!fieldName) {
      return '';
    }

    const explicitFieldKey = String(log.details?.['historyFieldKey'] ?? '').trim();
    const fieldKey = explicitFieldKey || this.normalizeFieldKey(fieldName);
    const translationKey = `audit.history.fields.${fieldKey}`;
    const translated = this.translate.instant(translationKey);

    if (translated !== translationKey) {
      return translated;
    }

    return this.formatFieldLabel(fieldName);
  }

  getLocalizedDescription(log: AuditLogDisplayItem): string {
    const action = this.getHistoryAction(log);
    if (action === 'other') {
      if (log.description === 'Unknown event') {
        return this.translate.instant('audit.unknownEvent');
      }
      return log.description;
    }

    const fieldLabel = this.getLocalizedFieldName(log);
    const actionLabel = this.translateOrFallback(`audit.history.actions.${action}`, action);
    const oldValue = String(log.details?.['oldValue'] ?? '').trim();
    const newValue = String(log.details?.['newValue'] ?? '').trim();
    const emptyLabel = this.translateOrFallback('audit.empty', 'empty');

    if (!fieldLabel) {
      return log.description;
    }

    if (oldValue || newValue) {
      return `${fieldLabel} ${actionLabel} : ${oldValue || emptyLabel} -> ${newValue || emptyLabel}`;
    }

    return `${fieldLabel} ${actionLabel}`;
  }

  ngOnInit(): void {
    this.loadAuditLogs();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if ((changes['companyId'] && !changes['companyId'].isFirstChange()) || 
        (changes['employeeId'] && !changes['employeeId'].isFirstChange())) {
      this.loadAuditLogs();
    }
  }

  loadAuditLogs(): void {
    this.isLoading.set(true);
    this.hasError.set(false);
    this.errorMessage.set(null);

    const filter: AuditLogFilter = {
      searchQuery: this.searchQuery() || undefined,
      eventTypes: this.selectedEventTypes().length > 0 ? this.selectedEventTypes() : undefined
    };

    if (this.companyId) {
      // Load company history/audit logs and attach company name for clarity
      const companyIdNum = Number(this.companyId);
      this.companyService.getManagedCompanies().subscribe({
        next: (companies) => {
          const company = companies.find(c => Number(c.id) === companyIdNum);
          const companyName = company?.legalName || String(companyIdNum);

          this.auditLogService.getCompanyAuditLogs(companyIdNum, filter).subscribe({
            next: (logs: AuditLogDisplayItem[]) => {
              const withNames = logs.map(l => ({ ...l, entityName: companyName }));
              this.auditLogs.set(withNames);
              this.isLoading.set(false);
              this.hasError.set(false);
            },
            error: (err) => {
              console.error('Failed to load company audit logs', err);
              this.auditLogs.set([]);
              this.isLoading.set(false);
              this.hasError.set(true);
              this.errorMessage.set(err.message || this.translate.instant('audit.errorMessage'));
            }
          });
        },
        error: (err) => {
          // Fallback: still load logs without company name
          this.auditLogService.getCompanyAuditLogs(companyIdNum, filter).subscribe({
            next: (logs: AuditLogDisplayItem[]) => {
              this.auditLogs.set(logs);
              this.isLoading.set(false);
              this.hasError.set(false);
            },
            error: (err2) => {
              console.error('Failed to load company audit logs', err2);
              this.auditLogs.set([]);
              this.isLoading.set(false);
              this.hasError.set(true);
              this.errorMessage.set(err2.message || this.translate.instant('audit.errorMessage'));
            }
          });
        }
      });
    } else if (this.employeeId) {
      // Load employee audit logs
      this.auditLogService.getEmployeeAuditLogs(this.employeeId, filter).subscribe({
        next: (logs) => {
          const displayItems = logs.map((log, index) => 
            this.auditLogService.convertToDisplayItem(log, 'employee', log.employeeName || 'Unknown')
          );
          this.auditLogs.set(displayItems);
          this.isLoading.set(false);
          this.hasError.set(false);
        },
        error: (err) => {
          console.error('Failed to load employee audit logs', err);
          this.auditLogs.set([]);
          this.isLoading.set(false);
          this.hasError.set(true);
          this.errorMessage.set(err.message || this.translate.instant('audit.errorMessage'));
        }
      });
    } else {
      // Load cabinet-wide audit logs if no specific ID
      this.auditLogService.getCabinetAuditLogs(filter).subscribe({
        next: (logs) => {
          this.auditLogs.set(logs);
          this.isLoading.set(false);
          this.hasError.set(false);
        },
        error: (err) => {
          console.error('Failed to load cabinet audit logs', err);
          this.auditLogs.set([]);
          this.isLoading.set(false);
          this.hasError.set(true);
          this.errorMessage.set(err.message || this.translate.instant('audit.errorMessage'));
        }
      });
    }
  }

  onSearch(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.searchQuery.set(target.value);
  }

  onEventTypeChange(types: AuditEventType[]): void {
    this.selectedEventTypes.set(types);
  }

  onStartDateChange(date: string): void {
    this.startDate.set(date);
    this.loadAuditLogs();
  }

  onEndDateChange(date: string): void {
    this.endDate.set(date);
    this.loadAuditLogs();
  }

  clearFilters(): void {
    this.searchQuery.set('');
    this.selectedEventTypes.set([]);
    this.startDate.set(null);
    this.endDate.set(null);
    this.loadAuditLogs();
  }

  refresh(): void {
    this.loadAuditLogs();
  }

  formatDate(date: Date): string {
    return new Intl.DateTimeFormat(this.getCurrentLocale(), {
      dateStyle: 'medium',
      timeStyle: 'short'
    }).format(new Date(date));
  }

  formatRelativeTime(date: Date): string {
    const now = new Date().getTime();
    const target = new Date(date).getTime();
    const diffMs = target - now;
    const diffMinutes = Math.round(diffMs / 60000);
    const diffHours = Math.round(diffMs / 3600000);
    const diffDays = Math.round(diffMs / 86400000);
    const rtf = new Intl.RelativeTimeFormat(this.getCurrentLocale(), { numeric: 'auto' });

    if (Math.abs(diffMinutes) < 1) {
      return this.translate.instant('audit.justNow');
    }
    if (Math.abs(diffMinutes) < 60) {
      return rtf.format(diffMinutes, 'minute');
    }
    if (Math.abs(diffHours) < 24) {
      return rtf.format(diffHours, 'hour');
    }
    if (Math.abs(diffDays) < 7) {
      return rtf.format(diffDays, 'day');
    }

    return this.formatDate(date);
  }

  toggleFilters(): void {
    this.filtersExpanded.update(v => !v);
  }

  removeFilter(type: 'search' | 'eventType' | 'startDate' | 'endDate', value?: any): void {
    switch (type) {
      case 'search':
        this.searchQuery.set('');
        break;
      case 'eventType':
        this.selectedEventTypes.set([]);
        break;
      case 'startDate':
        this.startDate.set(null);
        this.loadAuditLogs();
        break;
      case 'endDate':
        this.endDate.set(null);
        this.loadAuditLogs();
        break;
    }
  }

  getSeverityClass(severity: string): string {
    const classes: Record<string, string> = {
      success: 'text-green-600 bg-green-50 border-green-200',
      info: 'text-blue-600 bg-blue-50 border-blue-200',
      warn: 'text-amber-600 bg-amber-50 border-amber-200',
      danger: 'text-red-600 bg-red-50 border-red-200'
    };
    return classes[severity] || classes['info'];
  }

  private getHistoryAction(log: AuditLogDisplayItem): 'changed' | 'created' | 'updated' | 'deleted' | 'other' {
    const explicitAction = String(log.details?.['historyAction'] ?? '').trim().toLowerCase();
    if (explicitAction === 'changed' || explicitAction === 'created' || explicitAction === 'updated' || explicitAction === 'deleted') {
      return explicitAction;
    }

    const title = String(log.details?.['historyTitle'] ?? '').toLowerCase();
    if (title.includes('_changed')) return 'changed';
    if (title.includes('_created')) return 'created';
    if (title.includes('_updated')) return 'updated';
    if (title.includes('_deleted')) return 'deleted';

    if (log.eventType === AuditEventType.COMPANY_CREATED || log.eventType === AuditEventType.EMPLOYEE_CREATED) {
      return 'created';
    }
    if (log.eventType === AuditEventType.COMPANY_DELETED || log.eventType === AuditEventType.EMPLOYEE_DELETED) {
      return 'deleted';
    }
    if (log.eventType === AuditEventType.COMPANY_UPDATED || log.eventType === AuditEventType.EMPLOYEE_UPDATED) {
      return 'changed';
    }

    return 'other';
  }

  private normalizeFieldKey(value: string): string {
    return value
      .replace(/([a-z0-9])([A-Z])/g, '$1_$2')
      .replace(/[^\w]+/g, '_')
      .replace(/_+/g, '_')
      .replace(/^_+|_+$/g, '')
      .toLowerCase()
      .replace('departement', 'department');
  }

  private formatFieldLabel(value: string): string {
    return value
      .replace(/_/g, ' ')
      .replace(/([A-Z])/g, ' $1')
      .trim()
      .replace(/\s+/g, ' ')
      .replace(/^\w/, c => c.toUpperCase());
  }

  private translateOrFallback(key: string, fallback: string): string {
    const translated = this.translate.instant(key);
    return translated !== key ? translated : fallback;
  }

  private getCurrentLocale(): string {
    const lang = (this.translate.currentLang || this.translate.getDefaultLang() || 'en').toLowerCase();
    if (lang.startsWith('fr')) return 'fr-FR';
    if (lang.startsWith('ar')) return 'ar-MA';
    return 'en-US';
  }
}
