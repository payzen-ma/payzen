/**
 * Element Rule Models (Exemption Rules)
 * Complex nested structures for different exemption types
 */

import { ExemptionType, CapUnit, BaseReference, DualCapLogic } from './lookup.models';
import { ElementStatus } from './referentiel-element.model';

/**
 * Rule Cap (fixed or capped exemption)
 */
export interface RuleCapDto {
  id: number;
  capAmount: number;  // decimal
  capUnit: CapUnit;
}

/**
 * Rule Percentage (percentage-based exemption)
 */
export interface RulePercentageDto {
  id: number;
  percentage: number;           // decimal (e.g., 0.50 for 50%)
  baseReference: BaseReference;
  eligibilityId?: number;
  eligibilityName?: string;
}

/**
 * Rule Formula (formula-based like 2 × SMIG)
 */
export interface RuleFormulaDto {
  id: number;
  multiplier: number;          // decimal
  parameterId: number;
  parameterName: string;
  resultUnit: CapUnit;
  currentCapValue: number;     // Calculated: multiplier × parameter value
}

/**
 * Rule Tier (tiered exemption rates)
 */
export interface RuleTierDto {
  id: number;
  tierOrder: number;
  minAmount?: number;
  maxAmount?: number;
  exemptionRate: number;  // decimal
}

/**
 * Rule Variant (zone/grade-specific overrides)
 */
export interface RuleVariantDto {
  id: number;
  variantKey: string;           // e.g., "URBAN", "HORS_URBAN"
  variantLabel: string;         // e.g., "Urban Zone", "Rural Zone"
  overrideCap?: number;
  overrideEligibilityId?: number;
  overrideEligibilityName?: string;
}

/**
 * Rule Dual Cap (fixed cap AND percentage cap, e.g., DGI ticket-restaurant)
 */
export interface RuleDualCapDto {
  id: number;
  fixedCapAmount: number;
  fixedCapUnit: CapUnit;
  percentageCap: number;        // decimal (e.g., 0.20 for 20%)
  baseReference: BaseReference;
  logic: DualCapLogic;
}

/**
 * Element Rule (complete exemption rule for an authority)
 */
export interface ElementRuleDto {
  id: number;
  elementId: number;
  authorityId: number;
  authorityName: string;
  exemptionType: ExemptionType;
  sourceRef?: string;  // Legal reference (e.g., "Circulaire CNSS 2020")
  effectiveFrom: string;
  effectiveTo?: string;
  isActive: boolean;
  status: ElementStatus;
  ruleDetails: string;  // JSON string with rule configuration

  // Legacy: Only one will be populated based on exemptionType
  cap?: RuleCapDto;
  percentage?: RulePercentageDto;
  formula?: RuleFormulaDto;
  dualCap?: RuleDualCapDto;
  tiers: RuleTierDto[];
  variants: RuleVariantDto[];
}

/**
 * Create Element Rule DTO - Request for POST
 */
export interface CreateElementRuleDto {
  elementId: number;
  authorityId: number;
  authorityCode?: string;
  exemptionType: ExemptionType;
  sourceRef?: string;
  effectiveFrom: string;
  effectiveTo?: string;
  status?: ElementStatus;
  ruleDetails?: string;  // JSON string with rule configuration

  // Legacy: Provide only one based on exemptionType
  cap?: {
    capAmount: number;
    capUnit: CapUnit;
    minAmount?: number;
  };
  percentage?: {
    percentage: number;
    baseReference: BaseReference;
    eligibilityId?: number;
  };
  formula?: {
    multiplier: number;
    parameterId: number;
    resultUnit: CapUnit;
  };
  dualCap?: {
    fixedCapAmount: number;
    fixedCapUnit: CapUnit;
    percentageCap: number;
    baseReference: BaseReference;
    logic: DualCapLogic;
  };
  tiers?: {
    tierOrder: number;
    minAmount?: number;
    maxAmount?: number;
    exemptionRate: number;
  }[];
  variants?: {
    variantKey: string;
    variantLabel: string;
    overrideCap?: number;
    overrideEligibilityId?: number;
  }[];
}

/**
 * Update Element Rule DTO - Request for PUT
 */
export interface UpdateElementRuleDto {
  authorityId: number;
  exemptionType: ExemptionType;
  sourceRef?: string;
  effectiveFrom: string;
  effectiveTo?: string;
  isActive?: boolean;
  status?: ElementStatus;
  ruleDetails?: string;  // JSON string with rule configuration

  cap?: {
    capAmount: number;
    capUnit: CapUnit;
    minAmount?: number;
  };
  percentage?: {
    percentage: number;
    baseReference: BaseReference;
    eligibilityId?: number;
  };
  formula?: {
    multiplier: number;
    parameterId: number;
    resultUnit: CapUnit;
  };
  dualCap?: {
    fixedCapAmount: number;
    fixedCapUnit: CapUnit;
    percentageCap: number;
    baseReference: BaseReference;
    logic: DualCapLogic;
  };
  tiers?: {
    tierOrder: number;
    minAmount?: number;
    maxAmount?: number;
    exemptionRate: number;
  }[];
  variants?: {
    variantKey: string;
    variantLabel: string;
    overrideCap?: number;
    overrideEligibilityId?: number;
  }[];
}

// ============================================================
// Helper Types for Rule Creation (Phase 4)
// ============================================================

export type CreateRuleCapDto = {
  capAmount: number;
  capUnit: CapUnit;
  minAmount?: number;
};

export type CreateRulePercentageDto = {
  percentage: number;
  baseReference: BaseReference;
  eligibilityId?: number;
};

