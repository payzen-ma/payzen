export interface ContractType {
  id: number;
  contractTypeName: string;
  companyId: number;
  companyName?: string;
  legalContractTypeId?: number | null;
  stateEmploymentProgramId?: number | null;
  createdAt: string;
}

export interface ContractTypeCreateDto {
  ContractTypeName: string;
  CompanyId: number;
  LegalContractTypeId?: number | null;
  StateEmploymentProgramId?: number | null;
}

export interface ContractTypeUpdateDto {
  ContractTypeName: string;
  LegalContractTypeId?: number | null;
  StateEmploymentProgramId?: number | null;
}
