import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  CimrConfig,
  CimrRegime,
  CIMR_AL_KAMIL_RATES,
  CIMR_AL_MOUNASSIB_RATES,
  CNSS_CEILING,
  calculateCimrEmployerRate
} from '../../../../../models/salary-package.model';

export interface CimrCalculation {
  assiette: number;
  employeeContribution: number;
  employerContribution: number;
  totalCost: number;
}

@Component({
  selector: 'app-cimr-config-card',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
      <div class="px-6 py-4 border-b border-gray-100 bg-gradient-to-r from-orange-50 to-amber-50">
        <div class="flex items-center justify-between">
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
          @if (config.regime !== 'NONE') {
            <span class="px-2.5 py-1 text-xs font-medium rounded-full bg-orange-100 text-orange-700">
              {{ config.regime === 'AL_KAMIL' ? 'Al Kamil' : 'Al Mounassib' }}
            </span>
          }
        </div>
      </div>

      <div class="p-6 space-y-5">
        <!-- Regime Selection -->
        <div>
          <label class="block text-sm font-medium text-gray-700 mb-2">Régime CIMR</label>
          <div class="grid grid-cols-3 gap-2">
            <button
              type="button"
              (click)="setRegime('NONE')"
              [disabled]="readonly"
              [class.ring-2]="config.regime === 'NONE'"
              [class.ring-gray-400]="config.regime === 'NONE'"
              class="px-3 py-2.5 text-sm font-medium rounded-lg border border-gray-200 hover:bg-gray-50 transition disabled:opacity-50 disabled:cursor-not-allowed"
              [class.bg-gray-100]="config.regime === 'NONE'">
              Non affilié
            </button>
            <button
              type="button"
              (click)="setRegime('AL_KAMIL')"
              [disabled]="readonly"
              [class.ring-2]="config.regime === 'AL_KAMIL'"
              [class.ring-orange-500]="config.regime === 'AL_KAMIL'"
              class="px-3 py-2.5 text-sm font-medium rounded-lg border border-gray-200 hover:bg-orange-50 transition disabled:opacity-50 disabled:cursor-not-allowed"
              [class.bg-orange-50]="config.regime === 'AL_KAMIL'"
              [class.text-orange-700]="config.regime === 'AL_KAMIL'">
              Al Kamil
            </button>
            <button
              type="button"
              (click)="setRegime('AL_MOUNASSIB')"
              [disabled]="readonly"
              [class.ring-2]="config.regime === 'AL_MOUNASSIB'"
              [class.ring-amber-500]="config.regime === 'AL_MOUNASSIB'"
              class="px-3 py-2.5 text-sm font-medium rounded-lg border border-gray-200 hover:bg-amber-50 transition disabled:opacity-50 disabled:cursor-not-allowed"
              [class.bg-amber-50]="config.regime === 'AL_MOUNASSIB'"
              [class.text-amber-700]="config.regime === 'AL_MOUNASSIB'">
              Al Mounassib
            </button>
          </div>
          <!-- Regime description -->
          @if (config.regime !== 'NONE') {
            <p class="mt-2 text-xs text-gray-500">
              @if (config.regime === 'AL_KAMIL') {
                Assiette: <span class="font-medium">Salaire Brut Total</span> — Régime standard pour toutes les entreprises.
              } @else {
                Assiette: <span class="font-medium">Part dépassant le plafond CNSS ({{ formatCurrency(cnssCeiling) }} MAD)</span> — Destiné aux PME.
              }
            </p>
          }
        </div>

        @if (config.regime !== 'NONE') {
          <!-- Rate Selection -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">
              Taux Salarial
              <span class="font-normal text-gray-400 ml-1">(Part employé)</span>
            </label>
            <div class="flex flex-wrap gap-2">
              @for (rate of availableRates; track rate.employeeRate) {
                <button
                  type="button"
                  (click)="setEmployeeRate(rate.employeeRate)"
                  [disabled]="readonly"
                  [class.ring-2]="isSelectedRate(rate.employeeRate)"
                  [class.ring-orange-500]="isSelectedRate(rate.employeeRate)"
                  [class.bg-orange-50]="isSelectedRate(rate.employeeRate)"
                  [class.text-orange-700]="isSelectedRate(rate.employeeRate)"
                  class="px-3 py-1.5 text-sm rounded-lg border border-gray-200 hover:bg-gray-50 transition disabled:opacity-50 disabled:cursor-not-allowed">
                  {{ rate.label }}
                </button>
              }
            </div>
            <!-- Custom rate option -->
            <div class="mt-3 flex items-center gap-3">
              <label class="flex items-center gap-2 cursor-pointer text-sm text-gray-600">
                <input
                  type="checkbox"
                  [(ngModel)]="useCustomRate"
                  [disabled]="readonly"
                  (ngModelChange)="onCustomRateToggle($event)"
                  class="w-4 h-4 rounded border-gray-300 text-orange-600 focus:ring-orange-500/30" />
                Taux personnalisé
              </label>
              @if (useCustomRate) {
                <div class="flex items-center gap-2">
                  <input
                    type="number"
                    min="0"
                    max="20"
                    step="0.25"
                    [ngModel]="config.employeeRate * 100"
                    (ngModelChange)="setCustomEmployeeRate($event)"
                    [disabled]="readonly"
                    class="w-20 px-2 py-1.5 text-sm border border-gray-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-orange-500/30 focus:border-orange-500 disabled:bg-gray-50" />
                  <span class="text-sm text-gray-500">%</span>
                </div>
              }
            </div>
          </div>

          <!-- Rates Display -->
          <div class="bg-gray-50 rounded-lg p-4">
            <div class="grid grid-cols-2 gap-4">
              <div>
                <div class="text-xs text-gray-500 mb-1">Part Salariale</div>
                <div class="text-lg font-semibold text-gray-900">{{ formatPercent(config.employeeRate) }}</div>
              </div>
              <div>
                <div class="text-xs text-gray-500 mb-1 flex items-center gap-1">
                  Part Patronale
                  <span class="text-[10px] text-gray-400">(×1.3)</span>
                </div>
                <div class="text-lg font-semibold text-orange-600">{{ formatPercent(config.employerRate) }}</div>
              </div>
            </div>
          </div>

          <!-- Live Calculation Preview -->
          @if (baseSalary > 0) {
            <div class="border-t border-gray-100 pt-4">
              <div class="flex items-center justify-between mb-3">
                <span class="text-sm font-medium text-gray-700">Aperçu des cotisations</span>
                <span class="text-xs text-gray-400">Base: {{ formatCurrency(baseSalary) }} MAD</span>
              </div>
              <div class="space-y-2">
                @if (config.regime === 'AL_MOUNASSIB') {
                  <div class="flex items-center justify-between text-sm">
                    <span class="text-gray-500">Assiette CIMR (> {{ formatCurrency(cnssCeiling) }})</span>
                    <span class="font-medium text-gray-900">{{ formatCurrency(calculation.assiette) }} MAD</span>
                  </div>
                }
                <div class="flex items-center justify-between text-sm">
                  <span class="text-gray-500">Cotisation salariale</span>
                  <span class="font-medium text-gray-900">{{ formatCurrency(calculation.employeeContribution) }} MAD</span>
                </div>
                <div class="flex items-center justify-between text-sm">
                  <span class="text-gray-500">Cotisation patronale</span>
                  <span class="font-medium text-orange-600">{{ formatCurrency(calculation.employerContribution) }} MAD</span>
                </div>
                <div class="flex items-center justify-between text-sm pt-2 border-t border-gray-200">
                  <span class="text-gray-700 font-medium">Coût total employeur</span>
                  <span class="font-bold text-gray-900">{{ formatCurrency(calculation.totalCost) }} MAD</span>
                </div>
              </div>
            </div>
          } @else {
            <div class="border-t border-gray-100 pt-4">
              <div class="bg-blue-50 rounded-lg p-4 text-center">
                <svg class="w-8 h-8 mx-auto text-blue-400 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <p class="text-sm text-blue-700 font-medium">Salaire défini en frontoffice</p>
                <p class="text-xs text-blue-600 mt-1">Les cotisations seront calculées lors de la création du contrat</p>
              </div>
            </div>
          }
        } @else {
          <!-- Non-affiliated state -->
          <div class="bg-gray-50 rounded-lg p-4 text-center">
            <svg class="w-8 h-8 mx-auto text-gray-300 mb-2" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
            </svg>
            <p class="text-sm text-gray-500">Pas de cotisation CIMR</p>
            <p class="text-xs text-gray-400 mt-1">Le salarié ne bénéficie pas de retraite complémentaire</p>
          </div>
        }
      </div>
    </div>
  `
})
export class CimrConfigCardComponent implements OnInit {
  @Input() config: CimrConfig = { regime: 'NONE', employeeRate: 0, employerRate: 0 };
  @Input() baseSalary: number = 0;
  @Input() readonly: boolean = false;

  @Output() configChange = new EventEmitter<CimrConfig>();

  useCustomRate = false;
  readonly cnssCeiling = CNSS_CEILING;

  ngOnInit(): void {
    // Check if current rate is custom (not in standard rates)
    if (this.config.regime !== 'NONE' && this.config.employeeRate > 0) {
      const rates = this.config.regime === 'AL_KAMIL' ? CIMR_AL_KAMIL_RATES : CIMR_AL_MOUNASSIB_RATES;
      const isStandardRate = rates.some(r => Math.abs(r.employeeRate - this.config.employeeRate) < 0.0001);
      this.useCustomRate = !isStandardRate;
    }
  }

  get availableRates() {
    if (this.config.regime === 'AL_KAMIL') {
      return [...CIMR_AL_KAMIL_RATES];
    } else if (this.config.regime === 'AL_MOUNASSIB') {
      return [...CIMR_AL_MOUNASSIB_RATES];
    }
    return [];
  }

  get calculation(): CimrCalculation {
    if (this.config.regime === 'NONE' || this.baseSalary <= 0) {
      return { assiette: 0, employeeContribution: 0, employerContribution: 0, totalCost: 0 };
    }

    let assiette = this.baseSalary;

    // For Al Mounassib, assiette is only the part exceeding CNSS ceiling
    if (this.config.regime === 'AL_MOUNASSIB') {
      assiette = Math.max(0, this.baseSalary - CNSS_CEILING);
    }

    const employeeContribution = assiette * this.config.employeeRate;
    const employerContribution = assiette * this.config.employerRate;
    const totalCost = employerContribution; // Only employer contribution is company cost

    return {
      assiette,
      employeeContribution,
      employerContribution,
      totalCost
    };
  }

  setRegime(regime: CimrRegime): void {
    if (this.readonly) return;

    if (regime === 'NONE') {
      this.config = { regime: 'NONE', employeeRate: 0, employerRate: 0 };
      this.useCustomRate = false;
    } else {
      // Set default rate for the regime
      const rates = regime === 'AL_KAMIL' ? CIMR_AL_KAMIL_RATES : CIMR_AL_MOUNASSIB_RATES;
      const defaultRate = rates[0];
      this.config = {
        regime,
        employeeRate: defaultRate.employeeRate,
        employerRate: defaultRate.employerRate
      };
      this.useCustomRate = false;
    }
    this.configChange.emit(this.config);
  }

  setEmployeeRate(rate: number): void {
    if (this.readonly || this.config.regime === 'NONE') return;

    const rates = this.config.regime === 'AL_KAMIL' ? CIMR_AL_KAMIL_RATES : CIMR_AL_MOUNASSIB_RATES;
    const rateConfig = rates.find(r => r.employeeRate === rate);

    this.config = {
      ...this.config,
      employeeRate: rate,
      employerRate: rateConfig?.employerRate ?? calculateCimrEmployerRate(rate)
    };
    this.useCustomRate = false;
    this.configChange.emit(this.config);
  }

  setCustomEmployeeRate(ratePercent: number): void {
    if (this.readonly || this.config.regime === 'NONE') return;

    const rate = (ratePercent || 0) / 100;
    this.config = {
      ...this.config,
      employeeRate: rate,
      employerRate: calculateCimrEmployerRate(rate)
    };
    this.configChange.emit(this.config);
  }

  onCustomRateToggle(enabled: boolean): void {
    if (!enabled && this.config.regime !== 'NONE') {
      // Reset to first standard rate
      const rates = this.config.regime === 'AL_KAMIL' ? CIMR_AL_KAMIL_RATES : CIMR_AL_MOUNASSIB_RATES;
      const defaultRate = rates[0];
      this.config = {
        ...this.config,
        employeeRate: defaultRate.employeeRate,
        employerRate: defaultRate.employerRate
      };
      this.configChange.emit(this.config);
    }
  }

  isSelectedRate(rate: number): boolean {
    return !this.useCustomRate && Math.abs(this.config.employeeRate - rate) < 0.0001;
  }

  formatPercent(value: number): string {
    return (value * 100).toFixed(2).replace(/\.?0+$/, '') + '%';
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('fr-MA', { minimumFractionDigits: 0, maximumFractionDigits: 2 }).format(value);
  }
}
