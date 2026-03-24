import { Component, Input, Output, EventEmitter, computed, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SalaryPackage, SalaryPackageStatus } from '../../../../models/salary-package.model';
import { TemplateBadgesComponent } from '../shared/template-badges/template-badges.component';

export type TemplateAction = 'view' | 'edit' | 'publish' | 'deprecate' | 'duplicate' | 'delete';

export interface TemplateActionEvent {
  action: TemplateAction;
  template: SalaryPackage;
}

@Component({
  selector: 'app-template-list',
  standalone: true,
  imports: [CommonModule, FormsModule, TemplateBadgesComponent],
  template: `
    <div class="space-y-6">
      <!-- Header -->
      <div class="flex items-center justify-between">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Templates salariaux officiels</h1>
          <p class="text-sm text-gray-500 mt-1">Gérer les packages salariaux officiels PayZen</p>
        </div>
        <button
          type="button"
          (click)="onNewTemplate.emit()"
          class="inline-flex items-center gap-2 px-4 py-2.5 bg-primary text-white rounded-lg hover:bg-primary/90 transition font-medium shadow-sm">
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          Nouveau template
        </button>
      </div>

      <!-- Stats Cards -->
      <div class="grid grid-cols-2 md:grid-cols-5 gap-4">
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 bg-blue-50 rounded-lg flex items-center justify-center">
              <svg class="w-5 h-5 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            </div>
            <div>
              <div class="text-2xl font-bold text-gray-900">{{ stats().total }}</div>
              <div class="text-xs text-gray-500 uppercase tracking-wide">Total</div>
            </div>
          </div>
        </div>

        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 bg-emerald-50 rounded-lg flex items-center justify-center">
              <svg class="w-5 h-5 text-emerald-600" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
              </svg>
            </div>
            <div>
              <div class="text-2xl font-bold text-gray-900">{{ stats().published }}</div>
              <div class="text-xs text-gray-500 uppercase tracking-wide">Publiés</div>
            </div>
          </div>
        </div>

        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 bg-slate-50 rounded-lg flex items-center justify-center">
              <svg class="w-5 h-5 text-slate-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
              </svg>
            </div>
            <div>
              <div class="text-2xl font-bold text-gray-900">{{ stats().draft }}</div>
              <div class="text-xs text-gray-500 uppercase tracking-wide">Brouillons</div>
            </div>
          </div>
        </div>

        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 bg-amber-50 rounded-lg flex items-center justify-center">
              <svg class="w-5 h-5 text-amber-600" fill="currentColor" viewBox="0 0 20 20">
                <path fill-rule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clip-rule="evenodd" />
              </svg>
            </div>
            <div>
              <div class="text-2xl font-bold text-gray-900">{{ stats().deprecated }}</div>
              <div class="text-xs text-gray-500 uppercase tracking-wide">Obsolètes</div>
            </div>
          </div>
        </div>

        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 bg-purple-50 rounded-lg flex items-center justify-center">
              <svg class="w-5 h-5 text-purple-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
              </svg>
            </div>
            <div>
              <div class="text-2xl font-bold text-gray-900">{{ stats().official }}</div>
              <div class="text-xs text-gray-500 uppercase tracking-wide">Officiels</div>
            </div>
          </div>
        </div>
      </div>

      <!-- Filters -->
      <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
        <div class="flex flex-col lg:flex-row gap-4">
          <!-- Search -->
          <div class="flex-1 relative">
            <svg class="w-5 h-5 absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
            <input
              type="text"
              [(ngModel)]="searchTerm"
              (ngModelChange)="onSearchChange($event)"
              placeholder="Rechercher par nom, catégorie..."
              class="w-full pl-10 pr-4 py-2.5 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition" />
          </div>

          <!-- Category Filter -->
          <select
            [(ngModel)]="categoryFilter"
            (ngModelChange)="onCategoryChange($event)"
            class="px-4 py-2.5 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition bg-white min-w-[180px]">
            <option value="">Toutes catégories</option>
            @for (cat of categories; track cat) {
              <option [value]="cat">{{ cat }}</option>
            }
          </select>

          <!-- Status Filter -->
          <select
            [(ngModel)]="statusFilter"
            (ngModelChange)="onStatusChange($event)"
            class="px-4 py-2.5 border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary transition bg-white min-w-[160px]">
            <option value="">Tous statuts</option>
            <option value="published">Publiés</option>
            <option value="draft">Brouillons</option>
            <option value="deprecated">Obsolètes</option>
          </select>

          <!-- Clear Filters -->
          @if (hasActiveFilters()) {
            <button
              type="button"
              (click)="clearFilters()"
              class="px-4 py-2.5 text-gray-600 hover:text-gray-900 hover:bg-gray-100 rounded-lg transition flex items-center gap-2">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
              Effacer
            </button>
          }
        </div>
      </div>

      <!-- Loading State -->
      @if (isLoading) {
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-12">
          <div class="flex flex-col items-center justify-center">
            <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-primary"></div>
            <p class="mt-4 text-gray-500">Chargement des templates...</p>
          </div>
        </div>
      }

      <!-- Templates Table -->
      @if (!isLoading && filteredTemplates().length > 0) {
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-x-auto">
          <table class="w-full">
            <thead class="bg-gray-50 border-b border-gray-200">
              <tr>
                <th class="text-left px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wider">Template</th>
                <th class="text-left px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wider">Statut</th>
                <th class="text-center px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wider">CIMR</th>
                <th class="text-center px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wider">Éléments</th>
                <th class="text-left px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wider">Mis à jour</th>
                <th class="text-right px-6 py-4 text-xs font-semibold text-gray-500 uppercase tracking-wider">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100">
              @for (template of filteredTemplates(); track template.id) {
                <tr class="hover:bg-gray-50 transition cursor-pointer" (click)="onAction({ action: 'view', template })">
                  <td class="px-6 py-4">
                    <div class="flex items-center gap-3">
                      <div class="w-10 h-10 bg-primary/10 rounded-lg flex items-center justify-center">
                        <svg class="w-5 h-5 text-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                        </svg>
                      </div>
                      <div>
                        <div class="font-medium text-gray-900 flex items-center gap-2">
                          {{ template.name }}
                          @if (template.isLocked) {
                            <svg class="w-4 h-4 text-gray-400" fill="currentColor" viewBox="0 0 20 20">
                              <path fill-rule="evenodd" d="M5 9V7a5 5 0 0110 0v2a2 2 0 012 2v5a2 2 0 01-2 2H5a2 2 0 01-2-2v-5a2 2 0 012-2zm8-2v2H7V7a3 3 0 016 0z" clip-rule="evenodd" />
                            </svg>
                          }
                        </div>
                        <div class="text-sm text-gray-500">{{ template.category }}</div>
                      </div>
                    </div>
                  </td>
                  <td class="px-6 py-4">
                    <app-template-badges
                      [status]="template.status"
                      [regulationVersion]="template.regulationVersion"
                      [version]="template.version"
                      [showType]="false"
                      [showRegulation]="false">
                    </app-template-badges>
                  </td>
                  <td class="px-6 py-4 text-center">
                    @if (template.cimrConfig && template.cimrConfig.regime !== 'NONE') {
                      <span class="px-2.5 py-1 text-xs font-medium rounded-full"
                            [ngClass]="{
                              'bg-orange-100 text-orange-700': template.cimrConfig.regime === 'AL_KAMIL',
                              'bg-purple-100 text-purple-700': template.cimrConfig.regime === 'AL_MOUNASSIB'
                            }">
                        {{ template.cimrConfig.regime === 'AL_KAMIL' ? 'Al Kamil' : 'Al Mounassib' }}
                      </span>
                    } @else {
                      <span class="text-gray-400">—</span>
                    }
                  </td>
                  <td class="px-6 py-4 text-center">
                    <span class="text-gray-600">{{ template.items.length }}</span>
                  </td>
                  <td class="px-6 py-4 text-sm text-gray-500">
                    {{ formatDate(template.updatedAt) }}
                  </td>
                  <td class="px-6 py-4" (click)="$event.stopPropagation()">
                    <div class="flex items-center justify-end gap-2">
                      <!-- Primary Action Button -->
                      @if (template.status === 'draft') {
                        <button
                          type="button"
                          (click)="onAction({ action: 'edit', template })"
                          class="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-white bg-primary rounded-lg hover:bg-primary/90 transition">
                          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                          </svg>
                          Modifier
                        </button>
                      } @else {
                        <button
                          type="button"
                          (click)="onAction({ action: 'view', template })"
                          class="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition">
                          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                          </svg>
                          Voir
                        </button>
                      }

                      <!-- Overflow Menu -->
                      <div class="relative" #menuContainer>
                        <button
                          type="button"
                          (click)="toggleMenu(template.id, $event)"
                          class="p-2 text-gray-400 hover:text-gray-600 hover:bg-gray-100 rounded-lg transition">
                          <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20">
                            <path d="M10 6a2 2 0 110-4 2 2 0 010 4zM10 12a2 2 0 110-4 2 2 0 010 4zM10 18a2 2 0 110-4 2 2 0 010 4z" />
                          </svg>
                        </button>

                        @if (openMenuId === template.id) {
                          <div class="absolute right-0 w-48 bg-white rounded-lg shadow-lg border border-gray-200 py-1 z-50"
                               [ngClass]="menuPositionAbove ? 'bottom-full mb-2' : 'mt-2'">
                            @if (template.status === 'draft') {
                              <button
                                type="button"
                                (click)="onAction({ action: 'publish', template }); closeMenu()"
                                class="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-50 flex items-center gap-2">
                                <svg class="w-4 h-4 text-emerald-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
                                </svg>
                                Publier
                              </button>
                              <button
                                type="button"
                                (click)="onAction({ action: 'duplicate', template }); closeMenu()"
                                class="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-50 flex items-center gap-2">
                                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7v8a2 2 0 002 2h6M8 7V5a2 2 0 012-2h4.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V15a2 2 0 01-2 2h-2M8 7H6a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2v-2" />
                                </svg>
                                Dupliquer
                              </button>
                              <div class="border-t border-gray-100 my-1"></div>
                              <button
                                type="button"
                                (click)="onAction({ action: 'delete', template }); closeMenu()"
                                class="w-full text-left px-4 py-2 text-sm text-red-600 hover:bg-red-50 flex items-center gap-2">
                                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
                                </svg>
                                Supprimer
                              </button>
                            } @else if (template.status === 'published') {
                              <button
                                type="button"
                                (click)="onAction({ action: 'duplicate', template }); closeMenu()"
                                class="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-50 flex items-center gap-2">
                                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7v8a2 2 0 002 2h6M8 7V5a2 2 0 012-2h4.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V15a2 2 0 01-2 2h-2M8 7H6a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2v-2" />
                                </svg>
                                Dupliquer
                              </button>
                              <button
                                type="button"
                                (click)="onAction({ action: 'deprecate', template }); closeMenu()"
                                class="w-full text-left px-4 py-2 text-sm text-amber-600 hover:bg-amber-50 flex items-center gap-2">
                                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
                                </svg>
                                Marquer obsolète
                              </button>
                            } @else {
                              <button
                                type="button"
                                (click)="onAction({ action: 'duplicate', template }); closeMenu()"
                                class="w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-50 flex items-center gap-2">
                                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7v8a2 2 0 002 2h6M8 7V5a2 2 0 012-2h4.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V15a2 2 0 01-2 2h-2M8 7H6a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2v-2" />
                                </svg>
                                Dupliquer
                              </button>
                            }
                          </div>
                        }
                      </div>
                    </div>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }

      <!-- Empty State -->
      @if (!isLoading && filteredTemplates().length === 0) {
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-12 text-center">
          <div class="w-16 h-16 bg-gray-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <svg class="w-8 h-8 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
          </div>
          @if (hasActiveFilters()) {
            <h3 class="text-lg font-medium text-gray-900 mb-2">Aucun résultat</h3>
            <p class="text-gray-500 mb-6">Aucun template ne correspond à vos critères de recherche.</p>
            <button
              type="button"
              (click)="clearFilters()"
              class="inline-flex items-center gap-2 px-4 py-2 text-primary hover:bg-primary/5 rounded-lg transition">
              <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
              Effacer les filtres
            </button>
          } @else {
            <h3 class="text-lg font-medium text-gray-900 mb-2">Aucun template</h3>
            <p class="text-gray-500 mb-6">Commencez par créer votre premier template salarial officiel.</p>
            <button
              type="button"
              (click)="onNewTemplate.emit()"
              class="inline-flex items-center gap-2 px-4 py-2 bg-primary text-white rounded-lg hover:bg-primary/90 transition">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
              </svg>
              Créer un template
            </button>
          }
        </div>
      }
    </div>
  `
})
export class TemplateListComponent {
  @Input() templates: SalaryPackage[] = [];
  @Input() categories: string[] = [];
  @Input() isLoading = false;

