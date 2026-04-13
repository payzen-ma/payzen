import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { RouterModule } from '@angular/router';
import { DashboardSummary, RecentCompany } from '../../models/dashboard.model';
import { DashboardService } from '../../services/dashboard.service';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);

  summary: DashboardSummary | null = null;
  recentCompanies: RecentCompany[] = [];
  isLoading = false;
  error: string | null = null;
  showMock = true;

  ngOnInit(): void {
    this.loadData();
  }

  // Derived display data --------------------------------------------------
  get companiesByCity(): Array<{ city: string; count: number; percentage: number }> {
    const map = new Map<string, number>();
    const list = this.recentCompanies || [];
    list.forEach(c => {
      const city = c.cityName || 'Unknown';
      map.set(city, (map.get(city) || 0) + 1);
    });
    const total = Array.from(map.values()).reduce((s, v) => s + v, 0) || 1;
    return Array.from(map.entries()).map(([city, count]) => ({ city, count, percentage: Math.round((count / total) * 100) }));
  }

  get employeesDistribution(): Array<{ name: string; employees: number }> {
    return (this.recentCompanies || []).map(c => ({ name: c.companyName, employees: c.employeesCount || 0 }));
  }

  getMaxEmployees(): number {
    const arr = this.employeesDistribution.map(e => e.employees);
    return arr.length ? Math.max(...arr) : 1;
  }

  getEmployeePercentage(employees: number): number {
    const max = this.getMaxEmployees();
    return max ? Math.round((employees / max) * 100) : 0;
  }

  getStatusClass(status?: string): string {
    if (!status) return 'bg-gray-100 text-gray-800';
    return status === 'active' ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800';
  }

  getStatusLabel(status?: string): string {
    if (!status) return 'N/A';
    return status === 'active' ? 'Actif' : 'Inactif';
  }

  loadData() {
    this.isLoading = true;
    this.error = null;
    this.dashboardService.getSummary().subscribe({
      next: (s) => {
        this.summary = s;
        // Use RecentCompanies returned inside the summary
        this.recentCompanies = s.recentCompanies || [];
        this.isLoading = false;
      },
      error: (err) => { this.error = 'Erreur lors du chargement des métriques'; this.isLoading = false; }
    });
  }

  // Utility for distribution bar widths (percent fallback)
  bucketPercent(entry: import('../../models/dashboard.model').EmployeeDistributionEntry): number {
    return entry.percentage ?? 0;
  }

  // Stats object for mock UI compatibility
  get stats() {
    return {
      totalCompanies: this.summary?.totalCompanies ?? 0,
      totalEmployees: this.summary?.totalEmployees ?? 0,
      activeCompanies: this.summary?.totalCompanies ?? 0,
      accountingFirms: this.summary?.accountingFirmsCount ?? 0
    };
  }

  toggleMock() {
    this.showMock = !this.showMock;
  }
}

