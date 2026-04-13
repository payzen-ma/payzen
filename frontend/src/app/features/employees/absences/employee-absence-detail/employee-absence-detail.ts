import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Absence, AbsenceDurationType, AbsenceType, UpdateAbsenceRequest } from '@app/core/models/absence.model';
import { AbsenceService } from '@app/core/services/absence.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { EmployeeService } from '@app/core/services/employee.service';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { DialogModule } from 'primeng/dialog';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TextareaModule } from 'primeng/textarea';
import { TooltipModule } from 'primeng/tooltip';

@Component({
  selector: 'app-employee-absence-detail',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    ButtonModule,
    TableModule,
    TagModule,
    DialogModule,
    TextareaModule,
    CardModule,
    TooltipModule
  ],
  templateUrl: './employee-absence-detail.html',
  styleUrl: './employee-absence-detail.css'
})
export class EmployeeAbsenceDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private absenceService = inject(AbsenceService);
  private employeeService = inject(EmployeeService);
  private contextService = inject(CompanyContextService);

  employeeId = signal<number>(0);
  employeeName = signal<string>('');
  absences = signal<Absence[]>([]);
  isLoading = signal(false);

  readonly routePrefix = signal('/app');

  stats = signal({
    totalAbsences: 0,
    totalDays: 0
  });

  // Reject dialog
  showRejectDialog = signal(false);
  selectedAbsenceId = signal<number | null>(null);
  rejectReason = signal('');

  // Delete dialog
  showDeleteDialog = signal(false);
  selectedAbsenceIdForDelete = signal<number | null>(null);

  // Edit dialog
  showEditDialog = signal(false);
  editedAbsence = signal<Absence | null>(null);
  editForm = signal<UpdateAbsenceRequest & { absenceDate?: string }>({});

  ngOnInit() {
    // Determine route prefix
    if (this.contextService.isExpertMode()) {
      this.routePrefix.set('/expert');
    }

    this.route.params.subscribe(params => {
      const id = params['id'];
      if (id) {
        const employeeId = Number(id);
        this.employeeId.set(employeeId);
        this.loadEmployeeInfo(employeeId);
        this.loadAbsences(employeeId);
      }
    });
  }

  loadEmployeeInfo(id: number) {
    this.employeeService.getEmployeeById(String(id)).subscribe({
      next: (employee) => {
        this.employeeName.set(`${employee.firstName} ${employee.lastName}`);
      },
      error: (err) => alert('Failed to load employee info')
    });
  }

  loadAbsences(employeeId: number) {
    this.isLoading.set(true);

    this.absenceService.getEmployeeAbsences(String(employeeId)).subscribe({
      next: (response) => {
        this.absences.set(response.absences);
        this.stats.set(response.stats ?? { totalAbsences: 0, totalDays: 0 });
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
      }
    });
  }

  goBack() {
    this.router.navigate([`${this.routePrefix()}/absences/hr`]);
  }

  submitAbsence(absenceId: number) {
    this.absenceService.submitAbsence(absenceId).subscribe({
      next: () => {
        this.loadAbsences(this.employeeId());
      },
      error: (err) => {
        alert('Failed to submit absence');
      }
    });
  }

  approveAbsence(absenceId: number) {
    this.absenceService.approveAbsence(absenceId).subscribe({
      next: () => {
        // Reload absences after approval
        this.loadAbsences(this.employeeId());
      },
      error: (err) => {
        alert('Failed to approve absence');
      }
    });
  }

  rejectAbsence(absenceId: number) {
    this.selectedAbsenceId.set(absenceId);
    this.rejectReason.set('');
    this.showRejectDialog.set(true);
  }

  confirmRejection() {
    const absenceId = this.selectedAbsenceId();
    const reason = this.rejectReason();

    if (!absenceId) return;

    this.absenceService.rejectAbsence(absenceId, reason).subscribe({
      next: () => {
        this.showRejectDialog.set(false);
        this.selectedAbsenceId.set(null);
        this.rejectReason.set('');
        // Reload absences after rejection
        this.loadAbsences(this.employeeId());
      },
      error: (err) => {
        alert('Failed to reject absence');
      }
    });
  }

  cancelRejection() {
    this.showRejectDialog.set(false);
    this.selectedAbsenceId.set(null);
    this.rejectReason.set('');
  }

  canDelete(status: string | undefined): boolean {
    return status === 'Draft' || status === 'Rejected' || status === 'Cancelled' || status === 'Expired';
  }

  openDeleteDialog(absenceId: number) {
    this.selectedAbsenceIdForDelete.set(absenceId);
    this.showDeleteDialog.set(true);
  }

  cancelDelete() {
    this.showDeleteDialog.set(false);
    this.selectedAbsenceIdForDelete.set(null);
  }

  confirmDelete() {
    const id = this.selectedAbsenceIdForDelete();
    if (id == null) return;
    this.absenceService.deleteAbsence(id).subscribe({
      next: () => {
        this.cancelDelete();
        this.loadAbsences(this.employeeId());
      },
      error: (err) => alert('Failed to delete absence')
    });
  }

  openEditDialog(absence: Absence) {
    if (absence.status !== 'Draft') return;
    this.editedAbsence.set(absence);
    const dateStr = absence.absenceDate && absence.absenceDate.length >= 10
      ? absence.absenceDate.slice(0, 10)
      : '';
    this.editForm.set({
      absenceDate: dateStr,
      absenceType: absence.absenceType,
      durationType: absence.durationType,
      isMorning: absence.isMorning,
      startTime: absence.startTime ?? undefined,
      endTime: absence.endTime ?? undefined,
      reason: absence.reason ?? undefined
    });
    this.showEditDialog.set(true);
  }

  cancelEdit() {
    this.showEditDialog.set(false);
    this.editedAbsence.set(null);
    this.editForm.set({});
  }

  updateEditForm(partial: Partial<UpdateAbsenceRequest & { absenceDate?: string }>) {
    this.editForm.update(prev => ({ ...prev, ...partial }));
  }

  onEditDurationChange(durationType: AbsenceDurationType) {
    this.updateEditForm({ durationType });
    this.editForm.update(prev => {
      const next = { ...prev, durationType };
      if (durationType !== 'HalfDay') delete (next as Partial<UpdateAbsenceRequest>).isMorning;
      if (durationType !== 'Hourly') {
        delete (next as Partial<UpdateAbsenceRequest>).startTime;
        delete (next as Partial<UpdateAbsenceRequest>).endTime;
      }
      return next;
    });
  }

  canSaveEdit(): boolean {
    const f = this.editForm();
    return !!(f.absenceDate && f.absenceType && f.durationType);
  }

  saveEdit() {
    const absence = this.editedAbsence();
    const f = this.editForm();
    if (!absence || !this.canSaveEdit()) return;
    const payload: UpdateAbsenceRequest = {
      absenceDate: f.absenceDate,
      absenceType: f.absenceType as AbsenceType,
      durationType: f.durationType,
      reason: f.reason || undefined
    };
    if (f.durationType === 'HalfDay' && f.isMorning !== undefined) payload.isMorning = f.isMorning;
    if (f.durationType === 'Hourly') {
      payload.startTime = f.startTime;
      payload.endTime = f.endTime;
    }
    this.absenceService.updateAbsence(absence.id, payload).subscribe({
      next: () => {
        this.cancelEdit();
        this.loadAbsences(this.employeeId());
      },
      error: (err) => alert('Failed to update absence')
    });
  }

  absenceTypeOptions(): { label: string; value: AbsenceType }[] {
    return [
      { label: 'absences.types.ANNUAL_LEAVE', value: 'ANNUAL_LEAVE' },
      { label: 'absences.types.sick', value: 'SICK' },
      { label: 'absences.types.maternity', value: 'MATERNITY' },
      { label: 'absences.types.paternity', value: 'PATERNITY' },
      { label: 'absences.types.unpaid', value: 'UNPAID' },
      { label: 'absences.types.mission', value: 'MISSION' },
      { label: 'absences.types.training', value: 'TRAINING' },
      { label: 'absences.types.justified', value: 'JUSTIFIED' },
      { label: 'absences.types.unjustified', value: 'UNJUSTIFIED' },
      { label: 'absences.types.accidentWork', value: 'ACCIDENT_WORK' },
      { label: 'absences.types.exceptional', value: 'EXCEPTIONAL' },
      { label: 'absences.types.religious', value: 'RELIGIOUS' }
    ];
  }

  durationTypeOptions(): { label: string; value: AbsenceDurationType }[] {
    return [
      { label: 'absences.durations.fullDay', value: 'FullDay' },
      { label: 'absences.durations.halfDayMorning', value: 'HalfDay' },
      { label: 'absences.durations.hourly', value: 'Hourly' }
    ];
  }

  getAbsenceTypeLabel(type: AbsenceType): string {
    const typeMap: Partial<Record<AbsenceType, string>> = {
      'ANNUAL_LEAVE': 'absences.types.ANNUAL_LEAVE',
      'SICK': 'absences.types.sick',
      'MATERNITY': 'absences.types.maternity',
      'PATERNITY': 'absences.types.paternity',
      'UNPAID': 'absences.types.unpaid',
      'MISSION': 'absences.types.mission',
      'TRAINING': 'absences.types.training',
      'JUSTIFIED': 'absences.types.justified',
      'UNJUSTIFIED': 'absences.types.unjustified',
      'ACCIDENT_WORK': 'absences.types.accidentWork',
      'EXCEPTIONAL': 'absences.types.exceptional',
      'RELIGIOUS': 'absences.types.religious'
    };
    return typeMap[type] || type;
  }

  formatTime(time: string | undefined): string {
    if (!time) return '';
    const parts = time.split(':');
    return `${parts[0]}:${parts[1]}`;
  }

  getDurationLabel(absence: Absence): string {
    if (absence.durationType === 'FullDay') {
      return 'absences.durations.fullDay';
    } else if (absence.durationType === 'HalfDay') {
      return absence.isMorning ? 'absences.durations.halfDayMorning' : 'absences.durations.halfDayAfternoon';
    } else if (absence.durationType === 'Hourly' && absence.startTime && absence.endTime) {
      // Calculate hours
      const start = absence.startTime.split(':');
      const end = absence.endTime.split(':');
      const startMinutes = parseInt(start[0]) * 60 + parseInt(start[1]);
      const endMinutes = parseInt(end[0]) * 60 + parseInt(end[1]);
      const durationMinutes = endMinutes - startMinutes;
      const hours = Math.floor(durationMinutes / 60);
      const minutes = durationMinutes % 60;

      if (minutes > 0) {
        return `${hours}h ${minutes}min`;
      }
      return `${hours}h`;
    }
    return '-';
  }

  getAbsenceTypeSeverity(type: AbsenceType): 'success' | 'warn' | 'danger' | 'info' {
    switch (type) {
      case 'JUSTIFIED':
      case 'ANNUAL_LEAVE':
      case 'MATERNITY':
      case 'PATERNITY':
        return 'success';
      case 'SICK':
      case 'MISSION':
      case 'TRAINING':
      case 'RELIGIOUS':
      case 'EXCEPTIONAL':
        return 'info';
      case 'UNPAID':
      case 'ACCIDENT_WORK':
        return 'warn';
      case 'UNJUSTIFIED':
        return 'danger';
      default:
        return 'info';
    }
  }
}
