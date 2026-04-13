import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ModalComponent } from '../../../../shared/modal/modal.component';
import { PayrollReferentielService } from '../../../../services/payroll-referentiel/payroll-referentiel.service';
import {
  ReferentielElementListDto,
  ReferentielElementDto,
  ElementRuleDto,
  ExemptionType,
  getRuleSummary,
  formatRuleCap,
  formatRulePercentage,
  formatRuleFormula,
  formatRuleTier,
  getExemptionTypeLabel
} from '../../../../models/payroll-referentiel';

/**
 * Element Rules View Component
 * Displays all rules for a specific element
 */
@Component({
  selector: 'app-element-rules-view',
  standalone: true,
  imports: [CommonModule, ModalComponent],
  template: `
    <app-modal [(visible)]="visible" [title]="modalTitle" size="lg" (visibleChange)="onVisibleChange($event)">
      <div class="space-y-6">
        <!-- Loading -->
        <div *ngIf="loading" class="flex items-center justify-center py-12">
          <svg class="w-8 h-8 animate-spin text-primary-500" fill="none" viewBox="0 0 24 24">
            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
          </svg>
        </div>

        <!-- Element Info -->
        <div *ngIf="!loading && elementDetails" class="p-4 bg-gray-50 rounded-lg">
          <div class="flex items-center justify-between">
            <div>
              <h3 class="text-lg font-semibold text-gray-900">{{ elementDetails.name }}</h3>
              <p class="text-sm text-gray-500">Catégorie: {{ elementDetails.categoryName }}</p>
            </div>
            <span class="px-2 py-1 text-xs font-medium rounded-full"
                  [class.bg-green-100]="elementDetails.hasConvergence"
                  [class.text-green-800]="elementDetails.hasConvergence"
                  [class.bg-yellow-100]="!elementDetails.hasConvergence"
                  [class.text-yellow-800]="!elementDetails.hasConvergence">
              {{ elementDetails.hasConvergence ? 'Convergence' : 'Divergence' }}
            </span>
          </div>
        </div>

        <!-- Rules List -->
        <div *ngIf="!loading">
          <!-- Empty State -->
          <div *ngIf="rules.length === 0" class="text-center py-8">
            <div class="w-16 h-16 mx-auto mb-4 rounded-full bg-gray-100 flex items-center justify-center">
              <span class="text-3xl">📋</span>
            </div>
            <h3 class="text-lg font-medium text-gray-900 mb-2">Aucune règle définie</h3>
            <p class="text-sm text-gray-500 mb-4">Ajoutez des règles d'exonération pour cet élément</p>
            <button
              (click)="onAddRule()"
              class="px-4 py-2 text-sm font-medium text-white bg-primary-500 rounded-lg hover:bg-primary-600">
              Ajouter une règle
            </button>
          </div>

          <!-- Rules Cards -->
          <div *ngIf="rules.length > 0" class="space-y-4">
            <div class="flex items-center justify-between mb-2">
              <h4 class="text-sm font-medium text-gray-700">{{ rules.length }} règle(s) configurée(s)</h4>
              <button
                (click)="onAddRule()"
                class="text-sm text-primary-600 hover:text-primary-700 hover:underline">
                + Ajouter
              </button>
            </div>

            <div *ngFor="let rule of rules; trackBy: trackById"
                 class="border border-gray-200 rounded-lg overflow-hidden">
              <!-- Rule Header -->
              <div class="p-4 flex items-start justify-between"
                   [class.bg-green-50]="isRuleActive(rule)"
                   [class.bg-gray-50]="!isRuleActive(rule)">
                <div class="flex items-center gap-3">
                  <div class="w-10 h-10 rounded-full flex items-center justify-center"
                       [class.bg-green-100]="isRuleActive(rule)"
                       [class.bg-gray-200]="!isRuleActive(rule)">
                    <span class="text-lg">{{ getAuthorityIcon(rule.authorityName) }}</span>
                  </div>
                  <div>
                    <p class="font-medium text-gray-900">{{ rule.authorityName }}</p>
                    <p class="text-sm text-gray-500">{{ getExemptionTypeLabel(rule.exemptionType) }}</p>
                  </div>
                </div>
                <div class="flex items-center gap-2">
                  <span *ngIf="isRuleActive(rule)"
                        class="px-2 py-0.5 text-xs font-medium bg-green-100 text-green-800 rounded-full">
                    Actif
                  </span>
                  <span *ngIf="!isRuleActive(rule)"
                        class="px-2 py-0.5 text-xs font-medium bg-gray-100 text-gray-600 rounded-full">
                    Inactif
                  </span>
                  <button
                    (click)="onEditRule(rule)"
                    class="p-1.5 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded transition-colors"
                    title="Modifier">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"></path>
                    </svg>
                  </button>
                  <button
                    (click)="onDeleteRule(rule)"
                    class="p-1.5 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded transition-colors"
                    title="Supprimer">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                    </svg>
                  </button>
                </div>
              </div>

              <!-- Rule Details -->
              <div class="p-4 bg-white border-t border-gray-100">
                <div class="grid grid-cols-2 gap-4 text-sm">
                  <!-- Summary -->
                  <div>
                    <p class="text-xs text-gray-500 mb-1">Configuration</p>
                    <p class="text-gray-900">{{ getRuleSummary(rule) }}</p>
                  </div>

                  <!-- Period -->
                  <div>
                    <p class="text-xs text-gray-500 mb-1">Période</p>
                    <p class="text-gray-900">
                      {{ formatDate(rule.effectiveFrom) }} →
                      {{ rule.effectiveTo ? formatDate(rule.effectiveTo) : 'En cours' }}
                    </p>
                  </div>

                  <!-- Cap Details -->
                  <div *ngIf="rule.cap">
                    <p class="text-xs text-gray-500 mb-1">Plafond</p>
                    <p class="text-gray-900 font-medium">{{ formatCap(rule) }}</p>
                  </div>

                  <!-- Percentage Details -->
                  <div *ngIf="rule.percentage">
                    <p class="text-xs text-gray-500 mb-1">Pourcentage</p>
                    <p class="text-gray-900 font-medium">{{ formatPercentage(rule) }}</p>
                  </div>

                  <!-- Formula Details -->
                  <div *ngIf="rule.formula">
                    <p class="text-xs text-gray-500 mb-1">Formule</p>
                    <p class="text-gray-900 font-medium">{{ formatFormula(rule) }}</p>
                  </div>

                  <!-- Source Reference -->
                  <div *ngIf="rule.sourceRef" class="col-span-2">
                    <p class="text-xs text-gray-500 mb-1">Référence légale</p>
                    <p class="text-gray-600 italic">{{ rule.sourceRef }}</p>
                  </div>
                </div>

                <!-- Tiers -->
                <div *ngIf="rule.tiers && rule.tiers.length > 0" class="mt-4">
                  <p class="text-xs text-gray-500 mb-2">Tranches</p>
                  <div class="space-y-1">
                    <div *ngFor="let tier of rule.tiers" class="text-sm text-gray-700 pl-3 border-l-2 border-gray-200">
                      {{ formatTier(tier) }}
                    </div>
                  </div>
                </div>

                <!-- Variants -->
                <div *ngIf="rule.variants && rule.variants.length > 0" class="mt-4">
                  <p class="text-xs text-gray-500 mb-2">Variantes</p>
                  <div class="flex flex-wrap gap-2">
                    <span *ngFor="let variant of rule.variants"
                          class="px-2 py-1 text-xs bg-gray-100 text-gray-700 rounded">
                      {{ variant.variantLabel }}
                      <span *ngIf="variant.overrideCap" class="text-gray-500">({{ variant.overrideCap }} MAD)</span>
                    </span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Actions -->
        <div class="flex justify-end pt-4 border-t">
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
export class ElementRulesViewComponent implements OnChanges {
  @Input() visible = false;
  @Input() element: ReferentielElementListDto | null = null;

  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() addRule = new EventEmitter<ReferentielElementListDto>();
  @Output() editRule = new EventEmitter<ElementRuleDto>();
  @Output() deleteRule = new EventEmitter<ElementRuleDto>();

  elementDetails: ReferentielElementDto | null = null;
  rules: ElementRuleDto[] = [];
  loading = false;

  get modalTitle(): string {
    return this.element ? `Règles: ${this.element.name}` : 'Règles d\'exonération';
  }

  constructor(private payrollService: PayrollReferentielService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['visible'] && this.visible && this.element) {
      this.loadElementDetails();
    }
  }

  private loadElementDetails(): void {
    if (!this.element) return;

    this.loading = true;
    this.payrollService.getReferentielElementById(this.element.id).subscribe({
      next: (details: any) => {
        this.elementDetails = details;
        this.rules = details.rules || [];
        this.loading = false;
      },
      error: (err: any) => {
        this.loading = false;
      }
    });
  }

  isRuleActive(rule: ElementRuleDto): boolean {
    if (!rule.isActive) return false;

    const now = new Date();
    const effectiveFrom = new Date(rule.effectiveFrom);
    const effectiveTo = rule.effectiveTo ? new Date(rule.effectiveTo) : null;

    return effectiveFrom <= now && (!effectiveTo || effectiveTo >= now);
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
  }

  getAuthorityIcon(code: string): string {
    const icons: Record<string, string> = {
      'CNSS': '🏛️',
      'IR': '💰',
      'AMO': '🏥',
      'CIMR': '📊'
    };
    return icons[code] || '📋';
  }

  getExemptionTypeLabel(type: ExemptionType): string {
    return getExemptionTypeLabel(type);
  }

  getRuleSummary(rule: ElementRuleDto): string {
    return getRuleSummary(rule);
  }

  formatCap(rule: ElementRuleDto): string {
    return rule.cap ? formatRuleCap(rule.cap) : '';
  }

  formatPercentage(rule: ElementRuleDto): string {
    return rule.percentage ? formatRulePercentage(rule.percentage) : '';
  }

  formatFormula(rule: ElementRuleDto): string {
    return rule.formula ? formatRuleFormula(rule.formula) : '';
  }

  formatTier(tier: any): string {
    return formatRuleTier(tier);
  }

  trackById(index: number, item: ElementRuleDto): number {
    return item.id;
  }

  onAddRule(): void {
    if (this.element) {
      this.addRule.emit(this.element);
    }
  }

  onEditRule(rule: ElementRuleDto): void {
    this.editRule.emit(rule);
  }

  onDeleteRule(rule: ElementRuleDto): void {
    this.deleteRule.emit(rule);
  }

  onClose(): void {
    this.visibleChange.emit(false);
  }

  onVisibleChange(visible: boolean): void {
    this.visibleChange.emit(visible);
  }
}
