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

export interface CreateLeaveTypeLegalRuleRequest {
  leaveTypeId: number;
  eventCaseCode: string;
  description: string;
  daysGranted: number;
  legalArticle: string;
  canBeDiscontinuous?: boolean;
  mustBeUsedWithinDays?: number | null;
}

export interface UpdateLeaveTypeLegalRuleRequest {
  eventCaseCode?: string;
  description?: string;
  daysGranted?: number;
  legalArticle?: string;
  canBeDiscontinuous?: boolean;
  mustBeUsedWithinDays?: number | null;
}
