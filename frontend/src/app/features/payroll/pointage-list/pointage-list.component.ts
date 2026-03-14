import { Component, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '@environments/environment';
import { CompanyContextService } from '@app/core/services/companyContext.service';

interface EmployeeAttendance {
  id: number;
  employeeId: number;
  employeeName?: string;
  workDate: string;
  checkIn?: string | null;
  checkOut?: string | null;
  workedHours: number;
  breakMinutesApplied?: number;
  status?: 'Present' | 'Absent' | 'Holiday' | 'Leave';
  source?: 'System' | 'Manual';
  matricule?: string | number;
}

@Component({
  selector: 'app-pointage-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pointage-list.component.html',
  styleUrl: './pointage-list.component.scss',
})
export class PointageListComponent {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly contextService = inject(CompanyContextService);

  // État
  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly timesheets = signal<EmployeeAttendance[]>([]);

  // Filtres
  readonly selectedMonth = signal<number>(new Date().getMonth() + 1);
  readonly selectedYear = signal<number>(new Date().getFullYear());
  readonly selectedEmployeeId = signal<number | null>(null);

  readonly months = Array.from({ length: 12 }, (_, i) => i + 1);
  readonly years = Array.from({ length: 6 }, (_, i) => new Date().getFullYear() - 2 + i);
  readonly monthsNames = ['Janvier','Février','Mars','Avril','Mai','Juin','Juillet','Août','Septembre','Octobre','Novembre','Décembre'];

  // expose Math to the template (Angular template can't access global Math directly)
  readonly Math = Math;

  // Available employees derived from loaded timesheets
  readonly employees = computed(() => {
    const map = new Map<number, { id: number; name: string; matricule?: string | number }>();
    (this.timesheets() || []).forEach(t => {
      if (!map.has(t.employeeId)) {
        map.set(t.employeeId, { id: t.employeeId, name: t.employeeName || `Employé #${t.employeeId}`, matricule: t.matricule });
      }
    });
    return Array.from(map.values()).sort((a, b) => String(a.name).localeCompare(String(b.name)));
  });

  // Search query used to filter the displayed rows (not the select)
  readonly employeeSearch = signal<string>('');

  setEmployeeSearch(q: string): void {
    this.employeeSearch.set(q);
    this.firstPage.set(1);
    this.secondPage.set(1);
  }

  // Flat list used for the simplified table view (applies employee filter)
  readonly displayRows = computed(() => {
    const list = this.timesheets() || [];
    const sel = this.selectedEmployeeId();
    let filtered = sel != null ? list.filter(r => r.employeeId === sel) : list;
    const q = (this.employeeSearch() || '').trim().toLowerCase();
    if (q) {
      filtered = filtered.filter(r => {
        const name = String(r.employeeName || '').toLowerCase();
        const matricule = String(r.matricule || '').toLowerCase();
        const id = String(r.employeeId || '').toLowerCase();
        const date = String(r.workDate || '').toLowerCase();
        return name.includes(q) || matricule.includes(q) || id === q || date.includes(q);
      });
    }
    // sort descending by date
    return filtered.slice().sort((a, b) => new Date(b.workDate).getTime() - new Date(a.workDate).getTime());
  });

  // Pagination & split by half-month
  readonly pageSize = signal<number>(10);
  readonly firstPage = signal<number>(1);
  readonly secondPage = signal<number>(1);

  readonly firstHalfAll = computed(() => this.displayRows().filter(r => new Date(r.workDate).getDate() <= 15));
  readonly secondHalfAll = computed(() => this.displayRows().filter(r => new Date(r.workDate).getDate() >= 16));

  readonly firstTotalPages = computed(() => Math.max(1, Math.ceil(this.firstHalfAll().length / this.pageSize())));
  readonly secondTotalPages = computed(() => Math.max(1, Math.ceil(this.secondHalfAll().length / this.pageSize())));

  readonly firstPages = computed(() => Array.from({ length: this.firstTotalPages() }, (_, i) => i + 1));
  readonly secondPages = computed(() => Array.from({ length: this.secondTotalPages() }, (_, i) => i + 1));

  readonly firstPageRows = computed(() => {
    const p = this.firstPage();
    const size = this.pageSize();
    const start = (p - 1) * size;
    return this.firstHalfAll().slice(start, start + size);
  });

  readonly secondPageRows = computed(() => {
    const p = this.secondPage();
    const size = this.pageSize();
    const start = (p - 1) * size;
    return this.secondHalfAll().slice(start, start + size);
  });

  setFirstPage(p: number) { this.firstPage.set(p); }
  setSecondPage(p: number) { this.secondPage.set(p); }
  prevFirst() { if (this.firstPage() > 1) this.firstPage.set(this.firstPage() - 1); }
  nextFirst() { if (this.firstPage() < this.firstTotalPages()) this.firstPage.set(this.firstPage() + 1); }
  prevSecond() { if (this.secondPage() > 1) this.secondPage.set(this.secondPage() - 1); }
  nextSecond() { if (this.secondPage() < this.secondTotalPages()) this.secondPage.set(this.secondPage() + 1); }
  setPageSize(size: number) { this.pageSize.set(Number(size)); this.firstPage.set(1); this.secondPage.set(1); }

  constructor() {
    this.loadTimesheets();
  }

  loadTimesheets(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    const params = new URLSearchParams();
    params.set('month', String(this.selectedMonth()));
    params.set('year', String(this.selectedYear()));

    const contextCompanyId = this.contextService.companyId();
    const companyId = contextCompanyId ? parseInt(String(contextCompanyId)) : undefined;
    if (companyId) {
      params.set('companyId', companyId.toString());
    }

    const url = `${environment.apiUrl}/timesheets?${params.toString()}`;

    this.http.get<EmployeeAttendance[]>(url).subscribe({
      next: (data) => {
        this.timesheets.set(data);
        this.isLoading.set(false);
      },
      error: (err) => {
        const msg =
          err?.error?.Message ||
          err?.error?.message ||
          'Erreur lors du chargement des pointages.';
        this.errorMessage.set(msg);
        this.isLoading.set(false);
      }
    });
  }

  onFilterChange(): void {
    this.firstPage.set(1);
    this.secondPage.set(1);
    this.loadTimesheets();
  }

  setSelectedEmployee(id: number | null): void {
    this.selectedEmployeeId.set(id);
    this.firstPage.set(1);
    this.secondPage.set(1);
  }

  goToImport(): void {
    this.router.navigate(['/payroll/pointage-import']);
  }

  getStatusLabel(status: string): string {
    const labels: Record<string, string> = {
      Present: 'Présent',
      Absent: 'Absent',
      Holiday: 'Férié',
      Leave: 'Congé'
    };
    return labels[status] || status;
  }

  getSourceLabel(source: string): string {
    const labels: Record<string, string> = {
      System: 'Système',
      Manual: 'Manuel'
    };
    return labels[source] || source;
  }

  getStatusClass(status: string): string {
    const classes: Record<string, string> = {
      Present: 'bg-success text-white',
      Absent: 'bg-danger text-white',
      Holiday: 'bg-secondary text-white',
      Leave: 'bg-warning text-dark'
    };
    return classes[status] || '';
  }

  formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', {
      weekday: 'short',
      day: '2-digit',
      month: '2-digit'
    });
  }

  formatTime(timeStr: string | null): string {
    if (!timeStr) return '--:--';
    return timeStr.substring(0, 5); // HH:mm
  }
}
