import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EventLogService } from '../../services/event-log.service';

interface EventItem {
  // API fields (PascalCase mapped to camelCase)
  id: number;
  source?: string | null; // e.g. 'company' | 'employee'
  eventName: string;
  oldValue?: string | null;
  oldValueId?: number | null;
  newValue?: string | null;
  newValueId?: number | null;
  createdAt: string;
  createdBy?: number | null;
  companyId?: number | null;
  employeeId?: number | null;
  creatorFullName?: string | null;
  companyName?: string | null;
  employeeFullName?: string | null;

  // convenience/backward-compatible fields
  date?: string;
  actorId?: number | null;
  type?: string;
  targetType?: 'company' | 'employee';
}

@Component({
  selector: 'app-event-log',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './event-log.component.html'
})
export class EventLogComponent implements OnInit {
  activeTab: 'company' | 'employee' = 'company';

  // Filters
  search = '';
  fromDate: string | null = null;
  toDate: string | null = null;
  eventType: string | null = null;

  // will be filled from API EventName values
  eventTypes: string[] = [];

  events: EventItem[] = [];
  isLoading = false;
  error: string | null = null;
  totalCount: number | null = null;

  get filteredCompanyEvents() {
    return this.events
      .filter(e => e.targetType === 'company')
      .filter(e => this.applyFilters(e));
  }

  get filteredEmployeeEvents() {
    return this.events
      .filter(e => e.targetType === 'employee')
      .filter(e => this.applyFilters(e));
  }

  applyFilters(e: EventItem) {
    if (this.eventType && e.type !== this.eventType) return false;
    if (this.search) {
      const q = this.search.toLowerCase();
      const hay = [
        String(e.actorId || ''),
        String(e.type || ''),
        String(e.oldValue || ''),
        String(e.newValue || ''),
        String(e.companyId || ''),
        String(e.employeeId || '')
      ].join(' ').toLowerCase();
      if (!hay.includes(q)) return false;
    }
    const dateStr = (e.date ?? e.createdAt) as string | undefined;
    if (this.fromDate && dateStr) {
      const from = new Date(this.fromDate);
      if (new Date(dateStr) < from) return false;
    }
    if (this.toDate && dateStr) {
      const to = new Date(this.toDate);
      if (new Date(dateStr) > to) return false;
    }
    return true;
  }

  setTab(tab: 'company' | 'employee') {
    this.activeTab = tab;
  }

  constructor(private eventLogService: EventLogService) { }

  ngOnInit(): void {
    this.loadEvents();
  }

  loadEvents(filters?: any) {
    this.isLoading = true;
    this.error = null;
    const serverFilters = filters ?? {
      source: this.activeTab,
      eventName: this.eventType || undefined,
      fromDate: this.fromDate || undefined,
      toDate: this.toDate || undefined
    };

    // Fetch raw to read Count if available
    this.eventLogService.getEventsRaw(serverFilters).subscribe({
      next: (resp) => {
        try {
          const body = resp.body as any;
          this.totalCount = body?.Count ?? body?.count ?? null;
        } catch (e) {
          this.totalCount = null;
        }
      },
      error: (err: any) => {
      }
    });

    this.eventLogService.getEvents(serverFilters).subscribe({
      next: (rows: any[]) => {
        this.events = (rows || []).map((r: any) => {
          const src = r.Source || r.source || null;
          const companyId = r.CompanyId ?? r.companyId ?? null;
          const employeeId = r.EmployeeId ?? r.employeeId ?? null;
          const targetType = src && typeof src.toLowerCase === 'function'
            ? (src.toLowerCase() === 'company' ? 'company' : 'employee')
            : (companyId != null ? 'company' : (employeeId != null ? 'employee' : 'company'));

          return {
            id: r.Id || r.id,
            source: r.Source ?? r.source ?? null,
            eventName: r.EventName || r.eventName || r.Type || 'OTHER',
            oldValue: r.OldValue ?? r.oldValue ?? null,
            oldValueId: r.OldValueId ?? r.oldValueId ?? null,
            newValue: r.NewValue ?? r.newValue ?? null,
            newValueId: r.NewValueId ?? r.newValueId ?? null,
            createdAt: r.CreatedAt || r.createdAt || r.Date || new Date().toISOString(),
            createdBy: r.CreatedBy ?? r.createdBy ?? null,
            creatorFullName: r.CreatorFullName ?? r.creatorFullName ?? null,
            companyId,
            employeeId,
            companyName: r.CompanyName ?? r.companyName ?? null,
            employeeFullName: r.EmployeeFullName ?? r.employeeFullName ?? null,
            // convenience aliases
            date: r.CreatedAt || r.createdAt || r.Date || new Date().toISOString(),
            actorId: r.CreatedBy ?? r.createdBy ?? null,
            type: r.EventName || r.eventName || r.Type || 'OTHER',
            targetType
          } as EventItem;
        });
        // Populate eventTypes from API distinct eventName values
        const names = new Set<string>();
        for (const row of (rows || [])) {
          const n = row.EventName || row.eventName || row.Type;
          if (n) names.add(n);
        }
        this.eventTypes = Array.from(names).sort();
        this.isLoading = false;
      },
      error: (err: any) => {
        this.error = 'Impossible de charger les logs.';
        this.isLoading = false;
      }
    });
  }

  applyServerFilters() {
    this.loadEvents();
  }

  resetFilters() {
    this.search = '';
    this.fromDate = null;
    this.toDate = null;
    this.eventType = null;
    this.totalCount = null;
    this.loadEvents();
  }
}
