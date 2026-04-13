import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { DialogModule } from 'primeng/dialog';
import { TextareaModule } from 'primeng/textarea';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { FileUploadModule } from 'primeng/fileupload';
import { CardModule } from 'primeng/card';
import { TooltipModule } from 'primeng/tooltip';
import { AbsenceService } from '@app/core/services/absence.service';
import { AuthService } from '@app/core/services/auth.service';
import { EmployeeService } from '@app/core/services/employee.service';
import { LeaveTypeLegalRuleService } from '@app/core/services/leave-type-legal-rule.service';
import { Absence, AbsenceType, AbsenceDurationType, CreateAbsenceRequest } from '@app/core/models/absence.model';
import { LeaveTypeLegalRule } from '@app/core/models';

@Component({
  selector: 'app-employee-absences',
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
    SelectModule,
    DatePickerModule,
    FileUploadModule,
    CardModule,
    TooltipModule
  ],
  templateUrl: './employee-absences.html',
  styleUrl: './employee-absences.css'
})

export class EmployeeAbsencesComponent implements OnInit {
  private absenceService = inject(AbsenceService);
  private authService = inject(AuthService);
  private translate = inject(TranslateService);
  private employeeService = inject(EmployeeService);
  private leaveTypeLegalRuleService = inject(LeaveTypeLegalRuleService);

  absences = signal<Absence[]>([]);
  isLoading = signal(false);
  showCreateDialog = signal(false);
  showDetailDialog = signal(false);
  selectedAbsence = signal<Absence | null>(null);
  // Cancel confirmation dialog
  showCancelDialog = signal(false);
  cancelTargetId = signal<number | null>(null);
  // Cancel error dialog
  showCancelErrorDialog = signal(false);
  cancelErrorMessage = signal<string | null>(null);
  // General error dialog
  showErrorDialog = signal(false);
  errorMessage = signal<string | null>(null);

  stats = signal({
    totalAbsences: 0,
    totalDays: 0
  });

  // Create form
  newAbsence = signal<CreateAbsenceRequest>({
    employeeId: 0,
    absenceDate: '',
    durationType: 'FullDay',
    absenceType: 'JUSTIFIED',
    reason: ''
  });

  newAbsenceDate = computed(() => {
    const rawValue = this.newAbsence().absenceDate as unknown;
    if (!rawValue) return null;
    if (rawValue instanceof Date) return rawValue;
    if (typeof rawValue === 'string') {
      const parsed = new Date(rawValue);
      return Number.isNaN(parsed.getTime()) ? null : parsed;
    }
    return null;
  });

  legalLeaveRules = signal<LeaveTypeLegalRule[]>([]);

  durationTypes: Array<{ label: string; value: AbsenceDurationType }> = [];

  halfDayOptions: Array<{ label: string; value: boolean }> = [];

  // Computed: Available absence types based on duration type
  availableAbsenceTypes = computed(() => {
    const durationType = this.newAbsence().durationType;

    // For HalfDay or Hourly: only justified/unjustified
    if (durationType === 'HalfDay' || durationType === 'Hourly') {
      return [
        { label: this.translate.instant('absences.types.justified'), value: 'JUSTIFIED' as AbsenceType },
        { label: this.translate.instant('absences.types.unjustified'), value: 'UNJUSTIFIED' as AbsenceType }
      ];
    }

    // For FullDay: legal leave rules + justified/unjustified
    const legalRules = this.legalLeaveRules().map(rule => ({
      label: rule.description,
      value: rule.eventCaseCode as AbsenceType
    }));

    return [
      ...legalRules,
      { label: this.translate.instant('absences.types.justified'), value: 'JUSTIFIED' as AbsenceType },
      { label: this.translate.instant('absences.types.unjustified'), value: 'UNJUSTIFIED' as AbsenceType }
    ];
  });

  ngOnInit() {
    this.loadAbsences();
    this.loadLegalLeaveRules();

    // Initialize translated labels for select options so the UI shows localized text
    this.durationTypes = [
      { label: this.translate.instant('absences.durations.fullDay'), value: 'FullDay' },
      { label: this.translate.instant('absences.durations.halfDay'), value: 'HalfDay' },
      { label: this.translate.instant('absences.durations.hourly'), value: 'Hourly' }
    ];

    this.halfDayOptions = [
      { label: this.translate.instant('absences.morning'), value: true },
      { label: this.translate.instant('absences.afternoon'), value: false }
    ];
  }

  loadLegalLeaveRules() {
    this.leaveTypeLegalRuleService.getAll().subscribe({
      next: (rules) => {
        this.legalLeaveRules.set(rules);
      },
      error: (err) => {
        // Si le chargement échoue, on continue sans les règles légales
        this.legalLeaveRules.set([]);
      }
    });
  }

