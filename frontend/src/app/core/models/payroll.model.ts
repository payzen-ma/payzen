// Modèle pour les bulletins de paie

export enum PayrollResultStatus {
  SUCCESS = 'SUCCESS',
  ERROR = 'ERROR',
  PENDING = 'PENDING',
  APPROVED = 'APPROVED'
}

export interface PayrollResult {
  id: number;
  employeeId: number;
  employeeName: string;
  companyId: number;
  companyName: string;
  month: number;
  year: number;
  payHalf?: number | null;
  status: PayrollResultStatus;
  errorMessage?: string;
  salaireBase: number;
  totalBrut: number;
  totalCotisationsSalariales: number;
  totalCotisationsPatronales: number;
  impotRevenu: number;
  totalNet: number;
  totalNet2?: number;
  processedAt?: string;
  claudeModel?: string;
  tokensUsed?: number;
}

export interface PayrollResultsResponse {
  count: number;
  month: number;
  year: number;
  results: PayrollResult[];
}

export interface PayrollStats {
  month: number;
  year: number;
  companyId?: number;
  total: number;
  totalMontantBrut: number;
  totalMontantNet: number;
  parStatut: {
    status: PayrollResultStatus;
    count: number;
    totalBrut: number;
    totalNet: number;
  }[];
}

/** Détail d'une prime (imposable ou non) pour l'affichage bulletin */
export interface PayrollDetailPrime {
  label: string;
  montant: number;
  ordre: number;
  isTaxable: boolean;
}

/** Détail complet d'un bulletin (données salariales uniquement) */
export interface PayrollDetail extends PayrollResult {
  // Salaire de base et heures
  heuresSupp25?: number;
  heuresSupp50?: number;
  heuresSupp100?: number;
  conges?: number;
  joursFeries?: number;
  primeAnciennete?: number;
  // Primes imposables
  primeImposable1?: number;
  primeImposable2?: number;
  primeImposable3?: number;
  totalPrimesImposables?: number;
  // Frais et indemnités
  fraisProfessionnels?: number;
  indemniteRepresentation?: number;
  primeTransport?: number;
  primePanier?: number;
  indemniteDeplacement?: number;
  indemniteCaisse?: number;
  primeSalissure?: number;
  gratificationsFamilial?: number;
  primeVoyageMecque?: number;
  indemniteLicenciement?: number;
  indemniteKilometrique?: number;
  primeTourne?: number;
  primeOutillage?: number;
  aideMedicale?: number;
  autresPrimesNonImposable?: number;
  totalIndemnites?: number;
  /** Excédent imposable (partie des indemnités au-delà des plafonds DGI). */
  totalNiExcedentImposable?: number;
  // Cotisations salariales (détail)
  cnssPartSalariale?: number;
  cimrPartSalariale?: number;
  amoPartSalariale?: number;
  mutuellePartSalariale?: number;
  // Cotisations patronales (détail)
  cnssPartPatronale?: number;
  cimrPartPatronale?: number;
  amoPartPatronale?: number;
  mutuellePartPatronale?: number;
  arrondi?: number;
  avanceSurSalaire?: number;
  interetSurLogement?: number;
  brutImposable?: number;
  netImposable?: number;
  totalGains?: number;
  totalRetenues?: number;
  netAPayer?: number;
  totalNet2?: number;
  primes?: PayrollDetailPrime[];
  /** Audit trail du calcul : un enregistrement par module (formule + entrées/sorties). */
  calculationAuditSteps?: PayrollCalculationAuditStep[];
  /** Absences du mois (période du bulletin). */
  absences?: PayrollDetailAbsence[];
  /** Heures supplémentaires du mois (approuvées). */
  overtimes?: PayrollDetailOvertime[];
  /** Congés approuvés qui chevauchent le mois. */
  leaves?: PayrollDetailLeave[];
}

export interface PayrollDetailAbsence {
  id: number;
  absenceDate: string;
  absenceType: string;
  reason: string | null;
  durationType: string;
  status: string;
}

export interface PayrollDetailOvertime {
  id: number;
  overtimeDate: string;
  durationInHours: number;
  rateMultiplierApplied: number;
}

export interface PayrollDetailLeave {
  id: number;
  startDate: string;
  endDate: string;
  workingDaysDeducted: number;
  leaveTypeName: string | null;
}

export interface PayrollCalculationAuditStep {
  stepOrder: number;
  moduleName: string;
  formulaDescription: string;
  inputsJson: string | null;
  outputsJson: string | null;
}

export interface CalculatePayrollRequest {
  month: number;
  year: number;
  employeeId?: number;
}

export interface PayrollFilters {
  month?: number;
  year?: number;
  companyId?: number;
  half?: number | null;
  status?: PayrollResultStatus;
  searchQuery?: string;
  employeeId?: number;
}
