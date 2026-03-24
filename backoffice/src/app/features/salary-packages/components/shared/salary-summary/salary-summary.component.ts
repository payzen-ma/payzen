import { Component, Input, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  SalaryPackageItem,
  CimrConfig,
  CNSS_CEILING
} from '../../../../../models/salary-package.model';

@Component({
  selector: 'app-salary-summary',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="bg-white rounded-lg shadow-sm border border-gray-200 p-6">
      <h3 class="font-semibold text-gray-900 mb-4 flex items-center gap-2">
        <svg class="w-5 h-5 text-primary" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 7h6m0 10v-3m-3 3h.01M9 17h.01M9 14h.01M12 14h.01M15 11h.01M12 11h.01M9 11h.01M7 21h10a2 2 0 002-2V5a2 2 0 00-2-2H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
        </svg>
        Récapitulatif
      </h3>
      
      <div class="space-y-3 text-sm">
        <!-- Base Salary -->
        @if (baseSalary > 0) {
          <div class="flex items-center justify-between py-2 border-b border-gray-100">
            <span class="text-gray-500">Salaire de base</span>
            <span class="font-medium text-gray-900">{{ formatCurrency(baseSalary) }} MAD</span>
          </div>
        } @else {
          <div class="flex items-center justify-between py-2 border-b border-gray-100">
            <span class="text-gray-500">Salaire de base</span>
            <span class="text-sm text-blue-600 italic">Défini en frontoffice</span>
          </div>
        }

        <!-- Allowances Breakdown -->
        <div *ngIf="itemsBreakdown.allowances > 0" class="flex items-center justify-between">
          <span class="text-gray-500">Indemnités</span>
          <span class="font-medium text-gray-700">{{ formatCurrency(itemsBreakdown.allowances) }} MAD</span>
        </div>

        <div *ngIf="itemsBreakdown.bonuses > 0" class="flex items-center justify-between">
          <span class="text-gray-500">Primes</span>
          <span class="font-medium text-gray-700">{{ formatCurrency(itemsBreakdown.bonuses) }} MAD</span>
        </div>

        <div *ngIf="itemsBreakdown.benefits > 0" class="flex items-center justify-between">
          <span class="text-gray-500">Avantages en nature</span>
          <span class="font-medium text-gray-700">{{ formatCurrency(itemsBreakdown.benefits) }} MAD</span>
        </div>

        <!-- Total Components -->
        <div class="flex items-center justify-between py-2 border-t border-gray-100">
          <span class="text-gray-500">Total éléments</span>
          <span class="font-medium text-gray-900">{{ formatCurrency(totalComponentsValue) }} MAD</span>
        </div>

        <!-- Gross Total -->
        <div class="flex items-center justify-between pt-3 border-t-2 border-gray-200">
          <span class="font-semibold text-gray-900">Total brut mensuel</span>
          @if (baseSalary > 0) {
            <span class="text-xl font-bold text-primary">{{ formatCurrency(totalPackageValue) }} MAD</span>
          } @else {
            <span class="text-sm text-blue-600 italic">Calculé en frontoffice</span>
          }
        </div>
      </div>

      <!-- Quick Stats -->
      <div class="grid grid-cols-2 gap-3 mt-6">
        <div class="bg-gray-50 rounded-lg p-3 text-center">
          <div class="text-2xl font-bold text-gray-900">{{ itemsCount }}</div>
          <div class="text-xs text-gray-500 mt-1">Éléments</div>
        </div>
        <div class="bg-gray-50 rounded-lg p-3 text-center">
          <div class="text-2xl font-bold text-gray-900">v{{ version }}</div>
          <div class="text-xs text-gray-500 mt-1">Version</div>
        </div>
      </div>

      <!-- CIMR Section -->
      @if (cimrConfig && cimrConfig.regime !== 'NONE') {
        <div class="mt-4 p-4 bg-gradient-to-r from-orange-50 to-amber-50 rounded-lg border border-orange-100">
          <div class="flex items-center justify-between mb-3">
            <div class="flex items-center gap-2">
              <svg class="w-4 h-4 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <span class="text-sm font-medium text-orange-800">CIMR {{ cimrConfig.regime === 'AL_KAMIL' ? 'Al Kamil' : 'Al Mounassib' }}</span>
            </div>
            <span class="text-xs px-2 py-0.5 rounded-full bg-orange-100 text-orange-700">
              {{ formatPercent(cimrConfig.employeeRate) }}
            </span>
          </div>
          <div class="space-y-2 text-sm">
            @if (cimrConfig.regime === 'AL_MOUNASSIB') {
              <div class="flex items-center justify-between text-xs text-orange-600">
                <span>Assiette (> {{ formatCurrency(cnssCeiling) }})</span>
                <span>{{ formatCurrency(cimrAssiette) }} MAD</span>
              </div>
            }
            <div class="flex items-center justify-between">
              <span class="text-orange-700">Part salariale</span>
              <span class="font-medium text-orange-900">{{ formatCurrency(cimrEmployeeContribution) }} MAD</span>
            </div>
            <div class="flex items-center justify-between">
              <span class="text-orange-700">Part patronale <span class="text-xs">({{ formatPercent(cimrConfig.employerRate) }})</span></span>
              <span class="font-medium text-orange-900">{{ formatCurrency(cimrEmployerContribution) }} MAD</span>
            </div>
          </div>
        </div>
      } @else if (cimrRate) {
        <!-- Legacy CIMR display -->
        <div class="mt-4 p-3 bg-blue-50 rounded-lg">
          <div class="flex items-center gap-2 text-sm">
            <svg class="w-4 h-4 text-blue-500" fill="currentColor" viewBox="0 0 20 20">
              <path fill-rule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2v-3a1 1 0 00-1-1H9z" clip-rule="evenodd" />
            </svg>
            <span class="text-blue-700">CIMR: {{ formatPercent(cimrRate) }}</span>
          </div>
        </div>
      }
    </div>
  `
})
export class SalarySummaryComponent {
  @Input() baseSalary = 0;
  @Input() items: SalaryPackageItem[] = [];
  @Input() version = 1;
  @Input() cimrRate: number | null = null;
  @Input() cimrConfig: CimrConfig | null = null;

  readonly cnssCeiling = CNSS_CEILING;

  get itemsCount(): number {
    return this.items.length;
  }

  get totalComponentsValue(): number {
    return this.items.reduce((sum, item) => sum + (Number(item.defaultValue) || 0), 0);
  }

  get totalPackageValue(): number {
    return (Number(this.baseSalary) || 0) + this.totalComponentsValue;
  }

  get itemsBreakdown(): { allowances: number; bonuses: number; benefits: number } {
    return this.items.reduce(
      (acc, item) => {
        const value = Number(item.defaultValue) || 0;
        switch (item.type) {
          case 'allowance':
            acc.allowances += value;
            break;
          case 'bonus':
            acc.bonuses += value;
            break;
          case 'benefit_in_kind':
            acc.benefits += value;
            break;
        }
        return acc;
      },
      { allowances: 0, bonuses: 0, benefits: 0 }
    );
  }

  // CIMR calculations
  get cimrAssiette(): number {
    if (!this.cimrConfig || this.cimrConfig.regime === 'NONE') return 0;
    
    const grossSalary = this.totalPackageValue;
    if (this.cimrConfig.regime === 'AL_MOUNASSIB') {
      return Math.max(0, grossSalary - CNSS_CEILING);
    }
    return grossSalary;
  }

  get cimrEmployeeContribution(): number {
    if (!this.cimrConfig || this.cimrConfig.regime === 'NONE') return 0;
    return this.cimrAssiette * this.cimrConfig.employeeRate;
  }

  get cimrEmployerContribution(): number {
    if (!this.cimrConfig || this.cimrConfig.regime === 'NONE') return 0;
    return this.cimrAssiette * this.cimrConfig.employerRate;
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('fr-MA', { 
      minimumFractionDigits: 0, 
      maximumFractionDigits: 2 
    }).format(value);
  }

  formatPercent(value: number): string {
    return `${(value * 100).toFixed(2).replace(/\.?0+$/, '')}%`;
  }
}
