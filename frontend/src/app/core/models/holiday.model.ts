export enum HolidayScope {
  Global = 0,
  Company = 1
}

export interface Holiday {
  id: number;
  nameFr: string;
  nameAr: string;
  nameEn: string;
  holidayDate: string; // ISO date string
  description?: string;
  companyId?: number;
  companyName?: string;
  countryId: number;
  countryName: string;
  scope: HolidayScope;
  scopeDescription: string;
  holidayType: string;
  isMandatory: boolean;
  isPaid: boolean;
  isRecurring: boolean;
  recurrenceRule?: string;
  year?: number;
  affectPayroll: boolean;
  affectAttendance: boolean;
  isActive: boolean;
  createdAt: string;
}

export interface CreateHolidayRequest {
  nameFr: string;
  nameAr: string;
  nameEn: string;
  holidayDate: string;
  description?: string;
  companyId?: number;
  countryId: number;
  scope: HolidayScope;
  holidayType: string;
  isMandatory?: boolean;
  isPaid?: boolean;
  isRecurring?: boolean;
  recurrenceRule?: string;
  year?: number;
  affectPayroll?: boolean;
  affectAttendance?: boolean;
  isActive?: boolean;
}

export interface UpdateHolidayRequest {
  nameFr?: string;
  nameAr?: string;
  nameEn?: string;
  holidayDate?: string;
  description?: string;
  countryId?: number;
  scope?: HolidayScope;
  holidayType?: string;
  isMandatory?: boolean;
  isPaid?: boolean;
  isRecurring?: boolean;
  recurrenceRule?: string;
  year?: number;
  affectPayroll?: boolean;
  affectAttendance?: boolean;
  isActive?: boolean;
}

export interface HolidayCheckResponse {
  isHoliday: boolean;
  date?: string;
  message?: string;
  holiday?: {
    id: number;
    nameFr: string;
    nameAr: string;
    nameEn: string;
    holidayDate: string;
    scope: HolidayScope;
    holidayType: string;
    isMandatory: boolean;
    isPaid: boolean;
  };
}
