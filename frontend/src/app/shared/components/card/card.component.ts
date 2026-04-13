import { Component, Input } from '@angular/core';

export type CardSize = 'default' | 'compact' | 'large';

/**
 * Payzen Card Component
 * Base card container with consistent styling
 *
 * @example
 * <app-card>
 *   <h3 class="text-lg font-semibold">Card Title</h3>
 *   <p class="text-gray-600">Card content here</p>
 * </app-card>
 *
 * <app-card size="compact">
 *   <p class="text-sm">Compact card</p>
 * </app-card>
 */
@Component({
    selector: 'app-card',
    standalone: true,
    template: `
    <div [class]="cardClasses">
      <ng-content></ng-content>
    </div>
  `,
    styles: [`
    div {
      background-color: var(--bg-element);
      border: 1px solid var(--border-subtle);
      border-radius: var(--radius-lg);
      box-shadow: var(--shadow-md);
    }
  `]
})
export class CardComponent {
    @Input() size: CardSize = 'default';

    get cardClasses(): string {
        const baseClass = 'bg-white border border-[var(--border-subtle)] rounded-lg shadow-md';

        const sizeClasses: Record<CardSize, string> = {
            'default': 'p-6',
            'compact': 'p-5',
            'large': 'p-8'
        };

        return `${baseClass} ${sizeClasses[this.size]}`;
    }
}
