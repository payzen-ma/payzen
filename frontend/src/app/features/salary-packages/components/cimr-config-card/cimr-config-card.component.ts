import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import {
  CimrConfig,
  CimrRegime,
  CIMR_AL_KAMIL_RATES,
  CIMR_AL_MOUNASSIB_RATES,
  CNSS_CEILING,
  calculateCimrEmployerRate
} from '@app/core/models/salary-package.model';

export interface CimrCalculation {
  assiette: number;
  employeeContribution: number;
  employerContribution: number;
  totalCost: number;
}

@Component({
  selector: 'app-cimr-config-card',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  template: `
    <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6">
      <h3 class="font-semibold text-gray-900 mb-4">{{ 'salaryPackages.view.cimr.title' | translate }}</h3>

      <div class="space-y-4">
        @if (readonly) {
          @if (config.regime === 'NONE') {
            <div class="p-3 bg-gray-50 rounded-lg text-center">
              <p class="text-sm text-gray-500">{{ 'salaryPackages.cimr.noContribution' | translate }}</p>
            </div>
          } @else {
            <div class="grid gap-2 text-sm">
              <div class="flex justify-between items-center py-1.5 border-b border-gray-100">
                <span class="text-gray-500">{{ 'salaryPackages.cimr.regime' | translate }}</span>
                <span class="font-medium text-gray-900">
                  {{ config.regime === 'AL_KAMIL' ? 'Al Kamil' : ('salaryPackages.cimr.alMounassibPme' | translate) }}
                </span>
              </div>
              <div class="flex justify-between items-center py-1.5 border-b border-gray-100">
                <span class="text-gray-500">{{ 'salaryPackages.cimr.assiette' | translate }}</span>
                <span class="font-medium text-gray-900">
                  {{ config.regime === 'AL_KAMIL' ? ('salaryPackages.cimr.assietteGross' | translate) : ('salaryPackages.cimr.assietteExceeding' | translate:{ amount: formatCurrency(cnssCeiling) }) }}
                </span>
              </div>
              <div class="flex justify-between items-center py-1.5 border-b border-gray-100">
                <span class="text-gray-500">{{ 'salaryPackages.cimr.employeePart' | translate }}</span>
                <span class="font-medium text-gray-900">{{ formatPercent(config.employeeRate) }}</span>
              </div>
              <div class="flex justify-between items-center py-1.5">
                <span class="text-gray-500">{{ 'salaryPackages.cimr.employerPartCalculated' | translate }}</span>
                <span class="font-semibold text-primary">{{ formatPercent(config.employerRate) }}</span>
              </div>
            </div>

            @if (baseSalary > 0) {
              <div class="p-3 bg-gray-50 rounded-lg space-y-2">
                <div class="text-xs text-gray-500">{{ 'salaryPackages.cimr.previewBase' | translate:{ amount: formatCurrency(baseSalary) } }}</div>
                @if (config.regime === 'AL_MOUNASSIB') {
                  <div class="flex justify-between items-center text-sm">
                    <span class="text-gray-600">{{ 'salaryPackages.cimr.assietteCimr' | translate }}</span>
                    <span class="font-medium">{{ formatCurrency(calculation.assiette) }} MAD</span>
                  </div>
                }
                <div class="flex justify-between items-center text-sm">
                  <span class="text-gray-600">{{ 'salaryPackages.cimr.employeeContribution' | translate }}</span>
                  <span class="font-medium">{{ formatCurrency(calculation.employeeContribution) }} MAD</span>
                </div>
                <div class="flex justify-between items-center text-sm">
                  <span class="text-gray-600">{{ 'salaryPackages.cimr.employerContribution' | translate }}</span>
                  <span class="font-medium text-primary">{{ formatCurrency(calculation.employerContribution) }} MAD</span>
                </div>
              </div>
            }
          }
        } @else {
          <!-- Regime Selection -->
          <div>
            <label class="block text-sm font-medium text-gray-700 mb-2">{{ 'salaryPackages.cimr.regime' | translate }}</label>
            <select
              [ngModel]="config.regime"
              (ngModelChange)="setRegime($event)"
              [disabled]="readonly"
              class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary/30 focus:border-primary transition-colors bg-white disabled:bg-gray-50 disabled:cursor-not-allowed"
            >
              <option value="NONE">{{ 'salaryPackages.cimr.none' | translate }}</option>
              <option value="AL_KAMIL">Al Kamil</option>
              <option value="AL_MOUNASSIB">{{ 'salaryPackages.cimr.alMounassibPme' | translate }}</option>
            </select>
            @if (config.regime !== 'NONE') {
              <p class="mt-1.5 text-xs text-gray-500">
                @if (config.regime === 'AL_KAMIL') {
                  {{ 'salaryPackages.cimr.assietteLabel' | translate }}: {{ 'salaryPackages.cimr.assietteGross' | translate }}
                } @else {
                  {{ 'salaryPackages.cimr.assietteLabel' | translate }}: {{ 'salaryPackages.cimr.assietteExceeding' | translate:{ amount: formatCurrency(cnssCeiling) } }}
                }
              </p>
            }
          </div>

          @if (config.regime !== 'NONE') {
            <!-- Employee Rate Selection -->
            <div>
              <label class="block text-sm font-medium text-gray-700 mb-2">{{ 'salaryPackages.cimr.employeeRate' | translate }}</label>
              @if (!useCustomRate) {
                <select
                  [ngModel]="config.employeeRate"
                  (ngModelChange)="setEmployeeRate($event)"
                  [disabled]="readonly"
                  class="w-full px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary/30 focus:border-primary transition-colors bg-white disabled:bg-gray-50 disabled:cursor-not-allowed"
                >
                  @for (rate of availableRates; track rate.employeeRate) {
                    <option [ngValue]="rate.employeeRate">{{ rate.label }}</option>
                  }
                </select>
              } @else {
                <div class="flex items-center gap-2">
                  <input
                    type="number"
                    min="0"
                    max="20"
                    step="0.25"
                    [ngModel]="config.employeeRate * 100"
                    (ngModelChange)="setCustomEmployeeRate($event)"
                    [disabled]="readonly"
                    class="flex-1 px-3 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary/30 focus:border-primary transition-colors disabled:bg-gray-50 disabled:cursor-not-allowed"
                    [placeholder]="'salaryPackages.cimr.customRate' | translate"
                  />
                  <span class="text-sm text-gray-500">%</span>
                </div>
              }
            </div>

            <!-- Custom Rate Toggle -->
            <div class="flex items-center justify-between">
              <div>
                <label class="text-sm font-medium text-gray-700">{{ 'salaryPackages.cimr.customRate' | translate }}</label>
                <p class="text-xs text-gray-500">{{ 'salaryPackages.cimr.customRateHint' | translate }}</p>
              </div>
              <label class="relative inline-flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  [(ngModel)]="useCustomRate"
                  [disabled]="readonly"
                  (ngModelChange)="onCustomRateToggle($event)"
                  class="sr-only peer"
                />
                <div class="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary/30 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary disabled:opacity-50 disabled:cursor-not-allowed"></div>
              </label>
            </div>

            <!-- Rates Summary -->
            <div class="pt-3 border-t border-gray-100 space-y-2">
              <div class="flex justify-between items-center text-sm">
                <span class="text-gray-600">{{ 'salaryPackages.cimr.employeePart' | translate }}</span>
                <span class="font-medium">{{ formatPercent(config.employeeRate) }}</span>
              </div>
              <div class="flex justify-between items-center text-sm">
                <span class="text-gray-600">{{ 'salaryPackages.cimr.employerPart' | translate }} <span class="text-xs text-gray-400">(×1.3)</span></span>
                <span class="font-medium text-primary">{{ formatPercent(config.employerRate) }}</span>
              </div>
            </div>

            <!-- Calculation Preview -->
            @if (baseSalary > 0) {
              <div class="p-3 bg-gray-50 rounded-lg space-y-2">
                <div class="flex justify-between items-center text-xs text-gray-500 mb-2">
                  <span>{{ 'salaryPackages.cimr.previewBase' | translate:{ amount: formatCurrency(baseSalary) } }}</span>
                </div>
                @if (config.regime === 'AL_MOUNASSIB') {
                  <div class="flex justify-between items-center text-sm">
                    <span class="text-gray-600">{{ 'salaryPackages.cimr.assietteCimr' | translate }}</span>
                    <span class="font-medium">{{ formatCurrency(calculation.assiette) }} MAD</span>
                  </div>
                }
                <div class="flex justify-between items-center text-sm">
                  <span class="text-gray-600">{{ 'salaryPackages.cimr.employeeContribution' | translate }}</span>
                  <span class="font-medium">{{ formatCurrency(calculation.employeeContribution) }} MAD</span>
                </div>
                <div class="flex justify-between items-center text-sm">
                  <span class="text-gray-600">{{ 'salaryPackages.cimr.employerContribution' | translate }}</span>
                  <span class="font-medium text-primary">{{ formatCurrency(calculation.employerContribution) }} MAD</span>
                </div>
              </div>
            }
          } @else {
            <!-- Non-affiliated info -->
            <div class="p-3 bg-gray-50 rounded-lg text-center">
              <p class="text-sm text-gray-500">{{ 'salaryPackages.cimr.noContribution' | translate }}</p>
            </div>
          }
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
    const totalCost = employerContribution;

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

  formatPercent(value: number): string {
    return (value * 100).toFixed(2).replace(/\.?0+$/, '') + '%';
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('fr-MA', { minimumFractionDigits: 0, maximumFractionDigits: 0 }).format(value);
  }
}
