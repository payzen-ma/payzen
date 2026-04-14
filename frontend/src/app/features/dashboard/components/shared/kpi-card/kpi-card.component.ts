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

      :host .kpi-card {
        box-sizing: border-box;
        width: 100%;
        display: flex;
        flex-direction: column;
        gap: 8px;
        padding: 16px;
        border: 1px solid var(--surface-200, #e2e8f0);
        border-radius: var(--radius-xl, 12px);
        background: var(--bg-element, #ffffff);
        box-shadow: 0 1px 6px 1px rgba(0, 0, 0, 0.03);
        overflow: hidden;
      }

      :host .kpi-card__label {
        margin: 0;
        font-size: 11px;
        line-height: 1.1;
        font-weight: 600;
        text-transform: uppercase;
        color: #6a7282;
        white-space: nowrap;
      }

      :host .kpi-card__main {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 8px;
        min-width: 0;
      }

      :host .kpi-card__value {
        margin: 0;
        font-size: 32px;
        line-height: 1.1;
        font-weight: 600;
        letter-spacing: -0.04em;
        color: var(--text-primary, #1f2937);
        white-space: nowrap;
      }

      :host .kpi-card__trend-flat {
        margin: 0;
        padding-top: 2px;
        font-size: 13px;
        line-height: 1;
        font-weight: 500;
        color: #94a3b8;
        white-space: nowrap;
      }

      :host .kpi-card__trend-pill {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        gap: 4px;
        padding: 2px 8px;
        border-radius: var(--radius-full, 9999px);
        font-size: 14px;
        line-height: 1.1;
        font-weight: 500;
        white-space: nowrap;
        flex-shrink: 0;
      }

      :host .kpi-card__trend-pill--up {
        background: var(--success-light, #d1f9de);
        color: var(--success, #16a34a);
      }

      :host .kpi-card__trend-pill--down {
        background: var(--danger-light, #fee2e2);
        color: var(--danger, #dc2626);
      }

      :host .kpi-card__trend-pill--flat {
        background: var(--neutral-100, #f1f5f9);
        color: var(--text-secondary, #6b7280);
      }

      :host .kpi-card__trend-icon {
        font-size: 10px;
      }

      :host .kpi-card__caption {
        margin: 0;
        font-size: 13px;
        line-height: 1.2;
        font-weight: 400;
        letter-spacing: -0.04em;
        color: #4a5565;
        white-space: nowrap;
      }
    `
  ],
  template: `
    <article class="kpi-card">
      <p class="kpi-card__label">{{ metric().label }}</p>

      <div class="kpi-card__main">
        <p [class]="valueClass()" class="kpi-card__value">{{ metric().value }}</p>

        @if (metric().trend; as tr) {
          @if (tr.direction === 'flat') {
            <span class="kpi-card__trend-flat">{{ tr.value }}</span>
          } @else {
            <span [class]="trendPillClass()" class="kpi-card__trend-pill">
              <i [class]="trendIconClass()" aria-hidden="true"></i>
              {{ tr.value }}
            </span>
          }
        }
      </div>

      @if (metric().subLabel) {
        <p class="kpi-card__caption">{{ metric().subLabel }}</p>
      } @else if (metric().trend?.context) {
        <p class="kpi-card__caption">{{ metric().trend?.context }}</p>
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
      return 'pi pi-arrow-up kpi-card__trend-icon';
    }

    if (direction === 'down') {
      return 'pi pi-arrow-down kpi-card__trend-icon';
    }

    return 'pi pi-minus kpi-card__trend-icon';
  });

  readonly trendPillClass = computed(() => {
    const direction = this.metric().trend?.direction;

    if (direction === 'up') {
      return 'kpi-card__trend-pill--up';
    }

    if (direction === 'down') {
      return 'kpi-card__trend-pill--down';
    }

    return 'kpi-card__trend-pill--flat';
  });
}
