import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ModalComponent } from '../../../../shared/modal/modal.component';
import { LookupCacheService } from '../../../../services/payroll-referentiel/lookup-cache.service';
import { PayrollReferentielService } from '../../../../services/payroll-referentiel/payroll-referentiel.service';
import {
  ReferentielElementListDto,
  ElementRuleDto,
  CreateElementRuleDto,
  UpdateElementRuleDto,
  CreateRuleCapDto,
  CreateRulePercentageDto,
  CreateRuleFormulaDto,
  CreateRuleTierDto,
  CreateRuleVariantDto,
  CreateRuleDualCapDto,
  AuthorityDto,
  EligibilityCriteriaDto,
  LegalParameterDto,
  ExemptionType,
  CapUnit,
  BaseReference,
  DualCapLogic,
  getExemptionTypeLabel,
  getCapUnitLabel,
  getBaseReferenceLabel
} from '../../../../models/payroll-referentiel';

interface TierForm {
  tierOrder: number;
  minAmount: number | null;
  maxAmount: number | null;
  exemptionRate: number;
}

interface VariantForm {
  variantKey: string;
  variantLabel: string;
  overrideCap: number | null;
}

/**
 * Element Rule Form Component
 * Single form for creating/editing exemption rules (no wizard steps).
 */
@Component({
  selector: 'app-element-rule-wizard',
  standalone: true,
  imports: [CommonModule, FormsModule, ModalComponent],
  template: `
    <app-modal [(visible)]="visible" [title]="modalTitle" size="lg" (visibleChange)="onVisibleChange($event)">
      <div class="max-h-[75vh] overflow-y-auto pr-1">
        <!-- Error Message -->
        <div *ngIf="error" class="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg">
          <p class="text-sm text-red-600">{{ error }}</p>
        </div>

        <!-- 1. Authority -->
        <div class="mb-4">
          <label class="block text-sm font-medium text-gray-700 mb-1">Autorité</label>
          <select [(ngModel)]="form.authorityId"
            class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 bg-white">
            <option [ngValue]="null">Choisir une autorité...</option>
            <option *ngFor="let auth of authorities" [ngValue]="auth.id">{{ auth.code || auth.name }}</option>
          </select>
        </div>

        <!-- 2. Type d'exonération -->
        <div class="mb-4">
          <label class="block text-sm font-medium text-gray-700 mb-1">Type d'exonération</label>
          <select [(ngModel)]="form.exemptionType"
            class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500 bg-white">
            <option [ngValue]="null">Choisir un type...</option>
            <option *ngFor="let type of exemptionTypes" [ngValue]="type.value">{{ type.label }}</option>
          </select>
        </div>

        <!-- 3. Details (conditional by type) -->
        <div class="mb-6">
          <h3 class="text-sm font-semibold text-gray-900 mb-2">Détails de la règle</h3>

          <!-- CAPPED - Hide when variants with caps are defined -->
          <div *ngIf="showCapFields() && !hasVariantsWithCaps()" class="p-4 bg-blue-50 rounded-lg mb-4">
            <h4 class="text-sm font-medium text-blue-800 mb-2">Plafond</h4>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-medium text-gray-700 mb-1">Montant plafond</label>
                <input type="number" [(ngModel)]="capForm.capAmount" min="0"
                  class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500">
              </div>
              <div>
                <label class="block text-xs font-medium text-gray-700 mb-1">Unité</label>
                <select [(ngModel)]="capForm.capUnit"
                  class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500">
                  <option *ngFor="let unit of capUnits" [value]="unit.value">{{ unit.label }}</option>
                </select>
              </div>
            </div>
          </div>

          <!-- PERCENTAGE -->
          <div *ngIf="showPercentageFields()" class="p-4 bg-green-50 rounded-lg mb-4">
            <h4 class="text-sm font-medium text-green-800 mb-2">Pourcentage</h4>
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-medium text-gray-700 mb-1">Pourcentage (%)</label>
                <input type="number" [(ngModel)]="percentageForm.percentage" min="0" max="100" step="0.5"
                  class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500">
              </div>
              <div>
                <label class="block text-xs font-medium text-gray-700 mb-1">Base de calcul</label>
                <select [(ngModel)]="percentageForm.baseReference"
                  class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500">
                  <option *ngFor="let base of baseReferences" [value]="base.value">{{ base.label }}</option>
                </select>
              </div>
            </div>
            <div class="mt-3">
              <label class="block text-xs font-medium text-gray-700 mb-1">Critère d'éligibilité</label>
              <select [(ngModel)]="percentageForm.eligibilityId"
                class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500">
                <option [ngValue]="null">Aucun critère</option>
                <option *ngFor="let crit of eligibilityCriteria" [ngValue]="crit.id">{{ crit.name }}</option>
              </select>
            </div>
          </div>

          <!-- FORMULA -->
          <div *ngIf="showFormulaFields()" class="p-4 bg-purple-50 rounded-lg mb-4">
            <h4 class="text-sm font-medium text-purple-800 mb-2">Formule</h4>
            <div class="grid grid-cols-3 gap-4">
              <div>
                <label class="block text-xs font-medium text-gray-700 mb-1">Multiplicateur</label>
                <input type="number" [(ngModel)]="formulaForm.multiplier" min="0" step="0.5"
                  class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500">
              </div>
              <div>
                <label class="block text-xs font-medium text-gray-700 mb-1">Paramètre de référence</label>
                <select [(ngModel)]="formulaForm.parameterId"
                  class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500">
                  <option [ngValue]="null">Sélectionner...</option>
                  <option *ngFor="let param of legalParameters" [ngValue]="param.id">{{ param.name }}</option>
                </select>
              </div>
              <div>
                <label class="block text-xs font-medium text-gray-700 mb-1">Unité résultat</label>
                <select [(ngModel)]="formulaForm.resultUnit"
                  class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500">
                  <option *ngFor="let unit of capUnits" [value]="unit.value">{{ unit.label }}</option>
                </select>
              </div>
            </div>
            <p class="mt-2 text-xs text-purple-700">
              {{ formulaForm.multiplier }} × {{ getSelectedParameterName() }} = Plafond {{ getCapUnitLabel(formulaForm.resultUnit) }}
            </p>
          </div>

          <!-- TIERED -->
          <div *ngIf="showTieredFields()" class="p-4 bg-yellow-50 rounded-lg mb-4">
            <div class="flex items-center justify-between mb-3">
              <h4 class="text-sm font-medium text-yellow-800">Tranches d'exonération</h4>
              <button type="button" (click)="addTier()" class="text-xs text-yellow-700 hover:text-yellow-800 underline">+ Ajouter une tranche</button>
            </div>
            <div *ngFor="let tier of tiers; let i = index" class="flex items-center gap-2 mb-2 flex-wrap">
              <input type="number" [(ngModel)]="tier.minAmount" placeholder="Min MAD" class="w-24 px-2 py-1 text-sm border border-gray-300 rounded">
              <span class="text-gray-400">-</span>
              <input type="number" [(ngModel)]="tier.maxAmount" placeholder="Max MAD" class="w-24 px-2 py-1 text-sm border border-gray-300 rounded">
              <input type="number" [(ngModel)]="tier.exemptionRate" min="0" max="100" step="1" class="w-16 px-2 py-1 text-sm border border-gray-300 rounded">
              <span class="text-gray-500 text-sm">%</span>
              <button type="button" (click)="removeTier(i)" class="p-1 text-gray-400 hover:text-red-500">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
              </button>
            </div>
          </div>

          <!-- DUAL_CAP (for DGI ticket-restaurant: 20 DH/jour ET 20% SBI) -->
          <div *ngIf="showDualCapFields()" class="p-4 bg-orange-50 rounded-lg mb-4">
            <h4 class="text-sm font-medium text-orange-800 mb-2">Double plafond (fixe ET pourcentage)</h4>
            <p class="text-xs text-orange-600 mb-3">L'exonération est limitée par les deux conditions simultanément (ex: ticket-restaurant DGI).</p>
            <div class="grid grid-cols-2 gap-4 mb-3">
              <div>
                <label class="block text-xs font-medium text-gray-700 mb-1">Plafond fixe</label>
                <div class="flex gap-2">
                  <input type="number" [(ngModel)]="dualCapForm.fixedCapAmount" min="0"
                    class="flex-1 px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500">
                  <select [(ngModel)]="dualCapForm.fixedCapUnit"
                    class="w-28 px-2 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500">
                    <option *ngFor="let unit of capUnits" [value]="unit.value">{{ unit.label }}</option>
                  </select>
                </div>
              </div>
              <div>
                <label class="block text-xs font-medium text-gray-700 mb-1">Plafond en %</label>
                <div class="flex gap-2 items-center">
                  <input type="number" [(ngModel)]="dualCapForm.percentageCap" min="0" max="100" step="0.5"
                    class="w-20 px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500">
                  <span class="text-gray-500">% du</span>
                  <select [(ngModel)]="dualCapForm.baseReference"
                    class="flex-1 px-2 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500">
                    <option *ngFor="let base of baseReferences" [value]="base.value">{{ base.label }}</option>
                  </select>
                </div>
              </div>
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-700 mb-1">Logique de combinaison</label>
              <select [(ngModel)]="dualCapForm.logic"
                class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500">
                <option *ngFor="let logic of dualCapLogics" [value]="logic.value">{{ logic.label }}</option>
              </select>
              <p class="text-xs text-gray-500 mt-1">MIN = le plafond le plus bas s'applique (plus restrictif)</p>
            </div>
          </div>

          <!-- VARIANTS (for CAPPED rules with zone-specific caps) -->
          <div *ngIf="showVariantsSection()" class="p-4 bg-indigo-50 rounded-lg mb-4">
            <div class="flex items-center justify-between mb-3">
              <div>
                <h4 class="text-sm font-medium text-indigo-800">Plafonds par variante</h4>
                <p class="text-xs text-indigo-600">
                  {{ hasVariantsWithCaps() ? 'Les variantes définissent les plafonds (pas de plafond de base nécessaire)' : 'Optionnel : définir des plafonds différents par zone ou catégorie' }}
                </p>
              </div>
              <button type="button" (click)="addVariant()" class="text-xs text-indigo-700 hover:text-indigo-800 underline">+ Ajouter une variante</button>
            </div>
            <!-- Preset buttons -->
            <div class="flex gap-2 mb-3">
              <button *ngFor="let preset of variantPresets" type="button"
                (click)="addVariantPreset(preset)"
                class="px-2 py-1 text-xs bg-indigo-100 text-indigo-700 rounded hover:bg-indigo-200 transition-colors">
                + {{ preset.label }}
              </button>
            </div>
            <!-- Variant list -->
            <div *ngFor="let variant of variants; let i = index" class="flex items-center gap-2 mb-2 p-2 bg-white rounded border border-indigo-200">
              <input type="text" [(ngModel)]="variant.variantKey" placeholder="Clé (ex: URBAN)"
                class="w-28 px-2 py-1 text-sm border border-gray-300 rounded">
              <input type="text" [(ngModel)]="variant.variantLabel" placeholder="Libellé"
                class="flex-1 px-2 py-1 text-sm border border-gray-300 rounded">
              <input type="number" [(ngModel)]="variant.overrideCap" placeholder="Plafond MAD"
                class="w-28 px-2 py-1 text-sm border border-gray-300 rounded">
              <button type="button" (click)="removeVariant(i)" class="p-1 text-gray-400 hover:text-red-500">
                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12"></path></svg>
              </button>
            </div>
            <p *ngIf="variants.length === 0" class="text-xs text-gray-500 italic">Aucune variante définie. Le plafond principal s'applique à tous.</p>
          </div>

          <!-- Reference (optional) -->
          <div>
            <label class="block text-xs font-medium text-gray-700 mb-1">Référence légale (optionnel)</label>
            <input type="text" [(ngModel)]="form.sourceRef" placeholder="ex: Article 57 du CGI"
              class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500">
          </div>
        </div>

        <!-- 4. Period -->
        <div class="mb-6">
          <h3 class="text-sm font-semibold text-gray-900 mb-2">Période de validité</h3>
          <div class="grid grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-medium text-gray-700 mb-1">Date d'effet <span class="text-red-500">*</span></label>
              <input type="date" [(ngModel)]="form.effectiveFrom"
                class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500">
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-700 mb-1">Date de fin</label>
              <input type="date" [(ngModel)]="form.effectiveTo"
                class="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500">
              <p class="text-xs text-gray-400 mt-1">Vide = toujours en vigueur</p>
            </div>
          </div>
        </div>
      </div>

      <!-- Footer: Cancel + Save -->
      <div class="flex justify-end gap-3 mt-6 pt-4 border-t">
        <button type="button" (click)="onCancel()"
          class="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors">
          Annuler
        </button>
        <button type="button" (click)="onSubmit()"
          [disabled]="!isFormValid()"
          class="px-4 py-2 text-sm font-medium text-white bg-primary-500 rounded-lg hover:bg-primary-600 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors">
          {{ mode === 'create' ? 'Créer la règle' : 'Enregistrer' }}
        </button>
      </div>
    </app-modal>
  `
})
export class ElementRuleWizardComponent implements OnInit, OnChanges {
  @Input() visible = false;
  @Input() mode: 'create' | 'edit' = 'create';
  @Input() element: ReferentielElementListDto | null = null;
  @Input() rule: ElementRuleDto | null = null;
  
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() save = new EventEmitter<CreateElementRuleDto | { id: number; elementId: number; dto: UpdateElementRuleDto }>();

