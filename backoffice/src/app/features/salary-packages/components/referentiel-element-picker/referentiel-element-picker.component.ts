import { Component, Input, Output, EventEmitter, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ModalComponent } from '../../../../shared/modal/modal.component';
import { PayrollReferentielService } from '../../../../services/payroll-referentiel/payroll-referentiel.service';
import {
  ReferentielElementListDto,
  ReferentielElementDto,
  getConvergenceStatusText,
  getConvergenceStatusClass
} from '../../../../models/payroll-referentiel/referentiel-element.model';
import {
  ElementRuleDto,
  getRuleSummary,
  getExemptionTypeLabel
} from '../../../../models/payroll-referentiel/element-rule.model';
import { ExemptionType } from '../../../../models/payroll-referentiel/lookup.models';

export interface ReferentielElementSelection {
  element: ReferentielElementDto;
  cnssRule?: ElementRuleDto;
  irRule?: ElementRuleDto;
}

/**
 * Referentiel Element Picker Component
 * Modal to select referential pay elements with their CNSS/DGI rules
 */
@Component({
  selector: 'app-referentiel-element-picker',
  standalone: true,
  imports: [CommonModule, FormsModule, ModalComponent],
  template: `
    <app-modal
      [(visible)]="visible"
      [title]="'Sélectionner un élément référentiel'">

      <div class="space-y-4">
        <!-- Search and Filter -->
        <div class="flex gap-3">
          <div class="flex-1 relative">
            <svg class="w-5 h-5 absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
            <input
              type="text"
              [(ngModel)]="searchTerm"
              (ngModelChange)="onSearchChange()"
              placeholder="Rechercher par nom ou code..."
              class="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary focus:border-primary" />
          </div>

          <select
            [(ngModel)]="convergenceFilter"
            (ngModelChange)="onFilterChange()"
            class="px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary focus:border-primary bg-white">
            <option value="all">Tous</option>
            <option value="convergence">Convergence</option>
            <option value="divergence">Divergence</option>
          </select>
        </div>

        <!-- Loading State -->
        @if (isLoading) {
          <div class="py-12 text-center">
            <div class="animate-spin rounded-full h-10 w-10 border-b-2 border-primary mx-auto"></div>
            <p class="mt-4 text-sm text-gray-500">Chargement des éléments...</p>
          </div>
        }

        <!-- Elements List -->
        @if (!isLoading && filteredElements.length > 0) {
          <div class="max-h-96 overflow-y-auto border border-gray-200 rounded-lg divide-y divide-gray-100">
            @for (element of filteredElements; track element.id) {
              <button
                type="button"
                (click)="selectElement(element)"
                [disabled]="loadingElementId === element.id"
                class="w-full p-4 text-left hover:bg-gray-50 transition disabled:opacity-50 disabled:cursor-wait">
                <div class="flex items-start justify-between gap-3">
                  <div class="flex-1 min-w-0">
                    <div class="flex items-center gap-2 mb-1">
                      <h4 class="font-medium text-gray-900">{{ element.name }}</h4>
                      <span class="text-xs text-gray-500">{{ element.name }}</span>
                    </div>

                    <div class="flex flex-wrap items-center gap-2 text-xs">
                      <span class="px-2 py-0.5 bg-gray-100 text-gray-700 rounded">
                        {{ element.categoryName }}
                      </span>
                      <span
                        class="px-2 py-0.5 rounded font-medium"
                        [ngClass]="getConvergenceStatusClass(element.hasConvergence)">
                        {{ getConvergenceStatusText(element.hasConvergence) }}
                      </span>
                      <span class="text-gray-500">
                        {{ element.ruleCount }} règle(s)
                      </span>
                    </div>
                  </div>

                  <div class="shrink-0">
                    @if (loadingElementId === element.id) {
                      <svg class="w-5 h-5 text-primary animate-spin" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                      </svg>
                    } @else {
                      <svg class="w-5 h-5 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" />
                      </svg>
                    }
                  </div>
                </div>
              </button>
            }
          </div>
        }

        <!-- Empty State -->
        @if (!isLoading && filteredElements.length === 0) {
          <div class="py-12 text-center">
            <div class="w-16 h-16 bg-gray-100 rounded-full flex items-center justify-center mx-auto mb-4">
              <svg class="w-8 h-8 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            </div>
            <h3 class="text-sm font-medium text-gray-900 mb-1">Aucun élément trouvé</h3>
            <p class="text-sm text-gray-500">
              @if (searchTerm || convergenceFilter !== 'all') {
                Essayez de modifier vos critères de recherche
              } @else {
                Aucun élément référentiel disponible
              }
            </p>
          </div>
        }

        <!-- Element Details Preview (if selected) -->
        @if (selectedElementDetail && !isLoading) {
          <div class="mt-4 p-4 bg-blue-50 border border-blue-200 rounded-lg">
            <h4 class="font-semibold text-blue-900 mb-3">{{ selectedElementDetail.name }}</h4>

            @if (selectedElementDetail.description) {
              <p class="text-sm text-blue-800 mb-3">{{ selectedElementDetail.description }}</p>
            }

            <div class="space-y-2">
              @for (rule of selectedElementDetail.rules; track rule.id) {
                <div class="flex items-start gap-2 text-sm">
                  <span class="font-medium text-blue-900 min-w-[80px]">
                    {{ rule.authorityName }}:
                  </span>
                  <div class="flex-1">
                    <div class="text-blue-800">{{ getRuleSummary(rule) }}</div>
                    @if (rule.sourceRef) {
                      <div class="text-xs text-blue-600 mt-0.5">Réf: {{ rule.sourceRef }}</div>
                    }
                  </div>
                </div>
              }

              @if (selectedElementDetail.rules.length === 0) {
                <p class="text-sm text-blue-700">Aucune règle définie</p>
              }
            </div>

            <div class="mt-4 flex justify-end gap-2">
              <button
                type="button"
                (click)="cancelSelection()"
                class="px-3 py-1.5 text-sm text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition">
                Retour
              </button>
              <button
                type="button"
                (click)="confirmSelection()"
                class="px-3 py-1.5 text-sm text-white bg-primary rounded-lg hover:bg-primary/90 transition">
                Utiliser cet élément
              </button>
            </div>
          </div>
        }
      </div>

      <!-- Footer Actions -->
      <div class="flex justify-end gap-3 pt-4 border-t border-gray-200" *ngIf="!selectedElementDetail">
        <button
          type="button"
          (click)="onCancel()"
          class="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition">
          Annuler
        </button>
      </div>
    </app-modal>
  `
})
export class ReferentielElementPickerComponent implements OnInit {
  @Input() visible = false;
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() select = new EventEmitter<ReferentielElementSelection>();
  @Output() cancel = new EventEmitter<void>();

