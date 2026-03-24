export type LeaveScope = 'Global' | 'Company';

export interface LeaveType {
  id: number;
  leaveCode: string;
  leaveName: string;
  leaveDescription: string;
  scope: LeaveScope;
  companyId?: number | null;
  companyName?: string;
  isPaid: boolean;
  requiresBalance: boolean;
  requiresEligibility6Months: boolean;
  isActive: boolean;
  createdAt?: string | null;
}

export interface CreateLeaveTypeRequest {
  leaveCode: string;
  leaveName: string;
  leaveDescription: string;
  scope: LeaveScope;
  companyId?: number | null;
  isPaid?: boolean;
  requiresBalance?: boolean;
  requiresEligibility6Months?: boolean;
  isActive?: boolean;
}

export interface UpdateLeaveTypeRequest {
  leaveCode?: string;
  leaveName?: string;
  leaveDescription?: string;
  scope?: LeaveScope;
  companyId?: number | null;
  isPaid?: boolean;
  requiresBalance?: boolean;
  requiresEligibility6Months?: boolean;
  isActive?: boolean;
}