  // Lookup data
  authorities: AuthorityDto[] = [];
  eligibilityCriteria: EligibilityCriteriaDto[] = [];
  legalParameters: LegalParameterDto[] = [];

  // Form data
  form = {
    authorityId: null as number | null,
    exemptionType: null as ExemptionType | null,
    sourceRef: '',
    effectiveFrom: '',
    effectiveTo: ''
  };

  capForm: CreateRuleCapDto = { capAmount: 0, capUnit: CapUnit.PER_MONTH };
  percentageForm: CreateRulePercentageDto = { percentage: 0, baseReference: BaseReference.BASE_SALARY };
  formulaForm: CreateRuleFormulaDto = { multiplier: 1, parameterId: 0, resultUnit: CapUnit.PER_MONTH };
  dualCapForm: CreateRuleDualCapDto = {
    fixedCapAmount: 0,
    fixedCapUnit: CapUnit.PER_DAY,
    percentageCap: 0,
    baseReference: BaseReference.SBI,
    logic: DualCapLogic.MIN
  };
  tiers: TierForm[] = [];
  variants: VariantForm[] = [];

  error = '';

  exemptionTypes = [
    { value: ExemptionType.FULLY_EXEMPT, label: '100% Exonéré', description: 'Totalement exonéré, sans plafond' },
    { value: ExemptionType.FULLY_SUBJECT, label: '100% Soumis', description: 'Totalement assujetti' },
    { value: ExemptionType.CAPPED, label: 'Plafonné', description: 'Exonéré jusqu\'à un montant fixe' },
    { value: ExemptionType.PERCENTAGE, label: 'Pourcentage', description: 'Exonéré en % du salaire' },
    { value: ExemptionType.PERCENTAGE_CAPPED, label: '% Plafonné', description: 'Pourcentage avec plafond max' },
    { value: ExemptionType.FORMULA, label: 'Formule', description: 'Calcul dynamique (ex: 2×SMIG)' },
    { value: ExemptionType.FORMULA_CAPPED, label: 'Formule Plafonnée', description: 'Formule avec plafond max' },
    { value: ExemptionType.TIERED, label: 'Par tranches', description: 'Taux différents par tranche' },
    { value: ExemptionType.DUAL_CAP, label: 'Double plafond', description: 'Plafond fixe ET % (ex: ticket-restaurant DGI)' }
  ];

