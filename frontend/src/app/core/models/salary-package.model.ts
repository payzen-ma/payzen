// Component types for Moroccan payroll compliance
export type SalaryComponentType = 'base_salary' | 'allowance' | 'bonus' | 'benefit_in_kind' | 'social_charge';

// Status values matching backend
export type SalaryPackageStatus = 'draft' | 'published' | 'deprecated';

// Template types for 2-level system (Backoffice Official vs Client Company)
export type TemplateType = 'OFFICIAL' | 'COMPANY';

// Regulation version for Moroccan compliance
export type RegulationVersion = 'MA_2025';

// Auto-calculated rules based on Moroccan labor law
export interface AutoRules {
  seniorityBonusEnabled: boolean;  // Prime d'ancienneté
  ruleVersion: RegulationVersion;
}

// CIMR Regime types (Morocco 2025)
export type CimrRegime = 'AL_KAMIL' | 'AL_MOUNASSIB' | 'NONE';

// CIMR Configuration for salary package
export interface CimrConfig {
  regime: CimrRegime;
  employeeRate: number;      // Taux salarial (3% to 10%)
  employerRate: number;      // Taux patronal (auto-calculated: employeeRate * 1.3)
  customEmployerRate?: number | null; // Override for custom employer rate
}

// CNSS ceiling for Al Mounassib calculation
export const CNSS_CEILING = 6000; // MAD per month

// CIMR Al Kamil standard rates (Morocco 2025)
export const CIMR_AL_KAMIL_RATES = [
  { employeeRate: 0.03, employerRate: 0.039, label: '3,00%' },
  { employeeRate: 0.0375, employerRate: 0.0488, label: '3,75%' },
  { employeeRate: 0.045, employerRate: 0.0585, label: '4,50%' },
  { employeeRate: 0.0525, employerRate: 0.0683, label: '5,25%' },
  { employeeRate: 0.06, employerRate: 0.078, label: '6,00%' },
  { employeeRate: 0.07, employerRate: 0.091, label: '7,00%' },
  { employeeRate: 0.075, employerRate: 0.0975, label: '7,50%' },
  { employeeRate: 0.08, employerRate: 0.104, label: '8,00%' },
  { employeeRate: 0.085, employerRate: 0.1105, label: '8,50%' },
  { employeeRate: 0.09, employerRate: 0.117, label: '9,00%' },
  { employeeRate: 0.095, employerRate: 0.1235, label: '9,50%' },
  { employeeRate: 0.10, employerRate: 0.13, label: '10,00%' }
] as const;

// CIMR Al Mounassib standard rates (Morocco 2025 - for PME)
export const CIMR_AL_MOUNASSIB_RATES = [
  { employeeRate: 0.06, employerRate: 0.078, label: '6%' },
  { employeeRate: 0.07, employerRate: 0.091, label: '7%' },
  { employeeRate: 0.08, employerRate: 0.104, label: '8%' },
  { employeeRate: 0.09, employerRate: 0.117, label: '9%' },
  { employeeRate: 0.10, employerRate: 0.13, label: '10%' },
  { employeeRate: 0.11, employerRate: 0.143, label: '11%' },
  { employeeRate: 0.12, employerRate: 0.156, label: '12%' }
] as const;

// Helper to calculate employer rate from employee rate (coefficient 1.3)
export function calculateCimrEmployerRate(employeeRate: number): number {
  return Math.round(employeeRate * 1.3 * 10000) / 10000;
}

export interface SalaryPackageItem {
  id?: number;
  payComponentId?: number | null;
  payComponentCode?: string | null;
  label: string;
  defaultValue: number;
  sortOrder?: number;
  // Moroccan regulatory fields
  type: SalaryComponentType;
  isTaxable: boolean;  // Subject to IR (Impot sur le Revenu)
  isSocial: boolean;   // Subject to CNSS contributions
  isCIMR: boolean;     // Subject to CIMR contributions
  isVariable: boolean; // Monthly estimate vs fixed amount
  exemptionLimit?: number | null; // Cap for tax/social exemptions in MAD
  // Referentiel element link (from payroll-referentiel API)
  referentielElementId?: number | null;
  referentielElementCode?: string | null;
  isConvergence?: boolean; // CNSS/IR rules alignment status
}

