export interface Overtime {
  // Primary identifiers
  id?: number;
  employeeId: number;

  // Employee display names (keep both granular and combined forms)
  employeeFirstName?: string;
  employeeLastName?: string;
  // Friendly combined name used in UI (alias for employeeFullName)
  employeeFullName?: string;
  // Convenience field matching absence model naming
  employeeName?: string;

  // Date/time fields
  // `overtimeDate` is a DateOnly string (YYYY-MM-DD)
  overtimeDate: string;
  // overtimeType is a flag enum
  overtimeType?: OvertimeType;
  overtimeTypeDescription?: string;
  entryMode?: OvertimeEntryMode;
  
  // Holiday work
  holidayId?: number;
  holidayName?: string;
  
  // TimeOnly values from backend (HH:mm:ss or HH:mm)
  startTime?: string;
  endTime?: string;
  crossesMidnight?: boolean;

  // Duration / totals
  totalHours?: number;
  durationInHours?: number;
  standardDayHours?: number;

  // Rate rule (snapshot)
  rateRuleId?: number;
  rateRuleCodeApplied?: string;
  rateRuleNameApplied?: string;
  rateMultiplierApplied?: number;
  multiplierCalculationDetails?: string;
  
  // Split batching
  splitBatchId?: string;
  splitSequence?: number;
  splitTotalSegments?: number;
  
  // Workflow
  isProcessedInPayroll?: boolean;
  payrollBatchId?: number;
  processedInPayrollAt?: string;
  employeeComment?: string;
  managerComment?: string;
  reason?: string;

  // Status & audit
  status: OvertimeStatus;
  statusDescription?: string;
  createdAt?: string;
  createdBy?: number;
  createdByName?: string;
  approvedAt?: string | null;
  approvedBy?: number | null;
  approvedByName?: string | null;
  approvalComment?: string | null;
}

export enum OvertimeType {
  None = 0,
  Standard = 1,
  PublicHoliday = 2,
  WeeklyRest = 4,
  Night = 8
}

export enum OvertimeEntryMode {
  HoursRange = 1,
  DurationOnly = 2,
  FullDay = 3
}

export enum OvertimeStatus {
  Draft = 0,
  Submitted = 1,
  Approved = 2,
  Rejected = 3,
  Cancelled = 4
}

export interface CreateOvertimeRequest {
  employeeId: number;
  overtimeDate: string; // DateOnly string (YYYY-MM-DD)
  entryMode: OvertimeEntryMode;
  
  // HoursRange Mode
  startTime?: string;
  endTime?: string;
  
  // DurationOnly Mode  
  durationInHours?: number;
  
  // FullDay Mode
  standardDayHours?: number;
  
  employeeComment?: string;
}

export interface UpdateOvertimeRequest {
  overtimeDate?: string;
  startTime?: string;
  endTime?: string;
  durationInHours?: number;
  employeeComment?: string;
}

export interface OvertimeFilters {
  employeeId?: number;
  startDate?: string;
  endDate?: string;
  status?: OvertimeStatus;
  page?: number;
  pageSize?: number;
}

export interface OvertimesResponse {
  data: Overtime[];
  total: number;
  page: number;
  pageSize: number;
}

export interface OvertimeStats {
  totalOvertimeHours: number;
  pendingCount: number;
  approvedCount: number;
  rejectedCount: number;
}
