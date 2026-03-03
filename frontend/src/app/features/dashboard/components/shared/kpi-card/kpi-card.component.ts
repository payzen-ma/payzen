import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { KpiMetric } from '../../../state/dashboard-hr.models';

@Component({
  selector: 'app-kpi-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <article class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
      <p class="text-xs font-semibold uppercase tracking-wider text-gray-500">{{ metric().label }}</p>
      <p [class]="valueClass()" class="mt-2 text-4xl font-bold leading-none">{{ metric().value }}</p>

      @if (metric().subLabel) {
        <p class="mt-2 text-sm text-gray-600">{{ metric().subLabel }}</p>
      }

      @if (metric().trend) {
        <div class="mt-2 flex items-center gap-2 text-sm">
          <i [class]="trendIconClass()" aria-hidden="true"></i>
          <span [class]="trendTextClass()" class="font-medium">{{ metric().trend?.value }}</span>
          @if (metric().trend?.context) {
            <span class="text-gray-500">{{ metric().trend?.context }}</span>
          }
        </div>
      }
    </article>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class KpiCardComponent {
  readonly metric = input.required<KpiMetric>();

  readonly valueClass = computed(() => {
    switch (this.metric().accent) {
      case 'success':
        return 'text-green-700';
      case 'danger':
        return 'text-red-700';
      case 'purple':
        return 'text-violet-700';
      case 'blue':
        return 'text-blue-700';
      case 'info':
        return 'text-cyan-700';
      default:
        return 'text-gray-900';
    }
  });

  readonly trendIconClass = computed(() => {
    const direction = this.metric().trend?.direction;

    if (direction === 'up') {
      return 'pi pi-arrow-up text-green-700 text-xs';
    }

    if (direction === 'down') {
      return 'pi pi-arrow-down text-red-700 text-xs';
    }

    return 'pi pi-minus text-gray-500 text-xs';
  });

  readonly trendTextClass = computed(() => {
    const direction = this.metric().trend?.direction;

    if (direction === 'up') {
      return 'text-green-700';
    }

    if (direction === 'down') {
      return 'text-red-700';
    }

    return 'text-gray-600';
  });
}
