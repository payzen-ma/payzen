// Company information model
export interface Company {
  id: string;
  legalName: string;              // Raison sociale
  ice: string;                     // ICE number
  if?: string;                     // Identifiant Fiscal
  rc?: string;                     // Registre de Commerce
  patente?: string;                // Patente
  legalForm?: string;
  cnss: string;                    // CNSS number
  address: string;
  city: string;
  postalCode: string;
  country: string;
  email: string;
  phone: string;
  website?: string;
  taxRegime: TaxRegime;
  fiscalYear: number;
  employeeCount: number;
  hrParameters: HRParameters;
  documents: CompanyDocuments;
  isActive: boolean;
  status?: 'active' | 'suspended' | 'pending';
  logoUrl?: string;
  rib?: string;                    // Bank account number
  // Cabinet/Multi-company management fields
  managedByCompanyId?: number;     // Cabinet comptable managing this company
  managedByCompanyName?: string;   // Name of managing cabinet (enriched)
  isCabinetExpert?: boolean;       // True if this company is a cabinet
  createdAt: Date;
  updatedAt: Date;
}

// Tax regime enum
export enum TaxRegime {
  IS = 'IS',
  IR = 'IR',
  AUTO_ENTREPRENEUR = 'Auto-entrepreneur'
}

// HR parameters model
export interface HRParameters {
  workingDays: string[];
  workingHoursPerDay: number;
  workingHoursPerWeek: number;
  standardHoursPerDay?: number;
  startTime?: number; // Hour only (0-23)
  endTime?: number;   // Hour only (0-23)
  leaveCalculationMode: string;
  absenceCalculationMode: string;
  annualLeaveDays: number;
  publicHolidays: string[];
  probationPeriodDays: number;
  noticePeriodDays: number;
  defaultPaymentMode?: 'virement' | 'cheque' | 'especes' | null;
  leaveAccrualRate?: 1.5 | 2.0;
  includeSaturdays?: boolean;
  rib?: string;
  // New fields for Paramétrage Paie
  currency?: string;
  paymentFrequency?: 'monthly' | 'bimonthly' | 'weekly';
  fiscalYearStartMonth?: number;
  // New fields for Paramètres avancés
  sector?: string | null;
  collectiveAgreement?: string;
  cnssSpecificParameters?: string;
  irSpecificParameters?: string;
}

// Working day type
export type WorkingDay = 'monday' | 'tuesday' | 'wednesday' | 'thursday' | 'friday' | 'saturday' | 'sunday';

// Leave calculation modes
export type LeaveCalculationMode = 'calendar_days' | 'working_days';

// Absence calculation modes
export type AbsenceCalculationMode = 'full_day' | 'half_day' | 'hourly';

// Company documents structure
export interface CompanyDocuments {
  cnss_attestation: string | null;
  amo: string | null;
  logo: string | null;
  rib: string | null;
  other: string[];
}

// Company document types (for individual documents)
export interface CompanyDocument {
  id: string;
  companyId: string;
  type: DocumentType;
  fileName: string;
  fileUrl: string;
  uploadedAt: Date;
  uploadedBy: string;
}

export type DocumentType = 'cnss_attestation' | 'amo' | 'logo' | 'rib' | 'other';

// Company event for history tracking
export interface CompanyEvent {
  type: string;
  title: string;
  date: string;
  description: string;
  details?: Record<string, unknown>;
  modifiedBy?: {
    name: string;
    role: string;
  };
  timestamp: string;
}

export interface CompanyCreateByExpertDto {
  CompanyName: string;
  CompanyEmail: string;
  CompanyPhoneNumber: string;
  CompanyAddress: string;
  CountryId: number;
  CityId?: number;
  CityName?: string;
  CnssNumber: string;
  ManagedByCompanyId: number;
  AdminFirstName: string;
  AdminLastName: string;
  AdminEmail: string;
  AdminPhone: string;
  GeneratePassword?: boolean;
  AdminPassword?: string;
  CountryPhoneCode?: string;
  IsCabinetExpert?: boolean;
  IceNumber?: string;
  IfNumber?: string;
  RcNumber?: string;
  RibNumber?: string;
  LegalForm?: string;
  FoundingDate?: string;
  BusinessSector?: string;
  PaymentMethod?: string;
}
