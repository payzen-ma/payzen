export interface MaritalStatus {
  id: number;
  code: string;
  nameFr: string;
  nameAr: string;
  nameEn: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateMaritalRequest {
  code: string;
  nameFr: string;
  nameAr: string;
  nameEn: string;
  isActive?: boolean;
}

export interface UpdateMaritalRequest {
  code?: string;
  nameFr?: string;
  nameAr?: string;
  nameEn?: string;
  isActive?: boolean;
}
