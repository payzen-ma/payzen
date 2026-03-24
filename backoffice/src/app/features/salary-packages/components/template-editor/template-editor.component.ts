import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, inject } from '@angular/core';
import { CommonModule, KeyValuePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  SalaryPackage,
  SalaryPackageItem,
  SalaryPackageStatus,
  SalaryComponentType,
  PayComponent,
  AutoRules,
  CimrConfig,
  CimrRegime,
  CIMR_AL_KAMIL_RATES,
  CIMR_AL_MOUNASSIB_RATES
} from '../../../../models/salary-package.model';

export interface ValidationError {
  field: string;
  message: string;
  severity: 'error' | 'warning';
}
import { PayrollRulesService, ResolvedFlags } from '../../../../services/salary-packages/payroll-rules.service';
import { RuleMode, CeilingConfig, KnownElement } from '../../../../models/salary-packages/payroll-rules.model';
import { PayrollReferentielService } from '../../../../services/payroll-referentiel/payroll-referentiel.service';
import { ReferentielElementListDto, ElementRuleDto, ExemptionType } from '../../../../models/payroll-referentiel';
import { ReferentielElementDto } from '../../../../models/payroll-referentiel/referentiel-element.model';

export type EditorAction = 'back' | 'save' | 'publish' | 'duplicate' | 'delete' | 'discard';

export interface DraftItem extends SalaryPackageItem {
  clientId: number;
  // Auto/Manual mode
  isAuto: boolean;
  matchedElementId?: string;
  irMode?: RuleMode;
  cnssMode?: RuleMode;
  irCeiling?: CeilingConfig;
  cnssCeiling?: CeilingConfig;
  // Referentiel element reference
  referentielElementId?: number;
  referentielElementCode?: string;
  isConvergence?: boolean;
}

export interface DraftTemplate extends Omit<SalaryPackage, 'items'> {
  items: DraftItem[];
}

