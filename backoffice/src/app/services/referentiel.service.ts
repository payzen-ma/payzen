import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { CreateEducationLevelRequest, EducationLevel, UpdateEducationLevelRequest } from '../models/education-level.model';
import { CreateEmployeeStatusRequest, EmployeeStatus, UpdateEmployeeStatusRequest } from '../models/employee-status.model';
import { CreateGenderRequest, Gender, UpdateGenderRequest } from '../models/gender.model';
import { CreateLeaveTypeLegalRuleRequest, LeaveTypeLegalRule, UpdateLeaveTypeLegalRuleRequest } from '../models/leave-type-legal-rule.model';
import { CreateLeaveTypeRequest, LeaveScope, LeaveType, UpdateLeaveTypeRequest } from '../models/leave-type.model';
import { CreateLegalContractTypeRequest, LegalContractType, UpdateLegalContractTypeRequest } from '../models/legal-contract-type.model';
import { CreateMaritalRequest, MaritalStatus, UpdateMaritalRequest } from '../models/marital-status.model';
import { CreateStateEmploymentProgramRequest, StateEmploymentProgram, UpdateStateEmploymentProgramRequest } from '../models/state-employment-program.model';

@Injectable({ providedIn: 'root' })
export class ReferentielService {
  private baseUrl = 'https://api-test.payzenhr.com';

  constructor(private http: HttpClient) { }

  // Marital statuses
  getMaritalStatuses(includeInactive = true): Observable<MaritalStatus[]> {
    const url = includeInactive ? `${this.baseUrl}/api/marital-statuses?includeInactive=true` : `${this.baseUrl}/api/marital-statuses`;
    return this.http.get<any[]>(url).pipe(
      map(list => (list || []).map(d => ({
        id: d.Id,
        code: d.Code,
        nameFr: d.NameFr,
        nameAr: d.NameAr,
        nameEn: d.NameEn,
        isActive: d.IsActive,
        createdAt: d.CreatedAt
      } as MaritalStatus)))
    );
  }

  createMaritalStatus(payload: CreateMaritalRequest) {
    const dto: any = {
      Code: payload.code,
      NameFr: payload.nameFr,
      NameAr: payload.nameAr,
      NameEn: payload.nameEn,
      IsActive: payload.isActive ?? true
    };
    return this.http.post(`${this.baseUrl}/api/marital-statuses`, dto);
  }

  updateMaritalStatus(id: number, payload: UpdateMaritalRequest) {
    const dto: any = {};
    if (payload.code !== undefined) dto.Code = payload.code;
    if (payload.nameFr !== undefined) dto.NameFr = payload.nameFr;
    if (payload.nameAr !== undefined) dto.NameAr = payload.nameAr;
    if (payload.nameEn !== undefined) dto.NameEn = payload.nameEn;
    if (payload.isActive !== undefined) dto.IsActive = payload.isActive;
    return this.http.put(`${this.baseUrl}/api/marital-statuses/${id}`, dto);
  }

  deleteMaritalStatus(id: number) {
    return this.http.delete(`${this.baseUrl}/api/marital-statuses/${id}`);
  }

  // Genders
  getGenders(includeInactive = true): Observable<Gender[]> {
    const url = includeInactive ? `${this.baseUrl}/api/genders?includeInactive=true` : `${this.baseUrl}/api/genders`;
    return this.http.get<any[]>(url);
  }

  createGender(payload: CreateGenderRequest) {
    return this.http.post(`${this.baseUrl}/api/genders`, payload);
  }

  updateGender(id: number, payload: UpdateGenderRequest) {
    return this.http.put(`${this.baseUrl}/api/genders/${id}`, payload);
  }

  deleteGender(id: number) {
    return this.http.delete(`${this.baseUrl}/api/genders/${id}`);
  }

  // Education levels
  getEducationLevels(includeInactive = true): Observable<EducationLevel[]> {
    const url = includeInactive ? `${this.baseUrl}/api/education-levels?includeInactive=true` : `${this.baseUrl}/api/education-levels`;
    return this.http.get<any[]>(url).pipe(
      map(list => (list || []).map(d => ({
        id: d.Id,
        code: d.Code,
        nameFr: d.NameFr,
        nameAr: d.NameAr,
        nameEn: d.NameEn,
        isActive: d.IsActive,
        createdAt: d.CreatedAt
      } as EducationLevel)))
    );
  }

