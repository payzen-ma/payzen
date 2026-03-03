import { Component, OnInit, signal, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { TextareaModule } from 'primeng/textarea';
import { CardModule } from 'primeng/card';
import { TooltipModule } from 'primeng/tooltip';
import { AbsenceService } from '@app/core/services/absence.service';
import { EmployeeService } from '@app/core/services/employee.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { Absence, AbsenceType, AbsenceDurationType } from '@app/core/models/absence.model';

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
      error: (err) => console.error('Failed to load employee info', err)
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
        console.error('Failed to load absences', err);
        this.isLoading.set(false);
      }
    });
  }

  goBack() {
    this.router.navigate([`${this.routePrefix()}/absences/hr`]);
  }

  approveAbsence(absenceId: number) {
    this.absenceService.approveAbsence(absenceId).subscribe({
      next: () => {
        // Reload absences after approval
        this.loadAbsences(this.employeeId());
      },
      error: (err) => {
        console.error('Failed to approve absence', err);
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
        console.error('Failed to reject absence', err);
      }
    });
  }

  cancelRejection() {
    this.showRejectDialog.set(false);
    this.selectedAbsenceId.set(null);
    this.rejectReason.set('');
  }

  getAbsenceTypeLabel(type: AbsenceType): string {
    const typeMap: Partial<Record<AbsenceType, string>> = {
      'ANNUAL_LEAVE': 'absences.types.annual_leave',
      'SICK': 'absences.types.sick',
      'MATERNITY': 'absences.types.maternity',
      'PATERNITY': 'absences.types.paternity',
      'UNPAID': 'absences.types.unpaid',
      'MISSION': 'absences.types.mission',
      'TRAINING': 'absences.types.training',
      'JUSTIFIED': 'absences.types.justified',
      'UNJUSTIFIED': 'absences.types.unjustified',
      'ACCIDENT_WORK': 'absences.types.accident_work',
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
