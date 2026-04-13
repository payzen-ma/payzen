import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, OnInit, Output, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  ElementCategoryDto,
  ElementStatus,
  getConvergenceStatusClass,
  getConvergenceStatusText,
  getElementStatusBadge,
  getPaymentFrequencyLabel,
  PaymentFrequency,
  ReferentielElementListDto
} from '../../../../models/payroll-referentiel';
import { LookupCacheService } from '../../../../services/payroll-referentiel/lookup-cache.service';
import { PayrollReferentielService } from '../../../../services/payroll-referentiel/payroll-referentiel.service';

/**
 * Referential Elements List Component
 * Displays compensation elements (Transport, Panier, etc.) with their rules summary
 */
@Component({
  selector: 'app-referentiel-elements-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="space-y-6">
      <!-- Header -->
      <div class="flex items-center justify-between">
        <div>
          <h2 class="text-lg font-semibold text-gray-900">Éléments du Référentiel</h2>
          <p class="text-sm text-gray-500">Composantes de rémunération (Transport, Panier, Primes, etc.)</p>
        </div>
        <button
          (click)="onAdd()"
          class="flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-primary-500 rounded-lg hover:bg-primary-600 transition-colors">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path>
          </svg>
          Ajouter un élément
        </button>
      </div>

      <!-- Filters -->
      <div class="flex flex-wrap items-center gap-4">
        <div class="flex-1 min-w-[200px] max-w-md">
          <div class="relative">
            <svg class="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path>
            </svg>
            <input
              type="text"
              [(ngModel)]="searchTerm"
              (ngModelChange)="applyFilters()"
              class="w-full pl-10 pr-4 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
              placeholder="Rechercher par code ou nom...">
          </div>
        </div>

        <select
          [(ngModel)]="filterCategory"
          (ngModelChange)="applyFilters()"
          class="px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500">
          <option value="">Toutes les catégories</option>
          <option *ngFor="let cat of categories" [value]="cat.id">{{ cat.name }}</option>
        </select>

        <select
          [(ngModel)]="filterConvergence"
          (ngModelChange)="applyFilters()"
          class="px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500">
          <option value="">Convergence: Tous</option>
          <option value="convergence">Convergence</option>
          <option value="divergence">Divergence</option>
        </select>

        <label class="flex items-center gap-2 text-sm text-gray-600">
          <input
            type="checkbox"
            [(ngModel)]="includeInactive"
            (ngModelChange)="loadElements()"
            class="w-4 h-4 text-primary-500 border-gray-300 rounded focus:ring-primary-500">
          Inclure les inactifs
        </label>
      </div>

      <!-- Stats Bar -->
      <div *ngIf="!loading" class="flex items-center gap-4 text-sm">
        <span class="text-gray-500">{{ filteredElements.length }} élément(s) affiché(s)</span>
        <span class="text-gray-400">|</span>
        <span class="flex items-center gap-1">
          <span class="w-2 h-2 rounded-full bg-green-500"></span>
          <span class="text-gray-600">{{ convergenceCount }} convergence(s)</span>
        </span>
        <span class="flex items-center gap-1">
          <span class="w-2 h-2 rounded-full bg-yellow-500"></span>
          <span class="text-gray-600">{{ divergenceCount }} divergence(s)</span>
        </span>
      </div>

      <!-- Loading State -->
      <div *ngIf="loading" class="flex items-center justify-center py-12">
        <svg class="w-8 h-8 animate-spin text-primary-500" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
      </div>

      <!-- Elements Table -->
      <div *ngIf="!loading" class="bg-white rounded-lg border border-gray-200 overflow-hidden">
        <!-- Empty State -->
        <div *ngIf="filteredElements.length === 0" class="flex flex-col items-center justify-center py-12 text-center">
          <div class="w-16 h-16 mb-4 rounded-full bg-gray-100 flex items-center justify-center">
            <span class="text-3xl">📋</span>
          </div>
          <h3 class="text-lg font-medium text-gray-900 mb-2">Aucun élément trouvé</h3>
          <p class="text-sm text-gray-500 max-w-md">
            {{ allElements.length === 0
               ? 'Créez un élément pour définir les composantes de rémunération.'
               : 'Aucun élément ne correspond à vos critères de recherche.' }}
          </p>
        </div>

        <!-- Table -->
        <table *ngIf="filteredElements.length > 0" class="min-w-full divide-y divide-gray-200">
          <thead class="bg-gray-50">
            <tr>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Code / Nom</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Catégorie</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Fréquence</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Statut</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Convergence</th>
              <th class="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Règles</th>
              <th class="px-4 py-3 text-right text-xs font-medium text-gray-500 uppercase tracking-wider">Actions</th>
            </tr>
          </thead>
          <tbody class="bg-white divide-y divide-gray-200">
            <tr *ngFor="let element of filteredElements; trackBy: trackById"
                class="hover:bg-gray-50 transition-colors">
              <td class="px-4 py-3">
                <div class="flex flex-col">
                  <span class="text-sm font-medium text-gray-900">{{ element.name }}</span>
                  <span *ngIf="element.code" class="text-xs text-gray-500 font-mono">{{ element.code }}</span>
                </div>
              </td>
              <td class="px-4 py-3">
                <span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 text-gray-800">
                  {{ element.categoryName }}
                </span>
              </td>
              <td class="px-4 py-3 text-sm text-gray-600">
                {{ getFrequencyLabel(element.defaultFrequency) }}
              </td>
              <td class="px-4 py-3">
                <span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium"
                      [class]="getStatusBadge(element.status).class">
                  {{ getStatusBadge(element.status).text }}
                </span>
              </td>
              <td class="px-4 py-3">
                <div class="flex items-center gap-2">
                  <span class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium"
                        [class]="getConvergenceClass(element.hasConvergence)">
                    {{ getConvergenceText(element.hasConvergence) }}
                  </span>
                  <div *ngIf="!element.hasCnssRule || !element.hasDgiRule" class="flex gap-1">
                    <span *ngIf="!element.hasCnssRule" class="inline-flex items-center px-1.5 py-0.5 rounded text-xs bg-red-50 text-red-700" title="Règle CNSS manquante">
                      CNSS
                    </span>
                    <span *ngIf="!element.hasDgiRule" class="inline-flex items-center px-1.5 py-0.5 rounded text-xs bg-red-50 text-red-700" title="Règle DGI manquante">
                      DGI
                    </span>
                  </div>
                </div>
              </td>
              <td class="px-4 py-3">
                <button
                  (click)="onViewRules(element)"
                  class="inline-flex items-center gap-1 text-sm text-primary-600 hover:text-primary-700 hover:underline">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"></path>
                  </svg>
                  {{ element.ruleCount }} règle(s)
                </button>
              </td>
              <td class="px-4 py-3 text-right">
                <div class="flex items-center justify-end gap-1">
                  <button
                    (click)="onAddRule(element)"
                    class="p-1.5 text-gray-400 hover:text-green-600 hover:bg-green-50 rounded transition-colors"
                    title="Ajouter une règle">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v3m0 0v3m0-3h3m-3 0H9m12 0a9 9 0 11-18 0 9 9 0 0118 0z"></path>
                    </svg>
                  </button>
                  <button
                    (click)="onEdit(element)"
                    class="p-1.5 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded transition-colors"
                    title="Modifier">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"></path>
                    </svg>
                  </button>
                  <button
                    (click)="onDelete(element)"
                    class="p-1.5 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded transition-colors"
                    title="Supprimer">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                    </svg>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  `
})
export class ReferentielElementsListComponent implements OnInit, OnChanges {
  @Input() refreshTrigger = 0;
  @Output() add = new EventEmitter<void>();
  @Output() edit = new EventEmitter<ReferentielElementListDto>();
  @Output() delete = new EventEmitter<ReferentielElementListDto>();
  @Output() viewRules = new EventEmitter<ReferentielElementListDto>();
  @Output() addRule = new EventEmitter<ReferentielElementListDto>();
  @Output() loaded = new EventEmitter<number>();

  allElements: ReferentielElementListDto[] = [];
  filteredElements: ReferentielElementListDto[] = [];
  categories: ElementCategoryDto[] = [];

  loading = false;
  searchTerm = '';
  filterCategory = '';
  filterConvergence = '';
  includeInactive = false;

  constructor(
    private payrollService: PayrollReferentielService,
    private lookupCache: LookupCacheService
  ) { }

  ngOnInit(): void {
    this.loadCategories();
    this.loadElements();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['refreshTrigger'] && this.refreshTrigger > 0) {
      this.loadElements();
    }
  }

  private loadCategories(): void {
    this.lookupCache.getCategories().subscribe({
      next: (cats) => this.categories = cats,
      error: (err) => alert('Erreur lors du chargement des catégories d\'éléments.')
    });
  }

  loadElements(): void {
    this.loading = true;
    const categoryId = this.filterCategory ? parseInt(this.filterCategory) : undefined;

    this.payrollService.getAllReferentielElements(this.includeInactive, categoryId).subscribe({
      next: (elements: any) => {
        this.allElements = elements;
        this.applyFilters();
        this.loaded.emit(elements.length);
        this.loading = false;
      },
      error: (err: any) => {
        this.loading = false;
      }
    });
  }

  applyFilters(): void {
    let filtered = [...this.allElements];

    // Search filter
    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(e =>
        e.name.toLowerCase().includes(term) ||
        (e.code && e.code.toLowerCase().includes(term))
      );
    }

    // Convergence filter
    if (this.filterConvergence === 'convergence') {
      filtered = filtered.filter(e => e.hasConvergence);
    } else if (this.filterConvergence === 'divergence') {
      filtered = filtered.filter(e => !e.hasConvergence);
    }

    // Sort by name
    filtered.sort((a, b) => a.name.localeCompare(b.name));

    this.filteredElements = filtered;
  }

  get convergenceCount(): number {
    return this.filteredElements.filter(e => e.hasConvergence).length;
  }

  get divergenceCount(): number {
    return this.filteredElements.filter(e => !e.hasConvergence).length;
  }

  getFrequencyLabel(freq: PaymentFrequency): string {
    return getPaymentFrequencyLabel(freq);
  }

  getConvergenceText(isConvergence: boolean): string {
    return getConvergenceStatusText(isConvergence);
  }

  getConvergenceClass(isConvergence: boolean): string {
    return getConvergenceStatusClass(isConvergence);
  }

  getStatusBadge(status: ElementStatus): { text: string; class: string } {
    return getElementStatusBadge(status);
  }

  trackById(index: number, item: ReferentielElementListDto): number {
    return item.id;
  }

  onAdd(): void {
    this.add.emit();
  }

  onEdit(element: ReferentielElementListDto): void {
    this.edit.emit(element);
  }

  onDelete(element: ReferentielElementListDto): void {
    this.delete.emit(element);
  }

  onViewRules(element: ReferentielElementListDto): void {
    this.viewRules.emit(element);
  }

  onAddRule(element: ReferentielElementListDto): void {
    this.addRule.emit(element);
  }
}
