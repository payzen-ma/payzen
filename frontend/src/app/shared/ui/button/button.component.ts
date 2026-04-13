import { Component, Input, Output, EventEmitter, computed } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-button',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      [type]="type"
      [disabled]="disabled || loading"
      [class]="buttonClasses()"
      (click)="!disabled && !loading && onClick.emit($event)">
      @if (loading) {
        <svg class="animate-spin h-4 w-4 mr-2" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
      }
      @if (icon && iconPosition === 'left' && !loading) {
        <i [class]="icon + ' mr-2'"></i>
      }
      <ng-content></ng-content>
      @if (icon && iconPosition === 'right') {
        <i [class]="icon + ' ml-2'"></i>
      }
    </button>
  `
})
export class ButtonComponent {
  @Input() variant: 'primary' | 'secondary' | 'danger' | 'ghost' = 'primary';
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  @Input() disabled = false;
  @Input() loading = false;
  @Input() type: 'button' | 'submit' = 'button';
  @Input() icon?: string;
  @Input() iconPosition: 'left' | 'right' = 'left';
  @Output() onClick = new EventEmitter<MouseEvent>();

  readonly buttonClasses = computed(() => {
    const base = 'inline-flex items-center justify-center rounded-lg font-medium transition-all focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed';

    const variants: Record<string, string> = {
      primary:
        'bg-[color:var(--primary-500)] text-white hover:bg-[color:var(--primary-600)] focus:ring-[color:var(--primary-300)] shadow-sm',
      secondary: 'bg-white text-gray-700 border border-gray-300 hover:bg-gray-50 focus:ring-[color:var(--primary-300)] shadow-sm',
      danger: 'bg-red-600 text-white hover:bg-red-700 focus:ring-red-500 shadow-sm',
      ghost: 'text-gray-700 hover:bg-gray-100 focus:ring-gray-400'
    };

    const sizes: Record<string, string> = {
      sm: 'px-3 py-2 text-[13px] rounded-md',
      md: 'px-4 py-2 text-base',
      lg: 'px-6 py-3 text-lg'
    };

    return `${base} ${variants[this.variant]} ${sizes[this.size]}`;
  });
}
