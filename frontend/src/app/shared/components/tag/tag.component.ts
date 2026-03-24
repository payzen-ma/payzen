import { 
  Component, 
  input, 
  output, 
  computed, 
  ChangeDetectionStrategy 
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { TagSize, TagVariant } from './tag.types';

@Component({
  selector: 'app-tag',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      [class]="containerClasses()"
      [attr.role]="interactive() ? 'button' : null"
      [attr.tabindex]="interactive() ? 0 : null"
      (click)="handleInteraction($event)"
      (keydown.enter)="handleInteraction($event)"
      (keydown.space)="handleInteraction($event)"
    >
      @if (icon()) {
        <i [class]="icon()" aria-hidden="true" class="mr-1.5 text-current"></i>
      }

      <span class="truncate font-medium leading-none">
        {{ label() }}
        <ng-content></ng-content>
      </span>

      @if (removable()) {
        <button
          type="button"
          class="ml-1.5 -mr-1 flex h-4 w-4 items-center justify-center rounded-full opacity-60 hover:bg-black/10 hover:opacity-100 focus:bg-black/10 focus:opacity-100 focus:outline-none"
          (click)="handleRemove($event)"
          [attr.aria-label]="'Remove ' + label()"
        >
          <svg class="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      }
    </div>
  `,
  styles: [`
    :host {
      display: inline-flex;
      vertical-align: middle;
    }
  `]
})
export class TagComponent {
  // --- Inputs (Signals) ---
  label = input.required<string>();
  variant = input<TagVariant>('default');
  size = input<TagSize>('md');
  removable = input<boolean>(false);
  interactive = input<boolean>(false);
  icon = input<string | undefined>(undefined);

  // --- Outputs ---
  removed = output<void>();
  action = output<void>();

  // --- Computed Styles ---
  containerClasses = computed(() => {
    const baseClasses = 'inline-flex items-center justify-center rounded-sm border transition-colors duration-200 ease-in-out';
    const cursorClass = this.interactive() ? 'cursor-pointer hover:shadow-sm' : 'cursor-default';
    
    // Size Mappings
    const sizeClasses = {
      sm: 'px-2 py-1 text-xs',
      md: 'px-2.5 py-1 text-sm',
      lg: 'px-3 py-1 text-base',
    }[this.size()];

    // Variant Mappings (Backgrounds, Borders, Text)
    // Using Tailwind colors that guarantee WCAG 2.1 AA contrast
    const variantClasses = {
      default: 'bg-slate-100 text-slate-700 border-slate-200 hover:bg-slate-200',
      primary: 'bg-brand-50 text-brand-700 border-brand-200 hover:bg-brand-100', // Assuming 'brand' is in your tailwind config
      success: 'bg-green-50 text-green-700 border-green-200 hover:bg-green-100',
      warning: 'bg-amber-50 text-amber-700 border-amber-200 hover:bg-amber-100',
      danger:  'bg-red-50 text-red-700 border-red-200 hover:bg-red-100',
      info:    'bg-blue-50 text-blue-700 border-blue-200 hover:bg-blue-100',
    }[this.variant()];

    return `${baseClasses} ${sizeClasses} ${variantClasses} ${cursorClass}`;
  });

  // --- Event Handlers ---
  handleRemove(event: Event) {
    event.stopPropagation(); // Prevent triggering the container click
    this.removed.emit();
  }

  handleInteraction(event: Event) {
    if (!this.interactive()) return;
    
    // Prevent default scrolling for Space key
    if (event instanceof KeyboardEvent && event.key === ' ') {
      event.preventDefault();
    }
    
    this.action.emit();
  }
}