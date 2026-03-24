export interface Gender {
  id: number;
  code: string;
  nameFr: string;
  nameAr: string;
  nameEn: string;
  isActive: boolean;
  createdAt: string; // ISO string
}

export interface CreateGenderRequest {
  code: string;
  nameFr: string;
  nameAr: string;
  nameEn: string;
  isActive?: boolean;
}

export interface UpdateGenderRequest {
  code?: string;
  nameFr?: string;
  nameAr?: string;
  nameEn?: string;
  isActive?: boolean;
}
