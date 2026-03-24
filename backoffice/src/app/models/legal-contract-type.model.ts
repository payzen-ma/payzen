export interface LegalContractType {
  id: number;
  code: string;
  // server uses a single Name field
  name?: string;
  nameFr?: string;
  nameAr?: string;
  nameEn?: string;

  // Audit
  createdAt?: string | null;
  updatedAt?: string | null;
  deletedAt?: string | null;
  createdBy?: number | null;
  updatedBy?: number | null;
  deletedBy?: number | null;
}

export interface CreateLegalContractTypeRequest {
  code: string;
  name?: string;
}

export interface UpdateLegalContractTypeRequest {
  code?: string;
  name?: string;
}