@Component({
  selector: 'app-template-editor',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule
  ],
  template: `
    <div class="space-y-6">
      <!-- Back Button -->
      <div class="flex items-center justify-between">
        <button
          type="button"
          (click)="onBack()"
          class="inline-flex items-center gap-2 text-gray-600 hover:text-gray-900 transition">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
          </svg>
          Retour à la liste
        </button>
        @if (isDirty()) {
          <span class="text-sm text-amber-600 flex items-center gap-1">
            <span class="w-2 h-2 bg-amber-500 rounded-full"></span>
            Modifications non enregistrées
          </span>
        }
      </div>

      <!-- Locked Warning -->
      @if (!canEdit) {
        <div class="bg-amber-50 border border-amber-200 rounded-xl p-4">
          <div class="flex items-center gap-3">
            <svg class="w-5 h-5 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
            </svg>
            <div>
              <div class="font-medium text-amber-800">Ce template ne peut pas être modifié</div>
              <div class="text-sm text-amber-600">
                {{ draft.isLocked ? 'Il est verrouillé.' : 'Il est déjà publié.' }}
                Dupliquez-le pour créer une nouvelle version.
              </div>
            </div>
          </div>
        </div>
      }

      <!-- Two Column Layout -->
      <div class="grid lg:grid-cols-3 gap-6">
        <!-- Left Column: Editor Form -->
        <div class="lg:col-span-2 space-y-6">

          <!-- General Information Card -->
          <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
            <div class="px-6 py-4 border-b border-gray-100 bg-gray-50">
              <h2 class="font-semibold text-gray-900">Informations générales</h2>
            </div>
            <div class="p-6 space-y-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1.5">
                  Nom <span class="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  [(ngModel)]="draft.name"
                  [disabled]="!canEdit"
                  placeholder="Ex: Industrie - Cadre Standard"
                  class="w-full px-3 py-2.5 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition disabled:bg-gray-50 disabled:text-gray-500" />
              </div>

              <div class="relative">
                <label class="block text-sm font-medium text-gray-700 mb-1.5">
                  Catégorie <span class="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  [(ngModel)]="draft.category"
                  [disabled]="!canEdit"
                  (focus)="showCategoryDropdown = true"
                  (blur)="onCategoryBlur()"
                  placeholder="Ex: Industrie"
                  autocomplete="off"
                  class="w-full px-3 py-2.5 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition disabled:bg-gray-50 disabled:text-gray-500" />
                @if (showCategoryDropdown && filteredCategories.length > 0) {
                  <div class="absolute z-10 w-full mt-1 bg-white border border-gray-200 rounded-lg shadow-lg max-h-48 overflow-auto">
                    @for (cat of filteredCategories; track cat) {
                      <button
                        type="button"
                        (mousedown)="selectCategory(cat)"
                        class="w-full text-left px-3 py-2 hover:bg-gray-50 text-sm">
                        {{ cat }}
                      </button>
                    }
                  </div>
                }
              </div>

              <div>
                <label class="block text-sm font-medium text-gray-700 mb-1.5">Description</label>
                <textarea
                  rows="3"
                  [(ngModel)]="draft.description"
                  [disabled]="!canEdit"
                  placeholder="Décrivez l'usage du template..."
                  class="w-full px-3 py-2.5 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition resize-none disabled:bg-gray-50 disabled:text-gray-500"></textarea>
              </div>
            </div>
          </div>

          <!-- CIMR Configuration Card -->
          <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
            <div class="px-6 py-4 border-b border-gray-100 bg-gray-50">
              <div class="flex items-center gap-2">
                <span class="text-lg">🏦</span>
                <h2 class="font-semibold text-gray-900">Retraite Complémentaire CIMR</h2>
              </div>
            </div>
            <div class="p-6">
              <div class="grid md:grid-cols-2 gap-6">
                <!-- Regime Selection -->
                <div class="space-y-3">
                  <h3 class="text-sm font-medium text-gray-700 uppercase tracking-wide">Régime</h3>
                  <div class="space-y-2">
                    <label class="flex items-start gap-3 p-3 rounded-lg border transition-all"
                      [class.border-gray-200]="draft.cimrConfig?.regime !== 'NONE'"
                      [class.bg-white]="draft.cimrConfig?.regime !== 'NONE'"
                      [class.border-primary]="draft.cimrConfig?.regime === 'NONE'"
                      [class.bg-primary-50]="draft.cimrConfig?.regime === 'NONE'"
                      [class.opacity-50]="!canEdit"
                      [class.cursor-pointer]="canEdit"
                      [class.cursor-not-allowed]="!canEdit">
                      <input type="radio" name="cimrRegime"
                        [checked]="draft.cimrConfig?.regime === 'NONE'"
                        (change)="onCimrRegimeChange('NONE')"
                        [disabled]="!canEdit"
                        class="mt-1 w-4 h-4 text-primary border-gray-300 focus:ring-primary" />
                      <div>
                        <span class="block text-sm font-medium text-gray-900">Non affilié</span>
                        <span class="block text-xs text-gray-500 mt-0.5">Aucune cotisation CIMR</span>
                      </div>
                    </label>

                    <label class="flex items-start gap-3 p-3 rounded-lg border transition-all"
                      [class.border-gray-200]="draft.cimrConfig?.regime !== 'AL_KAMIL'"
                      [class.bg-white]="draft.cimrConfig?.regime !== 'AL_KAMIL'"
                      [class.border-primary]="draft.cimrConfig?.regime === 'AL_KAMIL'"
                      [class.bg-primary-50]="draft.cimrConfig?.regime === 'AL_KAMIL'"
                      [class.opacity-50]="!canEdit"
                      [class.cursor-pointer]="canEdit"
                      [class.cursor-not-allowed]="!canEdit">
                      <input type="radio" name="cimrRegime"
                        [checked]="draft.cimrConfig?.regime === 'AL_KAMIL'"
                        (change)="onCimrRegimeChange('AL_KAMIL')"
                        [disabled]="!canEdit"
                        class="mt-1 w-4 h-4 text-primary border-gray-300 focus:ring-primary" />
                      <div>
                        <span class="block text-sm font-medium text-gray-900">Al Kamil (Standard)</span>
                        <span class="block text-xs text-gray-500 mt-0.5">Calculé sur la totalité du salaire brut</span>
                      </div>
                    </label>

                    <label class="flex items-start gap-3 p-3 rounded-lg border transition-all"
                      [class.border-gray-200]="draft.cimrConfig?.regime !== 'AL_MOUNASSIB'"
                      [class.bg-white]="draft.cimrConfig?.regime !== 'AL_MOUNASSIB'"
                      [class.border-primary]="draft.cimrConfig?.regime === 'AL_MOUNASSIB'"
                      [class.bg-primary-50]="draft.cimrConfig?.regime === 'AL_MOUNASSIB'"
                      [class.opacity-50]="!canEdit"
                      [class.cursor-pointer]="canEdit"
                      [class.cursor-not-allowed]="!canEdit">
                      <input type="radio" name="cimrRegime"
                        [checked]="draft.cimrConfig?.regime === 'AL_MOUNASSIB'"
                        (change)="onCimrRegimeChange('AL_MOUNASSIB')"
                        [disabled]="!canEdit"
                        class="mt-1 w-4 h-4 text-primary border-gray-300 focus:ring-primary" />
                      <div>
                        <span class="block text-sm font-medium text-gray-900">Al Mounassib (PME)</span>
                        <span class="block text-xs text-gray-500 mt-0.5">Calculé sur la part dépassant le plafond CNSS</span>
                      </div>
                    </label>
                  </div>
                </div>

                <!-- Rate Selection -->
                <div class="space-y-3">
                  <h3 class="text-sm font-medium text-gray-700 uppercase tracking-wide">Taux de Cotisation</h3>
                  @if (draft.cimrConfig?.regime === 'NONE') {
                    <div class="h-full flex items-center justify-center border-2 border-dashed border-gray-100 rounded-lg bg-gray-50 p-6 text-center">
                      <p class="text-sm text-gray-400">Sélectionnez un régime pour configurer les taux</p>
                    </div>
                  } @else {
                    <div class="bg-white p-4 rounded-lg border border-gray-200">
                      <label class="block text-xs font-medium text-gray-600 mb-2">Sélectionnez le taux salarial</label>
                      <div class="grid grid-cols-3 gap-2">
                        @if (draft.cimrConfig?.regime === 'AL_KAMIL') {
                          @for (rate of AL_KAMIL_RATES; track rate.label) {
                            <button
                              type="button"
                              (click)="onCimrRateChange(rate)"
                              [disabled]="!canEdit"
                              [class.ring-2]="isCimrRateSelected(rate)"
                              [class.ring-primary]="isCimrRateSelected(rate)"
                              [class.bg-primary-50]="isCimrRateSelected(rate)"
                              [class.text-primary-700]="isCimrRateSelected(rate)"
                              [class.border-primary-200]="isCimrRateSelected(rate)"
                              class="px-2 py-2 text-sm border rounded hover:bg-gray-50 transition-all focus:outline-none disabled:opacity-50 disabled:cursor-not-allowed">
                              {{ rate.label }}
                            </button>
                          }
                        }
                        @if (draft.cimrConfig?.regime === 'AL_MOUNASSIB') {
                          @for (rate of AL_MOUNASSIB_RATES; track rate.label) {
                            <button
                              type="button"
                              (click)="onCimrRateChange(rate)"
                              [disabled]="!canEdit"
                              [class.ring-2]="isCimrRateSelected(rate)"
                              [class.ring-primary]="isCimrRateSelected(rate)"
                              [class.bg-primary-50]="isCimrRateSelected(rate)"
                              [class.text-primary-700]="isCimrRateSelected(rate)"
                              [class.border-primary-200]="isCimrRateSelected(rate)"
                              class="px-2 py-2 text-sm border rounded hover:bg-gray-50 transition-all focus:outline-none disabled:opacity-50 disabled:cursor-not-allowed">
                              {{ rate.label }}
                            </button>
                          }
                        }
                      </div>
                      @if (draft.cimrConfig && draft.cimrConfig.regime !== 'NONE') {
                        <div class="mt-4 pt-3 border-t border-gray-100 flex justify-between text-sm">
                          <div>
                            <span class="block text-gray-500 text-xs">Part Salariale</span>
                            <span class="font-semibold text-gray-900">{{ (draft.cimrConfig.employeeRate * 100).toFixed(2) }}%</span>
                          </div>
                          <div class="text-right">
                            <span class="block text-gray-500 text-xs">Part Patronale</span>
                            <span class="font-semibold text-gray-900">{{ (draft.cimrConfig.employerRate * 100).toFixed(2) }}%</span>
                          </div>
                        </div>
                      }
                    </div>
                  }
                </div>
              </div>
            </div>
          </div>

          <!-- Fixed Pay Elements Card (overflow-visible so dropdown can extend outside) -->
          <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-visible">
            <div class="px-6 py-4 border-b border-gray-100 bg-gray-50 flex items-center justify-between rounded-t-xl">
              <div>
                <h2 class="font-semibold text-gray-900">Éléments de rémunération</h2>
                <p class="text-xs text-gray-500 mt-0.5">Primes, indemnités et avantages</p>
              </div>
              @if (canEdit) {
                <button
                  type="button"
                  (click)="addItem()"
                  class="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm text-primary hover:bg-primary/5 rounded-lg transition">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                  </svg>
                  Ajouter
                </button>
              }
            </div>

            <div class="divide-y divide-gray-100">
              @for (item of draft.items; track item.clientId; let i = $index) {
                <div class="p-4">
                  <div class="flex items-start gap-4">
                    <div class="w-8 h-8 shrink-0 rounded-lg bg-gray-100 flex items-center justify-center text-sm font-medium text-gray-500">
                      {{ i + 1 }}
                    </div>

                    <div class="flex-1 space-y-3">
                      <!-- Row 1: Element Selector + Amount -->
                      <div class="grid md:grid-cols-3 gap-3">
                        <div class="md:col-span-2">
                          <!-- Inline Searchable Dropdown -->
                          <div class="relative">
                            @if (item.referentielElementId) {
                              <!-- Selected element display -->
                              <div 
                                class="w-full px-3 py-2 border border-gray-200 rounded-lg bg-white flex items-center justify-between gap-2"
                                [class.cursor-pointer]="canEdit"
                                [class.hover:border-primary]="canEdit"
                                (click)="canEdit && toggleElementDropdown(i)">
                                <div class="flex items-center gap-2 min-w-0">
                                  <span class="font-medium text-gray-900 truncate">{{ item.label }}</span>
                                  <span class="text-xs text-gray-500 font-mono shrink-0">{{ item.referentielElementCode }}</span>
                                  @if (item.isConvergence !== undefined) {
                                    <span 
                                      class="text-xs px-1.5 py-0.5 rounded shrink-0"
                                      [class.bg-green-100]="item.isConvergence"
                                      [class.text-green-700]="item.isConvergence"
                                      [class.bg-amber-100]="!item.isConvergence"
                                      [class.text-amber-700]="!item.isConvergence">
                                      {{ item.isConvergence ? '✓' : '!' }}
                                    </span>
                                  }
                                </div>
                                <div class="flex items-center gap-1 shrink-0">
                                  @if (canEdit) {
                                    <button
                                      type="button"
                                      (click)="clearReferentielElement(i); $event.stopPropagation()"
                                      class="text-xs text-gray-500 hover:text-red-600 px-1.5 py-0.5 rounded hover:bg-red-50"
                                      title="Détacher l'élément référentiel">
                                      Détacher
                                    </button>
                                  }
                                  @if (canEdit) {
                                    <svg class="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                                    </svg>
                                  }
                                </div>
                              </div>
                            } @else {
                              <!-- Placeholder / Trigger -->
                              <button
                                type="button"
                                (click)="toggleElementDropdown(i)"
                                [disabled]="!canEdit"
                                class="w-full px-3 py-2 border border-gray-200 rounded-lg bg-white text-left flex items-center justify-between gap-2 hover:border-primary focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition disabled:bg-gray-50 disabled:cursor-not-allowed">
                                <span class="text-gray-400 text-sm">Sélectionner un élément...</span>
                                <svg class="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
                                </svg>
                              </button>
                            }

                            <!-- Dropdown Panel (z-50 so it appears above card and other content) -->
                            @if (activeDropdownIndex === i) {
                              <div class="absolute z-50 w-full mt-1 bg-white border border-gray-200 rounded-lg shadow-lg max-h-64 overflow-hidden element-dropdown-panel">
                                <!-- Search Input -->
                                <div class="p-2 border-b border-gray-100">
                                  <div class="relative">
                                    <svg class="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                                    </svg>
                                    <input
                                      type="text"
                                      [(ngModel)]="elementSearchTerm"
                                      (ngModelChange)="filterElements()"
                                      placeholder="Rechercher..."
                                      class="w-full pl-9 pr-3 py-1.5 text-sm border border-gray-200 rounded-md focus:outline-none focus:ring-1 focus:ring-primary focus:border-primary"
                                      (click)="$event.stopPropagation()" />
                                  </div>
                                </div>

                                <!-- Elements List -->
                                <div class="overflow-y-auto max-h-48">
                                  @if (loadingReferentielElements) {
                                    <div class="p-4 text-center">
                                      <div class="animate-spin rounded-full h-5 w-5 border-b-2 border-primary mx-auto"></div>
                                    </div>
                                  } @else if (filteredReferentielElements.length === 0) {
                                    <div class="p-4 text-center text-sm text-gray-500">
                                      Aucun élément trouvé
                                    </div>
                                  } @else {
                                    @for (element of filteredReferentielElements; track element.id) {
                                      <button
                                        type="button"
                                        (click)="selectReferentielElement(i, element); $event.stopPropagation()"
                                        class="w-full px-3 py-2 text-left hover:bg-gray-50 flex items-center justify-between gap-2 transition"
                                        [class.bg-primary/5]="item.referentielElementId === element.id">
                                        <div class="min-w-0">
                                          <div class="font-medium text-gray-900 text-sm truncate">{{ element.name }}</div>
                                          <div class="text-xs text-gray-500 flex items-center gap-2">
                                            <span class="font-mono">{{ element.name }}</span>
                                            <span>•</span>
                                            <span>{{ element.categoryName }}</span>
                                          </div>
                                        </div>
                                        <div class="shrink-0 flex items-center gap-1.5">
                                          <span
                                            class="text-xs px-1.5 py-0.5 rounded"
                                            [class.bg-green-100]="element.hasConvergence"
                                            [class.text-green-700]="element.hasConvergence"
                                            [class.bg-amber-100]="!element.hasConvergence"
                                            [class.text-amber-700]="!element.hasConvergence">
                                            {{ element.hasConvergence ? 'Conv.' : 'Div.' }}
                                          </span>
                                          @if (item.referentielElementId === element.id) {
                                            <svg class="w-4 h-4 text-primary" fill="currentColor" viewBox="0 0 20 20">
                                              <path fill-rule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clip-rule="evenodd" />
                                            </svg>
                                          }
                                        </div>
                                      </button>
                                    }
                                  }
                                </div>
                              </div>
                            }
                          </div>
                        </div>
                        <div>
                          <input
                            type="number"
                            min="0"
                            [(ngModel)]="item.defaultValue"
                            [disabled]="!canEdit"
                            placeholder="0"
                            class="w-full px-3 py-2 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition text-sm disabled:bg-gray-50 disabled:text-gray-500" />
                        </div>
                      </div>
                    </div>

                    @if (canEdit) {
                      <button
                        type="button"
                        (click)="removeItem(item.clientId)"
                        class="p-2 text-gray-400 hover:text-red-500 hover:bg-red-50 rounded-lg transition"
                        title="Supprimer">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                        </svg>
                      </button>
                    }
                  </div>
                </div>
              }

              @if (draft.items.length === 0) {
                <div class="p-8 text-center">
                  <p class="text-sm text-gray-500 mb-3">Aucun élément défini</p>
                  @if (canEdit) {
                    <button
                      type="button"
                      (click)="addItem()"
                      class="text-sm text-primary hover:underline">
                      Ajouter un élément
                    </button>
                  }
                </div>
              }
            </div>
          </div>
        </div>

        <!-- Right Column: Preview & Actions -->
        <div class="space-y-6">
          <!-- Compliance (commented out for now)
          <app-compliance-card
            [items]="draft.items"
            [regulationVersion]="draft.regulationVersion"
            [cimrRate]="draft.cimrRate ?? null"
            [cimrConfig]="draft.cimrConfig ?? null"
            [hasPrivateInsurance]="draft.hasPrivateInsurance">
          </app-compliance-card>
          -->

          <!-- Actions -->
          <div class="sticky top-6 bg-white rounded-xl shadow-sm border border-gray-200 p-6">
            <h3 class="font-semibold text-gray-900 mb-4">Actions</h3>
            <div class="space-y-2">
              @if (canEdit) {
                <button
                  type="button"
                  (click)="onAction.emit('save')"
                  [disabled]="isSaving || !canSave()"
                  class="w-full flex items-center justify-center gap-2 px-4 py-2.5 bg-primary text-white rounded-lg hover:bg-primary/90 transition disabled:opacity-50 disabled:cursor-not-allowed">
                  @if (isSaving) {
                    <svg class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                      <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                      <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                    </svg>
                  }
                  {{ isSaving ? 'Enregistrement...' : 'Enregistrer le brouillon' }}
                </button>
              }

              @if (canPublish) {
                <button
                  type="button"
                  (click)="onAction.emit('publish')"
                  [disabled]="isSaving || !canPublishTemplate()"
                  class="w-full flex items-center justify-center gap-2 px-4 py-2.5 bg-emerald-50 text-emerald-700 rounded-lg hover:bg-emerald-100 transition disabled:opacity-50 disabled:cursor-not-allowed">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  Publier
                </button>
              }

              <button
                type="button"
                (click)="onAction.emit('duplicate')"
                [disabled]="isSaving || draft.id === 0"
                class="w-full flex items-center justify-center gap-2 px-4 py-2.5 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition disabled:opacity-50 disabled:cursor-not-allowed">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7v8a2 2 0 002 2h6M8 7V5a2 2 0 012-2h4.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V15a2 2 0 01-2 2h-2M8 7H6a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2v-2" />
                </svg>
                Dupliquer
              </button>

              @if (draft.id > 0 && draft.status === 'draft') {
                <button
                  type="button"
                  (click)="onAction.emit('delete')"
                  [disabled]="isSaving"
                  class="w-full flex items-center justify-center gap-2 px-4 py-2.5 bg-red-50 text-red-700 rounded-lg hover:bg-red-100 transition disabled:opacity-50">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                  </svg>
                  Supprimer
                </button>
              }

              @if (isDirty() && canEdit) {
                <button
                  type="button"
                  (click)="onAction.emit('discard')"
                  class="w-full flex items-center justify-center gap-2 px-4 py-2.5 text-gray-500 hover:text-gray-700 hover:bg-gray-50 rounded-lg transition">
                  Annuler les modifications
                </button>
              }
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Component Picker Modal -->
    @if (showComponentPicker) {
      <div class="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
        <div class="bg-white rounded-xl shadow-xl w-full max-w-xl max-h-[80vh] flex flex-col mx-4">
          <div class="px-6 py-4 border-b border-gray-200 flex items-center justify-between">
            <h3 class="font-semibold text-gray-900">Sélectionner un composant</h3>
            <button type="button" (click)="closeComponentPicker()" class="text-gray-500 hover:text-gray-700 p-1">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>
          <div class="flex-1 overflow-auto p-4">
            @if (payComponents.length === 0) {
              <div class="text-center py-8 text-gray-500">Aucun composant disponible</div>
            } @else {
              <div class="space-y-2">
                @for (component of payComponents; track component.id) {
                  <button
                    type="button"
                    (click)="selectPayComponent(component)"
                    class="w-full text-left p-4 rounded-lg border border-gray-200 hover:border-primary hover:bg-primary/5 transition">
                    <div class="flex items-start justify-between gap-3">
                      <div>
                        <div class="font-medium text-gray-900">{{ component.nameFr }}</div>
                        <div class="text-xs text-gray-500 font-mono">{{ component.code }}</div>
                      </div>
                      <span class="text-xs px-2 py-0.5 rounded bg-gray-100 text-gray-600">
                        {{ getTypeLabel(component.type) }}
                      </span>
                    </div>
                    <div class="flex items-center gap-3 mt-2 text-xs text-gray-500">
                      @if (component.isTaxable) { <span>IR</span> }
                      @if (component.isSocial) { <span>CNSS</span> }
                      @if (component.isCIMR) { <span>CIMR</span> }
                      @if (component.exemptionLimit) {
                        <span>Plafond: {{ formatCurrency(component.exemptionLimit) }} MAD</span>
                      }
                    </div>
                  </button>
                }
              </div>
            }
          </div>
        </div>
      </div>
    }

  `
})
export class TemplateEditorComponent implements OnInit, OnChanges {
  private payrollRulesService = inject(PayrollRulesService);
  private payrollReferentielService = inject(PayrollReferentielService);

