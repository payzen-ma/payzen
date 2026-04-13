import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { KpiMetric } from '../../../state/dashboard-hr.models';

@Component({
  selector: 'app-kpi-card',
  standalone: true,
  imports: [CommonModule],
  styles: [
    `
      :host {
        display: block;
        width: 100%;
        min-width: 0;
        box-sizing: border-box;
      }
      :host .stat-card {
        box-sizing: border-box;
        width: 100%;
      }
    `
  ],
  template: `
    <article class="stat-card">
      <p class="stat-label">{{ metric().label }}</p>

      <div class="flex items-start justify-between gap-2">
        <p [class]="valueClass()" class="stat-value text-[40px] leading-none">{{ metric().value }}</p>

        @if (metric().trend; as tr) {
          @if (tr.direction === 'flat') {
            <span class="pt-1 text-[13px] font-medium leading-none text-slate-400">{{ tr.value }}</span>
          } @else {
            <span [class]="trendPillClass()" class="inline-flex items-center gap-1.5 rounded-full px-3 py-1 text-[14px] font-medium leading-none">
              <i [class]="trendIconClass()" aria-hidden="true"></i>
              {{ tr.value }}
            </span>
          }
        }
      </div>

      @if (metric().subLabel) {
        <p class="stat-caption">{{ metric().subLabel }}</p>
      } @else if (metric().trend?.context) {
        <p class="stat-caption">{{ metric().trend?.context }}</p>
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
        return 'text-slate-800';
      case 'danger':
        return 'text-slate-800';
      case 'purple':
        return 'text-slate-800';
      case 'blue':
        return 'text-slate-800';
      case 'info':
        return 'text-slate-800';
      default:
        return 'text-slate-800';
    }
  });

  readonly trendIconClass = computed(() => {
    const direction = this.metric().trend?.direction;

    if (direction === 'up') {
      return 'pi pi-arrow-up text-emerald-600 text-[10px]';
    }

    if (direction === 'down') {
      return 'pi pi-arrow-down text-rose-600 text-[10px]';
    }

    return 'pi pi-minus text-slate-500 text-[10px]';
  });

  readonly trendPillClass = computed(() => {
    const direction = this.metric().trend?.direction;

    if (direction === 'up') {
      return 'bg-emerald-100 text-emerald-700';
    }

    if (direction === 'down') {
      return 'bg-rose-100 text-rose-700';
    }

    return 'bg-slate-100 text-slate-600';
  });
}
