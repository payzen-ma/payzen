import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-section-header',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="space-y-2">
      @if (eyebrow()) {
        <p class="text-xs font-bold uppercase tracking-[0.1em] text-gray-500">{{ eyebrow() }}</p>
      }
      <div class="flex flex-wrap items-center gap-2">
        <i [class]="icon()" class="text-lg text-gray-600" aria-hidden="true"></i>
        <h2 class="text-xl font-semibold text-gray-600">{{ title() }}</h2>
        @if (badge()) {
          <span class="inline-flex items-center rounded-md bg-blue-50 px-2 py-1 text-xs font-bold uppercase tracking-wide text-blue-700">{{ badge() }}</span>
        }
      </div>
      <p class="text-sm text-gray-600">{{ subtitle() }}</p>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SectionHeaderComponent {
  readonly eyebrow = input.required<string>();
  readonly title = input.required<string>();
  readonly subtitle = input.required<string>();
  readonly badge = input<string>('');
  readonly icon = input<string>('pi pi-chart-bar');
}
