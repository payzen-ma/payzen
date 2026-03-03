import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReferentielService } from '../../services/referentiel.service';
import { ConfirmService } from '../../shared/confirm/confirm.service';
import { MaritalStatus } from '../../models/marital-status.model';
import { EducationLevel } from '../../models/education-level.model';
import { EmployeeStatus } from '../../models/employee-status.model';
import { Gender } from '../../models/gender.model';
import { LegalContractType } from '../../models/legal-contract-type.model';
import { StateEmploymentProgram } from '../../models/state-employment-program.model';
import { GenderService } from '../../services/gender.service';
import { ModalComponent } from '../../shared/modal/modal.component';
import { LeaveType, CreateLeaveTypeRequest, UpdateLeaveTypeRequest } from '../../models/leave-type.model';
import { LeaveTypeLegalRule, CreateLeaveTypeLegalRuleRequest, UpdateLeaveTypeLegalRuleRequest } from '../../models/leave-type-legal-rule.model';

@Component({
  selector: 'app-referentiel',
  standalone: true,
  imports: [CommonModule, FormsModule, ModalComponent],
  templateUrl: './referentiel.component.html'
})
export class ReferentielComponent implements OnInit {
  maritalStatuses: MaritalStatus[] = [];
  educationLevels: EducationLevel[] = [];
  employeeStatuses: EmployeeStatus[] = [];
  genders: Gender[] = [];
  legalContractTypes: LegalContractType[] = [];
  stateEmploymentPrograms: StateEmploymentProgram[] = [];
  leaveTypes: LeaveType[] = [];
  leaveLegalRules: LeaveTypeLegalRule[] = [];

  // Getter pour filtrer les leave types avec scope Global uniquement
  get globalLeaveTypes(): LeaveType[] {
    return this.leaveTypes.filter(lt => lt.scope === 'Global');
  }

  newMarital = { code: '', nameFr: '', nameAr: '', nameEn: '', isActive: true };
  editingMarital: MaritalStatus | null = null;

  newEducation = { code: '', nameFr: '', nameAr: '', nameEn: '', isActive: true };
  editingEducation: EducationLevel | null = null;

  newStatus = { code: '', nameFr: '', nameAr: '', nameEn: '', isActive: true, affectsAccess: false, affectsPayroll: false, affectsAttendance: false };
  editingStatus: EmployeeStatus | null = null;

  // Gender fields
  newGender = { code: '', nameFr: '', nameAr: '', nameEn: '', isActive: true };
  editingGender: Gender | null = null;

  loading = false;

  // Modal states
  maritalModalVisible = false;
  maritalModalMode: 'create' | 'edit' = 'create';
  maritalModalModel: any = null;

  genderModalVisible = false;
  genderModalMode: 'create' | 'edit' = 'create';
  genderModalModel: any = null;

  educationModalVisible = false;
  educationModalMode: 'create' | 'edit' = 'create';
  educationModalModel: any = null;

  statusModalVisible = false;
  statusModalMode: 'create' | 'edit' = 'create';
  statusModalModel: any = null;

  legalContractModalVisible = false;
  legalContractModalMode: 'create' | 'edit' = 'create';
  legalContractModalModel: any = null;

  stateEmploymentModalVisible = false;
  stateEmploymentModalMode: 'create' | 'edit' = 'create';
  stateEmploymentModalModel: any = null;

  // Leave type modal/state
  leaveModalVisible = false;
  leaveModalMode: 'create' | 'edit' = 'create';
  leaveModalModel: any = null;
  // Leave-type legal rule modal/state
  leaveLegalModalVisible = false;
  leaveLegalModalMode: 'create' | 'edit' = 'create';
  leaveLegalModalModel: any = null;