export interface SalaryPackage {
  id: number;
  name: string;
  code?: string | null;
  category: string;
  description?: string | null;
  baseSalary: number;
  status: SalaryPackageStatus;
  companyId?: number | null;
  companyName?: string | null;

  // Business Sector (Morocco 2025)
  businessSectorId: number;
  businessSectorName?: string | null;

  // Template type (Official for backoffice, Company for client-owned)
  templateType: TemplateType;

  // Moroccan regulation version
  regulationVersion?: RegulationVersion;

  // Auto-calculated rules (Morocco 2025)
  autoRules?: AutoRules;

  // CIMR Configuration (Moroccan 2025 compliance)
  cimrConfig?: CimrConfig | null;
  cimrRate?: number | null; // Legacy field - use cimrConfig.employeeRate instead

  // Versioning and template tracking
  version: number;
  sourceTemplateId?: number | null;
  sourceTemplateName?: string | null;
  sourceTemplateVersion?: number | null;
  validFrom?: string | null;
  validTo?: string | null;
  isLocked: boolean;

  // Computed property from backend
  isGlobalTemplate?: boolean;

  items: SalaryPackageItem[];
  updatedAt?: string | null;
  createdAt?: string | null;
}

export interface SalaryPackageItemWriteRequest {
  id?: number;
  payComponentId?: number | null;
  label: string;
  defaultValue: number;
  sortOrder?: number;
  type: SalaryComponentType;
  isTaxable: boolean;
  isSocial: boolean;
  isCIMR: boolean;
  isVariable: boolean;
  exemptionLimit?: number | null;
  // Referentiel element link
  referentielElementId?: number | null;
}

export interface SalaryPackageWriteRequest {
  name: string;
  category: string;
  description?: string | null;
  baseSalary: number;
  status: SalaryPackageStatus;
  companyId?: number | null;
  templateType?: TemplateType;
  regulationVersion?: RegulationVersion;
  autoRules?: AutoRules;
  cimrConfig?: CimrConfig | null;
  cimrRate?: number | null; // Legacy field
  hasPrivateInsurance?: boolean;
  validFrom?: string | null;
  validTo?: string | null;
  items: SalaryPackageItemWriteRequest[];
}

export interface SalaryPackageCloneRequest {
  companyId: number;
  name?: string;
  validFrom?: string | null;
}

// Salary Package Assignment (employee-to-package linking)
export interface SalaryPackageAssignment {
  id: number;
  salaryPackageId: number;
  salaryPackageName: string;
  employeeId: number;
  employeeFullName: string;
  contractId: number;
  employeeSalaryId: number;
  effectiveDate: string;
  endDate?: string | null;
  packageVersion: number;
  createdAt: string;
}

export interface SalaryPackageAssignmentCreateRequest {
  salaryPackageId: number;
  employeeId: number;
  contractId: number;
  effectiveDate: string;
}

export interface SalaryPackageAssignmentUpdateRequest {
  endDate: string;
}

// Pay Component Catalog (global reference data)
export interface PayComponent {
  id: number;
  code: string;
  nameFr: string;
  nameAr?: string | null;
  nameEn?: string | null;
  type: SalaryComponentType;
  isTaxable: boolean;
  isSocial: boolean;
  isCIMR: boolean;
  exemptionLimit?: number | null;
  exemptionRule?: string | null;
  defaultAmount?: number | null;
  version: number;
  validFrom: string;
  validTo?: string | null;
  isRegulated: boolean;
  isActive: boolean;
  sortOrder: number;
  createdAt: string;
  updatedAt: string;
}
