import { Component, inject, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { MessageService, ConfirmationService } from 'primeng/api';

// PrimeNG
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { CheckboxModule } from 'primeng/checkbox';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';

import { HolidayService } from '../../core/services/holiday.service';
import { CompanyContextService } from '../../core/services/companyContext.service';
import { Holiday, HolidayScope, CreateHolidayRequest, UpdateHolidayRequest } from '../../core/models/holiday.model';

@Component({
  selector: 'app-holidays',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    TranslateModule,
    TableModule,
    ButtonModule,
    DialogModule,
    InputTextModule,
    TextareaModule,
    DatePickerModule,
    SelectModule,
    CheckboxModule,
    ToastModule,
    ConfirmDialogModule,
    TagModule,
    TooltipModule
  ],
  templateUrl: './holidays.html',
  styleUrls: ['./holidays.css'],
  providers: [MessageService, ConfirmationService]
})
export class HolidaysComponent {
  private readonly holidayService = inject(HolidayService);
  private readonly companyContext = inject(CompanyContextService);
  private readonly fb = inject(FormBuilder);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly translate = inject(TranslateService);

  // Expose HolidayScope for template
  readonly HolidayScope = HolidayScope;

  // Signals
  holidays = signal<Holiday[]>([]);
  holidayTypes = signal<string[]>([]);
  isLoading = signal<boolean>(false);
  showDialog = false;
  isEditMode = signal<boolean>(false);
  selectedHoliday = signal<Holiday | null>(null);
  selectedYear = signal<number>(new Date().getFullYear());
  selectedScope = signal<HolidayScope | null>(null);
  selectedType = signal<string | null>(null);
  selectedActive = signal<boolean | null>(true);

  // Form
  holidayForm!: FormGroup;

  // Computed
  scopeOptions = computed(() => [
    { label: 'holidays.scope.all', value: null },
    { label: 'holidays.scope.global', value: HolidayScope.Global },
    { label: 'holidays.scope.company', value: HolidayScope.Company }
  ]);

  typeOptions = computed(() => [
    { label: 'holidays.type.all', value: null },
    ...this.holidayTypes().map(type => ({ label: type, value: type }))
  ]);

  recurrenceOptions = computed(() => [
    { label: 'holidays.recurrence.none', value: 'none' },
    { label: 'holidays.recurrence.yearly', value: 'yearly' }
  ]);

  activeOptions = computed(() => [
    { label: 'holidays.status.all', value: null },
    { label: 'holidays.status.active', value: true },
    { label: 'holidays.status.inactive', value: false }
  ]);

  yearOptions = computed(() => {
    const currentYear = new Date().getFullYear();
    const years = [];
    for (let i = currentYear - 2; i <= currentYear + 3; i++) {
      years.push({ label: i.toString(), value: i });
    }
    return years;
  });

  constructor() {
    this.initForm();
    this.loadHolidayTypes();
    this.loadHolidays();
  }

  /**
   * Initialize form
   */
  private initForm(): void {
    this.holidayForm = this.fb.group({
      nameFr: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
      nameAr: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
      nameEn: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
      holidayDate: [null, Validators.required],
      description: ['', Validators.maxLength(1000)],
      holidayType: ['', [Validators.required, Validators.maxLength(50)]],
      isMandatory: [true],
      isPaid: [true],
      recurrence: ['none', Validators.required],
      year: [null, [Validators.min(2020), Validators.max(2100)]],
      affectPayroll: [true],
      affectAttendance: [true],
      isActive: [true]
    });
  }

