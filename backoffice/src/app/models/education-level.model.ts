export interface EducationLevel {
  id: number;
  code: string;
  nameFr: string;
  nameAr: string;
  nameEn: string;
  isActive: boolean;
  createdAt: string;
}

export interface CreateEducationLevelRequest {
  code: string;
  nameFr: string;
  nameAr: string;
  nameEn: string;
  isActive?: boolean;
}

export interface UpdateEducationLevelRequest {
  code?: string;
  nameFr?: string;
  nameAr?: string;
  nameEn?: string;
  isActive?: boolean;
}
