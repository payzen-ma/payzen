/**
 * Payroll Rules Model for Morocco 2025
 * Based on CNSS Décret 2.25.266 - 2025 and DGI Circulaire 03/2017
 */

// Rule modes for checkbox behavior
export type RuleMode = 
  | 'included'           // Checkbox checked, subject to contribution
  | 'excluded'           // Checkbox unchecked, not subject
  | 'exempt_with_ceiling' // Exempt up to a ceiling amount
  | 'conditional';        // Conditional exemption (requires specific conditions)

// Ceiling configuration for exemptions
export interface CeilingConfig {
  type: 'fixed' | 'percentage' | 'smig_multiple' | 'custom';
  value?: number;
  valueAlt?: number;        // Alternative value (e.g., urban vs non-urban)
  label: string;
  labelAlt?: string;
  hint?: string;
  unit?: string;
}

// Flag configuration for a specific element
export interface FlagConfig {
  ir: RuleMode;
  cnss: RuleMode;
  cimr: RuleMode;
  irCeiling?: CeilingConfig;
  cnssCeiling?: CeilingConfig;
}

// Known element in the referential
export interface KnownElement {
  id: string;
  keywords?: string[];      // Keywords for matching user input (optional for API elements)
  labelFr: string;          // Display label in French
  category: 'professional' | 'social' | 'specific' | 'termination';
  type: 'allowance' | 'bonus' | 'benefit_in_kind' | 'social_charge';
  flags: FlagConfig;
  isVariable: boolean;
  notes?: string;
  convergence?: boolean;    // CNSS/DGI alignment status (optional)
  apiElementId?: number;    // Reference to API element ID if loaded from PayrollReferentielService
}

// Default flags by component type
export interface TypeDefaults {
  ir: boolean;
  cnss: boolean;
  cimr: boolean;
  isVariable: boolean;
}

// Extended item with auto/manual mode
export interface PayrollItemFlags {
  ir: boolean;
  cnss: boolean;
  cimr: boolean;
  isVariable: boolean;
  isAuto: boolean;
  matchedElementId?: string;
  irMode?: RuleMode;
  cnssMode?: RuleMode;
  irCeiling?: CeilingConfig;
  cnssCeiling?: CeilingConfig;
}