  @Input() template!: SalaryPackage;
  @Input() payComponents: PayComponent[] = [];

  // API-loaded referentiel elements
  apiReferentielElements: ReferentielElementListDto[] = [];
  loadingReferentielElements = false;
  @Input() categories: string[] = [];
  @Input() isSaving = false;

  @Output() onAction = new EventEmitter<EditorAction>();
  @Output() onDraftChange = new EventEmitter<DraftTemplate>();

  draft!: DraftTemplate;
  originalDraft!: string; // JSON snapshot for dirty check

  showCategoryDropdown = false;
  showComponentPicker = false;
  showKnownElementPicker = false;
  activeItemIndex: number | null = null;

  // Inline element dropdown state
  activeDropdownIndex: number | null = null;
  elementSearchTerm = '';
  filteredReferentielElements: ReferentielElementListDto[] = [];

  readonly componentTypes: SalaryComponentType[] = ['allowance', 'bonus', 'benefit_in_kind', 'social_charge'];
  private nextItemId = 1;

  // CIMR rate constants exposed to template
  readonly AL_KAMIL_RATES = CIMR_AL_KAMIL_RATES;
  readonly AL_MOUNASSIB_RATES = CIMR_AL_MOUNASSIB_RATES;

  // Known elements grouped by category for picker modal
  get knownElementsByCategory(): Map<string, KnownElement[]> {
    // If API elements loaded, use them; otherwise fall back to hardcoded
    if (this.apiReferentielElements.length > 0) {
      return this.groupApiElementsByCategory();
    }
    return this.payrollRulesService.getElementsByCategory();
  }