  @Output() onNewTemplate = new EventEmitter<void>();
  @Output() onTemplateAction = new EventEmitter<TemplateActionEvent>();
  @Output() onFiltersChange = new EventEmitter<{ search: string; status: string; category: string }>();

  searchTerm = '';
  statusFilter = '';
  categoryFilter = '';
  openMenuId: number | null = null;
  menuPositionAbove = false;

  stats = computed(() => ({
    total: this.templates.length,
    published: this.templates.filter(t => t.status === 'published').length,
    draft: this.templates.filter(t => t.status === 'draft').length,
    deprecated: this.templates.filter(t => t.status === 'deprecated').length,
    official: this.templates.filter(t => t.templateType === 'OFFICIAL').length
  }));

  filteredTemplates = computed(() => {
    let result = [...this.templates];

    // Filter by status
    if (this.statusFilter) {
      result = result.filter(t => t.status === this.statusFilter);
    }

    // Filter by category
    if (this.categoryFilter) {
      result = result.filter(t => t.category === this.categoryFilter);
    }

    // Filter by search term
    const term = this.searchTerm.trim().toLowerCase();
    if (term) {
      result = result.filter(t =>
        t.name.toLowerCase().includes(term) ||
        t.category.toLowerCase().includes(term) ||
        (t.description || '').toLowerCase().includes(term)
      );
    }

    // Sort: published first, then draft, then deprecated
    const statusOrder: Record<string, number> = { published: 0, draft: 1, deprecated: 2 };
    return result.sort((a, b) => {
      const statusDiff = (statusOrder[a.status] ?? 3) - (statusOrder[b.status] ?? 3);
      if (statusDiff !== 0) return statusDiff;
      return a.name.localeCompare(b.name);
    });
  });

