import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AutoRules, SENIORITY_RATES, RegulationVersion } from '../../../../../models/salary-package.model';

@Component({
  selector: 'app-auto-rules-card',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="bg-white rounded-lg shadow-sm border border-gray-200 overflow-hidden">
      <div class="px-6 py-4 border-b border-gray-100 bg-gray-50">
        <h2 class="font-semibold text-gray-900 flex items-center gap-2">
          <svg class="w-5 h-5 text-purple-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 10V3L4 14h7v7l9-11h-7z" />
          </svg>
          Règles automatiques
          <span class="text-xs px-2 py-0.5 bg-purple-100 text-purple-700 rounded-full">{{ getRegulationLabel() }}</span>
        </h2>
        <p class="text-xs text-gray-500 mt-1">Calculs automatiques selon la réglementation marocaine</p>
      </div>

      <div class="p-6 space-y-6">
        <!-- Seniority Bonus Toggle -->
        <div class="flex items-start gap-4">
          <div class="flex-1">
            <label class="flex items-center gap-3 cursor-pointer" [class.cursor-not-allowed]="readonly" [class.opacity-60]="readonly">
              <div class="relative">
                <input 
                  type="checkbox" 
                  [ngModel]="autoRules.seniorityBonusEnabled"
                  (ngModelChange)="onSeniorityToggle($event)"
                  [disabled]="readonly"
                  class="sr-only peer" />
                <div class="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-purple-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-purple-600"></div>
              </div>
              <div>
                <div class="font-medium text-gray-900">Prime d'ancienneté</div>
                <div class="text-sm text-gray-500">Calcul automatique selon le Code du travail</div>
              </div>
            </label>
          </div>
          <span *ngIf="autoRules.seniorityBonusEnabled" class="px-2 py-1 text-xs font-medium bg-green-100 text-green-700 rounded">
            Activé
          </span>
        </div>

        <!-- Seniority Rates Table -->
        <div *ngIf="autoRules.seniorityBonusEnabled" class="bg-purple-50 rounded-lg p-4">
          <h4 class="text-sm font-medium text-purple-900 mb-3 flex items-center gap-2">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
            </svg>
            Barème d'ancienneté (base: salaire de base)
          </h4>

          <div class="space-y-2">
            <div *ngFor="let tier of seniorityRates" 
                 class="flex items-center justify-between py-2 px-3 bg-white rounded-lg">
              <span class="text-sm text-gray-700">{{ tier.label.split(':')[0] }}</span>
              <span class="font-semibold text-purple-700">{{ formatPercent(tier.rate) }}</span>
            </div>
          </div>

          <p class="text-xs text-purple-600 mt-3 flex items-start gap-1">
            <svg class="w-4 h-4 shrink-0 mt-0.5" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
            </svg>
            La prime s'applique automatiquement lors de la génération du bulletin de paie selon l'ancienneté de l'employé.
          </p>
        </div>

        <!-- Disabled State Info -->
        <div *ngIf="!autoRules.seniorityBonusEnabled" class="bg-gray-50 rounded-lg p-4">
          <div class="flex items-start gap-3">
            <svg class="w-5 h-5 text-gray-400 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <div>
              <p class="text-sm text-gray-600">
                La prime d'ancienneté est désactivée pour ce template. Elle ne sera pas calculée automatiquement lors de la génération des bulletins.
              </p>
              <p class="text-xs text-gray-500 mt-2">
                Note: Vous pouvez toujours ajouter un élément "Prime d'ancienneté" manuellement dans les éléments fixes.
              </p>
            </div>
          </div>
        </div>

        <!-- Future Rules Placeholder -->
        <div class="border-t border-gray-100 pt-4">
          <div class="flex items-center gap-2 text-sm text-gray-400">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v6m0 0v6m0-6h6m-6 0H6" />
            </svg>
            <span>Autres règles automatiques à venir...</span>
          </div>
        </div>
      </div>
    </div>
  `
})
export class AutoRulesCardComponent {
  @Input() autoRules: AutoRules = { seniorityBonusEnabled: true, ruleVersion: 'MA_2025' };
  @Input() regulationVersion: RegulationVersion = 'MA_2025';
  @Input() readonly = false;
  @Output() autoRulesChange = new EventEmitter<AutoRules>();

  seniorityRates = SENIORITY_RATES;

  onSeniorityToggle(enabled: boolean): void {
    const newRules: AutoRules = {
      ...this.autoRules,
      seniorityBonusEnabled: enabled
    };
    this.autoRulesChange.emit(newRules);
  }

  getRegulationLabel(): string {
    switch (this.regulationVersion) {
      case 'MA_2025':
        return 'Maroc 2025';
      default:
        return this.regulationVersion;
    }
  }

  formatPercent(value: number): string {
    return `${(value * 100).toFixed(0)}%`;
  }
}
