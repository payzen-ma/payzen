/**
 * Audit Log Models - Event tracking for Companies and Employees
 * Aligned with backend CompanyEventLog and EmployeeEventLog schemas
 */

/**
 * Base audit log interface with common fields
 */
export interface BaseAuditLog {
  id: number;
  eventType: AuditEventType;
  eventDescription: string;
  tableName?: string;
  recordId?: number;
  fieldName?: string;
  oldValue?: string;
  newValue?: string;
  oldValueId?: number;
  newValueId?: number;
  createdAt: Date;
  createdBy: number;
  createdByName?: string;  // Enriched from user lookup
  createdByRole?: string;  // Enriched from user lookup
}

/**
 * Company-level audit log
 */
export interface CompanyAuditLog extends BaseAuditLog {
  companyId: number;
  companyName?: string;  // Enriched
}

/**
 * Employee-level audit log
 */
export interface EmployeeAuditLog extends BaseAuditLog {
  employeeId: number;
  employeeName?: string;  // Enriched
}

/**
 * Audit event types based on backend operations
 */
export enum AuditEventType {
  // Company events
  COMPANY_CREATED = 'COMPANY_CREATED',
  COMPANY_UPDATED = 'COMPANY_UPDATED',
  COMPANY_DELETED = 'COMPANY_DELETED',
  COMPANY_DELEGATION_ADDED = 'COMPANY_DELEGATION_ADDED',
  COMPANY_DELEGATION_REMOVED = 'COMPANY_DELEGATION_REMOVED',
  
  // Employee events
  EMPLOYEE_CREATED = 'EMPLOYEE_CREATED',
  EMPLOYEE_UPDATED = 'EMPLOYEE_UPDATED',
  EMPLOYEE_DELETED = 'EMPLOYEE_DELETED',
  EMPLOYEE_STATUS_CHANGED = 'EMPLOYEE_STATUS_CHANGED',
  EMPLOYEE_MANAGER_CHANGED = 'EMPLOYEE_MANAGER_CHANGED',
  
  // User/Permission events
  USER_CREATED = 'USER_CREATED',
  USER_UPDATED = 'USER_UPDATED',
  USER_ROLE_ASSIGNED = 'USER_ROLE_ASSIGNED',
  USER_ROLE_REVOKED = 'USER_ROLE_REVOKED',
  PERMISSION_CREATED = 'PERMISSION_CREATED',
  PERMISSION_UPDATED = 'PERMISSION_UPDATED',
  ROLE_CREATED = 'ROLE_CREATED',
  ROLE_UPDATED = 'ROLE_UPDATED',
  
  // Payroll events (future)
  PAYROLL_GENERATED = 'PAYROLL_GENERATED',
  PAYROLL_VALIDATED = 'PAYROLL_VALIDATED',
  
  // Generic
  OTHER = 'OTHER'
}

/**
 * Audit log filter criteria
 */
export interface AuditLogFilter {
  startDate?: Date;
  endDate?: Date;
  eventTypes?: AuditEventType[];
  createdBy?: number;
  searchQuery?: string;
  companyId?: number;
  employeeId?: number;
}

/**
 * DTO for fetching company audit logs from API
 */
export interface CompanyAuditLogDto {
  id: number;
  companyId: number;
  eventType: string;
  eventDescription: string;
  tableName?: string;
  recordId?: number;
  fieldName?: string;
  oldValue?: string;
  newValue?: string;
  oldValueId?: number;
  newValueId?: number;
  createdAt: string;  // ISO date string from API
  createdBy: number;
}

/**
 * DTO for company history response from backend
 * GET /api/companies/{companyId}/history
 * Note: Properties are camelCase due to camelCaseInterceptor
 */
export interface CompanyHistoryDto {
  type: string;
  title: string;
  date: string;
  description: string;
  details: {
    oldValue: string | null;
    oldValueId: number | null;
    newValue: string | null;
    newValueId: number | null;
    employeeId: number;
    source: string;
  };
  modifiedBy: {
    name: string;
    role: string;
  };
  timestamp: string;
}

/**
 * DTO for fetching employee audit logs from API
 */
export interface EmployeeAuditLogDto {
  id: number;
  employeeId: number;
  eventType: string;
  eventDescription: string;
  tableName?: string;
  recordId?: number;
  fieldName?: string;
  oldValue?: string;
  newValue?: string;
  oldValueId?: number;
  newValueId?: number;
  createdAt: string;  // ISO date string from API
  createdBy: number;
}

/**
 * Audit log display item for UI (combines company and employee logs)
 */
export interface AuditLogDisplayItem {
  id: number;
  type: 'company' | 'employee';
  entityType?: string;
  entityId: number;
  entityName: string;
  eventType: AuditEventType;
  description: string;
  details?: {
    fieldName?: string;
    oldValue?: string;
    newValue?: string;
    [key: string]: any;
  };
  timestamp: Date;
  actor: {
    id: number;
    name: string;
    role?: string;
  };
  icon: string;
  severity: 'info' | 'success' | 'warn' | 'danger';
}
