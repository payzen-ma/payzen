export interface Overtime {
  id?: number;
  employeeId: number;
  employeeFirstName?: string;
  employeeLastName?: string;
  employeeFullName?: string;
  overtimeDate: string;
  overtimeType: OvertimeType;
  startTime?: string;
  endTime?: string;
  totalHours: number;
  reason?: string;
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
  Hourly = 1,
  Holiday = 2
}

export enum OvertimeStatus {
  Submitted = 1,
  Approved = 2,
  Rejected = 3,
  Cancelled = 4
}

export interface CreateOvertimeRequest {
  employeeId: number;
  overtimeDate: string;
  overtimeType: OvertimeType;
  startTime?: string;
  endTime?: string;
  reason?: string;
}

export interface UpdateOvertimeRequest {
  overtimeDate: string;
  overtimeType: OvertimeType;
  startTime?: string;
  endTime?: string;
  reason?: string;
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
