import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { ChartModule } from 'primeng/chart';
import { DonutChartConfig } from '../../../state/dashboard-hr.models';

@Component({
  selector: 'app-donut-chart',
  standalone: true,
  imports: [CommonModule, ChartModule],
  template: `
    <div class="relative mx-auto h-[250px] w-full max-w-[330px]">
      <p-chart type="doughnut" [data]="data()" [options]="options()" class="block h-full w-full"></p-chart>
      @if (config().centerLabel) {
        <div class="pointer-events-none absolute inset-0 flex items-center justify-center">
          <span class="text-[42px] leading-none font-semibold tracking-[-0.03em] text-slate-800">{{ config().centerLabel }}</span>
        </div>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DonutChartComponent {
  readonly config = input.required<DonutChartConfig>();

  readonly data = computed(() => ({
    labels: this.config().slices.map(slice => slice.label),
    datasets: [
      {
        data: this.config().slices.map(slice => slice.value),
        backgroundColor: this.config().slices.map(slice => slice.color),
        borderWidth: 0,
        hoverOffset: 2
      }
    ]
  }));

  readonly options = computed(() => ({
    responsive: true,
    maintainAspectRatio: false,
    cutout: '68%',
    plugins: {
      legend: {
        display: false
      },
      tooltip: {
        backgroundColor: '#111827',
        padding: 10,
        displayColors: true
      }
    }
  }));
}