  /**
   * Group API referentiel elements by category
   */
  private groupApiElementsByCategory(): Map<string, KnownElement[]> {
    const grouped = new Map<string, KnownElement[]>();

    for (const apiElement of this.apiReferentielElements) {
      // Convert API element to KnownElement format
      const knownElement: KnownElement = this.convertApiElementToKnownElement(apiElement);

      const category = this.mapCategoryNameToGroup(apiElement.categoryName);
      const existing = grouped.get(category) || [];
      existing.push(knownElement);
      grouped.set(category, existing);
    }

    return grouped;
  }

  /**
   * Convert API element to KnownElement format for UI compatibility
   */
  private convertApiElementToKnownElement(apiElement: ReferentielElementListDto): KnownElement {
    // Map API element to the legacy KnownElement structure
    // Note: We'll use simplified flags here - full rule resolution would require fetching rules
    return {
      id: `api-${apiElement.id}`,
      labelFr: apiElement.name,
      type: this.mapApiFrequencyToComponentType(apiElement.defaultFrequency) as 'allowance' | 'bonus' | 'benefit_in_kind' | 'social_charge',
      category: this.mapCategoryNameToGroup(apiElement.categoryName) as 'professional' | 'social' | 'specific' | 'termination',
      isVariable: apiElement.defaultFrequency !== 'MONTHLY',
      flags: {
        ir: 'included' as RuleMode,  // Default - would need rules API for accurate flags
        cnss: 'included' as RuleMode,
        cimr: 'included' as RuleMode,
      },
      // Store original API element ID for later rule lookup
      apiElementId: apiElement.id
    };
  }

