import { LeaveAccrualMethod } from '../leave.model';

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
  BonusDaysPerYearAfter5Years: number;
  AnnualCapDays: number;
  AllowCarryover: boolean;
  MaxCarryoverYears: number;
  MinConsecutiveDays: number;
  UseWorkingCalendar: boolean;
}

export interface EventRule {
  eventCaseCode: string;
  daysGranted: number;
  legalArticle?: string;
  discontinuous?: boolean;
  mustUseWithinDays?: number | null;
}

// Corresponds to LeaveTypePolicyCreateDto
export interface LeaveTypePolicyCreateDto {
  CompanyId?: number | null;  // null = global policy
  LeaveTypeId: number;
  IsEnabled?: boolean;
  AccrualMethod?: LeaveAccrualMethod;
  DaysPerMonthAdult?: number;
  DaysPerMonthMinor?: number;
  BonusDaysPerYearAfter5Years?: number;
  RequiresEligibility6Months?: boolean;
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
  BonusDaysPerYearAfter5Years?: number;
  RequiresEligibility6Months?: boolean;
  AnnualCapDays?: number;
  AllowCarryover?: boolean;
  MaxCarryoverYears?: number;
  MinConsecutiveDays?: number;
  UseWorkingCalendar?: boolean;
}
