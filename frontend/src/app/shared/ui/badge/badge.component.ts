import { Component, Input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span [class]="badgeClasses()">
      @if (icon) {
        <i [class]="icon + ' mr-1'"></i>
      }
      <ng-content></ng-content>
    </span>
  `
})
export class BadgeComponent {
  @Input() variant: 'primary' | 'secondary' | 'success' | 'warning' | 'danger' | 'info' | 'draft' | 'published' | 'deprecated' = 'secondary';
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  @Input() icon?: string;
  @Input() rounded: 'sm' | 'md' | 'full' = 'md';

  readonly badgeClasses = computed(() => {
    const base = 'inline-flex items-center font-medium';

    const variants: Record<string, string> = {
      primary: 'bg-indigo-100 text-indigo-700',
      secondary: 'bg-gray-100 text-gray-700',
      success: 'bg-green-100 text-green-700',
      warning: 'bg-amber-100 text-amber-700',
      danger: 'bg-red-100 text-red-700',
      info: 'bg-blue-100 text-blue-700',
      draft: 'bg-gray-100 text-gray-700',
      published: 'bg-green-100 text-green-700',
      deprecated: 'bg-amber-100 text-amber-700'
    };

    const sizes: Record<string, string> = {
      sm: 'px-2 py-0.5 text-xs',
      md: 'px-2.5 py-1 text-sm',
      lg: 'px-3 py-1.5 text-base'
    };

    const roundedClasses: Record<string, string> = {
      sm: 'rounded',
      md: 'rounded-md',
      full: 'rounded-full'
    };

    return `${base} ${variants[this.variant]} ${sizes[this.size]} ${roundedClasses[this.rounded]}`;
  });
}