  createEducationLevel(payload: CreateEducationLevelRequest) {
    const dto: any = {
      Code: payload.code,
      NameFr: payload.nameFr,
      NameAr: payload.nameAr,
      NameEn: payload.nameEn,
      IsActive: payload.isActive ?? true,
    };
    return this.http.post(`${this.baseUrl}/api/education-levels`, dto);
  }

  updateEducationLevel(id: number, payload: UpdateEducationLevelRequest) {
    const dto: any = {};
    if (payload.code !== undefined) dto.Code = payload.code;
    if (payload.nameFr !== undefined) dto.NameFr = payload.nameFr;
    if (payload.nameAr !== undefined) dto.NameAr = payload.nameAr;
    if (payload.nameEn !== undefined) dto.NameEn = payload.nameEn;
    if (payload.isActive !== undefined) dto.IsActive = payload.isActive;
    return this.http.put(`${this.baseUrl}/api/education-levels/${id}`, dto);
  }

  deleteEducationLevel(id: number) {
    return this.http.delete(`${this.baseUrl}/api/education-levels/${id}`);
  }

  // Employee statuses
  getEmployeeStatuses(includeInactive = true): Observable<EmployeeStatus[]> {
    const url = includeInactive ? `${this.baseUrl}/api/statuses?includeInactive=true` : `${this.baseUrl}/api/statuses`;
    return this.http.get<any[]>(url).pipe(
      map(list => (list || []).map(d => ({
        id: d.Id,
        code: d.Code,
        nameFr: d.NameFr,
        nameAr: d.NameAr,
        nameEn: d.NameEn,
        isActive: d.IsActive,
        affectsAccess: d.AffectsAccess,
        affectsPayroll: d.AffectsPayroll,
        affectsAttendance: d.AffectsAttendance,
        createdAt: d.CreatedAt
      } as EmployeeStatus)))
    );
  }

  createEmployeeStatus(payload: CreateEmployeeStatusRequest) {
    const dto: any = {
      Code: payload.code,
      NameFr: payload.nameFr,
      NameAr: payload.nameAr,
      NameEn: payload.nameEn,
      IsActive: payload.isActive ?? true,
      AffectsAccess: payload.affectsAccess ?? false,
      AffectsPayroll: payload.affectsPayroll ?? false,
      AffectsAttendance: payload.affectsAttendance ?? false,
    };
    return this.http.post(`${this.baseUrl}/api/statuses`, dto);
  }

  updateEmployeeStatus(id: number, payload: UpdateEmployeeStatusRequest) {
    const dto: any = {};
    if (payload.code !== undefined) dto.Code = payload.code;
    if (payload.nameFr !== undefined) dto.NameFr = payload.nameFr;
    if (payload.nameAr !== undefined) dto.NameAr = payload.nameAr;
    if (payload.nameEn !== undefined) dto.NameEn = payload.nameEn;
    if (payload.isActive !== undefined) dto.IsActive = payload.isActive;
    if (payload.affectsAccess !== undefined) dto.AffectsAccess = payload.affectsAccess;
    if (payload.affectsPayroll !== undefined) dto.AffectsPayroll = payload.affectsPayroll;
    if (payload.affectsAttendance !== undefined) dto.AffectsAttendance = payload.affectsAttendance;
    return this.http.put(`${this.baseUrl}/api/statuses/${id}`, dto);
  }

  deleteEmployeeStatus(id: number) {
    return this.http.delete(`${this.baseUrl}/api/statuses/${id}`);
  }

  // Legal Contract Types
  getLegalContractTypes(includeInactive = true): Observable<LegalContractType[]> {
    const url = includeInactive ? `${this.baseUrl}/api/legal-contract-types?includeInactive=true` : `${this.baseUrl}/api/legal-contract-types`;
    return this.http.get<any[]>(url).pipe(
      map(list => (list || []).map(d => ({
        id: d.Id,
        code: d.Code,
        // backend exposes a single Name property
        name: d.Name,
        nameFr: d.Name || undefined,
        nameAr: d.NameAr || undefined,
        nameEn: d.NameEn || undefined,
        createdAt: d.CreatedAt,
        updatedAt: d.UpdatedAt,
        deletedAt: d.DeletedAt,
        createdBy: d.CreatedBy,
        updatedBy: d.UpdatedBy,
        deletedBy: d.DeletedBy
      } as LegalContractType)))
    );
  }