  /**
   * Map API frequency to SalaryComponentType
   */
  private mapApiFrequencyToComponentType(frequency: string): SalaryComponentType {
    // Map payment frequency to component type
    switch (frequency) {
      case 'MONTHLY':
        return 'allowance';  // Regular monthly allowances
      case 'ONE_TIME':
        return 'bonus';      // One-time bonuses
      case 'ANNUAL':
        return 'bonus';      // Annual bonuses
      default:
        return 'allowance';
    }
  }

  /**
   * Map API category name (or legacy code) to UI category group
   */
  private mapCategoryNameToGroup(categoryName: string): string {
    if (!categoryName?.trim()) return 'professional';
    const n = categoryName.trim().toLowerCase();
    // By name (French)
    if (n.includes('professionnel') || n.includes('indemnité') && n.includes('pro')) return 'professional';
    if (n.includes('social') || n.includes('avantage')) return 'social';
    if (n.includes('spécifique') || n.includes('prime')) return 'specific';
    if (n.includes('rupture')) return 'termination';
    // Legacy codes
    const codeMapping: Record<string, string> = {
      'ind_pro': 'professional',
      'ind_social': 'social',
      'prime_spec': 'specific',
      'avantage': 'social',
      'rupture': 'termination'
    };
    return codeMapping[n] || 'professional';
  }

  /** Match CNSS by authority name */
  private isCnssAuthority(name: string | undefined): boolean {
    if (!name) return false;
    const n = name.toLowerCase();
    return n === 'cnss' || n.includes('sécurité sociale') || n.includes('securite sociale');
  }

  /** Match IR/DGI by authority name */
  private isIrAuthority(name: string | undefined): boolean {
    if (!name) return false;
    const n = name.toLowerCase();
    return n === 'ir' || n === 'dgi' || n.includes('impôt') || n.includes('impot') || n.includes('revenu');
  }

  ngOnInit(): void {
    this.initializeDraft();
    this.loadReferentielElements();
  }