  hasActiveFilters(): boolean {
    return !!(this.searchTerm || this.statusFilter || this.categoryFilter);
  }

  onSearchChange(value: string): void {
    this.emitFilters();
  }

  onStatusChange(value: string): void {
    this.emitFilters();
  }

  onCategoryChange(value: string): void {
    this.emitFilters();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.statusFilter = '';
    this.categoryFilter = '';
    this.emitFilters();
  }

  private emitFilters(): void {
    this.onFiltersChange.emit({
      search: this.searchTerm,
      status: this.statusFilter,
      category: this.categoryFilter
    });
  }

  onAction(event: TemplateActionEvent): void {
    this.onTemplateAction.emit(event);
  }

  toggleMenu(templateId: number, event?: MouseEvent): void {
    if (this.openMenuId === templateId) {
      this.openMenuId = null;
      return;
    }

    this.openMenuId = templateId;

    // Calculate if menu should appear above or below
    if (event) {
      const button = event.target as HTMLElement;
      const buttonRect = button.getBoundingClientRect();
      const viewportHeight = window.innerHeight;
      const spaceBelow = viewportHeight - buttonRect.bottom;
      const menuHeight = 200; // Approximate menu height

      // Show above if not enough space below
      this.menuPositionAbove = spaceBelow < menuHeight;
    }
  }