  createLegalContractType(payload: CreateLegalContractTypeRequest) {
    const dto: any = {
      Code: payload.code,
      Name: payload.name ?? (payload as any).nameFr ?? ''
    };
    return this.http.post(`${this.baseUrl}/api/legal-contract-types`, dto);
  }

  updateLegalContractType(id: number, payload: UpdateLegalContractTypeRequest) {
    const dto: any = {};
    if (payload.code !== undefined) dto.Code = payload.code;
    if (payload.name !== undefined) dto.Name = payload.name;
    return this.http.put(`${this.baseUrl}/api/legal-contract-types/${id}`, dto);
  }

  deleteLegalContractType(id: number) {
    return this.http.delete(`${this.baseUrl}/api/legal-contract-types/${id}`);
  }

  // Leave types
  getLeaveTypes(includeInactive = true): Observable<LeaveType[]> {
    const url = includeInactive ? `${this.baseUrl}/api/leave-types?includeInactive=true` : `${this.baseUrl}/api/leave-types`;
    return this.http.get<any[]>(url).pipe(
      map(list => (list || []).map(d => ({
        id: d.Id,
        leaveCode: d.LeaveCode,
        leaveName: d.LeaveName,
        leaveDescription: d.LeaveDescription,
        scope: (d.Scope as LeaveScope) || 'Global',
        companyId: d.CompanyId,
        companyName: d.CompanyName || '',
        isPaid: d.IsPaid,
        requiresBalance: d.RequiresBalance,
        requiresEligibility6Months: d.RequiresEligibility6Months,
        isActive: d.IsActive,
        createdAt: d.CreatedAt
      } as LeaveType)))
    );
  }

  createLeaveType(payload: CreateLeaveTypeRequest) {
    const dto: any = {
      LeaveCode: payload.leaveCode,
      LeaveName: payload.leaveName,
      LeaveDescription: payload.leaveDescription,
      Scope: payload.scope,
      CompanyId: payload.companyId ?? null,
      IsPaid: payload.isPaid ?? true,
      RequiresBalance: payload.requiresBalance ?? false,
      RequiresEligibility6Months: payload.requiresEligibility6Months ?? false,
      IsActive: payload.isActive ?? true
    };
    return this.http.post(`${this.baseUrl}/api/leave-types`, dto);
  }

  updateLeaveType(id: number, payload: UpdateLeaveTypeRequest) {
    const dto: any = {};
    if (payload.leaveCode !== undefined) dto.LeaveCode = payload.leaveCode;
    if (payload.leaveName !== undefined) dto.LeaveName = payload.leaveName;
    if (payload.leaveDescription !== undefined) dto.LeaveDescription = payload.leaveDescription;
    if (payload.scope !== undefined) dto.Scope = payload.scope;
    if (payload.companyId !== undefined) dto.CompanyId = payload.companyId;
    if (payload.isPaid !== undefined) dto.IsPaid = payload.isPaid;
    if (payload.requiresBalance !== undefined) dto.RequiresBalance = payload.requiresBalance;
    if (payload.requiresEligibility6Months !== undefined) dto.RequiresEligibility6Months = payload.requiresEligibility6Months;
    if (payload.isActive !== undefined) dto.IsActive = payload.isActive;
    return this.http.put(`${this.baseUrl}/api/leave-types/${id}`, dto);
  }

  deleteLeaveType(id: number) {
    return this.http.delete(`${this.baseUrl}/api/leave-types/${id}`);
  }

  // Leave type legal rules
  getLeaveTypeLegalRules(leaveTypeId?: number): Observable<LeaveTypeLegalRule[]> {
    const url = leaveTypeId ? `${this.baseUrl}/api/leave-type-legal-rules?leaveTypeId=${leaveTypeId}` : `${this.baseUrl}/api/leave-type-legal-rules`;
    return this.http.get<any[]>(url).pipe(
      map(list => (list || []).map(d => ({
        id: d.Id,
        leaveTypeId: d.LeaveTypeId,
        eventCaseCode: d.EventCaseCode,
        description: d.Description,
        daysGranted: d.DaysGranted,
        legalArticle: d.LegalArticle,
        canBeDiscontinuous: d.CanBeDiscontinuous,
        mustBeUsedWithinDays: d.MustBeUsedWithinDays
      } as LeaveTypeLegalRule)))
    );
  }

