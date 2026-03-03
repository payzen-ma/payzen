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
import { TabsModule } from 'primeng/tabs';
import { BadgeModule } from 'primeng/badge';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
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
    RippleModule,
    TabsModule,
    BadgeModule,
    ToastModule,
    ConfirmDialogModule
  ],
  providers: [MessageService, ConfirmationService],
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
  /** Raw list of all company employees for the first card (same layout as overtime-management). */
  allEmployeesList = signal<any[]>([]);
  pendingAbsences = signal<Absence[]>([]);
  isLoading = signal(false);
  searchQuery = signal('');

  // Submitted absences filters
  submittedEmployeeFilter = signal('');
  submittedTypeFilter = signal<AbsenceType | ''>('');
  submittedDurationFilter = signal<AbsenceDurationType | ''>('');

  // Tab management
  activeTabValue = 'submitted';

  // Computed statistics
  readonly submittedCount = computed(() => 
    this.allAbsences().filter((a: any) => a.status === 'Submitted').length
  );

  readonly approvedCount = computed(() => 
    this.allAbsences().filter((a: any) => a.status === 'Approved').length
  );

  readonly rejectedCount = computed(() => 
    this.allAbsences().filter((a: any) => a.status === 'Rejected').length
  );

  readonly cancelledCount = computed(() =>
    this.allAbsences().filter((a: any) => a.status === 'Cancelled').length
  );

  // All absences signal for statistics
  allAbsences = signal<Absence[]>([]);

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

  absenceTypes = signal<Array<{ label: string; value: AbsenceType }>>([]);
  durationTypes = signal<Array<{ label: string; value: AbsenceDurationType }>>([]);
  halfDayOptions = signal<Array<{ label: string; value: boolean }>>([]);

  // Template-friendly plain-array accessors (Angular template type-checker requires arrays)
  get absenceTypesOptions(): Array<{ label: string; value: AbsenceType }> {
    return this.absenceTypes();
  }

  get durationTypesOptions(): Array<{ label: string; value: AbsenceDurationType }> {
    return this.durationTypes();
  }

  get halfDayOptionsList(): Array<{ label: string; value: boolean }> {
    return this.halfDayOptions();
  }

  // Filter options with 'All' option
  absenceTypeFilterOptions = computed(() => [
    { label: this.translate.instant('absences.status.all'), value: '' },
    ...this.absenceTypes()
  ]);
  durationTypeFilterOptions = computed(() => [
    { label: this.translate.instant('absences.status.all'), value: '' },
    ...this.durationTypes()
  ]);

  readonly routePrefix = signal('/app');

  companyStats = signal({
    totalAbsences: 0,
    totalDays: 0
  });

  // Filtering submitted absences
  filteredSubmittedAbsences = computed(() => {
    let filtered = this.pendingAbsences();

    // Filter by employee name
    const nameFilter = this.submittedEmployeeFilter().toLowerCase().trim();
    if (nameFilter) {
      filtered = filtered.filter(a => 
        a.employeeName?.toLowerCase().includes(nameFilter)
      );
    }

    // Filter by absence type
    const typeFilter = this.submittedTypeFilter();
    if (typeFilter) {
      filtered = filtered.filter(a => a.absenceType === typeFilter);
    }

    // Filter by duration type
    const durationFilter = this.submittedDurationFilter();
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
    // populate translated option labels (use get() so translations are resolved)
    this.populateTranslatedOptions();

    // refresh translations when language changes
    this.translate.onLangChange.subscribe(() => this.populateTranslatedOptions());

    // Now load employees and absences (options are ready for immediate dialog use)
    this.loadEmployeesAbsences();
    this.loadAbsencesForTab();
  }

  /**
   * Set active tab
   */
  setActiveTab(tab: string): void {
    this.activeTabValue = tab;
    this.loadAbsencesForTab();
  }

  /**
   * Handle tab change event
   */
  onTabChange(event: any): void {
    if (event.value) {
      this.setActiveTab(event.value);
    }
  }

  /**
   * Load absences based on active tab
   */
  private loadAbsencesForTab(): void {
    this.isLoading.set(true);
    
    // Load all absences using the existing getAbsences method
    // The service automatically adds companyId from contextService
    this.absenceService.getAbsences().subscribe({
      next: (response: any) => {
        const absences = response.absences || [];
        this.allAbsences.set(absences);
        
        // Filter based on active tab
        switch (this.activeTabValue) {
          case 'submitted':
            this.pendingAbsences.set(absences.filter((a: any) => a.status === 'Submitted'));
            break;
          case 'approved':
            this.pendingAbsences.set(absences.filter((a: any) => a.status === 'Approved'));
            break;
          case 'rejected':
            this.pendingAbsences.set(absences.filter((a: any) => a.status === 'Rejected'));
            break;
          case 'cancelled':
            this.pendingAbsences.set(absences.filter((a: any) => a.status === 'Cancelled'));
            break;
          case 'all':
          default:
            this.pendingAbsences.set(absences);
            break;
        }
        
        this.isLoading.set(false);
      },
      error: (err: any) => {
        console.error('Error loading absences:', err);
        this.isLoading.set(false);
      }
    });
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

    this.absenceService.createAbsence(apiRequest).subscribe({
      next: () => {
        this.showGrantDialog.set(false);
        this.loadEmployeesAbsences();
        this.loadAbsencesForTab();
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
        this.allEmployeesList.set(employees);

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

  /**
   * Populate translated labels for selects (and refresh on language change)
   */
  private populateTranslatedOptions() {
    // Absence types (values kept as API enums, labels resolved from translations)
    const typeValues = [
      'ANNUAL_LEAVE', 'SICK', 'MATERNITY', 'PATERNITY', 'UNPAID', 'MISSION', 'TRAINING', 'JUSTIFIED', 'UNJUSTIFIED', 'ACCIDENT_WORK', 'EXCEPTIONAL', 'RELIGIOUS'
    ] as AbsenceType[];

    const typeKeys = typeValues.map(v => `absences.types.${v.toLowerCase()}`);

    // Set instant translations synchronously for initial render
    const instantTypes = typeValues.map(v => ({
      label: this.translate.instant(`absences.types.${v.toLowerCase()}`) || `absences.types.${v.toLowerCase()}`,
      value: v
    }));
    this.absenceTypes.set(instantTypes);

    // Update asynchronously in case translations load later or language changes
    this.translate.get(typeKeys).subscribe(trans => {
      const updated = typeValues.map(v => ({
        label: trans[`absences.types.${v.toLowerCase()}`] || this.translate.instant(`absences.types.${v.toLowerCase()}`) || `absences.types.${v.toLowerCase()}`,
        value: v
      }));
      this.absenceTypes.set(updated);
    });

    // Duration types
    const durValues = ['FullDay', 'HalfDay', 'Hourly'];
    const durKeys = ['absences.durations.fullDay', 'absences.durations.halfDay', 'absences.durations.hourly'];
    // Duration types - instant + async update
    const instantDur = [
      { label: this.translate.instant(durKeys[0]) || 'Full Day', value: 'FullDay' as AbsenceDurationType },
      { label: this.translate.instant(durKeys[1]) || 'Half Day', value: 'HalfDay' as AbsenceDurationType },
      { label: this.translate.instant(durKeys[2]) || 'Hourly', value: 'Hourly' as AbsenceDurationType }
    ];
    this.durationTypes.set(instantDur);
    this.translate.get(durKeys).subscribe(trans => {
      const updatedDur = [
        { label: trans[durKeys[0]] || this.translate.instant(durKeys[0]) || 'Full Day', value: 'FullDay' as AbsenceDurationType },
        { label: trans[durKeys[1]] || this.translate.instant(durKeys[1]) || 'Half Day', value: 'HalfDay' as AbsenceDurationType },
        { label: trans[durKeys[2]] || this.translate.instant(durKeys[2]) || 'Hourly', value: 'Hourly' as AbsenceDurationType }
      ];
      this.durationTypes.set(updatedDur);
    });

    // Half day options
    const halfKeys = ['absences.morning', 'absences.afternoon'];
    // Half day options - instant + async update
    const instantHalf = [
      { label: this.translate.instant(halfKeys[0]) || 'Morning', value: true },
      { label: this.translate.instant(halfKeys[1]) || 'Afternoon', value: false }
    ];
    console.debug('[HR] populateTranslatedOptions - instantHalf', this.translate.currentLang, instantHalf);
    this.halfDayOptions.set(instantHalf);
    this.translate.get(halfKeys).subscribe(trans => {
      const updatedHalf = [
        { label: trans[halfKeys[0]] || this.translate.instant(halfKeys[0]) || 'Morning', value: true },
        { label: trans[halfKeys[1]] || this.translate.instant(halfKeys[1]) || 'Afternoon', value: false }
      ];
      console.debug('[HR] populateTranslatedOptions - async half', this.translate.currentLang, updatedHalf);
      this.halfDayOptions.set(updatedHalf);
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

  /** Filtered list for the first card (all employees table), by search. */
  filteredAllEmployees(): any[] {
    const query = this.searchQuery().toLowerCase().trim();
    const list = this.allEmployeesList();
    if (!query) return list;
    return list.filter((emp: any) => {
      const name = `${emp.firstName || ''} ${emp.lastName || ''}`.toLowerCase();
      return name.includes(query);
    });
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
        this.loadAbsencesForTab();
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
        this.loadAbsencesForTab();
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
