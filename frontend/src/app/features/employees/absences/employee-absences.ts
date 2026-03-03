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
import { Absence, AbsenceType, AbsenceDurationType, CreateAbsenceRequest } from '@app/core/models/absence.model';

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

  absences = signal<Absence[]>([]);
  isLoading = signal(false);
  showCreateDialog = signal(false);
  showDetailDialog = signal(false);
  selectedAbsence = signal<Absence | null>(null);
  
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

  absenceTypes: Array<{ label: string; value: AbsenceType }> = [];

  durationTypes: Array<{ label: string; value: AbsenceDurationType }> = [];

  halfDayOptions: Array<{ label: string; value: boolean }> = [];

  ngOnInit() {
    this.loadAbsences();

    // Initialize translated labels for select options so the UI shows localized text
    this.absenceTypes = [
      { label: this.translate.instant('absences.types.annual_leave'), value: 'ANNUAL_LEAVE' },
      { label: this.translate.instant('absences.types.sick'), value: 'SICK' },
      { label: this.translate.instant('absences.types.maternity'), value: 'MATERNITY' },
      { label: this.translate.instant('absences.types.paternity'), value: 'PATERNITY' },
      { label: this.translate.instant('absences.types.unpaid'), value: 'UNPAID' },
      { label: this.translate.instant('absences.types.mission'), value: 'MISSION' },
      { label: this.translate.instant('absences.types.training'), value: 'TRAINING' },
      { label: this.translate.instant('absences.types.justified'), value: 'JUSTIFIED' },
      { label: this.translate.instant('absences.types.unjustified'), value: 'UNJUSTIFIED' },
      { label: this.translate.instant('absences.types.accident_work'), value: 'ACCIDENT_WORK' },
      { label: this.translate.instant('absences.types.exceptional'), value: 'EXCEPTIONAL' },
      { label: this.translate.instant('absences.types.religious'), value: 'RELIGIOUS' }
    ];

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

  loadAbsences() {
    this.isLoading.set(true);
    
    // Use getCurrentEmployee to get the real employeeId from Employees table
    this.employeeService.getCurrentEmployee().subscribe({
      next: (employee) => {
        const employeeId = employee.id;
        console.log('[EmployeeAbsences] Loading absences for employeeId:', employeeId);
        
        this.absenceService.getEmployeeAbsences(String(employeeId)).subscribe({
          next: (response) => {
            this.absences.set(response?.absences ?? []);
            this.stats.set(response?.stats ?? { totalAbsences: 0, totalDays: 0 });
            this.isLoading.set(false);
          },
          error: (err) => {
            console.error('Failed to load absences', err);
            this.isLoading.set(false);
          }
        });
      },
      error: (err) => {
        console.error('Failed to get current employee', err);
        this.isLoading.set(false);
      }
    });
  }

  openCreateDialog() {
    const user = this.authService.currentUser();
    const userId = user?.id;
    
    if (!userId) {
      console.error('[EmployeeAbsences] No userId found');
      alert('Erreur: Impossible de déterminer votre identité.');
      return;
    }
    
    console.log('[EmployeeAbsences] Getting current employee for userId:', userId);
    
    // Use the new getCurrentEmployee endpoint - much more efficient!
    this.employeeService.getCurrentEmployee().subscribe({
      next: (employee) => {
        const employeeId = Number(employee.id);
        console.log('[EmployeeAbsences] Current employee:', {
          employeeId: employeeId,
          name: `${employee.firstName} ${employee.lastName}`
        });
        this.initializeNewAbsence(employeeId);
        this.showCreateDialog.set(true);
      },
      error: (err) => {
        console.error('[EmployeeAbsences] Failed to get current employee:', err);
        if (err.status === 404) {
          alert(`Erreur: Aucun employé trouvé pour votre compte.\n\nVeuillez contacter l'administrateur pour créer votre fiche employé (lier UserId=${userId} à un employé).`);
        } else {
          alert('Erreur lors de la récupération de vos informations employé.');
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
      endTime: '17:00'
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
        console.error('Failed to load absence details', err);
        // Fallback to list data if detail fetch fails
        this.selectedAbsence.set(absence);
        this.showDetailDialog.set(true);
      }
    });
  }

  cancelAbsence(absenceId: number) {
    if (!confirm(this.translate.instant('absences.confirmCancel'))) {
      return;
    }

    this.absenceService.cancelAbsence(absenceId).subscribe({
      next: () => {
        console.log('[EmployeeAbsences] Absence cancelled successfully');
        this.loadAbsences();
      },
      error: (err) => {
        console.error('[EmployeeAbsences] Failed to cancel absence:', err);
        const errorMessage = err?.error?.Message || err?.error?.message || 'Erreur lors de l\'annulation de l\'absence';
        alert(`Erreur: ${errorMessage}`);
      }
    });
  }

  canCancelAbsence(absence: Absence): boolean {
    // Peut annuler si statut est Submitted ou Approved
    return absence.status === 'Submitted' || absence.status === 'Approved';
  }

  getStatusLabel(status?: string): string {
    if (!status) return 'absences.status.unknown';
    const statusMap: Record<string, string> = {
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
      console.error('[EmployeeAbsences] Invalid employeeId:', request.employeeId);
      alert('Erreur: Identifiant d\'employé invalide.');
      return;
    }
    
    if (!request.absenceDate) {
      alert('Veuillez sélectionner une date d\'absence.');
      return;
    }

    // Validate based on duration type
    if (request.durationType === 'HalfDay' && request.isMorning === undefined) {
      alert('Veuillez sélectionner matin ou après-midi.');
      return;
    }
    if (request.durationType === 'Hourly' && (!request.startTime || !request.endTime)) {
      alert('Veuillez sélectionner l\'heure de début et de fin.');
      return;
    }

    console.log('[EmployeeAbsences] Submitting absence request:', request);
    console.log('[EmployeeAbsences] EmployeeId being sent to backend:', request.employeeId);

    this.absenceService.createAbsence(request).subscribe({
      next: () => {
        this.showCreateDialog.set(false);
        this.loadAbsences();
      },
      error: (err) => {
        console.error('Failed to create absence request', err);
        console.error('Error response:', err?.error);
        console.error('Request that was sent:', request);
        
        if (err?.error?.errors) {
          console.error('Validation errors:', JSON.stringify(err.error.errors, null, 2));
          // Log each validation error for clarity
          Object.keys(err.error.errors).forEach(key => {
            console.error(`Field '${key}':`, err.error.errors[key]);
          });
        }
        
        // Show user-friendly error message
        let errorMessage = err?.error?.Message || err?.error?.message || 'Une erreur est survenue lors de la création de l\'absence';
        
        // Special handling for employee not found error
        if (err?.status === 404 && errorMessage.includes('Employé non trouvé')) {
          errorMessage = `Employé non trouvé (ID: ${request.employeeId}). Votre compte utilisateur n'est pas lié à un employé valide dans le système. Veuillez contacter l'administrateur pour créer votre fiche employé.`;
        }
        
        alert(`Erreur: ${errorMessage}`);
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
    return typeMap[type] || type;
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

    this.newAbsence.update(current => ({ ...current, [field]: normalized }));
  }

  get hoursList(): string[] {
    return this.absenceService.hoursList;
  }

  get isRH(): boolean {
    return this.authService.isRH();
  }

  approveAbsence(absenceId: number) {
    console.debug('[EmployeeAbsences] approveAbsence', absenceId);
    // TODO: integrate backend approval API; currently simulate by closing dialog and reloading list
    this.showDetailDialog.set(false);
    this.loadAbsences();
  }

  rejectAbsence(absenceId: number) {
    console.debug('[EmployeeAbsences] rejectAbsence', absenceId);
    // TODO: integrate backend rejection API; currently simulate by closing dialog and reloading list
    this.showDetailDialog.set(false);
    this.loadAbsences();
  }
}