  createLeaveTypeLegalRule(payload: CreateLeaveTypeLegalRuleRequest) {
    const dto: any = {
      LeaveTypeId: payload.leaveTypeId,
      EventCaseCode: payload.eventCaseCode,
      Description: payload.description,
      DaysGranted: payload.daysGranted,
      LegalArticle: payload.legalArticle,
      CanBeDiscontinuous: payload.canBeDiscontinuous ?? false,
      MustBeUsedWithinDays: payload.mustBeUsedWithinDays ?? null
    };
    return this.http.post(`${this.baseUrl}/api/leave-type-legal-rules`, dto);
  }

  updateLeaveTypeLegalRule(id: number, payload: UpdateLeaveTypeLegalRuleRequest) {
    const dto: any = {};
    if (payload.eventCaseCode !== undefined) dto.EventCaseCode = payload.eventCaseCode;
    if (payload.description !== undefined) dto.Description = payload.description;
    if (payload.daysGranted !== undefined) dto.DaysGranted = payload.daysGranted;
    if (payload.legalArticle !== undefined) dto.LegalArticle = payload.legalArticle;
    if (payload.canBeDiscontinuous !== undefined) dto.CanBeDiscontinuous = payload.canBeDiscontinuous;
    if (payload.mustBeUsedWithinDays !== undefined) dto.MustBeUsedWithinDays = payload.mustBeUsedWithinDays;
    return this.http.put(`${this.baseUrl}/api/leave-type-legal-rules/${id}`, dto);
  }

  deleteLeaveTypeLegalRule(id: number) {
    return this.http.delete(`${this.baseUrl}/api/leave-type-legal-rules/${id}`);
  }

  // State Employment Programs
  getStateEmploymentPrograms(includeInactive = true): Observable<StateEmploymentProgram[]> {
    const url = includeInactive ? `${this.baseUrl}/api/state-employment-programs?includeInactive=true` : `${this.baseUrl}/api/state-employment-programs`;
    return this.http.get<any[]>(url).pipe(
      map(list => (list || []).map(d => ({
        id: d.Id,
        code: d.Code,
        name: d.Name,
        nameFr: d.Name || undefined,
        nameAr: d.NameAr || undefined,
        nameEn: d.NameEn || undefined,

        // legal rules
        isIrExempt: d.IsIrExempt,
        isCnssEmployeeExempt: d.IsCnssEmployeeExempt,
        isCnssEmployerExempt: d.IsCnssEmployerExempt,
        maxDurationMonths: d.MaxDurationMonths,
        salaryCeiling: d.SalaryCeiling,

        // audit
        createdAt: d.CreatedAt,
        updatedAt: d.UpdatedAt,
        deletedAt: d.DeletedAt,
        createdBy: d.CreatedBy,
        updatedBy: d.UpdatedBy,
        deletedBy: d.DeletedBy
      } as StateEmploymentProgram)))
    );
  }

  createStateEmploymentProgram(payload: CreateStateEmploymentProgramRequest) {
    const dto: any = {
      Code: payload.code,
      Name: payload.name ?? payload.nameFr ?? '',
      IsIrExempt: payload.isIrExempt ?? false,
      IsCnssEmployeeExempt: payload.isCnssEmployeeExempt ?? false,
      IsCnssEmployerExempt: payload.isCnssEmployerExempt ?? false,
      MaxDurationMonths: payload.maxDurationMonths ?? null,
      SalaryCeiling: payload.salaryCeiling ?? null
    };
    return this.http.post(`${this.baseUrl}/api/state-employment-programs`, dto);
  }

  updateStateEmploymentProgram(id: number, payload: UpdateStateEmploymentProgramRequest) {
    const dto: any = {};
    if (payload.code !== undefined) dto.Code = payload.code;
    if (payload.name !== undefined) dto.Name = payload.name;
    if (payload.isIrExempt !== undefined) dto.IsIrExempt = payload.isIrExempt;
    if (payload.isCnssEmployeeExempt !== undefined) dto.IsCnssEmployeeExempt = payload.isCnssEmployeeExempt;
    if (payload.isCnssEmployerExempt !== undefined) dto.IsCnssEmployerExempt = payload.isCnssEmployerExempt;
    if (payload.maxDurationMonths !== undefined) dto.MaxDurationMonths = payload.maxDurationMonths;
    if (payload.salaryCeiling !== undefined) dto.SalaryCeiling = payload.salaryCeiling;
    return this.http.put(`${this.baseUrl}/api/state-employment-programs/${id}`, dto);
  }

  deleteStateEmploymentProgram(id: number) {
    return this.http.delete(`${this.baseUrl}/api/state-employment-programs/${id}`);
  }
}