  constructor(private service: ReferentielService, private confirm: ConfirmService, private genderService: GenderService) {}

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    this.loading = true;
    this.service.getMaritalStatuses(true).subscribe(list => { this.maritalStatuses = list; this.loading = false; });
    this.service.getEducationLevels(true).subscribe(list => { this.educationLevels = list; });
    this.service.getEmployeeStatuses(true).subscribe(list => { this.employeeStatuses = list; });
    this.genderService.getAll(true).subscribe(list => { this.genders = list; });
    this.service.getLegalContractTypes(true).subscribe(list => { this.legalContractTypes = list; });
    this.service.getStateEmploymentPrograms(true).subscribe(list => { this.stateEmploymentPrograms = list; });
    this.service.getLeaveTypes(true).subscribe(list => { this.leaveTypes = list; });
    this.service.getLeaveTypeLegalRules().subscribe(list => { this.leaveLegalRules = list; });
  }

  addMarital(): void {
    // deprecated inline add, prefer modal
    this.openMaritalModal('create');
  }

  startEditMarital(item: MaritalStatus): void { this.editingMarital = { ...item }; }
  saveEditMarital(): void {
    if (!this.editingMarital) return;
    const e = this.editingMarital;
    this.service.updateMaritalStatus(e.id, { code: e.code, nameFr: e.nameFr, nameAr: e.nameAr, nameEn: e.nameEn, isActive: e.isActive })
      .subscribe(() => { this.editingMarital = null; this.refresh(); });
  }
  cancelEditMarital(): void { this.editingMarital = null; }

  openMaritalModal(mode: 'create' | 'edit', item?: MaritalStatus) {
    this.maritalModalMode = mode;
    if (mode === 'edit' && item) this.maritalModalModel = { ...item };
    else this.maritalModalModel = { code: '', nameFr: '', nameAr: '', nameEn: '', isActive: true };
    this.maritalModalVisible = true;
  }

  saveMaritalFromModal() {
    const m = this.maritalModalModel;
    if (!m || !m.code || !m.nameFr) return;
    if (this.maritalModalMode === 'create') {
      this.service.createMaritalStatus({ code: m.code, nameFr: m.nameFr, nameAr: m.nameAr, nameEn: m.nameEn, isActive: m.isActive })
        .subscribe(() => { this.maritalModalVisible = false; this.refresh(); });
    } else {
      this.service.updateMaritalStatus(m.id, { code: m.code, nameFr: m.nameFr, nameAr: m.nameAr, nameEn: m.nameEn, isActive: m.isActive })
        .subscribe(() => { this.maritalModalVisible = false; this.refresh(); });
    }
  }

  deleteMarital(id: number): void {
    this.confirm.confirm('Supprimer ce statut marital ?').then(ok => {
      if (!ok) return;
      this.service.deleteMaritalStatus(id).subscribe(() => this.refresh());
    });
  }

  // Education levels
  addEducation(): void {
    const p = { ...this.newEducation };
    if (!p.code || !p.nameFr) return;
    this.service.createEducationLevel({ code: p.code, nameFr: p.nameFr, nameAr: p.nameAr, nameEn: p.nameEn, isActive: p.isActive })
      .subscribe(() => { this.newEducation = { code: '', nameFr: '', nameAr: '', nameEn: '', isActive: true }; this.refresh(); });
  }

  startEditEducation(item: EducationLevel): void { this.editingEducation = { ...item }; }
  saveEditEducation(): void {
    if (!this.editingEducation) return;
    const e = this.editingEducation;
    this.service.updateEducationLevel(e.id, { code: e.code, nameFr: e.nameFr, nameAr: e.nameAr, nameEn: e.nameEn, isActive: e.isActive })
      .subscribe(() => { this.editingEducation = null; this.refresh(); });
  }
  cancelEditEducation(): void { this.editingEducation = null; }

  deleteEducation(id: number): void {
    this.confirm.confirm('Supprimer ce niveau d\'études ?').then(ok => {
      if (!ok) return;
      this.service.deleteEducationLevel(id).subscribe(() => this.refresh());
    });
  }

  // Employee statuses
  addStatus(): void {
    this.openStatusModal('create');
  }

  startEditStatus(item: EmployeeStatus): void { this.editingStatus = { ...item }; }
  saveEditStatus(): void {
    if (!this.editingStatus) return;
    const e = this.editingStatus;
    this.service.updateEmployeeStatus(e.id, { code: e.code, nameFr: e.nameFr, nameAr: e.nameAr, nameEn: e.nameEn, isActive: e.isActive, affectsAccess: e.affectsAccess, affectsPayroll: e.affectsPayroll, affectsAttendance: e.affectsAttendance })
      .subscribe(() => { this.editingStatus = null; this.refresh(); });
  }
  cancelEditStatus(): void { this.editingStatus = null; }

  openStatusModal(mode: 'create' | 'edit', item?: EmployeeStatus) {
    this.statusModalMode = mode;
    if (mode === 'edit' && item) this.statusModalModel = { ...item };
    else this.statusModalModel = { code: '', nameFr: '', nameAr: '', nameEn: '', isActive: true, affectsAccess: false, affectsPayroll: false, affectsAttendance: false };
    this.statusModalVisible = true;
  }

  saveStatusFromModal() {
    const s = this.statusModalModel;
    if (!s || !s.code || !s.nameFr) return;
    if (this.statusModalMode === 'create') {
      this.service.createEmployeeStatus({ code: s.code, nameFr: s.nameFr, nameAr: s.nameAr, nameEn: s.nameEn, isActive: s.isActive, affectsAccess: s.affectsAccess, affectsPayroll: s.affectsPayroll, affectsAttendance: s.affectsAttendance })
        .subscribe(() => { this.statusModalVisible = false; this.refresh(); });
    } else {
      this.service.updateEmployeeStatus(s.id, { code: s.code, nameFr: s.nameFr, nameAr: s.nameAr, nameEn: s.nameEn, isActive: s.isActive, affectsAccess: s.affectsAccess, affectsPayroll: s.affectsPayroll, affectsAttendance: s.affectsAttendance })
        .subscribe(() => { this.statusModalVisible = false; this.refresh(); });
    }
  }

  deleteStatus(id: number): void {
    this.confirm.confirm('Supprimer ce statut employé ?').then(ok => {
      if (!ok) return;
      this.service.deleteEmployeeStatus(id).subscribe(() => this.refresh());
    });
  }

  // Genders
  addGender(): void {
    this.openGenderModal('create');
  }

  startEditGender(g: Gender): void { this.editingGender = { ...g }; }
  saveEditGender(): void {
    if (!this.editingGender) return;
    const e = this.editingGender;
    this.genderService.update(e.id, { code: e.code, nameFr: e.nameFr, nameAr: e.nameAr, nameEn: e.nameEn, isActive: e.isActive })
      .subscribe(() => { this.editingGender = null; this.refresh(); });
  }
  cancelEditGender(): void { this.editingGender = null; }

  deleteGender(id: number): void {
    this.confirm.confirm('Supprimer ce genre ?').then(ok => {
      if (!ok) return;
      this.genderService.delete(id).subscribe(() => this.refresh());
    });
  }

  openGenderModal(mode: 'create' | 'edit', item?: Gender) {
    this.genderModalMode = mode;
    if (mode === 'edit' && item) this.genderModalModel = { ...item };
    else this.genderModalModel = { code: '', nameFr: '', nameAr: '', nameEn: '', isActive: true };
    this.genderModalVisible = true;
  }

  saveGenderFromModal() {
    const g = this.genderModalModel;
    if (!g || !g.code || !g.nameFr) return;
    if (this.genderModalMode === 'create') {
      this.genderService.create({ code: g.code, nameFr: g.nameFr, nameAr: g.nameAr, nameEn: g.nameEn, isActive: g.isActive })
        .subscribe(() => { this.genderModalVisible = false; this.refresh(); });
    } else {
      this.genderService.update(g.id, { code: g.code, nameFr: g.nameFr, nameAr: g.nameAr, nameEn: g.nameEn, isActive: g.isActive })
        .subscribe(() => { this.genderModalVisible = false; this.refresh(); });
    }
  }

  // Education modal handlers (added)
  openEducationModal(mode: 'create' | 'edit', item?: EducationLevel) {
    this.educationModalMode = mode;
    if (mode === 'edit' && item) this.educationModalModel = { ...item };
    else this.educationModalModel = { code: '', nameFr: '', nameAr: '', nameEn: '', isActive: true };
    this.educationModalVisible = true;
  }

  saveEducationFromModal() {
    const e = this.educationModalModel;
    if (!e || !e.code || !e.nameFr) return;
    if (this.educationModalMode === 'create') {
      this.service.createEducationLevel({ code: e.code, nameFr: e.nameFr, nameAr: e.nameAr, nameEn: e.nameEn, isActive: e.isActive })
        .subscribe(() => { this.educationModalVisible = false; this.refresh(); });
    } else {
      this.service.updateEducationLevel(e.id, { code: e.code, nameFr: e.nameFr, nameAr: e.nameAr, nameEn: e.nameEn, isActive: e.isActive })
        .subscribe(() => { this.educationModalVisible = false; this.refresh(); });
    }
  }

  // Legal Contract Types
  openLegalContractModal(mode: 'create' | 'edit', item?: LegalContractType) {
    this.legalContractModalMode = mode;
    if (mode === 'edit' && item) this.legalContractModalModel = { ...item };
    else this.legalContractModalModel = { code: '', nameFr: '', nameAr: '', nameEn: '' };
    this.legalContractModalVisible = true;
  }

  saveLegalContractFromModal() {
    const l = this.legalContractModalModel;
    if (!l || !l.code || !(l.nameFr || l.name)) return;
    if (this.legalContractModalMode === 'create') {
      this.service.createLegalContractType({ code: l.code, name: l.nameFr ?? l.name })
        .subscribe(() => { this.legalContractModalVisible = false; this.refresh(); });
    } else {
      this.service.updateLegalContractType(l.id, { code: l.code, name: l.nameFr ?? l.name })
        .subscribe(() => { this.legalContractModalVisible = false; this.refresh(); });
    }
  }

  deleteLegalContract(id: number): void {
    this.confirm.confirm('Supprimer ce type de contrat légal ?').then(ok => {
      if (!ok) return;
      this.service.deleteLegalContractType(id).subscribe(() => this.refresh());
    });
  }

  // State Employment Programs
  openStateEmploymentModal(mode: 'create' | 'edit', item?: StateEmploymentProgram) {
    this.stateEmploymentModalMode = mode;
    if (mode === 'edit' && item) this.stateEmploymentModalModel = { ...item };
    else this.stateEmploymentModalModel = { 
      code: '', 
      nameFr: '', 
      nameAr: '', 
      nameEn: '',
      isIrExempt: false,
      isCnssEmployeeExempt: false,
      isCnssEmployerExempt: false,
      maxDurationMonths: null,
      salaryCeiling: null
    };
    this.stateEmploymentModalVisible = true;
  }

  saveStateEmploymentFromModal() {
    const s = this.stateEmploymentModalModel;
    if (!s || !s.code || !(s.nameFr || s.name)) return;
    
    const payload = {
      code: s.code,
      name: s.nameFr ?? s.name,
      nameFr: s.nameFr,
      nameAr: s.nameAr,
      nameEn: s.nameEn,
      isIrExempt: s.isIrExempt ?? false,
      isCnssEmployeeExempt: s.isCnssEmployeeExempt ?? false,
      isCnssEmployerExempt: s.isCnssEmployerExempt ?? false,
      maxDurationMonths: s.maxDurationMonths,
      salaryCeiling: s.salaryCeiling
    };
    
    if (this.stateEmploymentModalMode === 'create') {
      this.service.createStateEmploymentProgram(payload)
        .subscribe(() => { this.stateEmploymentModalVisible = false; this.refresh(); });
    } else {
      this.service.updateStateEmploymentProgram(s.id, payload)
        .subscribe(() => { this.stateEmploymentModalVisible = false; this.refresh(); });
    }
  }

  deleteStateEmployment(id: number): void {
    this.confirm.confirm('Supprimer ce programme d\'emploi d\'État ?').then(ok => {
      if (!ok) return;
      this.service.deleteStateEmploymentProgram(id).subscribe(() => this.refresh());
    });
  }

  // Leave types
  openLeaveModal(mode: 'create' | 'edit', item?: LeaveType) {
    this.leaveModalMode = mode;
    if (mode === 'edit' && item) this.leaveModalModel = { ...item };
    else this.leaveModalModel = {
      leaveCode: '',
      leaveName: '',
      leaveDescription: '',
      scope: 'Global',
      companyId: null,
      isPaid: true,
      requiresBalance: false,
      requiresEligibility6Months: false,
      isActive: true
    };
    this.leaveModalVisible = true;
  }

  saveLeaveFromModal() {
    const l = this.leaveModalModel;
    if (!l || !l.leaveCode || !l.leaveName) return;
    if (this.leaveModalMode === 'create') {
      const payload: CreateLeaveTypeRequest = {
        leaveCode: l.leaveCode,
        leaveName: l.leaveName,
        leaveDescription: l.leaveDescription,
        scope: l.scope,
        companyId: l.scope === 'Company' ? (l.companyId ?? null) : null,
        isPaid: l.isPaid,
        requiresBalance: l.requiresBalance,
        requiresEligibility6Months: l.requiresEligibility6Months,
        isActive: l.isActive
      };
      this.service.createLeaveType(payload).subscribe(() => { this.leaveModalVisible = false; this.refresh(); });
    } else {
      const id = l.id;
      const payload: UpdateLeaveTypeRequest = {};
      if (l.leaveCode !== undefined) payload.leaveCode = l.leaveCode;
      if (l.leaveName !== undefined) payload.leaveName = l.leaveName;
      if (l.leaveDescription !== undefined) payload.leaveDescription = l.leaveDescription;
      if (l.scope !== undefined) payload.scope = l.scope;
      if (l.scope === 'Company') {
        payload.companyId = l.companyId ?? null;
      } else {
        payload.companyId = null;
      }
      if (l.isPaid !== undefined) payload.isPaid = l.isPaid;
      if (l.requiresBalance !== undefined) payload.requiresBalance = l.requiresBalance;
      if (l.requiresEligibility6Months !== undefined) payload.requiresEligibility6Months = l.requiresEligibility6Months;
      if (l.isActive !== undefined) payload.isActive = l.isActive;
      this.service.updateLeaveType(id, payload).subscribe(() => { this.leaveModalVisible = false; this.refresh(); });
    }
  }

  deleteLeaveType(id: number): void {
    this.confirm.confirm('Supprimer ce type de congé ?').then(ok => {
      if (!ok) return;
      this.service.deleteLeaveType(id).subscribe(() => this.refresh());
    });
  }

  // Leave-type legal rules
  openLeaveLegalModal(mode: 'create' | 'edit', item?: LeaveTypeLegalRule) {
    this.leaveLegalModalMode = mode;
    if (mode === 'edit' && item) this.leaveLegalModalModel = { ...item };
    else this.leaveLegalModalModel = {
      leaveTypeId: null,
      eventCaseCode: '',
      description: '',
      daysGranted: 1,
      legalArticle: '',
      canBeDiscontinuous: false,
      mustBeUsedWithinDays: null
    };
    this.leaveLegalModalVisible = true;
  }

  saveLeaveLegalFromModal() {
    const r = this.leaveLegalModalModel;
    if (!r || !r.leaveTypeId || !r.eventCaseCode || !r.legalArticle) return;
    if (this.leaveLegalModalMode === 'create') {
      const payload: CreateLeaveTypeLegalRuleRequest = {
        leaveTypeId: r.leaveTypeId,
        eventCaseCode: r.eventCaseCode,
        description: r.description,
        daysGranted: r.daysGranted,
        legalArticle: r.legalArticle,
        canBeDiscontinuous: r.canBeDiscontinuous,
        mustBeUsedWithinDays: r.mustBeUsedWithinDays
      };
      this.service.createLeaveTypeLegalRule(payload).subscribe(() => { this.leaveLegalModalVisible = false; this.refresh(); });
    } else {
      const id = r.id;
      const payload: UpdateLeaveTypeLegalRuleRequest = {};
      if (r.eventCaseCode !== undefined) payload.eventCaseCode = r.eventCaseCode;
      if (r.description !== undefined) payload.description = r.description;
      if (r.daysGranted !== undefined) payload.daysGranted = r.daysGranted;
      if (r.legalArticle !== undefined) payload.legalArticle = r.legalArticle;
      if (r.canBeDiscontinuous !== undefined) payload.canBeDiscontinuous = r.canBeDiscontinuous;
      if (r.mustBeUsedWithinDays !== undefined) payload.mustBeUsedWithinDays = r.mustBeUsedWithinDays;
      this.service.updateLeaveTypeLegalRule(id, payload).subscribe(() => { this.leaveLegalModalVisible = false; this.refresh(); });
    }
  }

  deleteLeaveLegalRule(id: number): void {
    this.confirm.confirm('Supprimer cette règle légale ?').then(ok => {
      if (!ok) return;
      this.service.deleteLeaveTypeLegalRule(id).subscribe(() => this.refresh());
    });
  }

  // Genders are not part of this referentiel implementation per spec
}
