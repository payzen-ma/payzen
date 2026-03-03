/**
 * Payroll Referentiel Models
 * Models for compensation elements like Transport, Panier, Représentation, etc.
 */

// ============================================================
// Enums & Types
// ============================================================

export type PaymentFrequency = 'MONTHLY' | 'ONE_TIME' | 'ANNUAL';

export type ExemptionType =
  | 'FULLY_EXEMPT'
  | 'FULLY_SUBJECT'
  | 'CAPPED'
  | 'FORMULA'
  | 'FORMULA_CAPPED'
  | 'PERCENTAGE'
  | 'PERCENTAGE_CAPPED'
  | 'TIERED';

export type CapUnit = 'PER_DAY' | 'PER_MONTH' | 'PER_YEAR';

export type BaseReference = 'GROSS_SALARY' | 'BASE_SALARY' | 'FIXED_AMOUNT';

// ============================================================
// Rule Sub-Components
// ============================================================

export interface RuleCap {
  id: number;
  capAmount: number;
  capUnit: CapUnit;
  minAmount?: number;
}

export interface RulePercentage {
  id: number;
  percentage: number;
  baseReference: BaseReference;
  eligibilityId?: number;
  eligibilityName?: string;
}

export interface RuleFormula {
  id: number;
  multiplier: number;
  parameterId: number;
  parameterName: string;
  resultUnit: CapUnit;
  currentCapValue: number;
}

export interface RuleTier {
  id: number;
  tierOrder: number;
  minAmount?: number;
  maxAmount?: number;
  exemptionRate: number;
}

export interface RuleVariant {
  id: number;
  variantKey: string;
  variantLabel: string;
  overrideCap?: number;
  overrideEligibilityId?: number;
  overrideEligibilityName?: string;
}

// ============================================================
// Element Rule
// ============================================================

export interface ElementRuleDto {
  id: number;
  elementId: number;
  authorityId: number;
  authorityName: string;
  exemptionType: ExemptionType;
  sourceRef?: string;
  effectiveFrom: string;
  effectiveTo?: string;
  isActive: boolean;
  cap?: RuleCap;
  percentage?: RulePercentage;
  formula?: RuleFormula;
  tiers: RuleTier[];
  variants: RuleVariant[];
}

// ============================================================
// Referentiel Element DTOs
// ============================================================

/**
 * Referentiel Element (Summary for lists)
 */
export interface ReferentielElementListDto {
  id: number;
  name: string;
  categoryName: string;
  defaultFrequency: PaymentFrequency;
  isActive: boolean;
  isConvergence: boolean;
  ruleCount: number;
}

/**
 * Referentiel Element (Full details with rules)
 */
export interface ReferentielElementDto {
  id: number;
  name: string;
  categoryId: number;
  categoryName: string;
  description?: string;
  defaultFrequency: PaymentFrequency;
  isActive: boolean;
  isConvergence: boolean;
  rules: ElementRuleDto[];
}

// ============================================================
// Helper Functions
// ============================================================

/**
 * Get convergence status text
 */
export function getConvergenceStatusText(isConvergence: boolean): string {
  return isConvergence ? 'Conv.' : 'Div.';
}

/**
 * Get convergence status CSS classes
 */
export function getConvergenceStatusClass(isConvergence: boolean): { bg: string; text: string } {
  return isConvergence
    ? { bg: 'bg-green-100', text: 'text-green-700' }
    : { bg: 'bg-amber-100', text: 'text-amber-700' };
}

/**
 * Get human-readable label for cap unit
 */
export function getCapUnitLabel(unit: CapUnit): string {
  const labels: Record<CapUnit, string> = {
    PER_DAY: 'par jour',
    PER_MONTH: 'par mois',
    PER_YEAR: 'par an'
  };
  return labels[unit] || unit;
}
