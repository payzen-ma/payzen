export type AbsenceStatus = 'Submitted' | 'Approved' | 'Rejected' | 'Cancelled' | 'Expired';

export interface Absence {
  id: number;
  employeeId: number;
  employeeName?: string;
  absenceDate: string; // DateOnly from backend
  durationType: AbsenceDurationType;
  isMorning?: boolean; // For HalfDay: true = morning, false = afternoon
  startTime?: string; // TimeOnly from backend (HH:mm format)
  endTime?: string; // TimeOnly from backend (HH:mm format)
  absenceType: AbsenceType;
  reason?: string;
  status?: AbsenceStatus;
  statusDescription?: string;
  createdAt: string;
  createdBy: number;
  createdByName?: string; // Nom de la personne qui a créé l'absence
  decisionAt?: string; // Date de la décision (approval/rejection)
  decisionBy?: number; // ID de l'utilisateur qui a pris la décision
  decisionByName?: string; // Nom de la personne qui a pris la décision
  decisionComment?: string; // Commentaire de la décision
}

export type AbsenceType =
  | 'ANNUAL_LEAVE'
  | 'SICK'
  | 'MATERNITY'
  | 'PATERNITY'
  | 'UNPAID'
  | 'MISSION'
  | 'TRAINING'
  | 'JUSTIFIED'
  | 'UNJUSTIFIED'
  | 'ACCIDENT_WORK'
  | 'EXCEPTIONAL'
  | 'RELIGIOUS';

export type AbsenceDurationType = 'FullDay' | 'HalfDay' | 'Hourly';

export interface AbsenceFilters {
  employeeId?: number;
  absenceType?: AbsenceType;
  durationType?: AbsenceDurationType;
  status?: AbsenceStatus;
  startDate?: string;
  endDate?: string;
  page?: number;
  limit?: number;
}

export interface AbsenceStats {
  totalAbsences: number;
  totalDays: number;
}

export interface CreateAbsenceRequest {
  employeeId: number;
  absenceDate: string;
  durationType: AbsenceDurationType;
  isMorning?: boolean;
  startTime?: string;
  endTime?: string;
  absenceType: AbsenceType;
  reason?: string;
}

export interface UpdateAbsenceRequest {
  absenceDate?: string;
  durationType?: AbsenceDurationType;
  isMorning?: boolean;
  startTime?: string;
  endTime?: string;
  absenceType?: AbsenceType;
  reason?: string;
}

export interface AbsencesResponse {
  absences: Absence[];
  total: number;
  stats: AbsenceStats;
}