export type CreateRuleFormulaDto = {
  multiplier: number;
  parameterId: number;
  resultUnit: CapUnit;
};

export type CreateRuleTierDto = {
  tierOrder: number;
  minAmount?: number;
  maxAmount?: number;
  exemptionRate: number;
};

export type CreateRuleVariantDto = {
  variantKey: string;
  variantLabel: string;
  overrideCap?: number;
  overrideEligibilityId?: number;
};

export type CreateRuleDualCapDto = {
  fixedCapAmount: number;
  fixedCapUnit: CapUnit;
  percentageCap: number;
  baseReference: BaseReference;
  logic: DualCapLogic;
};

// ============================================================
// Helper Functions
// ============================================================

/**
 * Get human-readable label for ExemptionType
 */
export function getExemptionTypeLabel(type: ExemptionType): string {
  const labels: Record<ExemptionType, string> = {
    [ExemptionType.FULLY_EXEMPT]: 'Totalement exonéré',
    [ExemptionType.FULLY_SUBJECT]: 'Totalement soumis',
    [ExemptionType.CAPPED]: 'Exonéré avec plafond',
    [ExemptionType.PERCENTAGE]: 'Pourcentage du salaire',
    [ExemptionType.PERCENTAGE_CAPPED]: 'Pourcentage avec plafond',
    [ExemptionType.FORMULA]: 'Formule (ex: 2 × SMIG)',
    [ExemptionType.FORMULA_CAPPED]: 'Formule avec plafond',
    [ExemptionType.TIERED]: 'Barème à tranches',
    [ExemptionType.DUAL_CAP]: 'Double plafond (fixe + %)'
  };
  return labels[type] || type;
}

/**
 * Get human-readable label for CapUnit
 */
export function getCapUnitLabel(unit: CapUnit): string {
  const labels: Record<CapUnit, string> = {
    [CapUnit.PER_DAY]: 'par jour',
    [CapUnit.PER_MONTH]: 'par mois',
    [CapUnit.PER_YEAR]: 'par an'
  };
  return labels[unit] || unit;
}

/**
 * Get human-readable label for BaseReference
 */
export function getBaseReferenceLabel(base: BaseReference): string {
  const labels: Record<BaseReference, string> = {
    [BaseReference.BASE_SALARY]: 'Salaire de base',
    [BaseReference.GROSS_SALARY]: 'Salaire brut',
    [BaseReference.SBI]: 'Salaire Brut Imposable (SBI)'
  };
  return labels[base] || base;
}

/**
 * Get rule summary for display
 */
export function getRuleSummary(rule: ElementRuleDto): string {
  switch (rule.exemptionType) {
    case ExemptionType.FULLY_EXEMPT:
      return '100% exonéré';
    case ExemptionType.FULLY_SUBJECT:
      return '100% soumis';
    case ExemptionType.CAPPED:
      return rule.cap ? `Plafonné à ${rule.cap.capAmount} MAD ${getCapUnitLabel(rule.cap.capUnit)}` : 'Plafonné';
    case ExemptionType.PERCENTAGE:
      return rule.percentage ? `${(rule.percentage.percentage * 100).toFixed(0)}% du ${getBaseReferenceLabel(rule.percentage.baseReference)}` : 'Pourcentage';
    case ExemptionType.PERCENTAGE_CAPPED:
      return rule.percentage && rule.cap
        ? `${(rule.percentage.percentage * 100).toFixed(0)}% plafonné à ${rule.cap.capAmount} MAD`
        : 'Pourcentage avec plafond';
    case ExemptionType.FORMULA:
      return rule.formula ? `${rule.formula.multiplier} × ${rule.formula.parameterName}` : 'Formule';
    case ExemptionType.FORMULA_CAPPED:
      return rule.formula && rule.cap
        ? `${rule.formula.multiplier} × ${rule.formula.parameterName} (plafonné à ${rule.cap.capAmount} MAD)`
        : 'Formule avec plafond';
    case ExemptionType.TIERED:
      return `Barème à ${rule.tiers.length} tranches`;
    case ExemptionType.DUAL_CAP:
      if (rule.dualCap) {
        const pct = (rule.dualCap.percentageCap * 100).toFixed(0);
        return `${rule.dualCap.fixedCapAmount} MAD ${getCapUnitLabel(rule.dualCap.fixedCapUnit)} ET ${pct}% du ${getBaseReferenceLabel(rule.dualCap.baseReference)}`;
      }
      return 'Double plafond';
    default:
      return getExemptionTypeLabel(rule.exemptionType);
  }
}

/**
 * Format rule cap for display
 */
export function formatRuleCap(cap: RuleCapDto): string {
  return `${cap.capAmount} MAD ${getCapUnitLabel(cap.capUnit)}`;
}

/**
 * Format rule percentage for display
 */
export function formatRulePercentage(percentage: RulePercentageDto): string {
  const pct = (percentage.percentage * 100).toFixed(0);
  return `${pct}% du ${getBaseReferenceLabel(percentage.baseReference)}`;
}

/**
 * Format rule formula for display
 */
export function formatRuleFormula(formula: RuleFormulaDto): string {
  return `${formula.multiplier} × ${formula.parameterName} = ${formula.currentCapValue} MAD ${getCapUnitLabel(formula.resultUnit)}`;
}

/**
 * Format rule tier for display
 */
export function formatRuleTier(tier: RuleTierDto): string {
  const min = tier.minAmount ? `${tier.minAmount} MAD` : '0 MAD';
  const max = tier.maxAmount ? `${tier.maxAmount} MAD` : '∞';
  const rate = (tier.exemptionRate * 100).toFixed(0);
  return `${min} - ${max}: ${rate}% exonéré`;
}
