export interface WorkingCalendar {
  id: number;
  companyId: number;
  companyName: string;
  dayOfWeek: number; // 0 = Dimanche, 1 = Lundi, ..., 6 = Samedi
  dayOfWeekName: string;
  isWorkingDay: boolean;
  startTime: string | null;
  endTime: string | null;
  createdAt: string;
}

export interface CreateWorkingCalendarRequest {
  companyId: number;
  dayOfWeek: number;
  isWorkingDay: boolean;
  startTime?: string;
  endTime?: string;
}

export interface UpdateWorkingCalendarRequest {
  companyId?: number;
  dayOfWeek?: number;
  isWorkingDay?: boolean;
  startTime?: string;
  endTime?: string;
}

export interface WorkingDay {
  dayOfWeek: number;
  dayName: string;
  isWorkingDay: boolean;
  startTime: string | null;
  endTime: string | null;
  id?: number;
}
