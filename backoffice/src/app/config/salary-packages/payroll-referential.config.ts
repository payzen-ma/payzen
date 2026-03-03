/**
 * Moroccan Payroll Referential Configuration
 * Based on CNSS Décret 2.25.266 - 2025 and DGI Circulaire 03/2017
 * Effective: October 1, 2025
 */

import { KnownElement, TypeDefaults, FlagConfig, CeilingConfig, RuleMode } from '../../models/salary-packages/payroll-rules.model';
import { SalaryComponentType } from '../../models/salary-package.model';

// ============================================================
// SMIG 2025 Reference (for calculations)
// ============================================================
export const SMIG_2025 = {
  hourly: 17.10,  // MAD/hour (approximation, adjust as needed)
  monthly: 3000,  // MAD/month (approximation)
};

// ============================================================
// Default Flags by Component Type
// ============================================================
export const TYPE_DEFAULTS: Record<SalaryComponentType, TypeDefaults> = {
  base_salary: {
    ir: true,
    cnss: true,
    cimr: true,
    isVariable: false,
  },
  bonus: {
    ir: true,
    cnss: true,
    cimr: false,
    isVariable: true,
  },
  allowance: {
    ir: false,
    cnss: false,
    cimr: false,
    isVariable: true,
  },
  benefit_in_kind: {
    ir: true,
    cnss: true,
    cimr: false,
    isVariable: false,
  },
  social_charge: {
    ir: false,
    cnss: false,
    cimr: false,
    isVariable: false,
  },
};

