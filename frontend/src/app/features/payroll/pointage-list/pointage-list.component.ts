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

  readonly isLoading = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly timesheets = signal<EmployeeAttendance[]>([]);

  readonly selectedMonth = signal<number>(new Date().getMonth() + 1);
  readonly selectedYear = signal<number>(new Date().getFullYear());
  readonly selectedEmployeeId = signal<number | null>(null);

  readonly months = Array.from({ length: 12 }, (_, i) => i + 1);
  readonly years = Array.from({ length: 6 }, (_, i) => new Date().getFullYear() - 2 + i);
  readonly monthsNames = [
    'Janvier','Février','Mars','Avril','Mai','Juin',
    'Juillet','Août','Septembre','Octobre','Novembre','Décembre'
  ];

  readonly employees = computed(() => {
    const map = new Map<number, { id: number; name: string; matricule?: string | number }>();
    (this.timesheets() || []).forEach(t => {
      if (!map.has(t.employeeId)) {
        map.set(t.employeeId, {
          id: t.employeeId,
          name: t.employeeName || `Employé #${t.employeeId}`,
          matricule: t.matricule
        });
      }
    });
    return Array.from(map.values()).sort((a, b) => String(a.name).localeCompare(String(b.name)));
  });

  readonly displayRows = computed(() => {
    const list = this.timesheets() || [];
    const sel = this.selectedEmployeeId();
    const filtered = sel != null ? list.filter(r => r.employeeId === sel) : list;
    return filtered.slice().sort(
      (a, b) => new Date(b.workDate).getTime() - new Date(a.workDate).getTime()
    );
  });

  // Pagination unique
  readonly pageSize = signal<number>(10);
  readonly currentPage = signal<number>(1);

  readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.displayRows().length / this.pageSize()))
  );

  readonly pagedRows = computed(() => {
    const p = this.currentPage();
    const size = this.pageSize();
    const start = (p - 1) * size;
    return this.displayRows().slice(start, start + size);
  });

  readonly visiblePages = computed(() => {
    const total = this.totalPages();
    const current = this.currentPage();
    const delta = 2;
    const range: number[] = [];
    const start = Math.max(1, current - delta);
    const end = Math.min(total, current + delta);
    for (let i = start; i <= end; i++) {
      range.push(i);
    }
    return range;
  });

  setPage(p: number): void {
    const clamped = Math.max(1, Math.min(this.totalPages(), p));
    this.currentPage.set(clamped);
  }

  prevPage(): void { this.setPage(this.currentPage() - 1); }
  nextPage(): void { this.setPage(this.currentPage() + 1); }

  setPageSize(size: number): void {
    this.pageSize.set(Number(size));
    this.currentPage.set(1);
  }

  setSelectedEmployee(id: number | null): void {
    this.selectedEmployeeId.set(id);
    this.currentPage.set(1);
  }

  clearFilters(): void {
    this.selectedEmployeeId.set(null);
    this.currentPage.set(1);
  }

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
        this.currentPage.set(1);
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
    this.currentPage.set(1);
    this.loadTimesheets();
  }

  goToImport(): void {
    this.router.navigate(['/app/payroll/pointage-import']);
  }

  formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: 'long',
      year: 'numeric'
    });
  }
}
