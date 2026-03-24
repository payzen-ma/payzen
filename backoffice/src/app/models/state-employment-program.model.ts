export interface StateEmploymentProgram {
  id: number;
  code: string;
  // server model uses a single Name; UI kept multilingual fields for compatibility
  name?: string;
  nameFr?: string;
  nameAr?: string;
  nameEn?: string;

  // Legal rules
  isIrExempt?: boolean;
  isCnssEmployeeExempt?: boolean;
  isCnssEmployerExempt?: boolean;
  maxDurationMonths?: number | null;
  salaryCeiling?: number | null;

  // Audit
  createdAt?: string | null;
  updatedAt?: string | null;
  deletedAt?: string | null;
  createdBy?: number | null;
  updatedBy?: number | null;
  deletedBy?: number | null;
}

export interface CreateStateEmploymentProgramRequest {
  code: string;
  name?: string;
  nameFr?: string;
  nameAr?: string;
  nameEn?: string;
  isIrExempt?: boolean;
  isCnssEmployeeExempt?: boolean;
  isCnssEmployerExempt?: boolean;
  maxDurationMonths?: number | null;
  salaryCeiling?: number | null;
}

export interface UpdateStateEmploymentProgramRequest {
  code?: string;
  name?: string;
  nameFr?: string;
  nameAr?: string;
  nameEn?: string;
  isIrExempt?: boolean;
  isCnssEmployeeExempt?: boolean;
  isCnssEmployerExempt?: boolean;
  maxDurationMonths?: number | null;
  salaryCeiling?: number | null;
}