// ============================================================
// Known Elements Referential (Priority Indemnities)
// ============================================================
export const KNOWN_ELEMENTS: KnownElement[] = [
  // ---- 1. INDEMNITÉS À CARACTÈRE PROFESSIONNEL ----
  {
    id: 'transport_domicile',
    keywords: ['transport', 'domicile', 'travail', 'déplacement domicile'],
    labelFr: 'Indemnité de transport domicile-travail',
    category: 'professional',
    type: 'allowance',
    flags: {
      ir: 'exempt_with_ceiling',
      cnss: 'exempt_with_ceiling',
      cimr: 'excluded',
      irCeiling: {
        type: 'fixed',
        value: 500,
        valueAlt: 750,
        label: '500 DH/mois (urbain)',
        labelAlt: '750 DH/mois (hors urbain)',
        unit: 'MAD/mois',
      },
      cnssCeiling: {
        type: 'fixed',
        value: 500,
        valueAlt: 750,
        label: '500 DH/mois (urbain)',
        labelAlt: '750 DH/mois (hors urbain)',
        unit: 'MAD/mois',
      },
    },
    isVariable: false,
    convergence: true,
    notes: 'Convergence CNSS/DGI - Mêmes plafonds',
  },
  {
    id: 'deplacement_forfaitaire',
    keywords: ['déplacement', 'forfaitaire', 'mission', 'trajets'],
    labelFr: 'Indemnité de déplacement (forfaitaire)',
    category: 'professional',
    type: 'allowance',
    flags: {
      ir: 'exempt_with_ceiling',
      cnss: 'exempt_with_ceiling',
      cimr: 'excluded',
      irCeiling: {
        type: 'fixed',
        value: 5000,
        label: '5 000 DH/mois (trajets > 50 km)',
        unit: 'MAD/mois',
      },
      cnssCeiling: {
        type: 'fixed',
        value: 5000,
        label: '5 000 DH/mois (trajets > 50 km)',
        unit: 'MAD/mois',
      },
    },
    isVariable: true,
    convergence: true,
  },
  {
    id: 'indemnite_kilometrique',
    keywords: ['kilométrique', 'km', 'kilometrique'],
    labelFr: 'Indemnité kilométrique',
    category: 'professional',
    type: 'allowance',
    flags: {
      ir: 'exempt_with_ceiling',
      cnss: 'exempt_with_ceiling',
      cimr: 'excluded',
      irCeiling: {
        type: 'custom',
        label: '3 DH/km pour trajets > 50 km',
        hint: 'Quel que soit le véhicule',
      },
      cnssCeiling: {
        type: 'custom',
        label: '3 DH/km pour trajets > 50 km',
      },
    },
    isVariable: true,
    convergence: true,
  },
  {
    id: 'prime_tournee',
    keywords: ['tournée', 'tournee'],
    labelFr: 'Prime de tournée',
    category: 'professional',
    type: 'allowance',
    flags: {
      ir: 'exempt_with_ceiling',
      cnss: 'exempt_with_ceiling',
      cimr: 'excluded',
      irCeiling: {
        type: 'fixed',
        value: 1500,
        label: 'Plafond 1 500 DH/mois',
        unit: 'MAD/mois',
      },
      cnssCeiling: {
        type: 'fixed',
        value: 1500,
        label: 'Plafond 1 500 DH/mois',
        unit: 'MAD/mois',
      },
    },
    isVariable: true,
    convergence: true,
  },
  {
    id: 'voiture_fonction',
    keywords: ['voiture', 'fonction', 'service', 'véhicule'],
    labelFr: 'Voiture de fonction ou de service',
    category: 'professional',
    type: 'benefit_in_kind',
    flags: {
      ir: 'conditional',
      cnss: 'conditional',
      cimr: 'excluded',
      irCeiling: {
        type: 'custom',
        label: 'Exonérée pour cadres dirigeants ou personnel itinérant',
        hint: 'Usage professionnel et restituée en fin de journée',
      },
      cnssCeiling: {
        type: 'custom',
        label: 'Exonérée si usage pro. et restituée en fin de journée',
      },
    },
    isVariable: false,
    convergence: true,
    notes: 'Avec nuances selon la situation',
  },
  {
    id: 'representation',
    keywords: ['représentation', 'representation'],
    labelFr: 'Indemnité de représentation',
    category: 'professional',
    type: 'allowance',
    flags: {
      ir: 'conditional',
      cnss: 'exempt_with_ceiling',
      cimr: 'excluded',
      irCeiling: {
        type: 'percentage',
        value: 10,
        label: '10% du salaire de base (cadres supérieurs)',
        hint: 'DGI plus restrictif: PDG, DG, etc.',
      },
      cnssCeiling: {
        type: 'percentage',
        value: 10,
        label: '10% du salaire de base pour cadres supérieurs',
      },
    },
    isVariable: false,
    convergence: false,
    notes: 'Divergence CNSS/DGI - DGI plus restrictif',
  },
  {
    id: 'demenagement',
    keywords: ['déménagement', 'demenagement'],
    labelFr: 'Indemnité de déménagement',
    category: 'professional',
    type: 'allowance',
    flags: {
      ir: 'exempt_with_ceiling',
      cnss: 'exempt_with_ceiling',
      cimr: 'excluded',
      irCeiling: {
        type: 'custom',
        label: '10 DH/km',
      },
      cnssCeiling: {
        type: 'custom',
        label: '10 DH/km',
      },
    },
    isVariable: true,
    convergence: true,
  },

  // ---- 2. INDEMNITÉS SOCIALES ET AVANTAGES ----
  {
    id: 'prime_panier',
    keywords: ['panier', 'casse-croûte', 'casse croute', 'cassecroute', 'repas'],
    labelFr: 'Prime de panier / casse-croûte',
    category: 'social',
    type: 'allowance',
    flags: {
      ir: 'exempt_with_ceiling',
      cnss: 'exempt_with_ceiling',
      cimr: 'excluded',
      irCeiling: {
        type: 'smig_multiple',
        value: 2,
        label: '2 × SMIG horaire ≈ 34,20 DH/jour',
        hint: 'Basé sur SMIG horaire 2025',
        unit: 'DH/jour',
      },
      cnssCeiling: {
        type: 'smig_multiple',
        value: 2,
        label: '2 × SMIG horaire ≈ 34,20 DH/jour',
        unit: 'DH/jour',
      },
    },
    isVariable: true,
    convergence: true,
  },
  {
    id: 'ticket_restaurant',
    keywords: ['ticket', 'restaurant', 'bon', 'nourriture'],
    labelFr: 'Ticket-restaurant / bons de nourriture',
    category: 'social',
    type: 'allowance',
    flags: {
      ir: 'exempt_with_ceiling',
      cnss: 'exempt_with_ceiling',
      cimr: 'excluded',
      irCeiling: {
        type: 'fixed',
        value: 20,
        label: 'Plafond fixe de 20 DH/jour et 20% du SBI',
        unit: 'DH/jour',
      },
      cnssCeiling: {
        type: 'smig_multiple',
        value: 2,
        label: '2 × SMIG horaire ≈ 34,20 DH/jour',
        unit: 'DH/jour',
      },
    },
    isVariable: true,
    convergence: false,
    notes: 'Divergence - Plafonds différents CNSS/DGI',
  },
  {
    id: 'gratification_sociale',
    keywords: ['gratification', 'naissance', 'mariage', 'sociale'],
    labelFr: 'Gratifications sociales (naissance, mariage, etc.)',
    category: 'social',
    type: 'bonus',
    flags: {
      ir: 'exempt_with_ceiling',
      cnss: 'exempt_with_ceiling',
      cimr: 'excluded',
      irCeiling: {
        type: 'fixed',
        value: 2500,
        label: '2 500 DH/an',
        unit: 'MAD/an',
      },
      cnssCeiling: {
        type: 'fixed',
        value: 5000,
        label: '5 000 DH/an',
        unit: 'MAD/an',
      },
    },
    isVariable: true,
    convergence: false,
    notes: 'Divergence - Plafonds différents',
  },
  {
    id: 'aide_medicale',
    keywords: ['aide', 'médicale', 'medicale', 'santé', 'sante'],
    labelFr: 'Aide médicale',
    category: 'social',
    type: 'allowance',
    flags: {
      ir: 'conditional',
      cnss: 'conditional',
      cimr: 'excluded',
      irCeiling: {
        type: 'custom',
        label: 'Exonérée si factures justificatives',
      },
      cnssCeiling: {
        type: 'custom',
        label: 'Exonérée si factures justificatives',
      },
    },
    isVariable: true,
    convergence: true,
  },
  {
    id: 'prime_pelerinage',
    keywords: ['pèlerinage', 'pelerinage', 'hajj', 'omra'],
    labelFr: 'Prime de pèlerinage (Hajj)',
    category: 'social',
    type: 'bonus',
    flags: {
      ir: 'conditional',
      cnss: 'conditional',
      cimr: 'excluded',
      irCeiling: {
        type: 'custom',
        label: 'Billet A/R + dotation Office des Changes (1 seule fois)',
      },
      cnssCeiling: {
        type: 'custom',
        label: 'Billet A/R + dotation Office des Changes (1 seule fois)',
      },
    },
    isVariable: true,
    convergence: true,
  },
  {
    id: 'reduction_interet_pret_social',
    keywords: ['intérêt', 'interet', 'prêt', 'pret', 'social'],
    labelFr: 'Réduction intérêt prêts sociaux',
    category: 'social',
    type: 'benefit_in_kind',
    flags: {
      ir: 'exempt_with_ceiling',
      cnss: 'exempt_with_ceiling',
      cimr: 'excluded',
      irCeiling: {
        type: 'fixed',
        value: 50000,
        label: 'Exonérée jusqu\'à 50 000 DH',
        unit: 'MAD',
      },
      cnssCeiling: {
        type: 'fixed',
        value: 50000,
        label: 'Exonérée jusqu\'à 50 000 DH',
        unit: 'MAD',
      },
    },
    isVariable: false,
    convergence: true,
  },
  {
    id: 'reduction_interet_logement',
    keywords: ['logement', 'immobilier', 'habitation'],
    labelFr: 'Réduction intérêt prêts logement',
    category: 'social',
    type: 'benefit_in_kind',
    flags: {
      ir: 'conditional',
      cnss: 'conditional',
      cimr: 'excluded',
      irCeiling: {
        type: 'custom',
        label: 'Exonérée pour logement principal',
      },
      cnssCeiling: {
        type: 'custom',
        label: 'Exonérée pour logement principal',
      },
    },
    isVariable: false,
    convergence: true,
  },

  // ---- 3. PRIMES SPÉCIFIQUES ----
  {
    id: 'indemnite_caisse',
    keywords: ['caisse', 'caissier', 'caissière'],
    labelFr: 'Indemnité de caisse',
    category: 'specific',
    type: 'allowance',
    flags: {
      ir: 'exempt_with_ceiling',
      cnss: 'exempt_with_ceiling',
      cimr: 'excluded',
      irCeiling: {
        type: 'fixed',
        value: 190,
        label: '190 DH/mois (indexé au SMIG en 2017)',
        unit: 'MAD/mois',
        hint: 'DGI: montant fixe',
      },
      cnssCeiling: {
        type: 'smig_multiple',
        value: 14,
        label: '14 × SMIG horaire ≈ 239 DH/mois',
        unit: 'MAD/mois',
      },
    },
    isVariable: false,
    convergence: false,
    notes: 'Divergence - Plafonds différents',
  },
  {
    id: 'indemnite_lait',
    keywords: ['lait'],
    labelFr: 'Indemnité de lait',
    category: 'specific',
    type: 'allowance',
    flags: {
      ir: 'exempt_with_ceiling',
      cnss: 'exempt_with_ceiling',
      cimr: 'excluded',
      irCeiling: {
        type: 'fixed',
        value: 150,
        label: '150 DH/mois (indexé au SMIG en 2017)',
        unit: 'MAD/mois',
      },
      cnssCeiling: {
        type: 'smig_multiple',
        value: 11,
        label: '11 × SMIG horaire ≈ 196 DH/mois',
        unit: 'MAD/mois',
      },
    },
    isVariable: false,
    convergence: false,
  },
  {
    id: 'prime_equipement',
    keywords: ['équipement', 'equipement', 'outillage', 'outil'],
    labelFr: 'Prime d\'équipement personnel / outillage',
    category: 'specific',
    type: 'allowance',
    flags: {
      ir: 'exempt_with_ceiling',
      cnss: 'exempt_with_ceiling',
      cimr: 'excluded',
      irCeiling: {
        type: 'fixed',
        value: 100,
        label: '100 DH/mois (indexé au SMIG en 2017)',
        unit: 'MAD/mois',
      },
      cnssCeiling: {
        type: 'smig_multiple',
        value: 7,
        label: '7 × SMIG horaire ≈ 119 DH/mois',
        unit: 'MAD/mois',
      },
    },
    isVariable: false,
    convergence: false,
  },
  {
    id: 'prime_salissure',
    keywords: ['salissure', 'usure', 'vêtement', 'vetement'],
    labelFr: 'Prime de salissure',
    category: 'specific',
    type: 'allowance',
    flags: {
      ir: 'exempt_with_ceiling',
      cnss: 'exempt_with_ceiling',
      cimr: 'excluded',
      irCeiling: {
        type: 'fixed',
        value: 210,
        label: '210 DH/mois (indexé au SMIG en 2017)',
        unit: 'MAD/mois',
      },
      cnssCeiling: {
        type: 'smig_multiple',
        value: 14,
        label: '14 × SMIG horaire ≈ 239 DH/mois',
        unit: 'MAD/mois',
      },
    },
    isVariable: false,
    convergence: false,
  },

  // ---- 4. INDEMNITÉS DE RUPTURE OU FIN DE CONTRAT ----
  {
    id: 'licenciement',
    keywords: ['licenciement', 'dommages', 'intérêts', 'rupture'],
    labelFr: 'Indemnité de licenciement / dommages-intérêts',
    category: 'termination',
    type: 'bonus',
    flags: {
      ir: 'exempt_with_ceiling',
      cnss: 'exempt_with_ceiling',
      cimr: 'excluded',
      irCeiling: {
        type: 'fixed',
        value: 1000000,
        label: 'Exonérée jusqu\'à 1 000 000 DH',
        unit: 'MAD',
      },
      cnssCeiling: {
        type: 'fixed',
        value: 1000000,
        label: 'Exonérée jusqu\'à 1 000 000 DH',
        unit: 'MAD',
      },
    },
    isVariable: true,
    convergence: true,
  },
  {
    id: 'depart_volontaire',
    keywords: ['départ', 'depart', 'volontaire', 'retraite'],
    labelFr: 'Départ volontaire / mise à la retraite',
    category: 'termination',
    type: 'bonus',
    flags: {
      ir: 'conditional',
      cnss: 'exempt_with_ceiling',
      cimr: 'excluded',
      irCeiling: {
        type: 'fixed',
        value: 1000000,
        label: 'Exonéré dans la limite de 1 000 000 DH (départ volontaire)',
        hint: 'Imposable pour mise à la retraite selon CGI',
      },
      cnssCeiling: {
        type: 'smig_multiple',
        value: 2080,
        label: '2 080 × SMIG horaire ≈ 35 568 DH',
        unit: 'MAD',
      },
    },
    isVariable: true,
    convergence: false,
    notes: 'Divergence - Traitement différent DGI pour retraite',
  },
];