  dualCapLogics = [
    { value: DualCapLogic.MIN, label: 'Le plus restrictif (MIN)' },
    { value: DualCapLogic.MAX, label: 'Le plus favorable (MAX)' }
  ];

  // Common variant presets for quick selection
  variantPresets = [
    { type: 'ZONE', key: 'URBAN', label: 'Zone Urbaine' },
    { type: 'ZONE', key: 'HORS_URBAN', label: 'Zone Hors Urbaine' }
  ];

  capUnits = [
    { value: CapUnit.PER_DAY, label: 'par jour' },
    { value: CapUnit.PER_MONTH, label: 'par mois' },
    { value: CapUnit.PER_YEAR, label: 'par an' }
  ];

  baseReferences = [
    { value: BaseReference.BASE_SALARY, label: 'Salaire de base' },
    { value: BaseReference.GROSS_SALARY, label: 'Salaire brut' },
    { value: BaseReference.SBI, label: 'Salaire Brut Imposable' }
  ];

  get modalTitle(): string {
    if (this.mode === 'edit') {
      return 'Modifier la règle';
    }
    return this.element ? `Nouvelle règle pour ${this.element.name}` : 'Nouvelle règle d\'exonération';
  }

  constructor(
    private lookupCache: LookupCacheService,
    private payrollService: PayrollReferentielService
  ) {}

