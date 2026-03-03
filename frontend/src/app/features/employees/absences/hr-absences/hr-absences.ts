import { Component, OnInit, signal, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { DialogModule } from 'primeng/dialog';
import { DatePickerModule } from 'primeng/datepicker';
import { TextareaModule } from 'primeng/textarea';
import { CardModule } from 'primeng/card';
import { TooltipModule } from 'primeng/tooltip';
import { RippleModule } from 'primeng/ripple';
import { AbsenceService } from '@app/core/services/absence.service';
import { EmployeeService } from '@app/core/services/employee.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { Absence } from '@app/core/models/absence.model';
import { AbsenceType, AbsenceDurationType, CreateAbsenceRequest } from '@app/core/models/absence.model';
import { TranslateService } from '@ngx-translate/core';

interface EmployeeAbsenceSummary {
  employeeId: number;
  employeeName: string;
  totalAbsences: number;
  totalDays: number;
  absences?: Absence[]; // Store detailed absences
}

interface GrantAbsenceRequest extends CreateAbsenceRequest {
  employeeName?: string;
}

@Component({
  selector: 'app-hr-absences',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TranslateModule,
    ButtonModule,
    TableModule,
    TagModule,
    InputTextModule,
    SelectModule,
    DialogModule,
    DatePickerModule,
    TextareaModule,
    CardModule,
    TooltipModule,
    RippleModule
  ],
  templateUrl: './hr-absences.html',
  styleUrl: './hr-absences.css'
})
export class HrAbsencesComponent implements OnInit {
  private absenceService = inject(AbsenceService);
  private employeeService = inject(EmployeeService);
  private router = inject(Router);
  private contextService = inject(CompanyContextService);
  private translate = inject(TranslateService);

  employees = signal<EmployeeAbsenceSummary[]>([]);
  pendingAbsences = signal<Absence[]>([]);
  isLoading = signal(false);
  searchQuery = signal('');

  // Pending absences filters
  pendingEmployeeFilter = signal('');
  pendingTypeFilter = signal<AbsenceType | ''>('');
  pendingDurationFilter = signal<AbsenceDurationType | ''>('');

  // Grant absence dialog state
  showGrantDialog = signal(false);
  grantRequest = signal<GrantAbsenceRequest | null>(null);

  // Reject dialog
  showRejectDialog = signal(false);
  selectedAbsenceId = signal<number | null>(null);
  rejectReason = signal('');

  // Computed signals for form fields
  grantAbsenceDate = computed(() => {
    const rawValue = this.grantRequest()?.absenceDate as unknown;
    if (!rawValue) return null;
    if (rawValue instanceof Date) return rawValue;
    if (typeof rawValue === 'string') {
      const parsed = new Date(rawValue);
      return Number.isNaN(parsed.getTime()) ? null : parsed;
    }
    return null;
  });
  grantAbsenceType = computed(() => this.grantRequest()?.absenceType ?? 'JUSTIFIED');
  grantDurationType = computed(() => this.grantRequest()?.durationType ?? 'FullDay');
  grantIsMorning = computed(() => this.grantRequest()?.isMorning);
  grantStartTime = computed(() => this.grantRequest()?.startTime ?? '');
  grantEndTime = computed(() => this.grantRequest()?.endTime ?? '');
  grantReason = computed(() => this.grantRequest()?.reason ?? '');

  absenceTypes: Array<{ label: string; value: AbsenceType }> = [];
  durationTypes: Array<{ label: string; value: AbsenceDurationType }> = [];
  halfDayOptions: Array<{ label: string; value: boolean }> = [];

  // Filter options with 'All' option
  absenceTypeFilterOptions = computed(() => [
    { label: this.translate.instant('common.all'), value: '' },
    ...this.absenceTypes
  ]);
  durationTypeFilterOptions = computed(() => [
    { label: this.translate.instant('common.all'), value: '' },
    ...this.durationTypes
  ]);

  readonly routePrefix = signal('/app');

  companyStats = signal({
    totalAbsences: 0,
    totalDays: 0
  });

  // Filtered pending absences
  filteredPendingAbsences = computed(() => {
    let filtered = this.pendingAbsences();

    // Filter by employee name
    const nameFilter = this.pendingEmployeeFilter().toLowerCase().trim();
    if (nameFilter) {
      filtered = filtered.filter(a => 
        a.employeeName?.toLowerCase().includes(nameFilter)
      );
    }

    // Filter by absence type
    const typeFilter = this.pendingTypeFilter();
    if (typeFilter) {
      filtered = filtered.filter(a => a.absenceType === typeFilter);
    }

    // Filter by duration type
    const durationFilter = this.pendingDurationFilter();
    if (durationFilter) {
      filtered = filtered.filter(a => a.durationType === durationFilter);
    }

    return filtered;
  });

  ngOnInit() {
    // Determine route prefix based on context
    if (this.contextService.isExpertMode()) {
      this.routePrefix.set('/expert');
    }
    // init translated option labels (populate options before any dialog opens)
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

    // Now load employees (options are ready for immediate dialog use)
    this.loadEmployeesAbsences();
    this.loadPendingAbsences();
  }

  openGrantDialog(employeeId: number, employeeName: string) {
    this.grantRequest.set({
      employeeId: Number(employeeId),
      employeeName: employeeName,
      absenceDate: '',
      durationType: 'FullDay',
      absenceType: 'JUSTIFIED',
      reason: '',
      startTime: '08:00',
      endTime: '08:00'
    });
    this.showGrantDialog.set(true);
    console.debug('[HR] openGrantDialog', { employeeId, employeeName, grantRequest: this.grantRequest() });
  }