  loadAbsences() {
    this.isLoading.set(true);

    // Use getCurrentEmployee to get the real employeeId from Employees table
    this.employeeService.getCurrentEmployee().subscribe({
      next: (employee) => {
        const employeeId = employee.id;

        this.absenceService.getEmployeeAbsences(String(employeeId)).subscribe({
          next: (response) => {
            this.absences.set(response?.absences ?? []);
            this.stats.set(response?.stats ?? { totalAbsences: 0, totalDays: 0 });
            this.isLoading.set(false);
          },
          error: (err) => {
            this.isLoading.set(false);
          }
        });
      },
      error: (err) => {
        this.isLoading.set(false);
      }
    });
  }

  openCreateDialog() {
    const user = this.authService.currentUser();
    const userId = user?.id;

    if (!userId) {
      this.showError('Erreur: Impossible de déterminer votre identité.');
      return;
    }


    // Use the new getCurrentEmployee endpoint - much more efficient!
    this.employeeService.getCurrentEmployee().subscribe({
      next: (employee) => {
        const employeeId = Number(employee.id);

        this.initializeNewAbsence(employeeId);
        this.showCreateDialog.set(true);
      },
      error: (err) => {
        if (err.status === 404) {
          this.showError(`Erreur: Aucun employé trouvé pour votre compte.\n\nVeuillez contacter l'administrateur pour créer votre fiche employé (lier UserId=${userId} à un employé).`);
        } else {
          this.showError('Erreur lors de la récupération de vos informations employé.');
        }
      }
    });
  }

  private initializeNewAbsence(employeeId: number) {
    this.newAbsence.set({
      employeeId: employeeId,
      absenceDate: '',
      durationType: 'FullDay',
      absenceType: 'JUSTIFIED',
      reason: '',
      startTime: '08:00',
      endTime: '17:00',
      useLeaveBalance: false
    });
  }

  openDetailDialog(absence: Absence) {
    // Fetch full details to ensure all fields (createdBy, decisionBy, etc.) are populated
    this.absenceService.getAbsenceById(absence.id).subscribe({
      next: (fullAbsence) => {
        this.selectedAbsence.set(fullAbsence);
        this.showDetailDialog.set(true);
      },
      error: (err) => {
        // Fallback to list data if detail fetch fails
        this.selectedAbsence.set(absence);
        this.showDetailDialog.set(true);
      }
    });
  }

  cancelAbsence(absenceId: number) {
    // Open confirmation dialog instead of blocking confirm()
    this.cancelTargetId.set(absenceId);
    this.showCancelDialog.set(true);
  }

  confirmCancel() {
    const id = this.cancelTargetId();
    if (!id) return;

    this.absenceService.cancelAbsence(id).subscribe({
      next: () => {
        this.showCancelDialog.set(false);
        this.cancelTargetId.set(null);
        this.loadAbsences();
      },
      error: (err) => {
        const errorMessage = err?.error?.Message || err?.error?.message || 'Erreur lors de l\'annulation de l\'absence';
        this.cancelErrorMessage.set(errorMessage);
        this.showCancelDialog.set(false);
        this.showCancelErrorDialog.set(true);
      }
    });
  }

  closeCancelDialog() {
    this.showCancelDialog.set(false);
    this.cancelTargetId.set(null);
  }

  closeCancelErrorDialog() {
    this.showCancelErrorDialog.set(false);
    this.cancelErrorMessage.set(null);
  }

  showError(message: string) {
    this.errorMessage.set(message);
    this.showErrorDialog.set(true);
  }

  closeErrorDialog() {
    this.showErrorDialog.set(false);
    this.errorMessage.set(null);
  }

  /**
   * Submit a draft absence (change status from Draft to Submitted)
   */
  submitAbsence(absenceId: number) {
    this.absenceService.submitAbsence(absenceId).subscribe({
      next: () => {
        this.loadAbsences();
      },
      error: (err) => {
        const errorMessage = err?.error?.Message || err?.error?.message || 'Erreur lors de la soumission de l\'absence';
        this.cancelErrorMessage.set(errorMessage);
        this.showCancelErrorDialog.set(true);
      }
    });
  }

  canCancelAbsence(absence: Absence): boolean {
    // Peut annuler si statut est Draft, Submitted ou Approved
    return absence.status === 'Draft' || absence.status === 'Submitted' || absence.status === 'Approved';
  }

  getStatusLabel(status?: string): string {
    if (!status) return 'absences.status.unknown';
    const statusMap: Record<string, string> = {
      'Draft': 'absences.status.draft',
      'Submitted': 'absences.status.submitted',
      'Approved': 'absences.status.approved',
      'Rejected': 'absences.status.rejected',
      'Cancelled': 'absences.status.cancelled',
      'Expired': 'absences.status.expired'
    };
    return statusMap[status] || status;
  }

  getStatusSeverity(status?: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    if (!status) return 'secondary';
    const severityMap: Record<string, 'success' | 'info' | 'warn' | 'danger' | 'secondary'> = {
      'Draft': 'secondary',
      'Submitted': 'info',
      'Approved': 'success',
      'Rejected': 'danger',
      'Cancelled': 'secondary',
      'Expired': 'warn'
    };
    return severityMap[status] || 'secondary';
  }

