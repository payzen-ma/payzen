import { Component, Input, Output, EventEmitter, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (isOpen()) {
      <div class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black bg-opacity-50 animate-fadeIn"
           (click)="onBackdropClick()">
        <div [class]="modalClasses"
             (click)="$event.stopPropagation()"
             class="animate-scaleIn">
          <!-- Header -->
          @if (title || showClose) {
            <div class="flex items-center justify-between px-6 py-4 border-b border-gray-200">
              @if (title) {
                <h3 class="text-lg font-semibold text-gray-900">{{ title }}</h3>
              }
              @if (showClose) {
                <button
                  type="button"
                  class="text-gray-400 hover:text-gray-600 transition"
                  (click)="close()">
                  <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"/>
                  </svg>
                </button>
              }
            </div>
          }

          <!-- Body -->
          <div class="px-6 py-4">
            <ng-content></ng-content>
          </div>

          <!-- Footer -->
          @if (hasFooter) {
            <div class="flex items-center justify-end gap-3 px-6 py-4 border-t border-gray-200 bg-gray-50">
              <ng-content select="[footer]"></ng-content>
            </div>
          }
        </div>
      </div>
    }
  `,
  styles: [`
    @keyframes fadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }

    @keyframes scaleIn {
      from {
        opacity: 0;
        transform: scale(0.95);
      }
      to {
        opacity: 1;
        transform: scale(1);
      }
    }

    .animate-fadeIn {
      animation: fadeIn 0.2s ease-out;
    }

    .animate-scaleIn {
      animation: scaleIn 0.2s ease-out;
    }
  `]
})
export class ModalComponent {
  @Input() title?: string;
  @Input() size: 'sm' | 'md' | 'lg' | 'xl' = 'md';
  @Input() showClose = true;
  @Input() closeOnBackdrop = true;
  @Input() hasFooter = false;

  @Output() closed = new EventEmitter<void>();

  readonly isOpen = signal(false);

  get modalClasses(): string {
    const base = 'bg-white rounded-lg shadow-xl max-h-[90vh] overflow-y-auto';
    const sizes: Record<string, string> = {
      sm: 'w-full max-w-md',
      md: 'w-full max-w-lg',
      lg: 'w-full max-w-2xl',
      xl: 'w-full max-w-4xl'
    };
    return `${base} ${sizes[this.size]}`;
  }

  open(): void {
    this.isOpen.set(true);
  }

  close(): void {
    this.isOpen.set(false);
    this.closed.emit();
  }

  onBackdropClick(): void {
    if (this.closeOnBackdrop) {
      this.close();
    }
  }
}
