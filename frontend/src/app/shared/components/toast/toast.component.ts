import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { Toast, ToastService } from '@app/core/services/toast.service';

@Component({
    selector: 'app-toast',
    standalone: true,
    imports: [CommonModule],
    template: `
    <div class="toast-container">
      <div
        *ngFor="let toast of toasts"
        [class]="'toast toast-' + toast.type"
      >
        <div class="toast-icon">
          <span *ngIf="toast.type === 'success'">✓</span>
          <span *ngIf="toast.type === 'error'">✕</span>
          <span *ngIf="toast.type === 'warning'">⚠</span>
          <span *ngIf="toast.type === 'info'">ℹ</span>
        </div>
        <div class="toast-message">{{ toast.message }}</div>
        <button
          class="toast-close"
          (click)="closeToast(toast.id)"
          aria-label="Close notification"
        >
          ×
        </button>
      </div>
    </div>
  `,
    styles: [`
    .toast-container {
      position: fixed;
      top: 20px;
      right: 20px;
      z-index: 9999;
      display: flex;
      flex-direction: column;
      gap: 12px;
      pointer-events: auto;
      max-width: 400px;
    }

    .toast {
      display: flex;
      align-items: flex-start;
      gap: 12px;
      padding: 14px 16px;
      border-radius: 8px;
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.15);
      animation: slideIn 0.35s ease-out forwards;
      font-size: 14px;
      font-weight: 500;
      line-height: 1.4;
      overflow: hidden;
    }

    .toast-success {
      background: #10b981;
      color: white;
      border-left: 4px solid #059669;
    }

    .toast-error {
      background: #ef4444;
      color: white;
      border-left: 4px solid #dc2626;
    }

    .toast-warning {
      background: #f59e0b;
      color: white;
      border-left: 4px solid #d97706;
    }

    .toast-info {
      background: #3b82f6;
      color: white;
      border-left: 4px solid #2563eb;
    }

    .toast-icon {
      flex-shrink: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      width: 20px;
      height: 20px;
      font-weight: 700;
      font-size: 16px;
    }

    .toast-message {
      flex: 1;
      word-break: break-word;
      white-space: pre-wrap;
    }

    .toast-close {
      flex-shrink: 0;
      background: none;
      border: none;
      color: inherit;
      font-size: 24px;
      line-height: 1;
      cursor: pointer;
      padding: 0;
      opacity: 0.7;
      transition: opacity 0.2s;
    }

    .toast-close:hover {
      opacity: 1;
    }

    @keyframes slideIn {
      from {
        transform: translateX(400px);
        opacity: 0;
      }
      to {
        transform: translateX(0);
        opacity: 1;
      }
    }

    @keyframes slideOut {
      from {
        transform: translateX(0);
        opacity: 1;
      }
      to {
        transform: translateX(400px);
        opacity: 0;
      }
    }

    @media (max-width: 640px) {
      .toast-container {
        left: 12px;
        right: 12px;
        top: 12px;
        max-width: none;
      }

      .toast {
        padding: 12px 14px;
        font-size: 13px;
      }

      .toast-icon {
        width: 18px;
        height: 18px;
      }
    }
  `]
})
export class ToastComponent implements OnInit {
    private toastService = inject(ToastService);
    toasts: Toast[] = [];

    ngOnInit(): void {
        this.toastService.getToasts().subscribe(toasts => {
            this.toasts = toasts;
        });
    }

    closeToast(id: string): void {
        this.toastService.removeToast(id);
    }
}
