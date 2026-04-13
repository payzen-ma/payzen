import { Component, EventEmitter, Input, Output } from '@angular/core';

export type ButtonVariant = 'primary' | 'secondary' | 'danger' | 'ghost' | 'disabled';
export type ButtonSize = 'small' | 'medium' | 'large';

/**
 * Payzen Button Component
 * Implements the design system button styles from Figma
 *
 * @example
 * <app-button
 *   variant="primary"
 *   size="medium"
 *   (click)="onAction()">
 *   Click me
 * </app-button>
 */
@Component({
    selector: 'app-button',
    standalone: true,
    template: `
    <button
      [class]="buttonClasses"
      [disabled]="disabled"
      [type]="type"
      (click)="onClick()"
    >
      <ng-content></ng-content>
    </button>
  `,
    styles: [`
    button {
      font-family: var(--font-family-base);
      font-weight: var(--font-weight-base);
      border: none;
      cursor: pointer;
      transition: all 0.2s ease-in-out;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: var(--space-2);
      white-space: nowrap;
    }

    button:not(:disabled) {
      cursor: pointer;
    }

    button:disabled {
      cursor: not-allowed;
      opacity: 0.6;
    }
  `]
})
export class ButtonComponent {
    @Input() variant: ButtonVariant = 'primary';
    @Input() size: ButtonSize = 'medium';
    @Input() disabled = false;
    @Input() type: 'button' | 'submit' | 'reset' = 'button';
    @Output() clickEvent = new EventEmitter<void>();

    get buttonClasses(): string {
        const baseClasses = 'font-medium rounded-md transition-all duration-200';
        const sizeClasses = this.getSizeClasses();
        const variantClasses = this.getVariantClasses();

        return `${baseClasses} ${sizeClasses} ${variantClasses}`;
    }

    private getSizeClasses(): string {
        switch (this.size) {
            case 'small':
                return 'px-3 py-1.5 text-xs';
            case 'large':
                return 'px-6 py-3 text-base';
            case 'medium':
            default:
                return 'px-4 py-2 text-sm';
        }
    }

    private getVariantClasses(): string {
        switch (this.variant) {
            case 'primary':
                return 'bg-[var(--primary-500)] text-white hover:bg-[var(--primary-600)] active:bg-[var(--primary-700)] shadow-sm';
            case 'secondary':
                return 'bg-white text-[#374151] border border-[var(--border-medium)] hover:bg-[var(--bg-hover)] active:bg-[var(--bg-active)] shadow-sm';
            case 'danger':
                return 'bg-[var(--danger)] text-white hover:bg-[var(--danger-hover)] active:bg-red-700 shadow-sm';
            case 'ghost':
                return 'bg-[var(--primary-50)] text-[var(--primary-500)] hover:bg-[var(--primary-100)] active:bg-[var(--primary-200)]';
            case 'disabled':
                return 'bg-[var(--bg-disabled)] text-[var(--text-muted)] border border-[var(--border-subtle)]';
            default:
                return '';
        }
    }

    onClick(): void {
        if (!this.disabled) {
            this.clickEvent.emit();
        }
    }
}
