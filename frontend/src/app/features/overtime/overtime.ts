import { CommonModule } from '@angular/common';
import { Component, computed, DestroyRef, effect, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  CreateOvertimeRequest,
  Overtime,
  OvertimeFilters,
  OvertimeStatus,
  OvertimeType,
  UpdateOvertimeRequest
} from '@app/core/models/overtime.model';
import { AuthService } from '@app/core/services/auth.service';
import { EmployeeService } from '@app/core/services/employee.service';
import { HolidayService } from '@app/core/services/holiday.service';
import { OvertimeService } from '@app/core/services/overtime.service';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';

@Component({
  selector: 'app-overtime',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    TableModule,
    ButtonModule,
    DialogModule,
    TextareaModule,
    DatePickerModule,
    SelectModule,
    TagModule,
    ToastModule,
    ConfirmDialogModule,
    TooltipModule,
    TranslateModule
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './overtime.html',
  styleUrl: './overtime.css'
})
export class OvertimeComponent implements OnInit {
  private readonly overtimeService = inject(OvertimeService);
  private readonly employeeService = inject(EmployeeService);
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly translate = inject(TranslateService);
  private readonly holidayService = inject(HolidayService);
  private readonly destroyRef = inject(DestroyRef);

  // Signals
  readonly overtimes = signal<Overtime[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly showDialog = signal<boolean>(false);
  readonly isEditMode = signal<boolean>(false);
  readonly selectedOvertime = signal<Overtime | null>(null);
  readonly currentUserId = signal<number | null>(null);
  readonly selectedOvertimeType = signal<OvertimeType>(OvertimeType.Standard);
  readonly holidays = signal<any[]>([]);
  readonly selectedHolidayId = signal<number | null>(null);
  readonly holidayYear = signal<number>(new Date().getFullYear());
  readonly holidayOptions = computed(() => this.holidays().map(h => ({ label: h.nameFr || h.nameEn || h.name || '', value: h.id })));

  // Year options for holiday selection
  readonly yearOptions = computed(() => {
    const currentYear = new Date().getFullYear();
    return [
      { label: (currentYear - 1).toString(), value: currentYear - 1 },
      { label: currentYear.toString(), value: currentYear },
      { label: (currentYear + 1).toString(), value: currentYear + 1 }
    ];
  });

  // Expose enum for template
  readonly OvertimeType = OvertimeType;
  readonly OvertimeStatus = OvertimeStatus;

  // Form
  overtimeForm!: FormGroup;

  // Type options
  readonly typeOptions = computed(() => [
    { label: this.translate.instant('overtime.type.hourly'), value: OvertimeType.Standard },
    { label: this.translate.instant('overtime.type.holiday'), value: OvertimeType.PublicHoliday }
  ]);

  // Status options
  readonly statusOptions = computed(() => [
    { label: this.translate.instant('overtime.status.submitted'), value: OvertimeStatus.Submitted },
    { label: this.translate.instant('overtime.status.approved'), value: OvertimeStatus.Approved },
    { label: this.translate.instant('overtime.status.rejected'), value: OvertimeStatus.Rejected },
    { label: this.translate.instant('overtime.status.cancelled'), value: OvertimeStatus.Cancelled }
  ]);

  constructor() {
    // Load current user info using effect
    effect(() => {
      const user = this.authService.currentUser();
      if (user?.employee_id) {
        this.currentUserId.set(Number(user.employee_id));
      }
    });
  }

  applySelectedHoliday(): void {
    const id = this.selectedHolidayId();
    if (!id) {
      this.messageService.add({ severity: 'warn', summary: this.translate.instant('common.error'), detail: this.translate.instant('overtime.errors.noHolidaySelected') });
      return;
    }

    // find holiday in loaded list
    const h = this.holidays().find(x => x.id === id);
    if (!h) return;

    // set form fields: date and reason/comment
    this.overtimeForm.patchValue({
      overtimeDate: this.parseToLocalDate(h.holidayDate),
      reason: h.nameFr || h.nameEn || h.name || '',
      startTime: '',
      endTime: ''
    });
  }

  /**
   * Handle holiday selection change - auto-apply the selected holiday
   */
  onHolidayChange(holidayId: number | null): void {
    this.selectedHolidayId.set(holidayId);
    if (holidayId) {
      const h = this.holidays().find(x => x.id === holidayId);
      if (h) {
        // Auto-populate form fields
        this.overtimeForm.patchValue({
          overtimeDate: this.parseToLocalDate(h.holidayDate),
          reason: h.nameFr || h.nameEn || h.name || '',
          startTime: '',
          endTime: ''
        });
      }
    }
  }

  ngOnInit(): void {
    this.initForm();

    // Resolve current employee id: prefer the value on the authenticated user,
    // otherwise attempt to fetch the employee record for the current user
    // (GET /api/employee/current) and use its id. This prevents "Aucun employé trouvé"
    // when the backend does not include employeeId in the auth payload.
    if (this.currentUserId()) {
      this.loadOvertimes();
    } else {
      this.employeeService.getCurrentEmployee()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (emp) => {
            if (emp && (emp.id || (emp as any).Id)) {
              const id = String((emp as any).id ?? (emp as any).Id);
              this.currentUserId.set(Number(id));
            }
            this.loadOvertimes();
          },
          error: (err) => {
            this.loadOvertimes();
          }
        });
    }

