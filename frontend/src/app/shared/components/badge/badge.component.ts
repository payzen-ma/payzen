import { Component, Input } from '@angular/core';

export type BadgeStatus =
    | 'active'
    | 'on-leave'
    | 'inactive'
    | 'draft'
    | 'published'
    | 'deprecated'
    | 'warning'
    | 'error'
    | 'cdi'
    | 'cdd';

/**
 * Payzen Badge Component
 * Displays status and type information with semantic colors
 *
 * @example
 * <app-badge status="active">Active</app-badge>
 * <app-badge status="on-leave">On Leave</app-badge>
 * <app-badge status="error">Error</app-badge>
 */
@Component({
    selector: 'app-badge',
    standalone: true,
    template: `
    <span [class]="badgeClasses">
      <ng-content></ng-content>
    </span>
  `,
    styles: [`
    span {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      padding: var(--space-1) var(--space-2);
      border-radius: var(--radius-full);
      font-size: var(--font-size-xs);
      font-weight: var(--font-weight-xs);
      white-space: nowrap;
    }
  `]
})
export class BadgeComponent {
    @Input() status: BadgeStatus = 'active';

    get badgeClasses(): string {
        const baseClass = 'inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium';

        const statusClasses: Record<BadgeStatus, string> = {
            'active': 'bg-[#d1fae5] text-[#065f46]',
            'on-leave': 'bg-[#fef3c7] text-[#92400e]',
            'inactive': 'bg-[#fee2e2] text-[#991b1b]',
            'draft': 'bg-[#f1f5f9] text-[#475569]',
            'published': 'bg-[#eff6ff] text-[#1d4ed8]',
            'deprecated': 'bg-[#f5f3ff] text-[#6d28d9]',
            'warning': 'bg-[#fef9c3] text-[#854d0e]',
            'error': 'bg-[#fee2e2] text-[#b91c1c]',
            'cdi': 'bg-[#e0f2fe] text-[#0369a1]',
            'cdd': 'bg-[#fae8ff] text-[#7e22ce]'
        };

        return `${baseClass} ${statusClasses[this.status]}`;
    }
}
