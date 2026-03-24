import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';

@Component({
  selector: 'app-access-denied',
  standalone: true,
  imports: [CommonModule, TranslateModule, ButtonModule],
  template: `
    <div class="min-h-screen flex items-center justify-center">
      <div class="card p-6 text-center">
        <h2 class="text-xl font-semibold mb-2">{{ 'accessDenied.title' | translate }}</h2>
        <p class="text-sm text-gray-600 mb-4">{{ 'accessDenied.message' | translate }}</p>
        <button pButton label="{{ 'common.goBack' | translate }}" (click)="goBack()" class="btn btn-primary"></button>
      </div>
    </div>
  `
})
export class AccessDeniedComponent {
  goBack() {
    history.back();
  }
}
