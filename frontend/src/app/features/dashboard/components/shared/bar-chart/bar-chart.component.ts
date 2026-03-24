import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ChartModule } from 'primeng/chart';
import { BarChartConfig } from '../../../state/dashboard-hr.models';

@Component({
  selector: 'app-bar-chart',
  standalone: true,
  imports: [CommonModule, ChartModule],
  template: `
    <div class="h-56 md:h-64 lg:h-72">
      <p-chart type="bar" [data]="data()" [options]="options()" class="block h-full w-full"></p-chart>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BarChartComponent {
  readonly config = input.required<BarChartConfig>();

  readonly data = computed(() => {
    const cfg = this.config();

    const colors = cfg.values.map((_, index) => {
      if (cfg.highlightLast && index === cfg.values.length - 1) {
        return '#1d4ed8';
      }

      return cfg.color ?? '#2563eb';
    });

    return {
      labels: cfg.labels,
      datasets: [
        {
          label: cfg.datasetLabel,
          data: cfg.values,
          borderRadius: 6,
          maxBarThickness: 26,
          backgroundColor: colors
        }
      ]
    };
  });

  readonly options = computed(() => {
    const cfg = this.config();

    return {
      responsive: true,
      maintainAspectRatio: false,
      indexAxis: cfg.horizontal ? 'y' : 'x',
      plugins: {
        legend: {
          display: false
        },
        tooltip: {
          backgroundColor: '#111827',
          padding: 10,
          displayColors: false,
          callbacks: {
            label: (context: { parsed: { x?: number; y?: number } }) => {
              const value = cfg.horizontal ? (context.parsed.x ?? 0) : (context.parsed.y ?? 0);
              return cfg.suffix ? `${value} ${cfg.suffix}` : `${value}`;
            }
          }
        }
      },
      scales: {
        x: {
          grid: {
            display: false
          },
          border: {
            display: false
          },
          ticks: {
            color: '#6b7280',
            font: { size: 11 }
          }
        },
        y: {
          beginAtZero: true,
          grid: {
            color: '#f1f5f9'
          },
          border: {
            display: false
          },
          ticks: {
            color: '#6b7280',
            font: { size: 11 }
          }
        }
      }
    };
  });
}