  /**
   * Load referentiel elements from API for the element picker
   */
  private loadReferentielElements(): void {
    this.loadingReferentielElements = true;

    this.payrollReferentielService.getAllReferentielElements().subscribe({
      next: (elements: any) => {
        this.apiReferentielElements = elements;
        this.filteredReferentielElements = [...elements];
        this.loadingReferentielElements = false;
        // Rehydrate so items loaded from template show correct referentiel selection
        this.rehydrateReferentielFields();
      },
      error: (err: any) => {
        console.error('Failed to load referentiel elements, using hardcoded fallback:', err);
        this.apiReferentielElements = [];
        this.filteredReferentielElements = [];
        this.loadingReferentielElements = false;
      }
    });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['template'] && !changes['template'].firstChange) {
      this.initializeDraft();
    }
  }

  private initializeDraft(): void {
    if (!this.template) {
      this.draft = this.createEmptyDraft();
    } else {
      this.nextItemId = 1;
      const items: DraftItem[] = this.template.items.map(item => {
        const draftItem: DraftItem = {
          ...item,
          referentielElementId: item.referentielElementId ?? undefined,
          clientId: this.nextItemId++,
          isAuto: (item as any).isAuto ?? true, // Default to auto mode
        };
        // Display name for referentiel dropdown when loaded from API
        if (draftItem.referentielElementName && !draftItem.referentielElementCode) {
          draftItem.referentielElementCode = draftItem.referentielElementName;
        }
        // If in auto mode, resolve flags based on label and type
        if (draftItem.isAuto && draftItem.label) {
          this.applyAutoRules(draftItem);
        }
        return draftItem;
      });

      this.draft = {
        ...this.template,
        items
      };

      // Rehydrate referentiel fields from label so dropdown shows correct selection after load/save
      this.rehydrateReferentielFields();
    }
    this.originalDraft = JSON.stringify(this.draft);
  }

  /**
   * Match draft items to referentiel elements by label so the dropdown shows the right selection
   * after loading a template (e.g. from API after save). Called from initializeDraft and when
   * referentiel elements finish loading.
   */
  private rehydrateReferentielFields(): void {
    if (!this.draft?.items?.length || !this.apiReferentielElements?.length) return;

    const labelNorm = (s: string) => (s || '').trim().toLowerCase();

    for (const item of this.draft.items) {
      if (item.referentielElementId || !item.label?.trim()) continue;

      const match = this.apiReferentielElements.find(
        el => labelNorm(el.name) === labelNorm(item.label!)
      );
      if (match) {
        item.referentielElementId = match.id;
        item.referentielElementCode = match.name;
        item.isConvergence = match.hasConvergence;
      }
    }
  }

  private createEmptyDraft(): DraftTemplate {
    return {
      id: 0,
      name: '',
      category: '',
      description: '',
      baseSalary: 0,
      status: 'draft' as SalaryPackageStatus,
      companyId: null,
      companyName: null,
      templateType: 'OFFICIAL',
      regulationVersion: 'MA_2025',
      autoRules: { seniorityBonusEnabled: true, ruleVersion: 'MA_2025' },
      cimrRate: null,
      cimrConfig: { regime: 'NONE', employeeRate: 0, employerRate: 0 },
      hasPrivateInsurance: false,
      version: 1,
      sourceTemplateId: null,
      sourceTemplateName: null,
      sourceTemplateVersion: null,
      validFrom: null,
      validTo: null,
      isLocked: false,
      isGlobalTemplate: true,
      items: [],
      updatedAt: new Date().toISOString(),
      createdAt: new Date().toISOString()
    };
  }

  get canEdit(): boolean {
    return !this.draft.isLocked && this.draft.status !== 'published';
  }

  get canPublish(): boolean {
    return this.draft.status === 'draft' && !this.draft.isLocked;
  }

  // Total gross salary (used by compliance/summary)
  get totalGrossSalary(): number {
    const itemsTotal = this.draft.items.reduce((sum, item) => sum + (Number(item.defaultValue) || 0), 0);
    return (Number(this.draft.baseSalary) || 0) + itemsTotal;
  }

  isDirty(): boolean {
    return JSON.stringify(this.draft) !== this.originalDraft;
  }

  canSave(): boolean {
    return !!this.draft.name?.trim() && !!this.draft.category?.trim();
  }

  canPublishTemplate(): boolean {
    return this.validationErrors().filter((e: ValidationError) => e.severity === 'error').length === 0;
  }

  validationErrors(): ValidationError[] {
    const errors: ValidationError[] = [];

    if (!this.draft.name?.trim()) {
      errors.push({ field: 'name', message: 'Le nom est requis', severity: 'error' });
    }

    if (!this.draft.category?.trim()) {
      errors.push({ field: 'category', message: 'La catégorie est requise', severity: 'error' });
    }

    // Check items
    this.draft.items.forEach((item, index) => {
      if (!item.label?.trim()) {
        errors.push({ field: `item-${index}`, message: `Élément ${index + 1}: le libellé est requis`, severity: 'error' });
      }
      if (item.defaultValue < 0) {
        errors.push({ field: `item-${index}`, message: `Élément ${index + 1}: la valeur ne peut pas être négative`, severity: 'error' });
      }
    });

    // Warnings
    if (this.draft.items.length === 0) {
      errors.push({ field: 'items', message: 'Aucun élément de rémunération défini', severity: 'warning' });
    }

    return errors;
  }

  get filteredCategories(): string[] {
    const term = (this.draft.category || '').trim().toLowerCase();
    if (!term) return this.categories;
    return this.categories.filter(c => c.toLowerCase().includes(term));
  }

  // Item Management
  addItem(): void {
    if (!this.canEdit) return;
    
    // Get type defaults for allowance
    const resolved = this.payrollRulesService.resolveFlagsFromType('allowance');
    
    const newItem: DraftItem = {
      clientId: this.nextItemId++,
      label: '',
      defaultValue: 0,
      type: 'allowance',
      isTaxable: resolved.ir,
      isSocial: resolved.cnss,
      isCIMR: resolved.cimr,
      isVariable: resolved.isVariable,
      exemptionLimit: null,
      // Auto mode
      isAuto: true,
      irMode: resolved.irMode,
      cnssMode: resolved.cnssMode,
    };
    this.draft.items = [...this.draft.items, newItem];
  }

  removeItem(clientId: number): void {
    if (!this.canEdit) return;
    this.draft.items = this.draft.items.filter(i => i.clientId !== clientId);
    this.closeElementDropdown();
  }

  // ============ Inline Element Dropdown Methods ============

  /**
   * Toggle the element dropdown for a specific item
   */
  toggleElementDropdown(index: number): void {
    if (this.activeDropdownIndex === index) {
      this.closeElementDropdown();
    } else {
      this.activeDropdownIndex = index;
      this.elementSearchTerm = '';
      this.filterElements();
      
      // Defer adding click-outside listener so the current click doesn't close it immediately
      setTimeout(() => {
        document.addEventListener('click', this.onClickOutsideDropdown);
      }, 100);
    }
  }

  /**
   * Close the element dropdown
   */
  closeElementDropdown(): void {
    this.activeDropdownIndex = null;
    this.elementSearchTerm = '';
    document.removeEventListener('click', this.onClickOutsideDropdown);
  }

  /**
   * Handle click outside dropdown to close it
   */
  private onClickOutsideDropdown = (event: MouseEvent): void => {
    const target = event.target as HTMLElement;
    // Close only if click is outside both the trigger and the dropdown panel
    if (!target.closest('.relative') && !target.closest('.element-dropdown-panel')) {
      this.closeElementDropdown();
    }
  };

  /**
   * Filter referentiel elements based on search term
   */
  filterElements(): void {
    if (!this.elementSearchTerm.trim()) {
      this.filteredReferentielElements = [...this.apiReferentielElements];
    } else {
      const term = this.elementSearchTerm.toLowerCase();
      this.filteredReferentielElements = this.apiReferentielElements.filter(el =>
        el.name.toLowerCase().includes(term) ||
        el.categoryName.toLowerCase().includes(term)
      );
    }
  }

  /**
   * Select a referentiel element for an item
   */
  selectReferentielElement(itemIndex: number, element: ReferentielElementListDto): void {
    const item = this.draft.items[itemIndex];
    if (!item) return;

    // Set basic element info
    item.label = element.name;
    item.referentielElementId = element.id;
    item.referentielElementCode = element.name;
    item.isConvergence = element.hasConvergence;
    item.type = 'allowance';
    item.isAuto = true;
    item.matchedElementId = `ref-${element.id}`;

    // Load full element details with rules
    this.loadElementRulesAndApply(item, element.id);

    this.closeElementDropdown();
  }

  /**
   * Clear referentiel element from an item (rule-driven payroll will use IsTaxable/IsSocial/IsCIMR)
   */
  clearReferentielElement(itemIndex: number): void {
    const item = this.draft.items[itemIndex];
    if (!item) return;
    item.referentielElementId = undefined;
    item.referentielElementCode = undefined;
    item.matchedElementId = undefined;
    item.isConvergence = undefined;
    if (item.isAuto && item.label) {
      this.applyAutoRules(item);
    }
    this.closeElementDropdown();
  }

  /**
   * Load element rules from API and apply them to the item
   */
  private loadElementRulesAndApply(item: DraftItem, elementId: number): void {
    this.payrollReferentielService.getReferentielElementById(elementId).subscribe({
      next: (fullElement) => {
        // Find CNSS and IR rules (match by authority name)
        const cnssRule = fullElement.rules.find(r => this.isCnssAuthority(r.authorityName));
        const irRule = fullElement.rules.find(r => this.isIrAuthority(r.authorityName));

        // Apply rules
        this.applyReferentielRules(item, cnssRule, irRule);
      },
      error: (err) => {
        console.error('Failed to load element rules:', err);
        // Apply default rules if fetch fails
        item.isTaxable = true;
        item.isSocial = true;
        item.isCIMR = true;
        item.irMode = 'included';
        item.cnssMode = 'included';
      }
    });
  }

  // Component Picker
  openComponentPicker(index: number): void {
    this.activeItemIndex = index;
    this.showComponentPicker = true;
  }

  closeComponentPicker(): void {
    this.showComponentPicker = false;
    this.activeItemIndex = null;
  }

  selectPayComponent(component: PayComponent): void {
    if (this.activeItemIndex === null) return;

    const item = this.draft.items[this.activeItemIndex];
    if (!item) return;

    item.payComponentId = component.id;
    item.payComponentCode = component.code;
    item.label = component.nameFr;
    item.type = component.type;
    item.isTaxable = component.isTaxable;
    item.isSocial = component.isSocial;
    item.isCIMR = component.isCIMR;
    item.exemptionLimit = component.exemptionLimit;
    if (component.defaultAmount) {
      item.defaultValue = component.defaultAmount;
    }

    this.closeComponentPicker();
  }

  // Category
  selectCategory(category: string): void {
    this.draft.category = category;
    this.showCategoryDropdown = false;
  }

  onCategoryBlur(): void {
    setTimeout(() => this.showCategoryDropdown = false, 150);
  }

  onBack(): void {
    if (this.isDirty()) {
      // Parent will handle confirmation
      this.onAction.emit('back');
    } else {
      this.onAction.emit('back');
    }
  }

  getTypeLabel(type: SalaryComponentType): string {
    const labels: Record<SalaryComponentType, string> = {
      base_salary: 'Salaire de base',
      allowance: 'Indemnité',
      bonus: 'Prime',
      benefit_in_kind: 'Avantage en nature',
      social_charge: 'Charge sociale'
    };
    return labels[type] || type;
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('fr-MA', { minimumFractionDigits: 0, maximumFractionDigits: 2 }).format(value);
  }

  getDraft(): DraftTemplate {
    return this.draft;
  }

  markAsSaved(): void {
    this.originalDraft = JSON.stringify(this.draft);
  }

  // ============ Auto/Manual Mode Methods ============

  /**
   * Handle label change - detect known elements and apply auto rules
   */
  onLabelChange(item: DraftItem): void {
    if (!item.isAuto) return;
    this.applyAutoRules(item);
  }

  /**
   * Handle type change - apply type defaults if in auto mode
   */
  onTypeChange(item: DraftItem): void {
    if (!item.isAuto) return;
    // If no matched element, apply type defaults
    if (!item.matchedElementId) {
      const resolved = this.payrollRulesService.resolveFlagsFromType(item.type);
      this.applyResolvedFlags(item, resolved);
    }
  }

  /**
   * Toggle between auto and manual mode
   */
  toggleAutoMode(item: DraftItem): void {
    if (!this.canEdit) return;
    
    item.isAuto = !item.isAuto;
    
    if (item.isAuto) {
      // Re-apply auto rules when switching back to auto
      this.applyAutoRules(item);
    } else {
      // Clear auto-mode specific fields when switching to manual
      // Keep the current checkbox values
      item.matchedElementId = undefined;
      item.irMode = undefined;
      item.cnssMode = undefined;
      item.irCeiling = undefined;
      item.cnssCeiling = undefined;
    }
  }

  /**
   * Apply automatic rules based on label and type
   */
  private applyAutoRules(item: DraftItem): void {
    const resolved = this.payrollRulesService.resolveFlags(item.label || '', item.type);
    this.applyResolvedFlags(item, resolved);
    
    // Store matched element ID if found
    if (resolved.matchedElement) {
      item.matchedElementId = resolved.matchedElement.id;
      // Update type if the matched element has a different type
      if (resolved.matchedElement.type !== item.type) {
        item.type = resolved.matchedElement.type;
      }
    } else {
      item.matchedElementId = undefined;
    }
  }

  /**
   * Apply resolved flags to an item
   */
  private applyResolvedFlags(item: DraftItem, resolved: ResolvedFlags): void {
    item.isTaxable = resolved.ir;
    item.isSocial = resolved.cnss;
    item.isCIMR = resolved.cimr;
    item.isVariable = resolved.isVariable;
    item.irMode = resolved.irMode;
    item.cnssMode = resolved.cnssMode;
    item.irCeiling = resolved.irCeiling;
    item.cnssCeiling = resolved.cnssCeiling;
  }

  /**
   * Get tooltip text for ceiling badge
   */
  getCeilingTooltip(ceiling: CeilingConfig | undefined): string {
    if (!ceiling) return '';
    return this.payrollRulesService.formatCeilingDisplay(ceiling);
  }

  // ============ Referentiel Element Rules ============

  /**
   * Apply CNSS and IR rules from referentiel to item flags
   */
  private applyReferentielRules(
    item: DraftItem,
    cnssRule?: ElementRuleDto,
    irRule?: ElementRuleDto
  ): void {
    // Apply CNSS rule
    if (cnssRule) {
      const { isSocial, mode, ceiling } = this.interpretExemptionRule(cnssRule);
      item.isSocial = isSocial ?? true;
      item.cnssMode = mode;
      if (ceiling) {
        item.cnssCeiling = ceiling;
      }
    } else {
      // No CNSS rule = fully subject
      item.isSocial = true;
      item.cnssMode = 'included';
    }

    // Apply IR rule  
    if (irRule) {
      const { isTaxable, mode, ceiling } = this.interpretExemptionRule(irRule);
      item.isTaxable = isTaxable ?? true;
      item.irMode = mode;
      if (ceiling) {
        item.irCeiling = ceiling;
      }
    } else {
      // No IR rule = fully subject
      item.isTaxable = true;
      item.irMode = 'included';
    }

    // CIMR is typically not exempt for allowances
    item.isCIMR = true;
    
    // Allowances are usually variable
    item.isVariable = true;
  }

  /**
   * Interpret an exemption rule to determine flags and mode
   */
  private interpretExemptionRule(rule: ElementRuleDto): {
    isTaxable?: boolean;
    isSocial?: boolean;
    mode: RuleMode;
    ceiling?: CeilingConfig;
  } {
    switch (rule.exemptionType) {
      case 'FULLY_EXEMPT':
        return {
          isTaxable: false,
          isSocial: false,
          mode: 'excluded'
        };
        
      case 'FULLY_SUBJECT':
        return {
          isTaxable: true,
          isSocial: true,
          mode: 'included'
        };
        
      case 'CAPPED':
      case 'FORMULA':
      case 'FORMULA_CAPPED':
        // Exempt up to a ceiling, then subject
        const ceiling = this.extractCeilingFromRule(rule);
        return {
          isTaxable: false,
          isSocial: false,
          mode: 'exempt_with_ceiling',
          ceiling
        };
        
      case 'PERCENTAGE':
      case 'PERCENTAGE_CAPPED':
        // Partially subject based on percentage
        return {
          isTaxable: true,
          isSocial: true,
          mode: 'conditional'
        };
        
      case 'TIERED':
        // Complex tiered exemption
        return {
          isTaxable: true,
          isSocial: true,
          mode: 'conditional'
        };
        
      default:
        return {
          isTaxable: true,
          isSocial: true,
          mode: 'included'
        };
    }
  }

  /**
   * Extract ceiling information from a rule
   */
  private extractCeilingFromRule(rule: ElementRuleDto): CeilingConfig | undefined {
    if (rule.cap) {
      return {
        type: 'fixed',
        label: `${rule.cap.capAmount} MAD ${this.getCapUnitLabel(rule.cap.capUnit)}`,
        value: rule.cap.capAmount
      };
    }
    
    if (rule.formula) {
      return {
        type: 'smig_multiple',
        label: `${rule.formula.multiplier} × ${rule.formula.parameterName}`,
        labelAlt: `${rule.formula.currentCapValue} MAD ${this.getCapUnitLabel(rule.formula.resultUnit)}`,
        value: rule.formula.currentCapValue
      };
    }
    
    return undefined;
  }

  /**
   * Get human-readable label for cap unit
   */
  private getCapUnitLabel(unit: string): string {
    const labels: Record<string, string> = {
      'PER_DAY': 'par jour',
      'PER_MONTH': 'par mois',
      'PER_YEAR': 'par an'
    };
    return labels[unit] || unit;
  }

  /**
   * Get category label for display
   */
  getCategoryLabel(category: string): string {
    return this.payrollRulesService.getCategoryLabel(category);
  }

  // ============ CIMR Configuration Methods ============

  /**
   * Handle CIMR regime change
   */
  onCimrRegimeChange(regime: CimrRegime): void {
    if (!this.canEdit) return;

    if (regime === 'NONE') {
      this.draft.cimrConfig = { regime: 'NONE', employeeRate: 0, employerRate: 0 };
      this.draft.cimrRate = null;
    } else if (regime === 'AL_KAMIL') {
      const first = this.AL_KAMIL_RATES[0];
      this.draft.cimrConfig = { regime: 'AL_KAMIL', employeeRate: first.employeeRate, employerRate: first.employerRate };
      this.draft.cimrRate = first.employeeRate;
    } else {
      const first = this.AL_MOUNASSIB_RATES[0];
      this.draft.cimrConfig = { regime: 'AL_MOUNASSIB', employeeRate: first.employeeRate, employerRate: first.employerRate };
      this.draft.cimrRate = first.employeeRate;
    }
  }

  /**
   * Handle CIMR rate selection change
   */
  onCimrRateChange(rate: { employeeRate: number; employerRate: number }): void {
    if (!this.canEdit || !this.draft.cimrConfig) return;

    this.draft.cimrConfig = {
      ...this.draft.cimrConfig,
      employeeRate: rate.employeeRate,
      employerRate: rate.employerRate
    };
    this.draft.cimrRate = rate.employeeRate;
  }

  /**
   * Check if a rate option is currently selected
   */
  isCimrRateSelected(rate: { employeeRate: number }): boolean {
    if (!this.draft.cimrConfig) return false;
    return Math.abs(this.draft.cimrConfig.employeeRate - rate.employeeRate) < 0.0001;
  }
}
