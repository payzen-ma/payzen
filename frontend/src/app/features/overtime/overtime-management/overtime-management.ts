import { CommonModule } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { TextareaModule } from 'primeng/textarea';
// CalendarModule and SelectButtonModule not required (using native inputs in dialog)
import { Holiday } from '@app/core/models/holiday.model';
import {
  Overtime,
  OvertimeEntryMode,
  OvertimeFilters,
  OvertimeStatus,
  OvertimeType
} from '@app/core/models/overtime.model';
import { EmployeeService } from '@app/core/services/employee.service';
import { HolidayService } from '@app/core/services/holiday.service';
import { OvertimeService } from '@app/core/services/overtime.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { BadgeModule } from 'primeng/badge';
import { CardModule } from 'primeng/card';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { TabsModule } from 'primeng/tabs';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';

@Component({
  selector: 'app-overtime-management',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    TextareaModule,
    DatePickerModule,
    SelectModule,
    TagModule,
    ToastModule,
    ConfirmDialogModule,
    CardModule,
    TooltipModule,
    TabsModule,
    BadgeModule,
    TranslateModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './overtime-management.html'
})
export class OvertimeManagementComponent implements OnInit {
  private readonly overtimeService = inject(OvertimeService);
  private readonly employeeService = inject(EmployeeService);
  private readonly holidayService = inject(HolidayService);
  private readonly messageService = inject(MessageService);
  private readonly fb = inject(FormBuilder);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly translate = inject(TranslateService);
  private readonly router = inject(Router);

