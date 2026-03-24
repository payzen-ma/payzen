import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [CommonModule],
  template: `<div class="space-y-4">
    <h1 class="text-3xl font-bold text-surface-900">Audit Logs</h1>
    <div class="bg-white rounded-lg border border-surface-200 p-6">
      <p class="text-surface-600">Fonctionnalité en cours de développement...</p>
    </div>
  </div>`
})
export class AuditLogsComponent {}
