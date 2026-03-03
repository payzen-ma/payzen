import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SalaryPackageItem, RegulationVersion, CimrConfig } from '../../../../../models/salary-package.model';

@Component({
  selector: 'app-compliance-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
      <h3 class="font-semibold text-gray-900 mb-4 flex items-center gap-2">
        <svg class="w-5 h-5 text-purple-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
        </svg>
        Conformité {{ getRegulationLabel() }}
      </h3>

      <div class="space-y-4">
        <!-- IR (Taxable) -->
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2">
            <span class="w-8 h-8 rounded-lg bg-blue-50 flex items-center justify-center">
              <span class="text-xs font-bold text-blue-600">IR</span>
            </span>
            <div>
              <div class="text-sm font-medium text-gray-700">Imposables (IR)</div>
              <div class="text-xs text-gray-500">Impôt sur le Revenu</div>
            </div>
          </div>
          <div class="text-right">
            <div class="text-lg font-bold text-gray-900">{{ complianceStats.taxableCount }}</div>
            <div class="text-xs text-gray-500">/ {{ items.length }}</div>
          </div>
        </div>

        <!-- CNSS -->
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2">
            <span class="w-8 h-8 rounded-lg bg-teal-50 flex items-center justify-center">
              <span class="text-xs font-bold text-teal-600">CNSS</span>
            </span>
            <div>
              <div class="text-sm font-medium text-gray-700">Base CNSS</div>
              <div class="text-xs text-gray-500">Sécurité sociale</div>
            </div>
          </div>
          <div class="text-right">
            <div class="text-lg font-bold text-gray-900">{{ complianceStats.socialCount }}</div>
            <div class="text-xs text-gray-500">/ {{ items.length }}</div>
          </div>
        </div>

        <!-- CIMR -->
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2">
            <span class="w-8 h-8 rounded-lg bg-purple-50 flex items-center justify-center">
              <span class="text-xs font-bold text-purple-600">CIMR</span>
            </span>
            <div>
              <div class="text-sm font-medium text-gray-700">Base CIMR</div>
              <div class="text-xs text-gray-500">Retraite complémentaire</div>
            </div>
          </div>
          <div class="text-right">
            <div class="text-lg font-bold text-gray-900">{{ complianceStats.cimrCount }}</div>
            <div class="text-xs text-gray-500">/ {{ items.length }}</div>
          </div>
        </div>

        <!-- Variable Items -->
        <div *ngIf="complianceStats.variableCount > 0" class="flex items-center justify-between pt-3 border-t border-gray-100">
          <div class="flex items-center gap-2">
            <span class="w-8 h-8 rounded-lg bg-orange-50 flex items-center justify-center">
              <svg class="w-4 h-4 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 7h8m0 0v8m0-8l-8 8-4-4-6 6" />
              </svg>
            </span>
            <div>
              <div class="text-sm font-medium text-gray-700">Éléments variables</div>
              <div class="text-xs text-gray-500">Estimation mensuelle</div>
            </div>
          </div>
          <div class="text-right">
            <div class="text-lg font-bold text-orange-600">{{ complianceStats.variableCount }}</div>
          </div>
        </div>

        <!-- Exemption Items -->
        <div *ngIf="complianceStats.withExemptionCount > 0" class="flex items-center justify-between pt-3 border-t border-gray-100">
          <div class="flex items-center gap-2">
            <span class="w-8 h-8 rounded-lg bg-green-50 flex items-center justify-center">
              <svg class="w-4 h-4 text-green-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </span>
            <div>
              <div class="text-sm font-medium text-gray-700">Avec plafond d'exonération</div>
              <div class="text-xs text-gray-500">Limite fiscale applicable</div>
            </div>
          </div>
          <div class="text-right">
            <div class="text-lg font-bold text-green-600">{{ complianceStats.withExemptionCount }}</div>
          </div>
        </div>
      </div>

      <!-- CIMR Configuration -->
      @if (cimrConfig && cimrConfig.regime !== 'NONE') {
        <div class="mt-4 p-3 bg-orange-50 rounded-lg border border-orange-100">
          <div class="flex items-center justify-between mb-2">
            <span class="text-sm font-medium text-orange-800">CIMR {{ cimrConfig.regime === 'AL_KAMIL' ? 'Al Kamil' : 'Al Mounassib' }}</span>
          </div>
          <div class="grid grid-cols-2 gap-2 text-sm">
            <div>
              <span class="text-orange-600">Part salariale:</span>
              <span class="font-medium text-orange-800 ml-1">{{ formatPercent(cimrConfig.employeeRate) }}</span>
            </div>
            <div>
              <span class="text-orange-600">Part patronale:</span>
              <span class="font-medium text-orange-800 ml-1">{{ formatPercent(cimrConfig.employerRate) }}</span>
            </div>
          </div>
        </div>
      } @else if (cimrRate !== null) {
        <!-- Legacy CIMR Rate display -->
        <div class="mt-4 p-3 bg-purple-50 rounded-lg">
          <div class="flex items-center justify-between">
            <span class="text-sm text-purple-700">Taux CIMR configuré</span>
            <span class="font-bold text-purple-700">{{ formatPercent(cimrRate) }}</span>
          </div>
        </div>
      } @else {
        <div class="mt-4 p-3 bg-gray-50 rounded-lg">
          <div class="flex items-center gap-2 text-sm text-gray-500">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
            </svg>
            <span>CIMR non affilié</span>
          </div>
        </div>
      }

      <!-- Private Insurance: Removed - defined in frontoffice -->
    </div>
  `
})
export class ComplianceCardComponent {
  @Input() items: SalaryPackageItem[] = [];
  @Input() regulationVersion: RegulationVersion = 'MA_2025';
  @Input() cimrRate: number | null = null;
  @Input() cimrConfig: CimrConfig | null = null;
  @Input() hasPrivateInsurance = false;

  get complianceStats() {
    return this.items.reduce(
      (acc, item) => {
        if (item.isTaxable) acc.taxableCount++;
        if (item.isSocial) acc.socialCount++;
        if (item.isCIMR) acc.cimrCount++;
        if (item.isVariable) acc.variableCount++;
        if (item.exemptionLimit) acc.withExemptionCount++;
        return acc;
      },
      { taxableCount: 0, socialCount: 0, cimrCount: 0, variableCount: 0, withExemptionCount: 0 }
    );
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
    return `${(value * 100).toFixed(1)}%`;
  }
}