  private payrollService = inject(PayrollReferentielService);

  // Data
  elements: ReferentielElementListDto[] = [];
  filteredElements: ReferentielElementListDto[] = [];
  selectedElementDetail: ReferentielElementDto | null = null;

  // Filters
  searchTerm = '';
  convergenceFilter: 'all' | 'convergence' | 'divergence' = 'all';

  // State
  isLoading = false;
  loadingElementId: number | null = null;

  // Expose helper functions
  getConvergenceStatusText = getConvergenceStatusText;
  getConvergenceStatusClass = getConvergenceStatusClass;
  getRuleSummary = getRuleSummary;
  getExemptionTypeLabel = getExemptionTypeLabel;

  ngOnInit(): void {
    this.loadElements();
  }

  private loadElements(): void {
    this.isLoading = true;
    this.payrollService.getAllReferentielElements(false).subscribe({
      next: (elements) => {
        this.elements = elements;
        this.applyFilters();
        this.isLoading = false;
      },
      error: (err) => {
        this.isLoading = false;
      }
    });
  }

  onSearchChange(): void {
    this.applyFilters();
  }

  onFilterChange(): void {
    this.applyFilters();
  }

  private applyFilters(): void {
    let result = [...this.elements];

    // Filter by search term
    if (this.searchTerm.trim()) {
      const term = this.searchTerm.toLowerCase();
      result = result.filter(e =>
        e.name.toLowerCase().includes(term) ||
        e.categoryName.toLowerCase().includes(term)
      );
    }

    // Filter by convergence
    if (this.convergenceFilter === 'convergence') {
      result = result.filter(e => e.hasConvergence);
    } else if (this.convergenceFilter === 'divergence') {
      result = result.filter(e => !e.hasConvergence);
    }

    this.filteredElements = result;
  }

  selectElement(element: ReferentielElementListDto): void {
    this.loadingElementId = element.id;

    // Load full element details with rules
    this.payrollService.getReferentielElementById(element.id).subscribe({
      next: (fullElement) => {
        this.selectedElementDetail = fullElement;
        this.loadingElementId = null;
      },
      error: (err) => {
        this.loadingElementId = null;
      }
    });
  }

  cancelSelection(): void {
    this.selectedElementDetail = null;
  }

  confirmSelection(): void {
    if (!this.selectedElementDetail) return;

    // Find CNSS and IR rules (match by authority name; backend no longer exposes authorityCode)
    const cnssRule = this.selectedElementDetail.rules.find(r => this.isCnssAuthority(r.authorityName));
    const irRule = this.selectedElementDetail.rules.find(r => this.isIrAuthority(r.authorityName));

    const selection: ReferentielElementSelection = {
      element: this.selectedElementDetail,
      cnssRule,
      irRule
    };

    this.select.emit(selection);
    this.onCancel();
  }

  /** Match CNSS by authority name (e.g. "CNSS", "Caisse Nationale de Sécurité Sociale") */
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

  onCancel(): void {
    this.visibleChange.emit(false);
    this.selectedElementDetail = null;
    this.searchTerm = '';
    this.convergenceFilter = 'all';
    this.applyFilters();
    this.cancel.emit();
  }
}
