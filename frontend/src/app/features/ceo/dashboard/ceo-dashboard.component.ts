import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CeoDashboardService } from './ceo-dashboard.service';

export interface CeoKpi {
  title: string;
  value: string | number;
  subtitle: string;
  subtitleColor: string;
}

export interface CeoChartData {
  month: string;
  net: number;
  charges: number;
  netMad?: number;
  chargesMad?: number;
}

export interface CeoDepartment {
  name: string;
  value: number;
  color: string;
  percentage: number;
}

export interface CeoPayIndicator {
  label: string;
  value: string | number;
  valueColor?: string;
}

export interface CeoAlert {
  title: string;
  subtitle: string;
  dotColor: string;
}

@Component({
  selector: 'app-ceo-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './ceo-dashboard.component.html',
  styleUrls: [
    '../../employees/dashboard/employee-dashboard.component.css',
    './ceo-dashboard.component.css'
  ]
})
export class CeoDashboardComponent implements OnInit {
  private ceoService = inject(CeoDashboardService);

  readonly kpis = signal<CeoKpi[]>([]);
  readonly evolutionChart = signal<CeoChartData[]>([]);
  readonly departments = signal<CeoDepartment[]>([]);
  readonly payIndicators = signal<CeoPayIndicator[]>([]);
  readonly alerts = signal<CeoAlert[]>([]);
  readonly isLoading = signal<boolean>(true);
  readonly selectedParity = signal<'ALL' | 'F' | 'M'>('ALL');
  readonly fromMonth = signal<string>(this.toMonthValue(new Date(new Date().getFullYear(), new Date().getMonth() - 5, 1)));
  readonly toMonth = signal<string>(this.toMonthValue(new Date()));

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading.set(true);
    this.ceoService.getCeoDashboardData(this.selectedParity(), this.fromMonth(), this.toMonth()).subscribe({
      next: (data) => {
        this.kpis.set(data?.kpis || []);
        const rawChart = (data?.evolutionChart || []) as Array<{ month: string; netMad?: number; chargesMad?: number; net?: number; charges?: number }>;
        const maxValue = rawChart.reduce((acc, cur) => Math.max(acc, Number(cur.netMad ?? cur.net ?? 0) + Number(cur.chargesMad ?? cur.charges ?? 0)), 0);
        const denom = maxValue > 0 ? maxValue : 1;
        this.evolutionChart.set(
          rawChart.map((p) => {
            const netMad = Number(p.netMad ?? p.net ?? 0);
            const chargesMad = Number(p.chargesMad ?? p.charges ?? 0);
            return {
              month: p.month,
              netMad,
              chargesMad,
              net: Math.round((netMad / denom) * 100),
              charges: Math.round((chargesMad / denom) * 100)
            };
          })
        );
        this.departments.set(data?.departments || []);
        this.payIndicators.set(data?.payIndicators || []);
        this.alerts.set(data?.alerts || []);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
      }
    });
  }

  onParityChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value as 'ALL' | 'F' | 'M';
    this.selectedParity.set(value || 'ALL');
    this.loadDashboard();
  }

  onFromMonthChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.fromMonth.set(value);
    this.loadDashboard();
  }

  onToMonthChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.toMonth.set(value);
    this.loadDashboard();
  }

  private toMonthValue(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    return `${year}-${month}`;
  }
}