    // load holidays for current year by default
    this.loadHolidays(this.holidayYear());
  }

  loadHolidays(year: number): void {
    this.holidayService.getCompanyHolidays(year)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (list) => this.holidays.set(list || []),
        error: (err) => {
          this.holidays.set([]);
        }
      });
  }

  /**
   * Parse a value into a local Date (avoid timezone shifts for YYYY-MM-DD strings)
   */
  private parseToLocalDate(value: any): Date {
    if (!value) return new Date();
    if (value instanceof Date) return value;
    if (typeof value === 'string') {
      const m = value.match(/^(\d{4})-(\d{2})-(\d{2})$/);
      if (m) {
        const y = Number(m[1]);
        const mo = Number(m[2]) - 1;
        const d = Number(m[3]);
        return new Date(y, mo, d);
      }
    }
    return new Date(value);
  }

  /**
   * Format a date value as local YYYY-MM-DD without timezone conversion
   */
  private formatDateLocal(value: any): string {
    const d = this.parseToLocalDate(value);
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  /**
   * Initialize the form
   */
  private initForm(): void {
    this.overtimeForm = this.fb.group({
      overtimeDate: [null, Validators.required],
      overtimeType: [OvertimeType.Standard, Validators.required],
      startTime: [''],
      endTime: [''],
      reason: ['']
    });

    // Watch overtimeType changes to update validators
    this.overtimeForm.get('overtimeType')?.valueChanges.subscribe(type => {
      this.selectedOvertimeType.set(type);
      const startTimeControl = this.overtimeForm.get('startTime');
      const endTimeControl = this.overtimeForm.get('endTime');

      if (type === OvertimeType.Standard) {
        startTimeControl?.setValidators([Validators.required]);
        endTimeControl?.setValidators([Validators.required]);
      } else {
        startTimeControl?.clearValidators();
        endTimeControl?.clearValidators();
      }

      startTimeControl?.updateValueAndValidity();
      endTimeControl?.updateValueAndValidity();
    });
  }

  /**
   * Load all overtimes for current employee
   */
  loadOvertimes(): void {
    const employeeId = this.currentUserId();
    if (!employeeId) return;

    this.isLoading.set(true);

    const filters: OvertimeFilters = {
      employeeId: employeeId,
      pageSize: 100
    };

    this.overtimeService.getOvertimes(filters)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.overtimes.set(response.data);
          this.isLoading.set(false);
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: this.translate.instant('common.error'),
            detail: this.translate.instant('overtime.errors.loadFailed')
          });
          this.isLoading.set(false);
        }
      });
  }

  /**
   * Open dialog to create new overtime
   */
  openCreateDialog(): void {
    this.isEditMode.set(false);
    this.selectedOvertime.set(null);
    this.overtimeForm.reset({
      overtimeType: OvertimeType.Standard,
      startTime: '',
      endTime: '',
      reason: ''
    });
    this.selectedOvertimeType.set(OvertimeType.Standard);
    this.showDialog.set(true);
  }

  /**
   * Open dialog to edit overtime
   */
  openEditDialog(overtime: Overtime): void {
    this.isEditMode.set(true);
    this.selectedOvertime.set(overtime);

    // Parse date and time
    const overtimeDate = this.parseToLocalDate(overtime.overtimeDate ?? (overtime as any).OvertimeDate);

    this.overtimeForm.patchValue({
      overtimeDate: overtimeDate,
      overtimeType: overtime.overtimeType,
      startTime: (overtime.startTime ?? (overtime as any).StartTime) ? String(overtime.startTime ?? (overtime as any).StartTime).slice(0, 5) : '',
      endTime: (overtime.endTime ?? (overtime as any).EndTime) ? String(overtime.endTime ?? (overtime as any).EndTime).slice(0, 5) : '',
      reason: overtime.reason || ''
    });

    this.selectedOvertimeType.set(overtime.overtimeType ?? OvertimeType.Standard);
    this.showDialog.set(true);
  }

  /**
   * Close dialog
   */
  closeDialog(): void {
    this.showDialog.set(false);
    this.overtimeForm.reset();
    this.selectedOvertime.set(null);
  }

  /**
   * Save overtime (create or update)
   */
  saveOvertime(): void {
    if (this.overtimeForm.invalid) {
      this.overtimeForm.markAllAsTouched();
      return;
    }

    const formValue = this.overtimeForm.value;
    const employeeId = this.currentUserId();

    if (!employeeId) {
      this.messageService.add({
        severity: 'error',
        summary: this.translate.instant('common.error'),
        detail: this.translate.instant('overtime.errors.noEmployee')
      });
      return;
    }

    // Format date to local YYYY-MM-DD (avoid timezone shifts)
    const formattedDate = this.formatDateLocal(formValue.overtimeDate);

    if (this.isEditMode()) {
      this.updateOvertime(formattedDate, formValue);
    } else {
      this.createOvertime(employeeId, formattedDate, formValue);
    }
  }

  /**
   * Create new overtime
   */
  private createOvertime(employeeId: number, overtimeDate: string, formValue: any): void {
    // Determine entryMode expected by backend (numeric enum 1..3)
    // Map: HoursRange=1, DurationOnly=2, FullDay=3
    let entryModeNumeric: number;
    let durationInHours: number | undefined = undefined;
    let standardDayHours: number | undefined = undefined;

    // Decide entry mode from supplied fields:
    // - If user provided a duration value -> DurationOnly (2)
    // - Else if startTime and endTime provided -> HoursRange (1)
    // - Otherwise -> FullDay (3)
    if (formValue.durationInHours || formValue.duration) {
      entryModeNumeric = 2; // DurationOnly
      durationInHours = Number(formValue.durationInHours ?? formValue.duration);
    } else if (formValue.startTime && formValue.endTime) {
      entryModeNumeric = 1; // HoursRange
      // keep start/end as provided; optional: compute durationInHours from times
    } else {
      entryModeNumeric = 3; // FullDay
      standardDayHours = formValue.standardDayHours ?? 8;
    }

    const request: CreateOvertimeRequest = {
      employeeId: employeeId,
      overtimeDate: overtimeDate,
      entryMode: entryModeNumeric,
      startTime: formValue.overtimeType === OvertimeType.Standard && formValue.startTime ? String(formValue.startTime).slice(0, 5) : undefined,
      endTime: formValue.overtimeType === OvertimeType.Standard && formValue.endTime ? String(formValue.endTime).slice(0, 5) : undefined,
      durationInHours: durationInHours,
      standardDayHours: standardDayHours,
      employeeComment: formValue.employeeComment || formValue.reason || undefined
    };

    this.overtimeService.createOvertime(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: this.translate.instant('common.success'),
            detail: this.translate.instant('overtime.messages.createSuccess')
          });
          this.closeDialog();
          this.loadOvertimes();
        },
        error: (error) => {
          // Log detailed error and show API message when available
          const apiMsg = error?.error?.Message || error?.error?.message || error?.message || this.translate.instant('overtime.errors.createFailed');
          this.messageService.add({
            severity: 'error',
            summary: this.translate.instant('common.error'),
            detail: apiMsg
          });
        }
      });
  }

  /**
   * Update existing overtime
   */
  private updateOvertime(overtimeDate: string, formValue: any): void {
    const overtime = this.selectedOvertime();
    if (!overtime?.id) return;

    const request: UpdateOvertimeRequest = {
      overtimeDate: overtimeDate,
      startTime: formValue.overtimeType === OvertimeType.Standard ? formValue.startTime : undefined,
      endTime: formValue.overtimeType === OvertimeType.Standard ? formValue.endTime : undefined,
      employeeComment: formValue.reason || formValue.employeeComment || undefined
    };

    this.overtimeService.updateOvertime(overtime.id, request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: this.translate.instant('common.success'),
            detail: this.translate.instant('overtime.messages.updateSuccess')
          });
          this.closeDialog();
          this.loadOvertimes();
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: this.translate.instant('common.error'),
            detail: this.translate.instant('overtime.errors.updateFailed')
          });
        }
      });
  }

  /**
   * Submit overtime (Draft -> Submitted)
   */
  submitOvertime(overtime: Overtime): void {
    if (!overtime?.id) return;

    this.overtimeService.submitOvertime(overtime.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: this.translate.instant('common.success'),
            detail: this.translate.instant('overtime.messages.submitSuccess')
          });
          this.loadOvertimes();
        },
        error: (error) => {
          const apiMsg = error?.error?.Message || error?.error?.message || error?.message || this.translate.instant('overtime.errors.submitFailed');
          this.messageService.add({ severity: 'error', summary: this.translate.instant('common.error'), detail: apiMsg });
        }
      });
  }

  /**
   * Delete overtime
   */
  deleteOvertime(overtime: Overtime): void {
    if (!overtime.id) return;

    this.confirmationService.confirm({
      message: this.translate.instant('overtime.confirmDelete'),
      header: this.translate.instant('common.confirmation'),
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: this.translate.instant('common.yes'),
      rejectLabel: this.translate.instant('common.no'),
      accept: () => {
        this.overtimeService.deleteOvertime(overtime.id!)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.messageService.add({
                severity: 'success',
                summary: this.translate.instant('common.success'),
                detail: this.translate.instant('overtime.messages.deleteSuccess')
              });
              this.loadOvertimes();
            },
            error: (error) => {
              this.messageService.add({
                severity: 'error',
                summary: this.translate.instant('common.error'),
                detail: this.translate.instant('overtime.errors.deleteFailed')
              });
            }
          });
      }
    });
  }

  /**
   * Cancel overtime
   */
  cancelOvertime(overtime: Overtime): void {
    if (!overtime.id) return;

    this.confirmationService.confirm({
      message: this.translate.instant('overtime.confirmCancel'),
      header: this.translate.instant('common.confirmation'),
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: this.translate.instant('common.yes'),
      rejectLabel: this.translate.instant('common.no'),
      accept: () => {
        this.overtimeService.cancelOvertime(overtime.id!)
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.messageService.add({
                severity: 'success',
                summary: this.translate.instant('common.success'),
                detail: this.translate.instant('overtime.messages.cancelSuccess')
              });
              this.loadOvertimes();
            },
            error: (error) => {
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
   * Get status severity for tag
   */
  getStatusSeverity(status: OvertimeStatus): 'success' | 'info' | 'warn' | 'danger' {
    switch (status) {
      case OvertimeStatus.Approved:
        return 'success';
      case OvertimeStatus.Submitted:
        return 'info';
      case OvertimeStatus.Cancelled:
        return 'warn';
      case OvertimeStatus.Rejected:
        return 'danger';
      default:
        return 'info';
    }
  }

  /**
   * Get type label
   */
  getTypeLabel(type: OvertimeType | number | undefined): string {
    if (type == null) return this.translate.instant('common.unknown');

    // Known mappings from backend (French descriptions)
    const map: Record<number, string> = {
      0: 'Aucun',
      1: 'Standard',
      2: 'Repos hebdomadaire',
      3: 'Standard + Repos hebdomadaire',
      4: 'Jour férié',
      5: 'Standard + Jour férié',
      6: 'Jour férié / Repos hebdomadaire',
      7: 'Standard + Jour férié / Repos',
      8: 'Nuit',
      9: 'Standard + Nuit',
      10: 'Repos hebdomadaire + Nuit',
      11: 'Standard + Repos hebdomadaire + Nuit',
      12: 'Jour férié + Nuit',
      13: 'Standard + Jour férié + Nuit',
      14: 'Jour férié / Repos + Nuit',
      15: 'Standard + Jour férié / Repos + Nuit'
    };

    const n = Number(type);
    if (map[n]) return map[n];

    // Fallback: build description from bits (priority PublicHoliday > WeeklyRest > Standard)
    const parts: string[] = [];
    const STANDARD = 1;
    const WEEKLY = 2;
    const PUBLIC = 4;
    const NIGHT = 8;

    if ((n & PUBLIC) !== 0) parts.push('Jour férié');
    else if ((n & WEEKLY) !== 0) parts.push('Repos hebdomadaire');
    else if ((n & STANDARD) !== 0) parts.push('Standard');

    if ((n & NIGHT) !== 0) parts.push('Nuit');

    return parts.length > 0 ? parts.join(' + ') : this.translate.instant('common.unknown');
  }

  /**
   * Check if overtime can be edited
   */
  canEdit(overtime: Overtime): boolean {
    // Allow editing when the overtime is still a Draft
    return overtime.status === OvertimeStatus.Draft;
  }

  /**
   * Check if overtime can be cancelled
   */
  canCancel(overtime: Overtime): boolean {
    return overtime.status === OvertimeStatus.Submitted || overtime.status === OvertimeStatus.Approved;
  }
}
