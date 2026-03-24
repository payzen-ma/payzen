export enum LeaveScope {
  Global = 0,
  Company = 1
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
