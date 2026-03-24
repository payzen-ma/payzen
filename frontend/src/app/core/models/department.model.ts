export interface Department {
  id: number;
  departementName: string;
  companyId: number;
  companyName?: string;
  createdAt: string;
}

export interface DepartmentCreateUpdateDto {
  DepartementName: string;
  CompanyId?: number;
}

