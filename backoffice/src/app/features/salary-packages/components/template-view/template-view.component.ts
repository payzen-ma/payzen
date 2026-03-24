import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SalaryPackage, SalaryComponentType, CNSS_CEILING } from '../../../../models/salary-package.model';
import { TemplateBadgesComponent } from '../shared/template-badges/template-badges.component';

export type ViewAction = 'back' | 'duplicate' | 'deprecate' | 'edit';

@Component({
  selector: 'app-template-view',
  standalone: true,
  imports: [
    CommonModule,
    TemplateBadgesComponent
  ],
  template: `
    <div class="space-y-6">
      <!-- Back Button -->
      <button
        type="button"
        (click)="onAction.emit('back')"
        class="inline-flex items-center gap-2 text-gray-600 hover:text-gray-900 transition">
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 19l-7-7 7-7" />
        </svg>
        Retour à la liste
      </button>

      <!-- Header -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
        <div class="flex items-start justify-between gap-4">
          <div class="flex-1">
            <div class="flex items-center gap-3 mb-2">
              <h1 class="text-2xl font-bold text-gray-900">{{ template.name }}</h1>
              @if (template.isLocked) {
                <span class="p-1.5 bg-gray-100 rounded-lg" title="Verrouillé">
                  <svg class="w-4 h-4 text-gray-500" fill="currentColor" viewBox="0 0 20 20">
                    <path fill-rule="evenodd" d="M5 9V7a5 5 0 0110 0v2a2 2 0 012 2v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5a2 2 0 012-2zm8-2v2H7V7a3 3 0 016 0z" clip-rule="evenodd" />
                  </svg>
                </span>
              }
            </div>
            <app-template-badges
              [templateType]="template.templateType"
              [status]="template.status"
              [regulationVersion]="template.regulationVersion"
              [version]="template.version"
              [isLocked]="template.isLocked">
            </app-template-badges>
            @if (template.description) {
              <p class="mt-3 text-gray-600">{{ template.description }}</p>
            }
          </div>

          <!-- Actions -->
          <div class="flex items-center gap-2">
            @if (template.status === 'draft') {
              <button
                type="button"
                (click)="onAction.emit('edit')"
                class="inline-flex items-center gap-2 px-4 py-2 bg-primary text-white rounded-lg hover:bg-primary/90 transition">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                </svg>
                Modifier
              </button>
            }
            <button
              type="button"
              (click)="onAction.emit('duplicate')"
              class="inline-flex items-center gap-2 px-4 py-2 bg-gray-100 text-gray-700 rounded-lg hover:bg-gray-200 transition">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7v8a2 2 0 002 2h6M8 7V5a2 2 0 012-2h4.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V15a2 2 0 01-2 2h-2M8 7H6a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2v-2" />
              </svg>
              Dupliquer
            </button>
            @if (template.status === 'published') {
              <button
                type="button"
                (click)="onAction.emit('deprecate')"
                class="inline-flex items-center gap-2 px-4 py-2 bg-amber-50 text-amber-700 rounded-lg hover:bg-amber-100 transition">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
                </svg>
                Marquer obsolète
              </button>
            }
          </div>
        </div>
      </div>

      <!-- Deprecated Warning -->
      @if (template.status === 'deprecated') {
        <div class="bg-amber-50 border border-amber-200 rounded-xl p-4">
          <div class="flex items-center gap-3">
            <svg class="w-5 h-5 text-amber-500" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
            </svg>
            <div>
              <div class="font-medium text-amber-800">Ce template est obsolète</div>
              <div class="text-sm text-amber-600">Il ne doit plus être utilisé pour de nouvelles configurations. Vous pouvez le dupliquer pour créer une nouvelle version.</div>
            </div>
          </div>
        </div>
      }

      <!-- Two Column Layout -->
      <div class="grid lg:grid-cols-3 gap-6">
        <!-- Left Column: Template Details -->
        <div class="lg:col-span-2 space-y-6">
          <!-- General Information -->
          <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
            <div class="px-6 py-4 border-b border-gray-100 bg-gray-50">
              <h2 class="font-semibold text-gray-900">Informations générales</h2>
            </div>
            <div class="p-6">
              <dl class="grid grid-cols-2 gap-6">
                <div>
                  <dt class="text-sm font-medium text-gray-500">Nom</dt>
                  <dd class="mt-1 text-gray-900">{{ template.name }}</dd>
                </div>
                <div>
                  <dt class="text-sm font-medium text-gray-500">Catégorie</dt>
                  <dd class="mt-1">
                    <span class="px-2.5 py-1 text-sm bg-gray-100 text-gray-700 rounded-full">{{ template.category }}</span>
                  </dd>
                </div>
                <div>
                  <dt class="text-sm font-medium text-gray-500">Réglementation</dt>
                  <dd class="mt-1">
                    <span class="px-2.5 py-1 text-sm bg-purple-50 text-purple-700 rounded-full">Maroc 2025</span>
                  </dd>
                </div>
                @if (template.description) {
                  <div class="col-span-2">
                    <dt class="text-sm font-medium text-gray-500">Description</dt>
                    <dd class="mt-1 text-gray-900">{{ template.description }}</dd>
                  </div>
                }
              </dl>
            </div>
          </div>

          <!-- CIMR Configuration -->
          <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
            <div class="px-6 py-4 border-b border-gray-100 bg-gradient-to-r from-orange-50 to-amber-50">
              <div class="flex items-center gap-3">
                <div class="w-10 h-10 rounded-lg bg-orange-100 flex items-center justify-center">
                  <svg class="w-5 h-5 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                </div>
                <div>
                  <h2 class="font-semibold text-gray-900">Retraite Complémentaire CIMR</h2>
                  <p class="text-xs text-gray-500">Configuration du régime de retraite</p>
                </div>
              </div>
            </div>
            <div class="p-6">
              @if (template.cimrConfig && template.cimrConfig.regime !== 'NONE') {
                <div class="space-y-4">
                  <div class="flex items-center justify-between">
                    <span class="text-sm text-gray-600">Régime</span>
                    <span class="px-3 py-1 text-sm font-medium rounded-full"
                          [ngClass]="{
                            'bg-orange-100 text-orange-700': template.cimrConfig.regime === 'AL_KAMIL',
                            'bg-purple-100 text-purple-700': template.cimrConfig.regime === 'AL_MOUNASSIB'
                          }">
                      {{ template.cimrConfig.regime === 'AL_KAMIL' ? 'Al Kamil' : 'Al Mounassib' }}
                    </span>
                  </div>
                  <div class="grid grid-cols-2 gap-4 pt-3 border-t border-gray-100">
                    <div class="bg-gray-50 rounded-lg p-3">
                      <div class="text-xs text-gray-500 mb-1">Part Salariale</div>
                      <div class="text-lg font-semibold text-gray-900">{{ formatPercent(template.cimrConfig.employeeRate) }}</div>
                    </div>
                    <div class="bg-orange-50 rounded-lg p-3">
                      <div class="text-xs text-orange-600 mb-1">Part Patronale</div>
                      <div class="text-lg font-semibold text-orange-700">{{ formatPercent(template.cimrConfig.employerRate) }}</div>
                    </div>
                  </div>
                </div>
              } @else if (template.cimrRate) {
                <!-- Legacy CIMR display -->
                <div class="flex items-center justify-between">
                  <span class="text-sm text-gray-600">Taux CIMR (Legacy)</span>
                  <span class="text-lg font-semibold text-gray-900">{{ formatPercent(template.cimrRate) }}</span>
                </div>
              } @else {
                <div class="text-center py-4">
                  <svg class="w-8 h-8 mx-auto text-gray-300 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
                  </svg>
                  <p class="text-sm text-gray-500">Non affilié à la CIMR</p>
                </div>
              }
            </div>
          </div>

          <!-- Fixed Pay Elements -->
          <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
            <div class="px-6 py-4 border-b border-gray-100 bg-gray-50">
              <div class="flex items-center justify-between">
                <h2 class="font-semibold text-gray-900">Éléments de rémunération</h2>
                <span class="text-sm text-gray-500">{{ template.items.length }} éléments</span>
              </div>
            </div>
            @if (template.items.length > 0) {
              <div class="divide-y divide-gray-100">
                @for (item of template.items; track item.id; let i = $index) {
                  <div class="p-4 hover:bg-gray-50">
                    <div class="flex items-center justify-between">
                      <div class="flex items-center gap-3">
                        <div class="w-8 h-8 rounded-lg flex items-center justify-center text-sm font-medium"
                             [ngClass]="getTypeColorClass(item.type)">
                          {{ i + 1 }}
                        </div>
                        <div>
                          <div class="font-medium text-gray-900">{{ item.label }}</div>
                          <div class="flex items-center gap-2 mt-1 flex-wrap">
                            <span class="text-xs px-2 py-0.5 rounded"
                                  [ngClass]="getTypeColorClass(item.type)">
                              {{ getTypeLabel(item.type) }}
                            </span>
                            @if (item.payComponentCode) {
                              <span class="text-xs text-gray-400 font-mono">{{ item.payComponentCode }}</span>
                            }
                          </div>
                        </div>
                      </div>
                      <div class="text-right">
                        <div class="font-semibold text-gray-900">{{ formatCurrency(item.defaultValue) }} MAD</div>
                        <div class="flex items-center gap-2 mt-1 text-xs">
                          @if (item.isTaxable) {
                            <span class="px-1.5 py-0.5 bg-blue-100 text-blue-700 rounded">IR</span>
                          }
                          @if (item.isSocial) {
                            <span class="px-1.5 py-0.5 bg-teal-100 text-teal-700 rounded">CNSS</span>
                          }
                          @if (item.isCIMR) {
                            <span class="px-1.5 py-0.5 bg-purple-100 text-purple-700 rounded">CIMR</span>
                          }
                        </div>
                      </div>
                    </div>
                    @if (item.exemptionLimit) {
                      <div class="mt-2 text-xs text-gray-500 flex items-center gap-1">
                        <svg class="w-3 h-3" fill="currentColor" viewBox="0 0 20 20">
                          <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
                        </svg>
                        Plafond d'exonération: {{ formatCurrency(item.exemptionLimit) }} MAD
                      </div>
                    }
                  </div>
                }
              </div>
            } @else {
              <div class="p-8 text-center text-gray-500">
                Aucun élément de rémunération défini
              </div>
            }
          </div>

        </div>

        <!-- Right Column: Metadata & Info -->
        <div class="space-y-6">
          <!-- Source Template Info -->
          @if (template.sourceTemplateId) {
            <div class="bg-blue-50 border border-blue-200 rounded-xl p-4">
              <div class="flex items-center gap-2 mb-2">
                <svg class="w-5 h-5 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7v8a2 2 0 002 2h6M8 7V5a2 2 0 012-2h4.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V15a2 2 0 01-2 2h-2M8 7H6a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2v-2" />
                </svg>
                <h4 class="font-medium text-blue-900">Origine</h4>
              </div>
              <p class="text-sm text-blue-800">
                Basé sur: {{ template.sourceTemplateName || 'Template #' + template.sourceTemplateId }}
                <span class="text-blue-600">(v{{ template.sourceTemplateVersion }})</span>
              </p>
            </div>
          }

          <!-- Metadata -->
          <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
            <h4 class="text-sm font-medium text-gray-500 mb-3">Métadonnées</h4>
            <dl class="space-y-2 text-sm">
              <div class="flex justify-between">
                <dt class="text-gray-500">Créé le</dt>
                <dd class="text-gray-900">{{ formatDate(template.createdAt) }}</dd>
              </div>
              <div class="flex justify-between">
                <dt class="text-gray-500">Modifié le</dt>
                <dd class="text-gray-900">{{ formatDate(template.updatedAt) }}</dd>
              </div>
            </dl>
          </div>
        </div>
      </div>
    </div>
  `
})
export class TemplateViewComponent {
  @Input() template!: SalaryPackage;
  @Output() onAction = new EventEmitter<ViewAction>();

  readonly cnssCeiling = CNSS_CEILING;

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

  getTypeColorClass(type: SalaryComponentType): string {
    const classes: Record<SalaryComponentType, string> = {
      base_salary: 'bg-blue-100 text-blue-700',
      allowance: 'bg-teal-100 text-teal-700',
      bonus: 'bg-purple-100 text-purple-700',
      benefit_in_kind: 'bg-rose-100 text-rose-700',
      social_charge: 'bg-orange-100 text-orange-700'
    };
    return classes[type] || 'bg-gray-100 text-gray-700';
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('fr-MA', { minimumFractionDigits: 0, maximumFractionDigits: 2 }).format(value);
  }

  formatPercent(value: number): string {
    return `${(value * 100).toFixed(1)}%`;
  }

  formatDate(date: string | null | undefined): string {
    if (!date) return '-';
    return new Date(date).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
