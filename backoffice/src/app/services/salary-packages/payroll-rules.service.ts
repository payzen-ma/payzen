import { Injectable } from '@angular/core';
import { SalaryComponentType } from '../../models/salary-package.model';
import {
  RuleMode,
  KnownElement,
  CeilingConfig,
  PayrollItemFlags,
} from '../../models/salary-packages/payroll-rules.model';
import {
  findMatchingElement,
  getTypeDefaults,
  getKnownElementById,
  ruleModeToBoolean,
  hasCeiling,
  isConditional,
  KNOWN_ELEMENTS,
} from '../../config/salary-packages/payroll-referential.config';

export interface ResolvedFlags {
  ir: boolean;
  cnss: boolean;
  cimr: boolean;
  isVariable: boolean;
  irMode: RuleMode;
  cnssMode: RuleMode;
  irCeiling?: CeilingConfig;
  cnssCeiling?: CeilingConfig;
  matchedElement?: KnownElement;
}

@Injectable({
  providedIn: 'root'
})
export class PayrollRulesService {

  /**
   * Resolve flags based on label and type
   * Priority: 1) Known element match, 2) Type defaults
   */
  resolveFlags(label: string, type: SalaryComponentType): ResolvedFlags {
    // Try to find a matching known element
    const matchedElement = findMatchingElement(label);

    if (matchedElement) {
      return this.resolveFlagsFromElement(matchedElement);
    }

    // Fall back to type defaults
    return this.resolveFlagsFromType(type);
  }

  /**
   * Resolve flags from a known element
   */
  resolveFlagsFromElement(element: KnownElement): ResolvedFlags {
    const { flags } = element;

    return {
      ir: ruleModeToBoolean(flags.ir),
      cnss: ruleModeToBoolean(flags.cnss),
      cimr: ruleModeToBoolean(flags.cimr),
      isVariable: element.isVariable,
      irMode: flags.ir,
      cnssMode: flags.cnss,
      irCeiling: flags.irCeiling,
      cnssCeiling: flags.cnssCeiling,
      matchedElement: element,
    };
  }

  /**
   * Resolve flags from component type defaults
   */
  resolveFlagsFromType(type: SalaryComponentType): ResolvedFlags {
    const defaults = getTypeDefaults(type);

    return {
      ir: defaults.ir,
      cnss: defaults.cnss,
      cimr: defaults.cimr,
      isVariable: defaults.isVariable,
      irMode: defaults.ir ? 'included' : 'excluded',
      cnssMode: defaults.cnss ? 'included' : 'excluded',
      irCeiling: undefined,
      cnssCeiling: undefined,
      matchedElement: undefined,
    };
  }

  /**
   * Resolve flags by element ID (for dropdown selection)
   */
  resolveFlagsByElementId(elementId: string): ResolvedFlags | null {
    const element = getKnownElementById(elementId);
    if (!element) return null;
    return this.resolveFlagsFromElement(element);
  }

  /**
   * Check if a mode indicates a ceiling applies
   */
  hasCeilingRule(mode: RuleMode): boolean {
    return hasCeiling(mode);
  }

  /**
   * Check if a mode is conditional
   */
  isConditionalRule(mode: RuleMode): boolean {
    return isConditional(mode);
  }

  /**
   * Get all known elements for UI display
   */
  getKnownElements(): KnownElement[] {
    return KNOWN_ELEMENTS;
  }

  /**
   * Get elements grouped by category
   */
  getElementsByCategory(): Map<string, KnownElement[]> {
    const grouped = new Map<string, KnownElement[]>();

    for (const element of KNOWN_ELEMENTS) {
      const existing = grouped.get(element.category) || [];
      existing.push(element);
      grouped.set(element.category, existing);
    }

    return grouped;
  }

  /**
   * Get category label in French
   */
  getCategoryLabel(category: string): string {
    const labels: Record<string, string> = {
      professional: 'Indemnités professionnelles',
      social: 'Indemnités sociales et avantages',
      specific: 'Primes spécifiques',
      termination: 'Indemnités de rupture',
    };
    return labels[category] || category;
  }

  /**
   * Get rule mode badge info for UI display
   */
  getRuleModeBadge(mode: RuleMode): { label: string; class: string; icon: string } | null {
    switch (mode) {
      case 'exempt_with_ceiling':
        return {
          label: 'Plafond',
          class: 'bg-amber-100 text-amber-700',
          icon: '📊',
        };
      case 'conditional':
        return {
          label: 'Conditionnel',
          class: 'bg-purple-100 text-purple-700',
          icon: '⚠️',
        };
      default:
        return null;
    }
  }

  /**
   * Format ceiling for display
   */
  formatCeilingDisplay(ceiling: CeilingConfig | undefined): string {
    if (!ceiling) return '';

    let text = ceiling.label;
    if (ceiling.labelAlt) {
      text += ` ou ${ceiling.labelAlt}`;
    }
    if (ceiling.hint) {
      text += ` (${ceiling.hint})`;
    }
    return text;
  }
}