  closeMenu(): void {
    this.openMenuId = null;
    this.menuPositionAbove = false;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    // Close menu when clicking outside
    const target = event.target as HTMLElement;
    if (this.openMenuId !== null && !target.closest('.relative')) {
      this.openMenuId = null;
      this.menuPositionAbove = false;
    }
  }

  getAvailableActions(template: SalaryPackage): Array<{ key: TemplateAction; label: string; icon: string; class: string }> {
    const actions: Array<{ key: TemplateAction; label: string; icon: string; class: string }> = [];

    switch (template.status) {
      case 'draft':
        actions.push(
          { key: 'edit', label: 'Modifier', icon: 'M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z', class: 'text-gray-500 hover:text-primary hover:bg-primary/5' },
          { key: 'publish', label: 'Publier', icon: 'M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z', class: 'text-emerald-500 hover:text-emerald-700 hover:bg-emerald-50' },
          { key: 'duplicate', label: 'Dupliquer', icon: 'M8 7v8a2 2 0 002 2h6M8 7V5a2 2 0 012-2h4.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V15a2 2 0 01-2 2h-2M8 7H6a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2v-2', class: 'text-gray-500 hover:text-blue-600 hover:bg-blue-50' },
          { key: 'delete', label: 'Supprimer', icon: 'M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16', class: 'text-gray-400 hover:text-red-600 hover:bg-red-50' }
        );
        break;
      case 'published':
        actions.push(
          { key: 'view', label: 'Voir', icon: 'M15 12a3 3 0 11-6 0 3 3 0 016 0z M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z', class: 'text-gray-500 hover:text-primary hover:bg-primary/5' },
          { key: 'duplicate', label: 'Dupliquer', icon: 'M8 7v8a2 2 0 002 2h6M8 7V5a2 2 0 012-2h4.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V15a2 2 0 01-2 2h-2M8 7H6a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2v-2', class: 'text-gray-500 hover:text-blue-600 hover:bg-blue-50' },
          { key: 'deprecate', label: 'Marquer obsolète', icon: 'M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636', class: 'text-gray-400 hover:text-amber-600 hover:bg-amber-50' }
        );
        break;
      case 'deprecated':
        actions.push(
          { key: 'view', label: 'Voir', icon: 'M15 12a3 3 0 11-6 0 3 3 0 016 0z M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z', class: 'text-gray-500 hover:text-primary hover:bg-primary/5' },
          { key: 'duplicate', label: 'Dupliquer', icon: 'M8 7v8a2 2 0 002 2h6M8 7V5a2 2 0 012-2h4.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V15a2 2 0 01-2 2h-2M8 7H6a2 2 0 00-2 2v10a2 2 0 002 2h8a2 2 0 002-2v-2', class: 'text-gray-500 hover:text-blue-600 hover:bg-blue-50' }
        );
        break;
    }

    return actions;
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('fr-MA', { minimumFractionDigits: 0, maximumFractionDigits: 0 }).format(value);
  }

  formatDate(date: string | null | undefined): string {
    if (!date) return '-';
    return new Date(date).toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
  }
}