  submitAbsenceRequest() {
    const request = this.newAbsence();

    // Validate employee ID
    if (!request.employeeId || request.employeeId === 0) {
      this.showError('Erreur: Identifiant d\'employé invalide.');
      return;
    }

    if (!request.absenceDate) {
      this.showError('Veuillez sélectionner une date d\'absence.');
      return;
    }

    // Validate based on duration type
    if (request.durationType === 'HalfDay' && request.isMorning === undefined) {
      this.showError('Veuillez sélectionner matin ou après-midi.');
      return;
    }
    if (request.durationType === 'Hourly' && (!request.startTime || !request.endTime)) {
      this.showError('Veuillez sélectionner l\'heure de début et de fin.');
      return;
    }

    // Ensure we explicitly request creation as Draft (status = 0)
    const createRequest = { ...request, status: 0 } as any;

    this.absenceService.createAbsence(createRequest).subscribe({
      next: (response) => {
        // Created as draft by backend; close dialog and refresh list
        this.showCreateDialog.set(false);
        this.loadAbsences();
      },
      error: (err) => {

        if (err?.error?.errors) {
          // Log each validation error for clarity
          Object.keys(err.error.errors).forEach(key => {
          });
        }

        // Show user-friendly error message
        let errorMessage = err?.error?.Message || err?.error?.message || 'Une erreur est survenue lors de la création de l\'absence';

        // Special handling for employee not found error
        if (err?.status === 404 && errorMessage.includes('Employé non trouvé')) {
          errorMessage = `Employé non trouvé (ID: ${request.employeeId}). Votre compte utilisateur n'est pas lié à un employé valide dans le système. Veuillez contacter l'administrateur pour créer votre fiche employé.`;
        }

        this.showError(errorMessage);
      }
    });
  }

  getAbsenceTypeLabel(type: AbsenceType): string {
    const typeMap: Partial<Record<AbsenceType, string>> = {
      'JUSTIFIED': 'absences.types.justified',
      'UNJUSTIFIED': 'absences.types.unjustified',
      'SICK': 'absences.types.sick',
      'MISSION': 'absences.types.mission'
    };

    // If type is in predefined map, return translation key
    if (typeMap[type]) {
      return typeMap[type]!;
    }

    // Otherwise, check if it's a legal leave rule
    const legalRule = this.legalLeaveRules().find(rule => rule.eventCaseCode === type);
    if (legalRule) {
      return legalRule.description;
    }

    // Fallback to the type itself
    return type;
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
        return 'success';
      case 'SICK':
        return 'info';
      case 'MISSION':
        return 'info';
      case 'UNJUSTIFIED':
        return 'danger';
      default:
        return 'info';
    }
  }

  updateField(field: keyof CreateAbsenceRequest, value: any) {
    let normalized = value;
    if (field === 'absenceDate') {
      if (value instanceof Date) {
        const y = value.getFullYear();
        const m = String(value.getMonth() + 1).padStart(2, '0');
        const d = String(value.getDate()).padStart(2, '0');
        normalized = `${y}-${m}-${d}`;
      } else if (typeof value === 'string' && /^\d{2}\/\d{2}\/\d{4}$/.test(value)) {
        const [dd, mm, yyyy] = value.split('/');
        normalized = `${yyyy}-${mm.padStart(2, '0')}-${dd.padStart(2, '0')}`;
      }
    }

    this.newAbsence.update(current => {
      let updated = { ...current, [field]: normalized };
      // When duration type changes, reset absence type if current type is not available
      if (field === 'durationType') {
        const newDurationType = normalized as AbsenceDurationType;
        const currentAbsenceType = current.absenceType;
        const isHalfDayOrHourly = newDurationType === 'HalfDay' || newDurationType === 'Hourly';
        // Only JUSTIFIED/UNJUSTIFIED allowed
        if (isHalfDayOrHourly) {
          if (currentAbsenceType !== 'JUSTIFIED' && currentAbsenceType !== 'UNJUSTIFIED') {
            updated.absenceType = 'JUSTIFIED';
          }
        }
        // Reset useLeaveBalance if not justified/unjustified
        if (updated.absenceType !== 'JUSTIFIED' && updated.absenceType !== 'UNJUSTIFIED') {
          updated.useLeaveBalance = false;
        }
      }
      // When absenceType changes, reset useLeaveBalance if not justified/unjustified
      if (field === 'absenceType') {
        if (normalized !== 'JUSTIFIED' && normalized !== 'UNJUSTIFIED') {
          updated.useLeaveBalance = false;
        }
      }
      return updated;
    });
  }

  get hoursList(): string[] {
    return this.absenceService.hoursList;
  }

  get isRH(): boolean {
    return this.authService.isRH();
  }

  approveAbsence(absenceId: number) {
    // TODO: integrate backend approval API; currently simulate by closing dialog and reloading list
    this.showDetailDialog.set(false);
    this.loadAbsences();
  }

  rejectAbsence(absenceId: number) {
    // TODO: integrate backend rejection API; currently simulate by closing dialog and reloading list
    this.showDetailDialog.set(false);
    this.loadAbsences();
  }
}

