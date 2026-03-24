import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProgressRowModel } from '../../../state/dashboard-hr.models';

@Component({
  selector: 'app-progress-row',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="space-y-2">
      <div class="flex items-center justify-between gap-3 text-sm">
        <span class="font-medium text-gray-700">{{ row().label }}</span>
        <span class="text-gray-600">{{ row().rightLabel }}</span>
      </div>
      <div class="h-3 rounded-full bg-gray-100">
        <div class="h-3 rounded-full" [style.width.%]="safePercent()" [style.background]="row().color"></div>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProgressRowComponent {
  readonly row = input.required<ProgressRowModel>();

  readonly safePercent = computed(() => Math.max(0, Math.min(100, this.row().percent)));
}
