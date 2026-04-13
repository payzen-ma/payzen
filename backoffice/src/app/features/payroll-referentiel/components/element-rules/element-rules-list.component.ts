import { Component, OnInit, OnChanges, SimpleChanges, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { PayrollReferentielService } from '../../../../services/payroll-referentiel/payroll-referentiel.service';
import {
  ReferentielElementListDto,
  ElementRuleDto,
  getRuleSummary,
  getExemptionTypeLabel
} from '../../../../models/payroll-referentiel';

interface ElementWithRules {
  element: ReferentielElementListDto;
  rules: ElementRuleDto[];
  expanded: boolean;
}

/**
 * Element Rules List Component
 * Displays all elements with their exemption rules grouped and expandable
 */
@Component({
  selector: 'app-element-rules-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="space-y-6">
      <!-- Toolbar -->
      <div class="flex flex-wrap items-center justify-between gap-4">
        <div class="flex items-center gap-4">
          <!-- Search -->
          <div class="relative">
            <input
              type="text"
              [(ngModel)]="searchTerm"
              (ngModelChange)="applyFilters()"
              placeholder="Rechercher un élément..."
              class="pl-10 pr-4 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 w-64">
            <svg class="w-4 h-4 text-gray-400 absolute left-3 top-1/2 -translate-y-1/2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path>
            </svg>
          </div>

          <!-- Show Only With Rules -->
          <label class="flex items-center gap-2 text-sm text-gray-600 cursor-pointer">
            <input
              type="checkbox"
              [(ngModel)]="showOnlyWithRules"
              (ngModelChange)="applyFilters()"
              class="w-4 h-4 text-primary-500 border-gray-300 rounded focus:ring-primary-500">
            Afficher uniquement avec règles
          </label>
        </div>
      </div>

      <!-- Loading State -->
      <div *ngIf="loading" class="flex items-center justify-center py-12">
        <svg class="w-8 h-8 text-primary-500 animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15"></path>
        </svg>
        <span class="ml-2 text-gray-600">Chargement...</span>
      </div>

      <!-- Empty State -->
      <div *ngIf="!loading && filteredElementsWithRules.length === 0" class="text-center py-12">
        <div class="w-16 h-16 mx-auto mb-4 rounded-full bg-gray-100 flex items-center justify-center">
          <span class="text-3xl">📋</span>
        </div>
        <h3 class="text-lg font-medium text-gray-900 mb-2">Aucun élément trouvé</h3>
        <p class="text-sm text-gray-500">
          {{ searchTerm ? 'Aucun résultat pour vos critères de recherche.' : 'Les règles seront affichées ici une fois que vous aurez créé des éléments.' }}
        </p>
      </div>

      <!-- Elements with Rules -->
      <div *ngIf="!loading && filteredElementsWithRules.length > 0" class="space-y-4">
        <div *ngFor="let item of filteredElementsWithRules" class="border border-gray-200 rounded-lg overflow-hidden">
          <!-- Element Header -->
          <div class="p-4 bg-gray-50 flex items-center justify-between cursor-pointer hover:bg-gray-100 transition-colors"
               (click)="item.expanded = !item.expanded">
            <div class="flex items-center gap-3">
              <!-- Expand/Collapse Icon -->
              <svg class="w-5 h-5 text-gray-400 transition-transform"
                   [class.rotate-90]="item.expanded"
                   fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7"></path>
              </svg>

              <div>
                <h3 class="font-medium text-gray-900">{{ item.element.name }}</h3>
                <p class="text-sm text-gray-500">{{ item.element.name }} • {{ item.element.categoryName }}</p>
              </div>
            </div>

            <div class="flex items-center gap-3">
              <!-- Rules Count Badge -->
              <span class="px-2 py-1 text-xs font-medium rounded-full"
                    [class.bg-green-100]="item.rules.length > 0"
                    [class.text-green-800]="item.rules.length > 0"
                    [class.bg-gray-100]="item.rules.length === 0"
                    [class.text-gray-600]="item.rules.length === 0">
                {{ item.rules.length }} règle(s)
              </span>

              <!-- CNSS+DGI Completeness Badge -->
              <span *ngIf="getMissingAuthority(item.rules)"
                    class="px-2 py-1 text-xs font-medium rounded-full bg-orange-100 text-orange-800"
                    [title]="'Règle ' + getMissingAuthority(item.rules) + ' manquante'">
                {{ getMissingAuthority(item.rules) }} manquant
              </span>

              <!-- Convergence Badge -->
              <span *ngIf="item.element.hasConvergence !== undefined && !getMissingAuthority(item.rules)"
                    class="px-2 py-1 text-xs font-medium rounded-full"
                    [class.bg-green-100]="item.element.hasConvergence"
                    [class.text-green-800]="item.element.hasConvergence"
                    [class.bg-yellow-100]="!item.element.hasConvergence"
                    [class.text-yellow-800]="!item.element.hasConvergence">
                {{ item.element.hasConvergence ? 'Convergence' : 'Divergence' }}
              </span>

              <!-- Element actions (stop propagation so header click doesn't toggle expand) -->
              <div class="flex items-center gap-2" (click)="$event.stopPropagation()">
                <button
                  (click)="onAddRuleForElement(item.element, $event)"
                  class="px-3 py-1.5 text-sm font-medium text-primary-600 hover:bg-primary-50 rounded-lg transition-colors">
                  + Ajouter règle
                </button>
                <button
                  (click)="onEditElementClick(item.element)"
                  class="px-3 py-1.5 text-sm font-medium text-gray-600 hover:bg-gray-100 rounded-lg transition-colors"
                  title="Modifier l'élément">
                  Modifier
                </button>
                <button
                  (click)="onDeleteElementClick(item.element, $event)"
                  class="p-1.5 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                  title="Supprimer l'élément">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                  </svg>
                </button>
              </div>
            </div>
          </div>

          <!-- Rules List (Expanded) -->
          <div *ngIf="item.expanded" class="border-t border-gray-200">
            <!-- No Rules -->
            <div *ngIf="item.rules.length === 0" class="p-6 text-center text-gray-500">
              <p class="text-sm">Aucune règle définie pour cet élément</p>
              <button
                (click)="onAddRuleForElement(item.element, $event)"
                class="mt-2 text-sm text-primary-600 hover:text-primary-700 hover:underline">
                Ajouter la première règle
              </button>
            </div>

            <!-- Rules Cards -->
            <div *ngIf="item.rules.length > 0" class="p-4 space-y-3">
              <div *ngFor="let rule of item.rules"
                   class="p-4 border border-gray-200 rounded-lg hover:shadow-sm transition-shadow">
                <div class="flex items-start justify-between">
                  <div class="flex-1">
                    <!-- Authority -->
                    <div class="flex items-center gap-2 mb-2">
                      <span class="text-lg">{{ getAuthorityIcon(rule.authorityName) }}</span>
                      <h4 class="font-medium text-gray-900">{{ rule.authorityName }}</h4>
                      <span *ngIf="isRuleActive(rule)"
                            class="px-2 py-0.5 text-xs font-medium bg-green-100 text-green-800 rounded-full">
                        Actif
                      </span>
                      <span *ngIf="!isRuleActive(rule)"
                            class="px-2 py-0.5 text-xs font-medium bg-gray-100 text-gray-600 rounded-full">
                        Inactif
                      </span>
                    </div>

                    <!-- Exemption Type -->
                    <p class="text-sm text-gray-600 mb-2">
                      <span class="font-medium">Type:</span> {{ getExemptionTypeLabel(rule.exemptionType) }}
                    </p>

                    <!-- Rule Summary -->
                    <p class="text-sm text-gray-700">
                      {{ getRuleSummary(rule) }}
                    </p>

                    <!-- Effective Period -->
                    <p class="text-xs text-gray-500 mt-2">
                      Effectif: {{ formatDate(rule.effectiveFrom) }}
                      {{ rule.effectiveTo ? ' → ' + formatDate(rule.effectiveTo) : ' → actuel' }}
                    </p>
                  </div>

                  <!-- Actions -->
                  <div class="flex items-center gap-2 ml-4">
                    <button
                      (click)="onEditRuleClick(rule)"
                      class="px-3 py-1.5 text-sm font-medium text-primary-600 hover:bg-primary-50 rounded-lg transition-colors">
                      Modifier
                    </button>
                    <button
                      (click)="onDeleteRuleClick(rule)"
                      class="px-3 py-1.5 text-sm font-medium text-red-600 hover:bg-red-50 rounded-lg transition-colors">
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                      </svg>
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class ElementRulesListComponent implements OnInit, OnChanges {
  @Input() refreshTrigger = 0;
  @Output() addRule = new EventEmitter<ReferentielElementListDto>();
  @Output() editRule = new EventEmitter<ElementRuleDto>();
  @Output() deleteRule = new EventEmitter<ElementRuleDto>();
  @Output() editElement = new EventEmitter<ReferentielElementListDto>();
  @Output() deleteElement = new EventEmitter<ReferentielElementListDto>();
  @Output() loaded = new EventEmitter<number>();
  @Output() elementCount = new EventEmitter<number>();

  // Data
  allElementsWithRules: ElementWithRules[] = [];
  filteredElementsWithRules: ElementWithRules[] = [];

  // Filters
  searchTerm = '';
  showOnlyWithRules = false;

  // State
  loading = false;

  constructor(private payrollService: PayrollReferentielService) {}

  ngOnInit(): void {
    this.loadElementsAndRules();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['refreshTrigger'] && this.refreshTrigger > 0) {
      this.loadElementsAndRules();
    }
  }

  /**
   * Load all elements and their rules
   */
  loadElementsAndRules(): void {
    this.loading = true;

    this.payrollService.getAllReferentielElements().subscribe({
      next: (elements: any) => {
        if (elements.length === 0) {
          this.allElementsWithRules = [];
          this.applyFilters();
          this.loaded.emit(0);
          this.elementCount.emit(0);
          this.loading = false;
          return;
        }

        // For each element, fetch its rules and combine with element
        const requests = elements.map((element: any) =>
          this.payrollService.getElementRules(element.id).pipe(
            map((rules: any) => ({
              element,
              rules,
              expanded: false
            })),
            catchError(() => of({ element, rules: [], expanded: false }))
          )
        );

        // Wait for all requests using forkJoin
        forkJoin(requests).subscribe({
          next: (results: any) => {
            this.allElementsWithRules = results;
            this.applyFilters();

            const totalRules = results.reduce((sum: any, item: any) => sum + item.rules.length, 0);
            this.loaded.emit(totalRules);
            this.elementCount.emit(results.length);

            this.loading = false;
          },
          error: (err: any) => {
            this.loading = false;
          }
        });
      },
      error: (err: any) => {
        this.loading = false;
      }
    });
  }

  /**
   * Apply filters
   */
  applyFilters(): void {
    let filtered = [...this.allElementsWithRules];

    // Filter by search term
    if (this.searchTerm) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(item =>
        item.element.name.toLowerCase().includes(term) ||
        item.element.name.toLowerCase().includes(term)
      );
    }

    // Filter by "only with rules"
    if (this.showOnlyWithRules) {
      filtered = filtered.filter(item => item.rules.length > 0);
    }

    this.filteredElementsWithRules = filtered;
  }

  /**
   * Check if rule is currently active
   */
  isRuleActive(rule: ElementRuleDto): boolean {
    const now = new Date();
    const effectiveFrom = new Date(rule.effectiveFrom);
    const effectiveTo = rule.effectiveTo ? new Date(rule.effectiveTo) : null;

    return effectiveFrom <= now && (!effectiveTo || effectiveTo >= now);
  }

  /**
   * Get authority icon
   */
  getAuthorityIcon(code: string): string {
    const icons: Record<string, string> = {
      'CNSS': '🏛️',
      'DGI': '📊',
      'IR': '📊',  // Keep for backwards compatibility
      'AMO': '🏥',
      'CIMR': '🏦'
    };
    return icons[code] || '📋';
  }

  /**
   * Format date for display
   */
  formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { year: 'numeric', month: 'short', day: 'numeric' });
  }

  /**
   * Get rule summary
   */
  getRuleSummary(rule: ElementRuleDto): string {
    return getRuleSummary(rule);
  }

  /**
   * Get exemption type label
   */
  getExemptionTypeLabel(type: string): string {
    return getExemptionTypeLabel(type as any);
  }

  /**
   * Check if an element is missing CNSS or DGI rule
   * Returns the missing authority name or null if both are present
   */
  getMissingAuthority(rules: ElementRuleDto[]): string | null {
    if (rules.length === 0) return null; // No rules at all - different case

    const hasCnss = rules.some(r => {
      const name = r.authorityName?.toLowerCase() || '';
      return name === 'cnss' || name.includes('sécurité') || name.includes('securite');
    });

    const hasDgi = rules.some(r => {
      const name = r.authorityName?.toLowerCase() || '';
      return name === 'dgi' || name === 'ir' || name.includes('impôt') || name.includes('impot');
    });

    if (hasCnss && !hasDgi) return 'DGI';
    if (hasDgi && !hasCnss) return 'CNSS';
    return null; // Both present or neither (neither is a different case)
  }

  // Event handlers
  onAddRuleForElement(element: ReferentielElementListDto, event: Event): void {
    event.stopPropagation(); // Prevent collapse/expand
    this.addRule.emit(element);
  }

  onEditRuleClick(rule: ElementRuleDto): void {
    this.editRule.emit(rule);
  }

  onDeleteRuleClick(rule: ElementRuleDto): void {
    this.deleteRule.emit(rule);
  }

  onEditElementClick(element: ReferentielElementListDto): void {
    this.editElement.emit(element);
  }

  onDeleteElementClick(element: ReferentielElementListDto, event: Event): void {
    event.stopPropagation();
    this.deleteElement.emit(element);
  }
}
