import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ModalComponent } from '../../../../shared/modal/modal.component';
import { PayrollReferentielService } from '../../../../services/payroll-referentiel/payroll-referentiel.service';
import {
  LegalParameterDto,
  formatParameterValue,
  formatEffectivePeriod
} from '../../../../models/payroll-referentiel';

/**
 * Legal Parameter History Modal Component
 * Shows historical versions of a legal parameter
 */
@Component({
  selector: 'app-legal-parameter-history-modal',
  standalone: true,
  imports: [CommonModule, ModalComponent],
  template: `
    <app-modal
      [(visible)]="visible"
      [title]="'Historique: ' + (parameterName || '')"
      (visibleChange)="onVisibleChange($event)">

      <div class="space-y-4">
        <!-- Loading State -->
        <div *ngIf="loading" class="flex items-center justify-center py-8">
          <svg class="w-6 h-6 text-primary-500 animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path>
          </svg>
          <span class="ml-2 text-gray-600">Chargement de l'historique...</span>
        </div>

        <!-- Empty State -->
        <div *ngIf="!loading && history.length === 0" class="text-center py-8">
          <p class="text-gray-500">Aucun historique disponible</p>
        </div>

        <!-- History Timeline -->
        <div *ngIf="!loading && history.length > 0" class="relative">
          <div class="absolute left-4 top-0 bottom-0 w-0.5 bg-gray-200"></div>

          <div *ngFor="let item of history; let i = index; let first = first" class="relative pl-10 pb-6 last:pb-0">
            <!-- Timeline Dot -->
            <div
              class="absolute left-2.5 w-3 h-3 rounded-full border-2 border-white"
              [ngClass]="{
                'bg-green-500': isCurrentlyActive(item),
                'bg-blue-500': isFuture(item),
                'bg-gray-400': !isCurrentlyActive(item) && !isFuture(item)
              }">
            </div>

            <!-- Version Card -->
            <div
              class="bg-white border rounded-lg p-4"
              [class.border-green-300]="isCurrentlyActive(item)"
              [class.border-gray-200]="!isCurrentlyActive(item)">

              <!-- Header -->
              <div class="flex items-start justify-between mb-2">
                <div>
                  <h4 class="font-medium text-gray-900">{{ item.name }}</h4>
                  <p class="text-xs text-gray-500">{{ formatPeriod(item) }}</p>
                </div>
                <span
                  class="px-2 py-0.5 text-xs font-medium rounded-full"
                  [ngClass]="getStatusClass(item)">
                  {{ getStatusLabel(item) }}
                </span>
              </div>

              <!-- Value -->
              <div class="text-xl font-bold text-primary-600 mb-2">
                {{ formatValue(item) }}
              </div>

              <!-- Description -->
              <p *ngIf="item.description" class="text-sm text-gray-600">
                {{ item.description }}
              </p>
            </div>
          </div>
        </div>

        <!-- Close Button -->
        <div class="flex justify-end pt-4 border-t border-gray-200">
          <button
            (click)="onClose()"
            class="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors">
            Fermer
          </button>
        </div>
      </div>
    </app-modal>
  `
})
export class LegalParameterHistoryModalComponent implements OnChanges {
  @Input() visible = false;
  @Input() parameterName: string | null = null;

  @Output() visibleChange = new EventEmitter<boolean>();

  // Data
  history: LegalParameterDto[] = [];

  // State
  loading = false;

  constructor(private payrollService: PayrollReferentielService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['visible'] && this.visible && this.parameterName) {
      this.loadHistory();
    }
  }

  /**
   * Load parameter history from API
   */
  private loadHistory(): void {
    if (!this.parameterName) return;

    this.loading = true;
    this.history = [];

    this.payrollService.getLegalParameterHistory(this.parameterName).subscribe({
      next: (data) => {
        // Sort by effectiveFrom descending (newest first)
        this.history = data.sort((a, b) =>
          new Date(b.effectiveFrom).getTime() - new Date(a.effectiveFrom).getTime()
        );
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
      }
    });
  }

  /**
   * Check if parameter version is currently active
   */
  isCurrentlyActive(param: LegalParameterDto): boolean {
    if (!param.isActive) return false;

    const now = new Date();
    const effectiveFrom = new Date(param.effectiveFrom);
    const effectiveTo = param.effectiveTo ? new Date(param.effectiveTo) : null;

    if (now < effectiveFrom) return false;
    if (effectiveTo && now > effectiveTo) return false;

    return true;
  }

  /**
   * Check if parameter version is in the future
   */
  isFuture(param: LegalParameterDto): boolean {
    const now = new Date();
    const effectiveFrom = new Date(param.effectiveFrom);
    return now < effectiveFrom;
  }

  /**
   * Get status label
   */
  getStatusLabel(param: LegalParameterDto): string {
    if (!param.isActive) return 'Inactif';
    if (this.isFuture(param)) return 'Futur';
    if (this.isCurrentlyActive(param)) return 'Actuel';
    return 'Expiré';
  }

  /**
   * Get status CSS class
   */
  getStatusClass(param: LegalParameterDto): string {
    const status = this.getStatusLabel(param);
    switch (status) {
      case 'Actuel': return 'bg-green-100 text-green-800';
      case 'Inactif': return 'bg-gray-100 text-gray-600';
      case 'Futur': return 'bg-blue-100 text-blue-800';
      case 'Expiré': return 'bg-yellow-100 text-yellow-800';
      default: return 'bg-gray-100 text-gray-600';
    }
  }

  /**
   * Format parameter value for display
   */
  formatValue(param: LegalParameterDto): string {
    return formatParameterValue(param);
  }

  /**
   * Format effective period for display
   */
  formatPeriod(param: LegalParameterDto): string {
    return formatEffectivePeriod(param);
  }

  /**
   * Close modal
   */
  onClose(): void {
    this.visibleChange.emit(false);
  }

  onVisibleChange(visible: boolean): void {
    this.visibleChange.emit(visible);
  }
}
