export enum HolidayScope {
  Global = 0,
  Company = 1
}

export interface HolidayReadDto {
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
  // Backend uses DeletedAt for soft-delete; expose it for UI checks when needed
  deletedAt?: string | null;
}

export interface HolidayCreateDto {
  nameFr: string;
  nameAr: string;
  nameEn: string;
  holidayDate: string; // ISO date string (YYYY-MM-DD)
  description?: string;
  companyId?: number; // NULL = Global holiday
  countryId: number;
  scope: HolidayScope;
  holidayType: string; // National, Religieux, Company, etc.
  isMandatory?: boolean;
  isPaid?: boolean;
  isRecurring?: boolean;
  recurrenceRule?: string;
  year?: number;
  affectPayroll?: boolean;
  affectAttendance?: boolean;
  isActive?: boolean;
}

export interface HolidayUpdateDto {
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
