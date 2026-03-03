// Re-export existing models for backwards compatibility
// export * from './leave-type.model';
// export * from './leave-type-policy.model';

// Enums matching backend LeaveStatus
export enum LeaveScope {
  Global = 0,
  Company = 1
}

export enum LeaveAccrualMethod {
  None = 0,
  Monthly = 1,
  Annual = 2,
  PerPayPeriod = 3,
  HoursWorked = 4
}

export enum LeaveRequestStatus {
  Draft = 0,
  Submitted = 1,
  Approved = 2,
  Rejected = 3,
  Cancelled = 4,
  Renounced = 5
}

// Corresponds to LeaveTypeReadDto
export interface LeaveType {
  Id: number;
  LeaveCode: string;
  LeaveName: string;
  LeaveDescription: string;
  Scope: LeaveScope;
  CompanyId?: number | null;
  CompanyName: string;
  IsActive: boolean;
  CreatedAt?: string;
}

export interface LeaveTypeLegalRule {
  id: number;
  leaveTypeId: number;
  eventCaseCode: string;
  description: string;
  daysGranted: number;
  legalArticle: string;
  canBeDiscontinuous: boolean;
  mustBeUsedWithinDays?: number | null;
}

// Corresponds to LeaveTypePolicyReadDto
export interface LeaveTypePolicy {
  Id: number;
  CompanyId?: number | null;
  LeaveTypeId: number;
  IsEnabled: boolean;
  AccrualMethod: LeaveAccrualMethod;
  DaysPerMonthAdult: number;
  DaysPerMonthMinor: number;
  RequiresEligibility6Months: boolean;
  RequiresBalance: boolean;
  BonusDaysPerYearAfter5Years: number;
  AnnualCapDays: number;
  AllowCarryover: boolean;
  MaxCarryoverYears: number;
  MinConsecutiveDays: number;
  UseWorkingCalendar: boolean;
}

// Corresponds to LeaveTypeCreateDto
export interface LeaveTypeCreateDto {
  LeaveCode: string;
  LeaveName: string;
  LeaveDescription: string;
  Scope: LeaveScope;
  CompanyId?: number | null;  // Required if Scope=Company
  IsActive?: boolean;
}

// Corresponds to LeaveTypePatchDto
export interface LeaveTypePatchDto {
  LeaveCode?: string;
  LeaveName?: string;
  LeaveDescription?: string;
  Scope?: LeaveScope;
  CompanyId?: number | null;
  IsActive?: boolean;
}

// Corresponds to LeaveTypePolicyCreateDto
export interface LeaveTypePolicyCreateDto {
  CompanyId?: number | null;  // null = global policy
  LeaveTypeId: number;
  IsEnabled?: boolean;
  AccrualMethod?: LeaveAccrualMethod;
  DaysPerMonthAdult?: number;
  DaysPerMonthMinor?: number;
  RequiresEligibility6Months?: boolean;
  RequiresBalance?: boolean;
  BonusDaysPerYearAfter5Years?: number;
  AnnualCapDays?: number;
  AllowCarryover?: boolean;
  MaxCarryoverYears?: number;
  MinConsecutiveDays?: number;
  UseWorkingCalendar?: boolean;
}

// Corresponds to LeaveTypePolicyPatchDto
export interface LeaveTypePolicyPatchDto {
  IsEnabled?: boolean;
  AccrualMethod?: LeaveAccrualMethod;
  DaysPerMonthAdult?: number;
  DaysPerMonthMinor?: number;
  RequiresEligibility6Months?: boolean;
  RequiresBalance?: boolean;
  BonusDaysPerYearAfter5Years?: number;
  AnnualCapDays?: number;
  AllowCarryover?: boolean;
  MaxCarryoverYears?: number;
  MinConsecutiveDays?: number;
  UseWorkingCalendar?: boolean;
}

// Filter and search interfaces
export interface LeaveTypeFilters {
  search?: string;
  scope?: LeaveScope;
  isActive?: boolean;
}

export interface LeaveTypePolicyFilters {
  search?: string;
  isEnabled?: boolean;
  accrualMethod?: LeaveAccrualMethod;
}

// Leave Request interfaces
export interface LeaveRequestReadDto {
  id: number;
  employeeId: number;
  leaveTypeId: number;
  startDate: string;  // ISO date string
  endDate: string;    // ISO date string
  reason: string;
  status: LeaveRequestStatus;
  createdAt: string;  // ISO datetime string
  updatedAt: string;  // ISO datetime string
  
  // Navigation properties  
  employee?: any;  // Employee entity
  leaveType?: LeaveType;  // LeaveType entity
  
  // Calculated properties
  durationDays?: number;
  statusLabel?: string;
}

export interface LeaveRequestCreateDto {
  employeeId: number;
  leaveTypeId: number;
  startDate: string;  // ISO date string (YYYY-MM-DD)
  endDate: string;    // ISO date string (YYYY-MM-DD)
  employeeNote: string;
}

// DTO pour créer une demande pour un employé (RH/Manager)
export interface LeaveRequestCreateForEmployeeDto {
  leaveTypeId: number;
  startDate: string;  // ISO date string (YYYY-MM-DD)
  endDate: string;    // ISO date string (YYYY-MM-DD)
  employeeNote: string;
  legalRuleId?: number;
}

export interface LeaveRequestPatchDto {
  leaveTypeId?: number;
  startDate?: string;  // ISO date string (YYYY-MM-DD)
  endDate?: string;    // ISO date string (YYYY-MM-DD)
  employeeNote?: string;
}

// For the frontend component
export interface LeaveRequest {
  id: number;
  employeeId: number;
  leaveTypeId: number;
  startDate: Date;
  endDate: Date;
  reason: string;
  status: LeaveRequestStatus;
  createdAt: Date;
  updatedAt: Date;
  
  // Navigation properties
  employee?: any;
  leaveType?: LeaveType;
  
  // Calculated properties
  durationDays?: number;
  workingDaysDeducted?: number;
  statusLabel?: string;
}

// Approval DTO for workflow actions
export interface ApprovalDto {
  approverNotes?: string;
  // Backend expects a `comment` property (ApprovalDto.Comment)
  comment?: string;
}