  ngOnInit(): void {
    this.loadLookupData();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['visible'] && this.visible) {
      this.initWizard();
    }
  }

  private loadLookupData(): void {
    // Only show CNSS and IR authorities for element rules (not CIMR or AMO)
    this.lookupCache.getAuthorities().subscribe(a => {
      const filtered = a.filter(auth => this.isCnssOrIrAuthority(auth));
      this.authorities = filtered.length > 0 ? filtered : this.getFallbackAuthorities();
    });
    this.lookupCache.getEligibilityCriteria().subscribe(e => this.eligibilityCriteria = e);
    this.payrollService.getAllLegalParameters().subscribe((p: LegalParameterDto[]) => this.legalParameters = p);
  }

  /**
   * Check if authority is CNSS or IR (element rules only apply to these two)
   */
  private isCnssOrIrAuthority(authority: AuthorityDto): boolean {
    const code = authority.code?.toLowerCase().trim();
    if (code === 'cnss' || code === 'ir' || code === 'dgi') {
      return true;
    }
    const n = authority.name?.toLowerCase().trim();
    if (!n) return false;
    // Match CNSS
    if (n === 'cnss' || n.includes('sécurité sociale') || n.includes('securite sociale')) {
      return true;
    }
    // Match IR/DGI
    if (n === 'ir' || n === 'dgi' || n.includes('impôt') || n.includes('impot') || n.includes('revenu')) {
      return true;
    }
    return false;
  }

  private getFallbackAuthorities(): AuthorityDto[] {
    return [
      { id: -1, code: 'CNSS', name: 'CNSS', isActive: true },
      { id: -2, code: 'IR', name: 'IR', isActive: true }
    ];
  }

  private initWizard(): void {
    this.error = '';

    if (this.mode === 'edit' && this.rule) {
      this.form = {
        authorityId: this.rule.authorityId,
        exemptionType: this.rule.exemptionType,
        sourceRef: this.rule.sourceRef || '',
        effectiveFrom: this.rule.effectiveFrom,
        effectiveTo: this.rule.effectiveTo || ''
      };
      
      if (this.rule.cap) {
        this.capForm = { ...this.rule.cap };
      }
      if (this.rule.percentage) {
        this.percentageForm = {
          percentage: this.rule.percentage.percentage * 100,
          baseReference: this.rule.percentage.baseReference,
          eligibilityId: this.rule.percentage.eligibilityId
        };
      }
      if (this.rule.formula) {
        this.formulaForm = {
          multiplier: this.rule.formula.multiplier,
          parameterId: this.rule.formula.parameterId,
          resultUnit: this.rule.formula.resultUnit
        };
      }
      if (this.rule.tiers?.length) {
        this.tiers = this.rule.tiers.map(t => ({
          tierOrder: t.tierOrder,
          minAmount: t.minAmount ?? null,
          maxAmount: t.maxAmount ?? null,
          exemptionRate: t.exemptionRate * 100
        }));
      }
      if (this.rule.dualCap) {
        this.dualCapForm = {
          fixedCapAmount: this.rule.dualCap.fixedCapAmount,
          fixedCapUnit: this.rule.dualCap.fixedCapUnit,
          percentageCap: this.rule.dualCap.percentageCap * 100,
          baseReference: this.rule.dualCap.baseReference,
          logic: this.rule.dualCap.logic
        };
      }
      if (this.rule.variants?.length) {
        this.variants = this.rule.variants.map(v => ({
          variantKey: v.variantKey,
          variantLabel: v.variantLabel,
          overrideCap: v.overrideCap ?? null
        }));
      }
    } else {
      this.form = {
        authorityId: null,
        exemptionType: null,
        sourceRef: '',
        effectiveFrom: new Date().toISOString().split('T')[0],
        effectiveTo: ''
      };
      this.capForm = { capAmount: 0, capUnit: CapUnit.PER_MONTH };
      this.percentageForm = { percentage: 0, baseReference: BaseReference.BASE_SALARY };
      this.formulaForm = { multiplier: 1, parameterId: 0, resultUnit: CapUnit.PER_MONTH };
      this.dualCapForm = {
        fixedCapAmount: 0,
        fixedCapUnit: CapUnit.PER_DAY,
        percentageCap: 0,
        baseReference: BaseReference.SBI,
        logic: DualCapLogic.MIN
      };
      this.tiers = [];
      this.variants = [];
    }
  }

  /** Single validation for the whole form (used for submit button and onSubmit). */
  isFormValid(): boolean {
    if (this.form.authorityId === null) return false;
    if (this.form.exemptionType === null) return false;
    if (!this.form.effectiveFrom) return false;
    return this.validateDetails();
  }

  private validateDetails(): boolean {
    // For CAPPED type: valid if base cap > 0 OR variants with caps exist
    if (this.form.exemptionType === ExemptionType.CAPPED) {
      return this.capForm.capAmount > 0 || this.hasVariantsWithCaps();
    }
    if (this.showCapFields()) {
      return this.capForm.capAmount > 0;
    }
    if (this.showPercentageFields()) {
      return this.percentageForm.percentage > 0;
    }
    if (this.showFormulaFields()) {
      return this.formulaForm.multiplier > 0 && !!this.formulaForm.parameterId;
    }
    if (this.showTieredFields()) {
      return this.tiers.length > 0;
    }
    if (this.showDualCapFields()) {
      return this.dualCapForm.fixedCapAmount > 0 && this.dualCapForm.percentageCap > 0;
    }
    return true; // FULLY_EXEMPT / FULLY_SUBJECT
  }

  // Field visibility
  showCapFields(): boolean {
    return [ExemptionType.CAPPED, ExemptionType.PERCENTAGE_CAPPED, ExemptionType.FORMULA_CAPPED].includes(this.form.exemptionType!);
  }

  showPercentageFields(): boolean {
    return [ExemptionType.PERCENTAGE, ExemptionType.PERCENTAGE_CAPPED].includes(this.form.exemptionType!);
  }

  showFormulaFields(): boolean {
    return [ExemptionType.FORMULA, ExemptionType.FORMULA_CAPPED].includes(this.form.exemptionType!);
  }

  showTieredFields(): boolean {
    return this.form.exemptionType === ExemptionType.TIERED;
  }

  showDualCapFields(): boolean {
    return this.form.exemptionType === ExemptionType.DUAL_CAP;
  }

  showVariantsSection(): boolean {
    // Variants can be added to CAPPED and FORMULA_CAPPED rules (for zone-specific caps)
    return [ExemptionType.CAPPED, ExemptionType.FORMULA_CAPPED].includes(this.form.exemptionType!);
  }

  /**
   * Check if variants with caps are defined (for zone-based rules like transport allowance).
   * When true, the base cap is not needed - variants ARE the caps.
   */
  hasVariantsWithCaps(): boolean {
    return this.variants.some(v => v.overrideCap !== null && v.overrideCap > 0);
  }

  // Tier management
  addTier(): void {
    this.tiers.push({
      tierOrder: this.tiers.length + 1,
      minAmount: null,
      maxAmount: null,
      exemptionRate: 50
    });
  }

  removeTier(index: number): void {
    this.tiers.splice(index, 1);
    this.tiers.forEach((t, i) => t.tierOrder = i + 1);
  }

  // Variant management
  addVariant(): void {
    this.variants.push({
      variantKey: '',
      variantLabel: '',
      overrideCap: null
    });
  }

  addVariantPreset(preset: { type: string; key: string; label: string }): void {
    if (!this.variants.find(v => v.variantKey === preset.key)) {
      this.variants.push({
        variantKey: preset.key,
        variantLabel: preset.label,
        overrideCap: null
      });
    }
  }

  removeVariant(index: number): void {
    this.variants.splice(index, 1);
  }

  // Helpers
  getSelectedAuthorityName(): string {
    const auth = this.authorities.find(a => a.id === this.form.authorityId);
    return auth?.code || auth?.name || '';
  }

  private getSelectedAuthority(): AuthorityDto | undefined {
    return this.authorities.find(a => a.id === this.form.authorityId);
  }

  getSelectedParameterName(): string {
    const param = this.legalParameters.find(p => p.id === this.formulaForm.parameterId);
    return param?.name || '?';
  }

  getExemptionTypeLabel(type: ExemptionType): string {
    return getExemptionTypeLabel(type);
  }

  getCapUnitLabel(unit: CapUnit): string {
    return getCapUnitLabel(unit);
  }

  getBaseReferenceLabel(base: BaseReference): string {
    return getBaseReferenceLabel(base);
  }

  setError(message: string): void {
    this.error = message;
  }

  onSubmit(): void {
    if (!this.element) return;
    if (!this.isFormValid()) {
      this.error = 'Veuillez remplir tous les champs obligatoires (autorité, type, date d\'effet et détails si requis).';
      return;
    }
    this.error = '';

    const selectedAuthority = this.getSelectedAuthority();
    const authorityCode = selectedAuthority?.code;

    const baseDto = {
      elementId: this.element.id,
      authorityId: this.form.authorityId!,
      authorityCode: authorityCode || undefined,
      exemptionType: this.form.exemptionType!,
      sourceRef: this.form.sourceRef || undefined,
      effectiveFrom: this.form.effectiveFrom,
      effectiveTo: this.form.effectiveTo || undefined
    };

    // Add type-specific data
    let cap: CreateRuleCapDto | undefined;
    let percentage: CreateRulePercentageDto | undefined;
    let formula: CreateRuleFormulaDto | undefined;
    let dualCap: CreateRuleDualCapDto | undefined;
    let tierDtos: CreateRuleTierDto[] | undefined;
    let variantDtos: CreateRuleVariantDto[] | undefined;

    // Always include cap for cap-based exemption types (variants override, don't replace)
    if (this.showCapFields()) {
      cap = { ...this.capForm };
    }
    if (this.showPercentageFields()) {
      percentage = {
        percentage: this.percentageForm.percentage / 100,
        baseReference: this.percentageForm.baseReference,
        eligibilityId: this.percentageForm.eligibilityId || undefined
      };
    }
    if (this.showFormulaFields()) {
      formula = { ...this.formulaForm };
    }
    if (this.showDualCapFields()) {
      dualCap = {
        fixedCapAmount: this.dualCapForm.fixedCapAmount,
        fixedCapUnit: this.dualCapForm.fixedCapUnit,
        percentageCap: this.dualCapForm.percentageCap / 100,
        baseReference: this.dualCapForm.baseReference,
        logic: this.dualCapForm.logic
      };
    }
    if (this.showTieredFields()) {
      tierDtos = this.tiers.map(t => ({
        tierOrder: t.tierOrder,
        minAmount: t.minAmount ?? undefined,
        maxAmount: t.maxAmount ?? undefined,
        exemptionRate: t.exemptionRate / 100
      }));
    }
    if (this.showVariantsSection() && this.variants.length > 0) {
      variantDtos = this.variants.map(v => ({
        variantKey: v.variantKey,
        variantLabel: v.variantLabel,
        overrideCap: v.overrideCap ?? undefined
      }));
    }

    if (this.mode === 'create') {
      const createDto: CreateElementRuleDto = {
        ...baseDto,
        cap,
        percentage,
        formula,
        dualCap,
        tiers: tierDtos,
        variants: variantDtos
      };
      this.save.emit(createDto);
    } else {
      const updateDto: UpdateElementRuleDto = {
        authorityId: this.form.authorityId!,
        exemptionType: this.form.exemptionType!,
        sourceRef: this.form.sourceRef || undefined,
        effectiveFrom: this.form.effectiveFrom,
        effectiveTo: this.form.effectiveTo || undefined,
        isActive: this.rule!.isActive,
        cap,
        percentage,
        formula,
        dualCap,
        tiers: tierDtos,
        variants: variantDtos
      };
      this.save.emit({ id: this.rule!.id, elementId: this.element.id, dto: updateDto });
    }
  }

  onCancel(): void {
    this.visibleChange.emit(false);
  }

  onVisibleChange(visible: boolean): void {
    this.visibleChange.emit(visible);
  }
}
