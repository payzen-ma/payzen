export interface EmployeeStatus {
  id: number;
  code: string;
  nameFr: string;
  nameAr: string;
  nameEn: string;
  isActive: boolean;
  affectsAccess: boolean;
  affectsPayroll: boolean;
  affectsAttendance: boolean;
  createdAt: string;
}

export interface CreateEmployeeStatusRequest {
  code: string;
  nameFr: string;
  nameAr: string;
  nameEn: string;
  isActive?: boolean;
  affectsAccess?: boolean;
  affectsPayroll?: boolean;
  affectsAttendance?: boolean;
}

export interface UpdateEmployeeStatusRequest {
  code?: string;
  nameFr?: string;
  nameAr?: string;
  nameEn?: string;
  isActive?: boolean;
  affectsAccess?: boolean;
  affectsPayroll?: boolean;
  affectsAttendance?: boolean;
}
