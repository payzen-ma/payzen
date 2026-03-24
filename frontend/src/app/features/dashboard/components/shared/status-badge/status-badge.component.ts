import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StatusPill } from '../../../state/dashboard-hr.models';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span [class]="classes()" class="inline-flex items-center rounded-md px-2 py-1 text-xs font-semibold">
      <span class="mr-1 text-[10px]">&bull;</span>
      {{ value().label }}
    </span>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StatusBadgeComponent {
  readonly value = input.required<StatusPill>();

  readonly classes = computed(() => {
    switch (this.value().severity) {
      case 'success':
        return 'bg-green-100 text-green-700';
      case 'warn':
        return 'bg-amber-100 text-amber-700';
      case 'danger':
        return 'bg-red-100 text-red-700';
      case 'info':
        return 'bg-blue-100 text-blue-700';
      default:
        return 'bg-gray-100 text-gray-600';
    }
  });
}
