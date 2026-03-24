/**
 * Referentiel Element Models (Compensation Elements)
 * Represents elements like Transport, Panier, Représentation, etc.
 */

import { PaymentFrequency } from './lookup.models';
import { ElementRuleDto } from './element-rule.model';

/**
 * Element Status Enum
 */
export enum ElementStatus {
  DRAFT = 'DRAFT',
  ACTIVE = 'ACTIVE',
  ARCHIVED = 'ARCHIVED'
}

/**
 * Referentiel Element (Summary for lists)
 */
export interface ReferentielElementListDto {
  id: number;
  code?: string;
  name: string;
  categoryName: string;
  defaultFrequency: PaymentFrequency;
  status: ElementStatus;
  isActive: boolean;
  hasConvergence: boolean;  // Are CNSS & DGI rules aligned?
  ruleCount: number;
  hasCnssRule: boolean;
  hasDgiRule: boolean;
}

/**
 * Referentiel Element (Full details with rules)
 */
export interface ReferentielElementDto {
  id: number;
  code?: string;
  name: string;
  categoryId: number;
  categoryName: string;
  description?: string;
  defaultFrequency: PaymentFrequency;
  status: ElementStatus;
  isActive: boolean;
  hasConvergence: boolean;
  rules: ElementRuleDto[];
}

/**
 * Create Referentiel Element DTO - Request for POST
 */
export interface CreateReferentielElementDto {
  code?: string;
  name: string;
  categoryId: number;
  description?: string;
  defaultFrequency: PaymentFrequency;
  status?: ElementStatus;
}

/**
 * Update Referentiel Element DTO - Request for PUT
 */
export interface UpdateReferentielElementDto {
  code?: string;
  name: string;
  categoryId: number;
  description?: string;
  defaultFrequency: PaymentFrequency;
  isActive: boolean;
}

/**
 * Update Element Status DTO
 */
export interface UpdateElementStatusDto {
  status: ElementStatus;
}

/**
 * Convergence Check Response
 */
export interface ConvergenceResultDto {
  elementId: number;
  elementName: string;
  isConvergence: boolean;
  checkDate: string;
  cnssRule?: {
    exemptionType: string;
    effectiveFrom: string;
    effectiveTo?: string;
  };
  irRule?: {
    exemptionType: string;
    effectiveFrom: string;
    effectiveTo?: string;
  };
}

// ============================================================
// Helper Functions
// ============================================================

/**
 * Get status badge for an element
 */
export function getElementStatusBadge(status: ElementStatus): { text: string; class: string } {
  switch (status) {
    case ElementStatus.DRAFT:
      return {
        text: 'Brouillon',
        class: 'bg-gray-100 text-gray-600'
      };
    case ElementStatus.ACTIVE:
      return {
        text: 'Actif',
        class: 'bg-green-100 text-green-800'
      };
    case ElementStatus.ARCHIVED:
      return {
        text: 'Archivé',
        class: 'bg-orange-100 text-orange-600'
      };
    default:
      return {
        text: 'Inconnu',
        class: 'bg-gray-100 text-gray-600'
      };
  }
}

/**
 * Get convergence status text
 */
export function getConvergenceStatusText(hasConvergence: boolean): string {
  return hasConvergence ? 'Convergence' : 'Divergence';
}

/**
 * Get convergence status CSS classes
 */
export function getConvergenceStatusClass(hasConvergence: boolean): string {
  return hasConvergence
    ? 'bg-green-100 text-green-800'
    : 'bg-yellow-100 text-yellow-800';
}