  /**
   * Load holidays
   */
  loadHolidays(): void {
    this.isLoading.set(true);
    const companyId = this.companyContext.companyId();

    if (!companyId) {
      this.messageService.add({
        severity: 'error',
        summary: this.translate.instant('common.error'),
        detail: this.translate.instant('holidays.errors.noCompany')
      });
      this.isLoading.set(false);
      return;
    }

    const filters: any = {
      companyId: parseInt(companyId, 10),
      year: this.selectedYear()
    };

    if (this.selectedScope() !== null) {
      filters.scope = this.selectedScope();
    }
    if (this.selectedType()) {
      filters.holidayType = this.selectedType();
    }
    if (this.selectedActive() !== null) {
      filters.isActive = this.selectedActive();
    }

    this.holidayService.getHolidays(filters).subscribe({
      next: (holidays) => {
        this.holidays.set(holidays);
        this.isLoading.set(false);
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('holidays.errors.loadFailed')
        });
        this.isLoading.set(false);
      }
    });
  }

  /**
   * Load holiday types
   */
  loadHolidayTypes(): void {
    this.holidayService.getHolidayTypes().subscribe({
      next: (types) => {
        this.holidayTypes.set(types);
      },
      error: (error) => {
        alert('Error loading holiday types:');
      }
    });
  }

  /**
   * Open create dialog
   */
  openCreateDialog(): void {
    this.isEditMode.set(false);
    this.selectedHoliday.set(null);
    this.holidayForm.reset({
      isMandatory: true,
      isPaid: true,
      isRecurring: false,
      affectPayroll: true,
      affectAttendance: true,
      isActive: true
    });
    this.showDialog = true;
  }

  /**
   * Open edit dialog
   */
  openEditDialog(holiday: Holiday): void {
    this.isEditMode.set(true);
    this.selectedHoliday.set(holiday);

    const holidayDate = holiday.holidayDate ? new Date(holiday.holidayDate) : null;

    this.holidayForm.patchValue({
      nameFr: holiday.nameFr,
      nameAr: holiday.nameAr,
      nameEn: holiday.nameEn,
      holidayDate: holidayDate,
      description: holiday.description || '',
      holidayType: holiday.holidayType,
      isMandatory: holiday.isMandatory,
      isPaid: holiday.isPaid,
      recurrence: holiday.isRecurring ? 'yearly' : 'none',
      year: holiday.year,
      affectPayroll: holiday.affectPayroll,
      affectAttendance: holiday.affectAttendance,
      isActive: holiday.isActive
    });

    this.showDialog = true;
  }

  /**
   * Close dialog
   */
  closeDialog(): void {
    this.showDialog = false;
    this.holidayForm.reset();
    this.selectedHoliday.set(null);
  }

  /**
   * Save holiday
   */
  saveHoliday(): void {
    if (this.holidayForm.invalid) {
      Object.keys(this.holidayForm.controls).forEach(key => {
        this.holidayForm.get(key)?.markAsTouched();
      });
      return;
    }

    const formValue = this.holidayForm.value;
    const holidayDate = formValue.holidayDate ? this.formatDate(formValue.holidayDate) : '';

    if (this.isEditMode()) {
      this.updateHoliday(holidayDate, formValue);
    } else {
      this.createHoliday(holidayDate, formValue);
    }
  }

  /**
   * Create new holiday
   */
  private createHoliday(holidayDate: string, formValue: any): void {
    const companyId = this.companyContext.companyId();

    if (!companyId) {
      this.messageService.add({
        severity: 'error',
        summary: this.translate.instant('common.error'),
        detail: this.translate.instant('holidays.errors.noCompany')
      });
      return;
    }

    const request: CreateHolidayRequest = {
      nameFr: formValue.nameFr,
      nameAr: formValue.nameAr,
      nameEn: formValue.nameEn,
      holidayDate: holidayDate,
      description: formValue.description || undefined,
      companyId: parseInt(companyId, 10),
      countryId: 1, // TODO: Get from company context
      scope: HolidayScope.Company,
      holidayType: formValue.holidayType,
      isMandatory: formValue.isMandatory,
      isPaid: formValue.isPaid,
      isRecurring: formValue.recurrence !== 'none',
      recurrenceRule: formValue.recurrence === 'yearly' ? 'FREQ=YEARLY' : undefined,
      year: formValue.year || undefined,
      affectPayroll: formValue.affectPayroll,
      affectAttendance: formValue.affectAttendance,
      isActive: formValue.isActive
    };

    this.holidayService.createHoliday(request).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('holidays.messages.createSuccess')
        });
        this.closeDialog();
        this.loadHolidays();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('holidays.errors.createFailed')
        });
      }
    });
  }

  /**
   * Update existing holiday
   */
  private updateHoliday(holidayDate: string, formValue: any): void {
    const holiday = this.selectedHoliday();
    if (!holiday?.id) return;

    const request: UpdateHolidayRequest = {
      nameFr: formValue.nameFr,
      nameAr: formValue.nameAr,
      nameEn: formValue.nameEn,
      holidayDate: holidayDate,
      description: formValue.description || undefined,
      holidayType: formValue.holidayType,
      isMandatory: formValue.isMandatory,
      isPaid: formValue.isPaid,
      isRecurring: formValue.recurrence !== 'none',
      recurrenceRule: formValue.recurrence === 'yearly' ? 'FREQ=YEARLY' : undefined,
      year: formValue.year || undefined,
      affectPayroll: formValue.affectPayroll,
      affectAttendance: formValue.affectAttendance,
      isActive: formValue.isActive
    };

    this.holidayService.updateHoliday(holiday.id, request).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.translate.instant('common.success'),
          detail: this.translate.instant('holidays.messages.updateSuccess')
        });
        this.closeDialog();
        this.loadHolidays();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('common.error'),
          detail: this.translate.instant('holidays.errors.updateFailed')
        });
      }
    });
  }

  /**
   * Delete holiday
   */
  deleteHoliday(holiday: Holiday): void {
    this.confirmationService.confirm({
      message: this.translate.instant('holidays.confirmDelete'),
      header: this.translate.instant('common.confirmation'),
      accept: () => {
        this.holidayService.deleteHoliday(holiday.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: this.translate.instant('common.success'),
              detail: this.translate.instant('holidays.messages.deleteSuccess')
            });
            this.loadHolidays();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: this.translate.instant('common.error'),
              detail: this.translate.instant('holidays.errors.deleteFailed')
            });
          }
        });
      }
    });
  }

  /**
   * Apply filters
   */
  applyFilters(): void {
    this.loadHolidays();
  }

  /**
   * Get scope severity for tag
   */
  getScopeSeverity(scope: HolidayScope): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
    return scope === HolidayScope.Global ? 'info' : 'success';
  }

  /**
   * Get active severity for tag
   */
  getActiveSeverity(isActive: boolean): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
    return isActive ? 'success' : 'danger';
  }

  /**
   * Get holiday name based on current language
   */
  getHolidayName(holiday: Holiday): string {
    const currentLang = this.translate.currentLang || 'en';

    switch (currentLang) {
      case 'fr':
        return holiday.nameFr;
      case 'ar':
        return holiday.nameAr;
      case 'en':
      default:
        return holiday.nameEn;
    }
  }

  /**
   * Format date for API
   */
  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