  // Signals
  readonly overtimes = signal<Overtime[]>([]);
  // All overtimes used for company-wide statistics (not table-filtered)
  readonly allOvertimesForStats = signal<Overtime[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly showApprovalDialog = signal<boolean>(false);
  readonly selectedOvertime = signal<Overtime | null>(null);

  // Filters
  selectedStatus = signal<OvertimeStatus | null>(null);
  selectedStartDate = signal<Date | null>(null);
  selectedEndDate = signal<Date | null>(null);
  // Description search filter (client-side)
  descriptionFilter = signal<string>('');
  // Employee name filter (client-side)
  employeeFilter = signal<string>('');

  // Approval
  approvalComment = '';

  // Status enum for template
  readonly OvertimeStatus = OvertimeStatus;
  readonly OvertimeType = OvertimeType;
  readonly OvertimeEntryMode = OvertimeEntryMode;

  // Overtime types for dropdown
  readonly overtimeTypes = computed(() => [
    { label: this.translate.instant('overtime.type.standard'), value: OvertimeType.Standard },
    { label: this.translate.instant('overtime.type.publicHoliday'), value: OvertimeType.PublicHoliday }
  ]);

  // Filtered holidays by selected year
  readonly filteredHolidays = computed(() => {
    const year = this.selectedYear();
    const currentLang = this.translate.currentLang || 'en';

    return this.holidays()
      .filter(h => new Date(h.holidayDate).getFullYear() === year)
      .map(h => ({
        label: this.getHolidayName(h, currentLang),
        value: h.id
      }));
  });

  /**
   * Get holiday name based on current language
   */
  getHolidayName(holiday: Holiday, lang: string): string {
    switch (lang) {
      case 'fr':
        return holiday.nameFr || holiday.nameEn || holiday.nameAr || 'Holiday';
      case 'ar':
        return holiday.nameAr || holiday.nameEn || holiday.nameFr || 'عطلة';
      default:
        return holiday.nameEn || holiday.nameFr || holiday.nameAr || 'Holiday';
    }
  }

  // Employee full name computed
  readonly employeeFullName = computed(() => {
    const emp = this.selectedEmployeeForDeclare();
    if (!emp) return '';
    return `${emp.firstName || ''} ${emp.lastName || ''}`.trim() || emp.fullName || emp.name || emp.employeeName || 'Employé';
  });

  // Position to display in declare dialog (hide "Non assigné" / "Unassigned")
  readonly selectedEmployeePositionDisplay = computed(() => {
    const pos = this.selectedEmployeeForDeclare()?.position;
    if (!pos || typeof pos !== 'string') return '';
    const lower = pos.trim().toLowerCase();
    if (lower === 'non assigné' || lower === 'unassigned' || lower === 'non assigne') return '';
    return pos;
  });

  // Computed
  readonly statusOptions = computed(() => [
    { label: this.translate.instant('overtime.status.all'), value: null },
    { label: this.translate.instant('overtime.status.submitted'), value: OvertimeStatus.Submitted },
    { label: this.translate.instant('overtime.status.approved'), value: OvertimeStatus.Approved },
    { label: this.translate.instant('overtime.status.rejected'), value: OvertimeStatus.Rejected },
    { label: this.translate.instant('overtime.status.cancelled'), value: OvertimeStatus.Cancelled }
  ]);

  // Use the unfiltered allOvertimesForStats to compute counters so they don't change when table is filtered
  readonly pendingCount = computed(() =>
    this.allOvertimesForStats().filter(o => o.status === OvertimeStatus.Submitted).length
  );

  readonly approvedCount = computed(() =>
    this.allOvertimesForStats().filter(o => o.status === OvertimeStatus.Approved).length
  );

  readonly rejectedCount = computed(() =>
    this.allOvertimesForStats().filter(o => o.status === OvertimeStatus.Rejected).length
  );

  readonly draftCount = computed(() =>
    this.allOvertimesForStats().filter(o => o.status === OvertimeStatus.Draft).length
  );

  // Tab management
  activeTabValue = 'draft';

  // Filtered overtimes based on active tab
  readonly filteredOvertimes = computed(() => {
    const all = this.overtimes();
    switch (this.activeTabValue) {
      case 'draft':
        return all.filter(o => o.status === OvertimeStatus.Draft);
      case 'pending':
        return all.filter(o => o.status === OvertimeStatus.Submitted);
      case 'approved':
        return all.filter(o => o.status === OvertimeStatus.Approved);
      case 'rejected':
        return all.filter(o => o.status === OvertimeStatus.Rejected);
      case 'cancelled':
        return all.filter(o => o.status === OvertimeStatus.Cancelled);
      default:
        break;
    }

    // Apply additional client-side filters (description, date range)
    let list = all;

    const desc = this.descriptionFilter().toLowerCase().trim();
    if (desc) {
      list = list.filter(o => {
        // Combine all string fields from the overtime object for a broad search
        const combined = Object.values(o)
          .filter(v => typeof v === 'string')
          .join(' ')
          .toLowerCase();
        return combined.includes(desc);
      });
    }

    const empQuery = this.employeeFilter().toLowerCase().trim();
    if (empQuery) {
      list = list.filter(o => {
        const nameCandidates = [
          o.employeeFullName,
          (o as any).employeeName,
          (o as any).firstName,
          (o as any).lastName,
          (o as any).employee?.firstName,
          (o as any).employee?.lastName
        ];
        return nameCandidates.some(n => !!n && String(n).toLowerCase().includes(empQuery));
      });
    }

    const start = this.selectedStartDate();
    const end = this.selectedEndDate();
    if (start || end) {
      list = list.filter(o => {
        const d = o.overtimeDate ? new Date(o.overtimeDate) : (o as any).OvertimeDate ? new Date((o as any).OvertimeDate) : null;
        if (!d) return false;
        if (start && d < new Date(start.setHours(0, 0, 0, 0))) return false;
        if (end && d > new Date(end.setHours(23, 59, 59, 999))) return false;
        return true;
      });
    }

    return list;
  });

  // Employees list (for RH/Manager quick actions)
  readonly employees = signal<any[]>([]);
  readonly employeesLoading = signal<boolean>(false);
  readonly employeeTableSearch = signal<string>('');

  readonly filteredEmployeesForTable = computed(() => {
    const list = this.employees();
    const q = this.employeeTableSearch().toLowerCase().trim();
    if (!q) return list;
    return list.filter((emp: any) => {
      const firstName = (emp.firstName || '').toLowerCase();
      const lastName = (emp.lastName || '').toLowerCase();
      const full = `${firstName} ${lastName}`.trim();
      return full.includes(q) || firstName.includes(q) || lastName.includes(q);
    });
  });
  readonly showDeclareDialog = signal<boolean>(false);
  readonly selectedEmployeeForDeclare = signal<any | null>(null);
  declareForm!: FormGroup;

  // Holidays management
  readonly holidays = signal<Holiday[]>([]);
  readonly selectedYear = signal<number>(new Date().getFullYear());
  readonly years = signal<{ label: string, value: number }[]>([]);

  ngOnInit(): void {
    this.loadOvertimes();
    this.loadEmployees();
    this.loadHolidays();
    this.initYears();
    this.initDeclareForm();
  }

  initDeclareForm(): void {
    this.declareForm = this.fb.group({
      overtimeType: [OvertimeType.Standard, Validators.required],
      overtimeDate: [null],
      year: [new Date().getFullYear()],
      holidayId: [null],
      startTime: [''],
      endTime: [''],
      reason: [''] // Optional field - no validators
    });

    // Subscribe to type changes to update validators
    this.declareForm.get('overtimeType')?.valueChanges.subscribe(type => {
      this.updateValidators(type);
    });

    // Initialize validators for default type
    this.updateValidators(OvertimeType.Standard);
  }

  updateValidators(type: OvertimeType): void {
    const overtimeDateControl = this.declareForm.get('overtimeDate');
    const yearControl = this.declareForm.get('year');
    const holidayIdControl = this.declareForm.get('holidayId');
    const startTimeControl = this.declareForm.get('startTime');
    const endTimeControl = this.declareForm.get('endTime');

    // Clear all validators first
    overtimeDateControl?.clearValidators();
    yearControl?.clearValidators();
    holidayIdControl?.clearValidators();
    startTimeControl?.clearValidators();
    endTimeControl?.clearValidators();

    if (type === OvertimeType.PublicHoliday) {
      // Holiday type validation
      yearControl?.setValidators([Validators.required]);
      holidayIdControl?.setValidators([Validators.required]);
    } else {
      // Standard type validation
      overtimeDateControl?.setValidators([Validators.required]);
      startTimeControl?.setValidators([Validators.required]);
      endTimeControl?.setValidators([Validators.required]);
    }

    // Update validity
    overtimeDateControl?.updateValueAndValidity();
    yearControl?.updateValueAndValidity();
    holidayIdControl?.updateValueAndValidity();
    startTimeControl?.updateValueAndValidity();
    endTimeControl?.updateValueAndValidity();

    // Update selected year signal
    if (type === OvertimeType.PublicHoliday && yearControl?.value) {
      this.selectedYear.set(yearControl.value);
    }
  }

  openDeclareDialog(employee: any): void {
    this.selectedEmployeeForDeclare.set(employee);
    this.declareForm.reset({
      overtimeType: OvertimeType.Standard,
      year: new Date().getFullYear(),
      overtimeDate: null,
      holidayId: null,
      startTime: '',
      endTime: '',
      reason: ''
    });
    this.updateValidators(OvertimeType.Standard);
    this.showDeclareDialog.set(true);
  }

  closeDeclareDialog(): void {
    this.showDeclareDialog.set(false);
    this.selectedEmployeeForDeclare.set(null);
  }

  private formatDateLocal(value: any): string {
    if (!value) return '';
    const d = value instanceof Date ? value : new Date(value);
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  submitDeclare(): void {
    if (this.declareForm.invalid) {
      this.declareForm.markAllAsTouched();
      return;
    }
    const emp = this.selectedEmployeeForDeclare();
    if (!emp) return;
    const v = this.declareForm.value;

    const payload: any = {
      employeeId: Number(emp.id ?? emp.Id),
      employeeComment: v.reason || undefined
    };

    if (v.overtimeType === OvertimeType.PublicHoliday) {
      // Holiday type: find the selected holiday and use its date
      const selectedHoliday = this.holidays().find(h => h.id === v.holidayId);
      if (!selectedHoliday) {
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('overtime.errors.noHolidaySelected')
        });
        return;
      }
      // Use the holiday's date and full day mode
      payload.overtimeDate = selectedHoliday.holidayDate;
      payload.entryMode = OvertimeEntryMode.FullDay;
      payload.standardDayHours = 8; // Default 8 hours for public holiday work
    } else {
      // Standard hourly type: use date and times (formatted as HH:00)
      payload.overtimeDate = this.formatDateLocal(v.overtimeDate);
      payload.entryMode = OvertimeEntryMode.HoursRange;
      payload.startTime = v.startTime ? this.formatTime(v.startTime) : undefined;
      payload.endTime = v.endTime ? this.formatTime(v.endTime) : undefined;

      // Calculate duration in hours
      if (v.startTime && v.endTime) {
        const [startHour] = v.startTime.split(':').map(Number);
        const [endHour] = v.endTime.split(':').map(Number);
        const durationHours = Math.abs(endHour - startHour);
        payload.durationInHours = durationHours > 0 ? durationHours : 1;
      }
    }

    this.overtimeService.createOvertime(payload).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: this.translate.instant('common.success'), detail: this.translate.instant('overtime.messages.createSuccess') });
        this.closeDeclareDialog();
        this.loadOvertimes();
      },
      error: (err) => {
        this.messageService.add({ severity: 'error', summary: this.translate.instant('common.error'), detail: this.translate.instant('overtime.errors.createFailed') });
      }
    });
  }

  /**
   * Format time to HH:00 format
   */
  formatTime(timeValue: string): string {
    if (!timeValue) return '';
    const parts = timeValue.split(':');
    const hours = parts[0] || '00';
    return `${hours.padStart(2, '0')}:00`;
  }

  navigateToHrDeclare(employee: any): void {
    const id = employee?.id ?? employee?.Id;
    if (!id) return;
    this.router.navigate(['/app/overtime/hr'], { queryParams: { employeeId: id } });
  }

  /** Navigate to employee profile page */
  viewEmployee(employee: any): void {
    const id = employee?.id ?? employee?.Id ?? employee;
    if (!id) return;
    this.router.navigate(['/app/employees', id]);
  }

  loadEmployees(): void {
    this.employeesLoading.set(true);
    this.employeeService.getEmployees().subscribe({
      next: (resp) => {
        this.employees.set(resp.employees || []);
        this.employeesLoading.set(false);
      },
      error: (err) => {
        this.employeesLoading.set(false);
      }
    });
  }

  /**
   * Load holidays
   */
  loadHolidays(): void {
    this.holidayService.getHolidays().subscribe({
      next: (holidays) => {
        this.holidays.set(holidays || []);
      },
      error: (err) => {
      }
    });
  }

  /**
   * Initialize available years (current year and next year)
   */
  initYears(): void {
    const currentYear = new Date().getFullYear();
    const years = [
      { label: (currentYear - 1).toString(), value: currentYear - 1 },
      { label: currentYear.toString(), value: currentYear },
      { label: (currentYear + 1).toString(), value: currentYear + 1 }
    ];
    this.years.set(years);
  }

  /**
   * Handle year selection change
   */
  onYearChange(year: number): void {
    this.selectedYear.set(year);
    // Reset holiday selection when year changes
    this.declareForm.get('holidayId')?.setValue(null);
  }

  /**
   * Load all overtimes for the company
   */
  loadOvertimes(): void {
    this.isLoading.set(true);

    const filters: OvertimeFilters = {
      pageSize: 100
    };

    if (this.selectedStatus() !== null) {
      filters.status = this.selectedStatus()!;
    }
    if (this.selectedStartDate()) {
      filters.startDate = this.formatDate(this.selectedStartDate()!);
    }
    if (this.selectedEndDate()) {
      filters.endDate = this.formatDate(this.selectedEndDate()!);
    }

    this.overtimeService.getOvertimes(filters).subscribe({
      next: (response) => {
        // Defensive: deduplicate items by id, prefer the most recently created (createdAt)
        const byId = new Map<number, Overtime>();
        (response.data || []).forEach(item => {
          const id = item.id;
          if (id == null) return; // skip items without id
          const existing = byId.get(id);
          if (!existing) {
            byId.set(id, item);
            return;
          }
          const a = existing.createdAt ? new Date(existing.createdAt).getTime() : 0;
          const b = item.createdAt ? new Date(item.createdAt).getTime() : 0;
          if (b >= a) {
            byId.set(id, item);
          }
        });
        const deduped = Array.from(byId.values());
        // Show all items (including Draft items from HR declarations)
        let visible = deduped;
        console.debug('[OvertimeManagement] fetched items:', response.data.length, 'deduped to', deduped.length, 'showing all:', visible.length);
        this.overtimes.set(visible);
        this.isLoading.set(false);
      },
      error: (error) => {
        console.error('Error loading overtimes:', error);
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('overtime.errors.loadFailed')
        });
        this.isLoading.set(false);
      }
    });

    // Also load a (larger) unfiltered list for statistics so counters remain stable
    // Do not block UI on this call; it's to compute totals across all statuses
    this.overtimeService.getOvertimes({ pageSize: 1000 }).subscribe({
      next: (resp) => {
        const all = (resp.data || []).filter(item => item && item.id != null);
        this.allOvertimesForStats.set(all);
      },
      error: (err) => {
        console.warn('Failed to load all overtimes for stats:', err);
      }
    });
  }

  /**
   * Set active tab and reload data if needed
   */
  setActiveTab(tab: string): void {
    this.activeTabValue = tab;
    // Reset status filter when changing tabs
    if (tab !== 'all') {
      this.selectedStatus.set(this.getStatusFromTab(tab));
    } else {
      this.selectedStatus.set(null);
    }
    this.loadOvertimes();
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
   * Get status enum value from tab name
   */
  private getStatusFromTab(tab: string): OvertimeStatus | null {
    switch (tab) {
      case 'pending':
        return OvertimeStatus.Submitted;
      case 'approved':
        return OvertimeStatus.Approved;
      case 'rejected':
        return OvertimeStatus.Rejected;
      case 'cancelled':
        return OvertimeStatus.Cancelled;
      default:
        return null;
    }
  }

  /**
   * Filter overtimes
   */
  applyFilters(): void {
    this.loadOvertimes();
  }

  /**
   * Reset filters
   */
  resetFilters(): void {
    this.selectedStatus.set(null);
    this.selectedStartDate.set(null);
    this.selectedEndDate.set(null);
    this.loadOvertimes();
  }

  /**
   * Open approval dialog
   */
  openApprovalDialog(overtime: Overtime): void {
    this.selectedOvertime.set(overtime);
    this.approvalComment = '';
    this.showApprovalDialog.set(true);
  }

  /**
   * Close approval dialog
   */
  closeApprovalDialog(): void {
    this.showApprovalDialog.set(false);
    this.selectedOvertime.set(null);
    this.approvalComment = '';
  }

  /**
   * Approve overtime
   */
  approveOvertime(): void {
    const overtime = this.selectedOvertime();
    if (!overtime?.id) return;

    this.overtimeService.approveOvertime(overtime.id, this.approvalComment).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('overtime.messages.approved')
        });
        this.closeApprovalDialog();
        this.loadOvertimes();
      },
      error: (error) => {
        console.error('Error approving overtime:', error);
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('overtime.errors.approveFailed')
        });
      }
    });
  }

  /**
   * Approve overtime directly from the table row (no comment)
   */
  approveOvertimeRow(overtime: Overtime): void {
    if (!overtime?.id) return;

    this.overtimeService.approveOvertime(overtime.id, '').subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('overtime.messages.approved')
        });
        this.loadOvertimes();
      },
      error: (error) => {
        console.error('Error approving overtime (row):', error);
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('overtime.errors.approveFailed')
        });
      }
    });
  }

  /**
   * Submit overtime (Draft -> Submitted) - allow HR to submit drafts
   */
  submitOvertime(overtime: Overtime): void {
    if (!overtime?.id) return;

    this.overtimeService.submitOvertime(overtime.id).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('overtime.messages.submitSuccess')
        });
        this.loadOvertimes();
      },
      error: (error) => {
        console.error('Error submitting overtime (management):', error);
        const apiMsg = error?.error?.Message || error?.error?.message || error?.message || this.translate.instant('overtime.errors.submitFailed');
        this.messageService.add({ severity: 'error', summary: this.translate.instant('common.error'), detail: apiMsg });
      }
    });
  }

  /**
   * Reject overtime
   */
  rejectOvertime(): void {
    const overtime = this.selectedOvertime();
    if (!overtime?.id) return;

    if (!this.approvalComment.trim()) {
      this.messageService.add({
        severity: 'warn',
        summary: this.translate.instant('common.warning'),
        detail: this.translate.instant('overtime.validation.rejectCommentRequired')
      });
      return;
    }

    this.overtimeService.rejectOvertime(overtime.id, this.approvalComment).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('overtime.messages.rejected')
        });
        this.closeApprovalDialog();
        this.loadOvertimes();
      },
      error: (error) => {
        console.error('Error rejecting overtime:', error);
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('overtime.errors.rejectFailed')
        });
      }
    });
  }

  /**
   * Get status severity for PrimeNG Tag
   */
  getStatusSeverity(status: OvertimeStatus): 'success' | 'warn' | 'danger' | 'info' | 'secondary' | 'contrast' | undefined {
    switch (status) {
      case OvertimeStatus.Submitted:
        return 'info';
      case OvertimeStatus.Approved:
        return 'success';
      case OvertimeStatus.Rejected:
        return 'danger';
      case OvertimeStatus.Cancelled:
        return 'secondary';
      default:
        return undefined;
    }
  }

  /**
   * Return translated status label for display.
   * Accepts enum value or string and falls back to a default when unknown.
   */
  getStatusLabel(status: OvertimeStatus | string | number | undefined, fallback?: string): string {
    if (status == null) return fallback ?? this.translate.instant('common.unknown');

    // If it's numeric (enum), map directly
    const asNumber = Number(status);
    if (!Number.isNaN(asNumber)) {
      switch (asNumber) {
        case OvertimeStatus.Submitted:
          return this.translate.instant('overtime.status.submitted');
        case OvertimeStatus.Approved:
          return this.translate.instant('overtime.status.approved');
        case OvertimeStatus.Rejected:
          return this.translate.instant('overtime.status.rejected');
        case OvertimeStatus.Cancelled:
          return this.translate.instant('overtime.status.cancelled');
        case OvertimeStatus.Draft:
          return this.translate.instant('overtime.status.draft');
        default:
          return fallback ?? String(status);
      }
    }

    // If it's a string, normalize and map
    const s = String(status).toLowerCase();
    if (s.includes('submitted') || s.includes('pending')) return this.translate.instant('overtime.status.submitted');
    if (s.includes('approved')) return this.translate.instant('overtime.status.approved');
    if (s.includes('rejected')) return this.translate.instant('overtime.status.rejected');
    if (s.includes('cancel')) return this.translate.instant('overtime.status.cancelled');
    if (s.includes('draft')) return this.translate.instant('overtime.status.draft');

    // Last fallback: return provided fallback or raw string
    return fallback ?? String(status);
  }

  /**
   * Get type label
   */
  getTypeLabel(type: OvertimeType | number | undefined): string {
    if (type == null) return this.translate.instant('common.unknown');
    const typeNum = Number(type);

    if (typeNum === OvertimeType.Standard) {
      return this.translate.instant('overtime.type.standard');
    } else if (typeNum === OvertimeType.PublicHoliday || (typeNum & OvertimeType.PublicHoliday) !== 0) {
      return this.translate.instant('overtime.type.publicHoliday');
    } else if ((typeNum & OvertimeType.WeeklyRest) !== 0) {
      return this.translate.instant('overtime.type.weeklyRest');
    }

    return this.translate.instant('overtime.type.standard');
  }

  /**
   * Check whether the given overtime is standard hourly type (not public holiday)
   */
  hasHourlyFlag(o?: Overtime | null): boolean {
    if (!o) return false;
    // Check if startTime or endTime are present - if so, it's hourly
    if (o.startTime || o.endTime) {
      return true;
    }
    // Otherwise, check the overtime type
    const raw = (o as any)?.overtimeType ?? (o as any)?.OvertimeType ?? 0;
    const n = Number(raw) || 0;
    return n === OvertimeType.Standard || (n & OvertimeType.Standard) !== 0;
  }

  /**
   * Check if can approve/reject
   */
  canApprove(overtime: Overtime): boolean {
    return overtime.status === OvertimeStatus.Submitted;
  }

  /**
   * Check if overtime can be cancelled (management)
   */
  canCancel(overtime: Overtime): boolean {
    return overtime.status === OvertimeStatus.Submitted || overtime.status === OvertimeStatus.Approved;
  }

  /**
   * Cancel overtime (management)
   */
  cancelOvertime(overtime: Overtime): void {
    if (!overtime?.id) return;

    this.confirmationService.confirm({
      message: this.translate.instant('overtime.confirmCancel'),
      header: this.translate.instant('common.confirmation'),
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: this.translate.instant('common.yes'),
      rejectLabel: this.translate.instant('common.no'),
      accept: () => {
        this.overtimeService.cancelOvertime(overtime.id!).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: this.translate.instant('common.success'),
              detail: this.translate.instant('overtime.messages.cancelSuccess')
            });
            this.loadOvertimes();
          },
          error: (error) => {
            console.error('Error cancelling overtime (management):', error);
            this.messageService.add({
              severity: 'error',
              summary: this.translate.instant('common.error'),
              detail: this.translate.instant('overtime.errors.cancelFailed')
            });
          }
        });
      }
    });
  }

  /**
   * Get employee initials for avatar
   */
  getEmployeeInitials(name: string): string {
    if (!name || name === 'N/A') return 'NA';
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return (parts[0][0] + parts[1][0]).toUpperCase();
    }
    return name.substring(0, 2).toUpperCase();
  }

  /**
   * Format date to YYYY-MM-DD
   */
  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