  submitGrant() {
    const req = this.grantRequest();
    if (!req) return;
    if (!req.absenceDate) return;

    // Validate based on duration type (same logic as employee flow)
    if (req.durationType === 'HalfDay' && req.isMorning === undefined) {
      return;
    }
    if (req.durationType === 'Hourly' && (!req.startTime || !req.endTime)) {
      return;
    }

    // Remove employeeName from the request as it's not part of the API
    const { employeeName, ...apiRequest } = req;

    console.log('[HR submitGrant] Original request:', req);
    console.log('[HR submitGrant] API request (before service):', apiRequest);

    this.absenceService.createAbsence(apiRequest).subscribe({
      next: () => {
        this.showGrantDialog.set(false);
        this.loadEmployeesAbsences();
        this.loadPendingAbsences();
      },
      error: (err) => {
        console.error('Failed to grant absence', err);
        console.error('Error response:', err?.error);
        if (err?.error?.errors) {
          console.error('Validation errors:', JSON.stringify(err.error.errors, null, 2));
          // Log each validation error for clarity
          Object.keys(err.error.errors).forEach(key => {
            console.error(`Field '${key}':`, err.error.errors[key]);
          });
        }
      }
    });
  }

  updateGrantField(field: keyof CreateAbsenceRequest, value: any) {
    // Normalize date/time formats for backend compatibility
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

    this.grantRequest.update(current => {
      const base = current ?? ({} as GrantAbsenceRequest);
      return { ...base, [field]: normalized } as GrantAbsenceRequest;
    });
    // log after update to help debug binding issues in the dialog
    console.debug('[HR] updateGrantField', field, value, this.grantRequest());
  }

  get hoursList(): string[] {
    return this.absenceService.hoursList;
  }

  get hoursOptions(): Array<{ label: string; value: string }> {
    return this.hoursList.map(h => ({ label: h, value: h }));
  }

  loadEmployeesAbsences() {
    this.isLoading.set(true);

    // Load all employees
    this.employeeService.getEmployees().subscribe({
      next: (response) => {
        const employees = response.employees || [];
        
        // For each employee, get their absence stats
        const summaries: EmployeeAbsenceSummary[] = employees.map(emp => ({
          employeeId: Number(emp.id),
          employeeName: `${emp.firstName} ${emp.lastName}`,
          totalAbsences: 0,
          totalDays: 0
        }));

        this.employees.set(summaries);

        // Load company-wide stats by fetching all employees' absences
        // In production, this should be a dedicated endpoint
        this.companyStats.set({ totalAbsences: 0, totalDays: 0 });

        // Load individual employee stats using the new endpoint
        employees.forEach((emp, index) => {
          this.absenceService.getEmployeeAbsences(String(emp.id)).subscribe({
            next: (response) => {
              this.employees.update(current => {
                const updated = [...current];
                updated[index] = {
                  ...updated[index],
                  totalAbsences: response.stats?.totalAbsences ?? 0,
                  totalDays: response.stats?.totalDays ?? 0,
                  absences: response.absences ?? [] // Store absences
                };
                return updated;
              });
            },
            error: (err) => console.error(`Failed to load stats for employee ${emp.id}`, err)
          });
        });

        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load employees', err);
        this.isLoading.set(false);
      }
    });
  }

  viewEmployeeAbsences(employeeId: number | string) {
    this.router.navigate([`${this.routePrefix()}/absences/employee`, String(employeeId)]);
  }

  approveEmployee(employeeId: number) {
    console.debug('[HR] approveEmployee', employeeId);
    // TODO: integrate with backend approval endpoint once available.
    // For now open the employee absences page for review and log action.
    this.viewEmployeeAbsences(String(employeeId));
  }

  rejectEmployee(employeeId: number) {
    console.debug('[HR] rejectEmployee', employeeId);
    // TODO: integrate with backend rejection endpoint once available.
    // For now open the employee absences page for review and log action.
    this.viewEmployeeAbsences(String(employeeId));
  }

  filteredEmployees() {
    const query = this.searchQuery().toLowerCase();
    if (!query) {
      return this.employees();
    }
    return this.employees().filter(emp => 
      emp.employeeName.toLowerCase().includes(query)
    );
  }

  getEmployeeInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase();
  }

  /**
   * Get translated absence type label
   */
  getAbsenceTypeLabel(type: AbsenceType): string {
    return `absences.types.${type.toLowerCase()}`;
  }

  /**
   * Format time string from HH:mm:ss to HH:mm
   */
  formatTime(time: string | undefined): string {
    if (!time) return '';
    // Remove seconds if present (HH:mm:ss -> HH:mm)
    return time.substring(0, 5);
  }



  /**
   * Load all pending absences for the company (HR view)
   */
  loadPendingAbsences() {
    const companyId = this.contextService.companyId();
    if (!companyId) return;

    this.absenceService.getAbsences({ status: 'Submitted', limit: 200 }).subscribe({
      next: (response) => {
        this.pendingAbsences.set(response.absences);
      },
      error: (err) => {
        console.error('Failed to load pending absences', err);
      }
    });
  }

  /**
   * Approve a pending absence
   */
  approveAbsence(absenceId: number) {
    this.absenceService.approveAbsence(absenceId).subscribe({
      next: () => {
        this.loadPendingAbsences();
        this.loadEmployeesAbsences();
      },
      error: (err) => {
        console.error('Failed to approve absence', err);
      }
    });
  }

  /**
   * Reject a pending absence
   */
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
        this.loadPendingAbsences();
        this.loadEmployeesAbsences();
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
}
