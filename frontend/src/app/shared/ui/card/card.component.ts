import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div [class]="cardClasses">
      @if (title || hasHeaderContent) {
        <div class="px-6 py-4 border-b border-gray-200">
          @if (title) {
            <h3 class="text-lg font-semibold text-gray-900">{{ title }}</h3>
          }
          <ng-content select="[header]"></ng-content>
        </div>
      }

      <div [class]="bodyClasses">
        <ng-content></ng-content>
      </div>

      @if (hasFooterContent) {
        <div class="px-6 py-4 border-t border-gray-200 bg-gray-50">
          <ng-content select="[footer]"></ng-content>
        </div>
      }
    </div>
  `
})
export class CardComponent {
  @Input() title?: string;
  @Input() padding: 'none' | 'sm' | 'md' | 'lg' = 'md';
  @Input() hover = false;

  hasHeaderContent = false;
  hasFooterContent = false;

  get cardClasses(): string {
    const base = 'bg-white rounded-lg border border-gray-200 shadow-sm';
    const hoverClass = this.hover ? 'hover:shadow-md transition-shadow cursor-pointer' : '';
    return `${base} ${hoverClass}`;
  }

  get bodyClasses(): string {
    const paddings: Record<string, string> = {
      none: '',
      sm: 'p-4',
      md: 'p-6',
      lg: 'p-8'
    };
    return paddings[this.padding];
  }

  ngAfterContentInit(): void {
    // Check if header/footer content is projected
    // This is a simplified check - in production, use ContentChild
  }
}
