import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-section-header',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="space-y-1.5">
      @if (eyebrow()) {
        <p class="text-[11px] font-semibold uppercase tracking-[0.08em] text-slate-500">{{ eyebrow() }}</p>
      }
      <div class="flex flex-wrap items-center gap-2">
        @if (icon()) {
          <i [class]="icon()" class="text-[16px] text-slate-500" aria-hidden="true"></i>
        }
        <h2 class="text-[32px] leading-[1.1] font-semibold tracking-[-0.02em] text-slate-800">{{ title() }}</h2>
        @if (badge()) {
          <span class="inline-flex items-center rounded-md bg-blue-50 px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-blue-700">{{ badge() }}</span>
        }
      </div>
      <p class="text-[13px] text-slate-500">{{ subtitle() }}</p>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SectionHeaderComponent {
  readonly eyebrow = input.required<string>();
  readonly title = input.required<string>();
  readonly subtitle = input.required<string>();
  readonly badge = input<string>('');
  readonly icon = input<string>('');
}
