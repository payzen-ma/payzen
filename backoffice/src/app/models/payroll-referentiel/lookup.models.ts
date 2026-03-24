/**
 * Lookup Models and Enums for Payroll Referentiel
 * Based on backend API documentation
 */

// ============================================================
// Enums
// ============================================================

/**
 * Payment frequency for compensation elements
 */
export enum PaymentFrequency {
  DAILY = 'DAILY',
  MONTHLY = 'MONTHLY',
  QUARTERLY = 'QUARTERLY',
  ANNUAL = 'ANNUAL',
  ONE_TIME = 'ONE_TIME'
}

/**
 * Type of exemption calculation
 */
export enum ExemptionType {
  FULLY_EXEMPT = 'FULLY_EXEMPT',           // 100% exempt, no cap
  FULLY_SUBJECT = 'FULLY_SUBJECT',         // 0% exempt, fully taxable
  CAPPED = 'CAPPED',                       // Exempt up to a fixed cap
  PERCENTAGE = 'PERCENTAGE',               // Exempt as % of base salary
  PERCENTAGE_CAPPED = 'PERCENTAGE_CAPPED', // % of base with max cap
  FORMULA = 'FORMULA',                     // Dynamic calculation (e.g., 2 × SMIG)
  FORMULA_CAPPED = 'FORMULA_CAPPED',       // Formula with max cap
  TIERED = 'TIERED',                       // Multiple tiers with different rates
  DUAL_CAP = 'DUAL_CAP'                    // Fixed cap AND percentage cap (e.g., DGI ticket-restaurant)
}

/**
 * Logic for combining dual caps
 */
export enum DualCapLogic {
  MIN = 'MIN',   // Take the minimum of (fixed, percentage) - most restrictive
  MAX = 'MAX'    // Take the maximum of (fixed, percentage) - most favorable
}

/**
 * Unit for cap amounts
 */
export enum CapUnit {
  PER_DAY = 'PER_DAY',
  PER_MONTH = 'PER_MONTH',
  PER_YEAR = 'PER_YEAR'
}

/**
 * Base reference for percentage calculations
 */
export enum BaseReference {
  BASE_SALARY = 'BASE_SALARY',   // Salaire de base only
  GROSS_SALARY = 'GROSS_SALARY', // Salaire brut (includes primes)
  SBI = 'SBI'                    // Salaire Brut Imposable
}

// ============================================================
// Lookup DTOs (Read-only reference data)
// ============================================================

/**
 * Authority DTO (CNSS, IR, AMO, CIMR)
 */
export interface AuthorityDto {
  id: number;
  code?: string;
  name: string;
  description?: string;
  isActive: boolean;
}

/**
 * Element Category DTO (IND_PRO, IND_SOCIAL, PRIME_SPEC, AVANTAGE)
 */
export interface ElementCategoryDto {
  id: number;
  name: string;
  description?: string;
  isActive: boolean;
}

/**
 * Eligibility Criteria DTO (ALL, CADRES_SUP, PDG_DG, etc.)
 */
export interface EligibilityCriteriaDto {
  id: number;
  name: string;
  description?: string;
  isActive: boolean;
}

// ============================================================
// Helper Functions
// ============================================================

/**
 * Get human-readable label for PaymentFrequency
 */
export function getPaymentFrequencyLabel(frequency: PaymentFrequency): string {
  const labels: Record<PaymentFrequency, string> = {
    [PaymentFrequency.DAILY]: 'Quotidien',
    [PaymentFrequency.MONTHLY]: 'Mensuel',
    [PaymentFrequency.QUARTERLY]: 'Trimestriel',
    [PaymentFrequency.ANNUAL]: 'Annuel',
    [PaymentFrequency.ONE_TIME]: 'Ponctuel'
  };
  return labels[frequency] || frequency;
}
