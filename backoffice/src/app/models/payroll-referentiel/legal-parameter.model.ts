/**
 * Legal Parameter Models (SMIG, SMAG, CIMR rates, etc.)
 * With temporal versioning support
 */

/**
 * Legal Parameter DTO - Response from GET
 */
export interface LegalParameterDto {
  id: number;
  name: string;
  description?: string;
  value: number;  // decimal value
  unit: string;   // e.g., "MAD/heure", "MAD/mois", "taux"
  effectiveFrom: string;  // DateOnly (YYYY-MM-DD)
  effectiveTo?: string;   // DateOnly (YYYY-MM-DD), null if currently active
  isActive: boolean;
}

/**
 * Create Legal Parameter DTO - Request for POST
 */
export interface CreateLegalParameterDto {
  name: string;
  description?: string;
  value: number;
  unit: string;
  effectiveFrom: string;  // YYYY-MM-DD
  effectiveTo?: string;   // YYYY-MM-DD, optional
}

/**
 * Update Legal Parameter DTO - Request for PUT
 */
export interface UpdateLegalParameterDto {
  name: string;
  description?: string;
  value: number;
  unit: string;
  effectiveFrom: string;
  effectiveTo?: string;
}

// ============================================================
// Legal Parameter Types & Helper Functions
// ============================================================

/**
 * Legal Parameter Type Classification
 */
export enum LegalParameterType {
  SMIG = 'SMIG',
  SMAG = 'SMAG',
  CIMR = 'CIMR',
  CNSS = 'CNSS',
  AMO = 'AMO',
  IR = 'IR',
  OTHER = 'OTHER'
}

/**
 * Determine parameter type from name prefix
 */
export function getLegalParameterType(name: string): LegalParameterType {
  const upperName = name.toUpperCase();

  if (upperName.startsWith('SMIG')) return LegalParameterType.SMIG;
  if (upperName.startsWith('SMAG')) return LegalParameterType.SMAG;
  if (upperName.startsWith('CIMR')) return LegalParameterType.CIMR;
  if (upperName.startsWith('CNSS')) return LegalParameterType.CNSS;
  if (upperName.startsWith('AMO')) return LegalParameterType.AMO;
  if (upperName.startsWith('IR')) return LegalParameterType.IR;

  return LegalParameterType.OTHER;
}

/**
 * Get human-readable label for parameter type
 */
export function getLegalParameterTypeLabel(type: LegalParameterType): string {
  const labels: Record<LegalParameterType, string> = {
    [LegalParameterType.SMIG]: 'SMIG (Salaire Minimum)',
    [LegalParameterType.SMAG]: 'SMAG (Salaire Agricole)',
    [LegalParameterType.CIMR]: 'CIMR (Retraite)',
    [LegalParameterType.CNSS]: 'CNSS (Sécurité Sociale)',
    [LegalParameterType.AMO]: 'AMO (Assurance Maladie)',
    [LegalParameterType.IR]: 'IR (Impôt sur le Revenu)',
    [LegalParameterType.OTHER]: 'Autres Paramètres'
  };
  return labels[type] || type;
}

/**
 * Group parameters by type
 */
export function groupParametersByType(parameters: LegalParameterDto[]): Map<LegalParameterType, LegalParameterDto[]> {
  const groups = new Map<LegalParameterType, LegalParameterDto[]>();

  parameters.forEach(param => {
    const type = getLegalParameterType(param.name);
    const existing = groups.get(type) || [];
    existing.push(param);
    groups.set(type, existing);
  });

  return groups;
}

/**
 * Format parameter value for display
 */
export function formatParameterValue(param: LegalParameterDto): string {
  // Handle null/undefined values
  if (param.value === null || param.value === undefined) {
    return `N/A`;
  }

  // For percentages/rates (0.06 -> 6%)
  if (param.unit === 'taux' || param.unit === '%') {
    return `${(param.value * 100).toFixed(2)}%`;
  }

  // For monetary values
  if (param.unit.includes('MAD')) {
    return `${param.value.toFixed(2)} ${param.unit}`;
  }

  // Default formatting
  return `${param.value} ${param.unit}`;
}

/**
 * Format effective period for display
 */
export function formatEffectivePeriod(param: LegalParameterDto): string {
  const formatDate = (dateStr: string) => {
    const date = new Date(dateStr);
    return date.toLocaleDateString('fr-FR', { day: '2-digit', month: 'short', year: 'numeric' });
  };

  const from = formatDate(param.effectiveFrom);
  const to = param.effectiveTo ? formatDate(param.effectiveTo) : 'en cours';

  return `${from} → ${to}`;
}

/**
 * Check if a parameter is currently active
 */
export function isParameterCurrentlyActive(param: LegalParameterDto): boolean {
  if (!param.isActive) return false;

  const now = new Date();
  const effectiveFrom = new Date(param.effectiveFrom);
  const effectiveTo = param.effectiveTo ? new Date(param.effectiveTo) : null;

  return effectiveFrom <= now && (!effectiveTo || effectiveTo >= now);
}
