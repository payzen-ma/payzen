import { ChangeDetectionStrategy, Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-salary-metric-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      type="button"
      class="metric-card"
      [class.metric-card--active]="active"
      [class.metric-card--accent]="accent"
      [disabled]="disabled"
      (click)="cardClick.emit()">
      <span class="metric-card__label">{{ label }}</span>
      <span class="metric-card__value">{{ value }}</span>
      @if (description) {
        <span class="metric-card__description">{{ description }}</span>
      }
    </button>
  `,
  styleUrl: './salary-metric-card.component.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SalaryMetricCardComponent {
  @Input({ required: true }) label = '';
  @Input({ required: true }) value = '0';
  @Input() description = '';
  @Input() active = false;
  @Input() accent = false;
  @Input() disabled = false;

  @Output() readonly cardClick = new EventEmitter<void>();
}
