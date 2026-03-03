import { Component, OnInit, OnChanges, SimpleChanges, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { PayrollReferentielService } from '../../../../services/payroll-referentiel/payroll-referentiel.service';
import {
  LegalParameterDto,
  LegalParameterType,
  getLegalParameterType,
  getLegalParameterTypeLabel,
  groupParametersByType,
  formatParameterValue,
  formatEffectivePeriod,
  isParameterCurrentlyActive
} from '../../../../models/payroll-referentiel';

/**
 * Legal Parameters List Component
 * Displays legal parameters (SMIG, CIMR rates, etc.) in a card layout grouped by type
 */
@Component({
  selector: 'app-legal-parameters-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="space-y-6">
      <!-- Toolbar -->
      <div class="flex flex-wrap items-center justify-between gap-4">
        <div class="flex items-center gap-4">
          <!-- Type Filter -->
          <div class="relative">
            <select
              [(ngModel)]="filterType"
              (ngModelChange)="applyFilters()"
              class="pl-3 pr-10 py-2 text-sm border border-gray-300 rounded-lg bg-white focus:ring-2 focus:ring-primary-500 focus:border-primary-500">
              <option value="">Tous les types</option>
              <option *ngFor="let type of parameterTypes" [value]="type.value">{{ type.label }}</option>
            </select>
          </div>
          
          <!-- Search -->
          <div class="relative">
            <input
              type="text"
              [(ngModel)]="searchTerm"
              (ngModelChange)="applyFilters()"
              placeholder="Rechercher..."
              class="pl-10 pr-4 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 w-64">
            <svg class="w-4 h-4 text-gray-400 absolute left-3 top-1/2 -translate-y-1/2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path>
            </svg>
          </div>
          
          <!-- Include Inactive Toggle -->
          <label class="flex items-center gap-2 text-sm text-gray-600 cursor-pointer">
            <input
              type="checkbox"
              [(ngModel)]="includeInactive"
              (ngModelChange)="loadParameters()"
              class="w-4 h-4 text-primary-500 border-gray-300 rounded focus:ring-primary-500">
            Inclure les inactifs
          </label>
        </div>
        
        <!-- Add Button -->
        <button
          (click)="onAddParameter()"
          class="flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-primary-500 rounded-lg hover:bg-primary-600 transition-colors">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path>
          </svg>
          Ajouter un paramètre
        </button>
      </div>

      <!-- Freshness Warning Banner -->
      <div *ngIf="freshnessData?.hasCriticalStale" class="bg-amber-50 border border-amber-200 rounded-lg p-4">
        <div class="flex items-start gap-3">
          <div class="flex-shrink-0">
            <svg class="w-5 h-5 text-amber-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"></path>
            </svg>
          </div>
          <div class="flex-1">
            <h4 class="text-sm font-semibold text-amber-800">Paramètres critiques à vérifier</h4>
            <p class="text-sm text-amber-700 mt-1">
              Les paramètres suivants n'ont pas été mis à jour depuis plus de 6 mois. Veuillez vérifier si les valeurs légales ont changé.
            </p>
            <ul class="mt-2 space-y-1">
              <li *ngFor="let param of freshnessData?.criticalStale" class="text-sm text-amber-800 flex items-center gap-2">
                <span class="font-medium">{{ param.name }}</span>
                <span class="text-amber-600">({{ param.value }} {{ param.unit }})</span>
                <span class="text-amber-500">- dernière MAJ {{ formatLastUpdated(param.lastUpdated) }}</span>
              </li>
            </ul>
          </div>
          <button
            (click)="dismissFreshnessWarning()"
            class="flex-shrink-0 text-amber-500 hover:text-amber-700">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
            </svg>
          </button>
        </div>
      </div>

      <!-- Non-critical stale parameters info (collapsible) -->
      <div *ngIf="freshnessData?.hasStaleParameters && !freshnessData?.hasCriticalStale" class="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <div class="flex items-start gap-3">
          <div class="flex-shrink-0">
            <svg class="w-5 h-5 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path>
            </svg>
          </div>
          <div class="flex-1">
            <h4 class="text-sm font-semibold text-blue-800">{{ freshnessData?.staleParameters?.length }} paramètre(s) non mis à jour récemment</h4>
            <p class="text-sm text-blue-700 mt-1">
              Certains paramètres n'ont pas été modifiés depuis plus de 6 mois. Cliquez sur "Modifier" pour vérifier leur validité.
            </p>
          </div>
          <button
            (click)="dismissFreshnessWarning()"
            class="flex-shrink-0 text-blue-500 hover:text-blue-700">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path>
            </svg>
          </button>
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
      <div *ngIf="!loading && filteredParameters.length === 0" class="text-center py-12">
        <div class="w-16 h-16 mx-auto mb-4 rounded-full bg-gray-100 flex items-center justify-center">
          <span class="text-3xl">📭</span>
        </div>
        <h3 class="text-lg font-medium text-gray-900 mb-2">Aucun paramètre trouvé</h3>
        <p class="text-sm text-gray-500 mb-4">
          {{ searchTerm || filterType ? 'Aucun résultat pour vos critères de recherche.' : 'Commencez par ajouter un paramètre légal.' }}
        </p>
        <button
          *ngIf="!searchTerm && !filterType"
          (click)="onAddParameter()"
          class="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-primary-500 rounded-lg hover:bg-primary-600">
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"></path>
          </svg>
          Ajouter un paramètre
        </button>
      </div>

      <!-- Parameters Grid - Grouped by Type -->
      <div *ngIf="!loading && filteredParameters.length > 0" class="space-y-8">
        <div *ngFor="let group of groupedParameters" class="space-y-4">
          <!-- Group Header -->
          <div class="flex items-center gap-3">
            <span class="text-lg" [ngClass]="getTypeIcon(group.type)">{{ getTypeEmoji(group.type) }}</span>
            <h3 class="text-lg font-semibold text-gray-900">{{ group.label }}</h3>
            <span class="px-2 py-0.5 text-xs font-medium rounded-full bg-gray-100 text-gray-600">
              {{ group.parameters.length }}
            </span>
          </div>
          
          <!-- Parameter Cards -->
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            <div
              *ngFor="let param of group.parameters"
              class="bg-white border rounded-lg p-4 hover:shadow-md transition-shadow"
              [class.border-gray-200]="param.isActive"
              [class.border-gray-300]="!param.isActive"
              [class.bg-gray-50]="!param.isActive">
              
              <!-- Card Header -->
              <div class="flex items-start justify-between mb-3">
                <div>
                  <h4 class="font-medium text-gray-900">{{ param.name }}</h4>
                </div>
                <span
                  class="px-2 py-1 text-xs font-medium rounded-full"
                  [ngClass]="getStatusClass(param)">
                  {{ getStatusLabel(param) }}
                </span>
              </div>
              
              <!-- Value -->
              <div class="mb-3">
                <div class="text-2xl font-bold text-primary-600">
                  {{ formatValue(param) }}
                </div>
              </div>
              
              <!-- Description -->
              <p *ngIf="param.description" class="text-sm text-gray-600 mb-3 line-clamp-2">
                {{ param.description }}
              </p>
              
              <!-- Effective Period -->
              <div class="text-xs text-gray-500 mb-4 flex items-center gap-1">
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z"></path>
                </svg>
                {{ formatPeriod(param) }}
              </div>
              
              <!-- Actions -->
              <div class="flex items-center gap-2 pt-3 border-t border-gray-100">
                <button
                  (click)="onEditParameter(param)"
                  class="flex-1 px-3 py-1.5 text-sm font-medium text-primary-600 hover:bg-primary-50 rounded-lg transition-colors">
                  Modifier
                </button>
                <button
                  (click)="onViewHistory(param)"
                  class="flex-1 px-3 py-1.5 text-sm font-medium text-gray-600 hover:bg-gray-100 rounded-lg transition-colors">
                  Historique
                </button>
                <button
                  (click)="onDeleteParameter(param)"
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
  `
})
export class LegalParametersListComponent implements OnInit, OnChanges {
  @Input() refreshTrigger = 0;
  @Output() add = new EventEmitter<void>();
  @Output() edit = new EventEmitter<LegalParameterDto>();
  @Output() delete = new EventEmitter<LegalParameterDto>();
  @Output() viewHistory = new EventEmitter<LegalParameterDto>();
  @Output() loaded = new EventEmitter<number>();
  
  // Data
  allParameters: LegalParameterDto[] = [];
  filteredParameters: LegalParameterDto[] = [];
  groupedParameters: { type: LegalParameterType; label: string; parameters: LegalParameterDto[] }[] = [];
  
  // Filters
  filterType = '';
  searchTerm = '';
  includeInactive = false;
  
  // State
  loading = false;

  // Freshness check
  freshnessData: {
    hasStaleParameters: boolean;
    hasCriticalStale: boolean;
    staleParameters: { id: number; name: string; value: number; unit: string; lastUpdated: string; effectiveFrom: string }[];
    criticalStale: { id: number; name: string; value: number; unit: string; lastUpdated: string; effectiveFrom: string }[];
  } | null = null;

  // Parameter types for dropdown
  parameterTypes = [
    { value: LegalParameterType.SMIG, label: 'SMIG' },
    { value: LegalParameterType.SMAG, label: 'SMAG' },
    { value: LegalParameterType.CIMR, label: 'CIMR' },
    { value: LegalParameterType.CNSS, label: 'CNSS' },
    { value: LegalParameterType.AMO, label: 'AMO' },
    { value: LegalParameterType.IR, label: 'IR' },
    { value: LegalParameterType.OTHER, label: 'Autre' }
  ];
  
  constructor(
    private payrollService: PayrollReferentielService
  ) {}
  
  ngOnInit(): void {
    this.loadParameters();
    this.checkFreshness();
  }

  /**
   * Check parameter freshness (SMIG/SMAG staleness warning)
   */
  checkFreshness(): void {
    this.payrollService.checkParameterFreshness().subscribe({
      next: (data) => {
        this.freshnessData = data;
      },
      error: (err) => {
        console.error('Failed to check parameter freshness:', err);
      }
    });
  }

  /**
   * Dismiss freshness warning
   */
  dismissFreshnessWarning(): void {
    this.freshnessData = null;
  }

  /**
   * Format last updated date for freshness warning
   */
  formatLastUpdated(dateStr: string): string {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMonths = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60 * 24 * 30));
    if (diffMonths >= 12) {
      const years = Math.floor(diffMonths / 12);
      return `il y a ${years} an${years > 1 ? 's' : ''}`;
    }
    return `il y a ${diffMonths} mois`;
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['refreshTrigger'] && this.refreshTrigger > 0) {
      this.loadParameters();
    }
  }

  /**
   * Load parameters from API
   */
  loadParameters(): void {
    this.loading = true;

    this.payrollService.getAllLegalParameters(this.includeInactive).subscribe({
      next: (params) => {
        this.allParameters = params;
        this.applyFilters();
        this.loaded.emit(params.length);
        this.loading = false;
      },
      error: (err) => {
        console.error('Failed to load legal parameters:', err);
        this.loading = false;
      }
    });
  }
  
  /**
   * Apply filters and group parameters
   */
  applyFilters(): void {
    let filtered = [...this.allParameters];
    
    // Filter by type
    if (this.filterType) {
      filtered = filtered.filter(p => getLegalParameterType(p.name) === this.filterType);
    }
    
    // Filter by search term
    if (this.searchTerm) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(p =>
        p.name.toLowerCase().includes(term) ||
        (p.description && p.description.toLowerCase().includes(term))
      );
    }
    
    this.filteredParameters = filtered;
    this.groupParameters();
  }
  
  /**
   * Group parameters by type for display
   */
  private groupParameters(): void {
    const groups = groupParametersByType(this.filteredParameters);
    
    this.groupedParameters = Array.from(groups.entries())
      .map(([type, params]) => ({
        type,
        label: getLegalParameterTypeLabel(type),
        parameters: params.sort((a, b) => a.name.localeCompare(b.name))
      }))
      .sort((a, b) => {
        // Sort groups: SMIG first, then CIMR, CNSS, AMO, IR, OTHER
        const order = [
          LegalParameterType.SMIG,
          LegalParameterType.SMAG,
          LegalParameterType.CIMR,
          LegalParameterType.CNSS,
          LegalParameterType.AMO,
          LegalParameterType.IR,
          LegalParameterType.OTHER
        ];
        return order.indexOf(a.type) - order.indexOf(b.type);
      });
  }
  
  /**
   * Get emoji for parameter type
   */
  getTypeEmoji(type: LegalParameterType): string {
    const emojis: Record<LegalParameterType, string> = {
      [LegalParameterType.SMIG]: '💰',
      [LegalParameterType.SMAG]: '🌾',
      [LegalParameterType.CIMR]: '🏦',
      [LegalParameterType.CNSS]: '🏛️',
      [LegalParameterType.AMO]: '🏥',
      [LegalParameterType.IR]: '📊',
      [LegalParameterType.OTHER]: '📋'
    };
    return emojis[type] || '📋';
  }
  
  /**
   * Get icon class for parameter type (unused, for future reference)
   */
  getTypeIcon(type: LegalParameterType): string {
    return '';
  }
  
  /**
   * Get status label
   */
  getStatusLabel(param: LegalParameterDto): string {
    if (!param.isActive) return 'Inactif';
    
    const now = new Date();
    const effectiveFrom = new Date(param.effectiveFrom);
    const effectiveTo = param.effectiveTo ? new Date(param.effectiveTo) : null;
    
    if (now < effectiveFrom) return 'Futur';
    if (effectiveTo && now > effectiveTo) return 'Expiré';
    
    return 'Actif';
  }
  
  /**
   * Get status CSS class
   */
  getStatusClass(param: LegalParameterDto): string {
    const status = this.getStatusLabel(param);
    switch (status) {
      case 'Actif': return 'bg-green-100 text-green-800';
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
  
  // Event handlers
  onAddParameter(): void {
    this.add.emit();
  }
  
  onEditParameter(param: LegalParameterDto): void {
    this.edit.emit(param);
  }
  
  onViewHistory(param: LegalParameterDto): void {
    this.viewHistory.emit(param);
  }
  
  onDeleteParameter(param: LegalParameterDto): void {
    // Emit delete event to parent - parent handles confirmation
    this.delete.emit(param);
  }
}