// ============================================================
// Helper Functions
// ============================================================

/**
 * Find matching known element by label (case-insensitive keyword matching)
 */
export function findMatchingElement(label: string): KnownElement | null {
  if (!label || label.trim().length < 3) return null;
  
  const normalizedLabel = label.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
  
  for (const element of KNOWN_ELEMENTS) {
    if (element.keywords) {
      for (const keyword of element.keywords) {
        const normalizedKeyword = keyword.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '');
        if (normalizedLabel.includes(normalizedKeyword)) {
          return element;
        }
      }
    }
  }

  return null;
}

/**
 * Get default flags for a component type
 */
export function getTypeDefaults(type: SalaryComponentType): TypeDefaults {
  return TYPE_DEFAULTS[type] || TYPE_DEFAULTS.allowance;
}

/**
 * Convert RuleMode to boolean for checkbox display
 */
export function ruleModeToBoolean(mode: RuleMode): boolean {
  return mode === 'included';
}

/**
 * Check if a rule mode has a ceiling
 */
export function hasCeiling(mode: RuleMode): boolean {
  return mode === 'exempt_with_ceiling';
}

/**
 * Check if a rule mode is conditional
 */
export function isConditional(mode: RuleMode): boolean {
  return mode === 'conditional';
}

/**
 * Get ceiling display text
 */
export function getCeilingDisplayText(ceiling: CeilingConfig | undefined): string {
  if (!ceiling) return '';
  
  if (ceiling.labelAlt) {
    return `${ceiling.label} / ${ceiling.labelAlt}`;
  }
  return ceiling.label;
}

/**
 * Get all known elements for dropdown selection
 */
export function getKnownElementsForDropdown(): Array<{ id: string; label: string; category: string }> {
  return KNOWN_ELEMENTS.map(el => ({
    id: el.id,
    label: el.labelFr,
    category: el.category,
  }));
}

/**
 * Get known element by ID
 */
export function getKnownElementById(id: string): KnownElement | undefined {
  return KNOWN_ELEMENTS.find(el => el.id === id);
}
