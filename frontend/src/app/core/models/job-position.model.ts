export interface JobPosition {
  id: number;
  name: string;
  companyId: number;
  companyName?: string;
  createdAt: string;
}

export interface JobPositionCreateUpdateDto {
  Name: string;
  CompanyId?: number;
}

