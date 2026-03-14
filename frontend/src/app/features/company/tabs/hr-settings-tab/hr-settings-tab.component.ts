import { Component, OnInit, inject, signal, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { SelectButtonModule } from 'primeng/selectbutton';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { InputNumberModule } from 'primeng/inputnumber';
import { RadioButtonModule } from 'primeng/radiobutton';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { ToastModule } from 'primeng/toast';
import { PopoverModule } from 'primeng/popover';
import { MessageService } from 'primeng/api';
import { forkJoin } from 'rxjs';
import { CompanyService } from '@app/core/services/company.service';
import { WorkingCalendarService } from '@app/core/services/working-calendar.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { Company, HRParameters } from '@app/core/models/company.model';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-hr-settings-tab',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    TranslateModule,
    SelectButtonModule,
    SelectModule,
    ButtonModule,
    InputNumberModule,
    RadioButtonModule,
    InputTextModule,
    TextareaModule,
    ToastModule,
    PopoverModule
  ],
  providers: [MessageService],
  templateUrl: './hr-settings-tab.component.html'
})
export class HrSettingsTabComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly companyService = inject(CompanyService);
  private readonly workingCalendarService = inject(WorkingCalendarService);
  private readonly messageService = inject(MessageService);
  private readonly contextService = inject(CompanyContextService);
  private readonly translate = inject(TranslateService);
  private contextSub?: Subscription;

  // Form state
  hrForm!: FormGroup;
  loading = signal(false);
  company = signal<Company | null>(null);
  workingCalendars = signal<any[]>([]); // Store loaded working calendars with per-day times
  formSubmitted = false;

  // Help popover content
  readonly helpPoints = [
    'company.hrSettings.helpPoint1',
    'company.hrSettings.helpPoint2',
    'company.hrSettings.helpPoint3'
  ];

  // Working days selector (used in HR parameters quick selection)
  readonly workingDaysOptions = [
    { shortLabel: 'company.hrSettings.daysShort.monday', value: 'monday', fullName: 'workingCalendar.days.monday', icon: 'pi-briefcase', isWeekend: false },
    { shortLabel: 'company.hrSettings.daysShort.tuesday', value: 'tuesday', fullName: 'workingCalendar.days.tuesday', icon: 'pi-briefcase', isWeekend: false },
    { shortLabel: 'company.hrSettings.daysShort.wednesday', value: 'wednesday', fullName: 'workingCalendar.days.wednesday', icon: 'pi-briefcase', isWeekend: false },
    { shortLabel: 'company.hrSettings.daysShort.thursday', value: 'thursday', fullName: 'workingCalendar.days.thursday', icon: 'pi-briefcase', isWeekend: false },
    { shortLabel: 'company.hrSettings.daysShort.friday', value: 'friday', fullName: 'workingCalendar.days.friday', icon: 'pi-briefcase', isWeekend: false },
    { shortLabel: 'company.hrSettings.daysShort.saturday', value: 'saturday', fullName: 'workingCalendar.days.saturday', icon: 'pi-sun', isWeekend: true },
    { shortLabel: 'company.hrSettings.daysShort.sunday', value: 'sunday', fullName: 'workingCalendar.days.sunday', icon: 'pi-home', isWeekend: true }
  ];

  // Hour presets and visual indicators
  readonly hoursPresets = [6, 7, 8, 9];
  readonly visualHours = Array.from({ length: 24 }, (_, i) => i + 1);

  // Form options
  readonly leaveRateOptions = [
    { label: 'company.hrSettings.leaveRateOptions.standard', value: 1.5 },
    { label: 'company.hrSettings.leaveRateOptions.extended', value: 2.0 }
  ];

  readonly currencyOptions = [
    { label: 'company.hrSettings.currencies.mad', value: 'MAD' },
    { label: 'company.hrSettings.currencies.eur', value: 'EUR' },
    { label: 'company.hrSettings.currencies.usd', value: 'USD' }
  ];

  readonly paymentFrequencyOptions = [
    { label: 'company.hrSettings.frequencies.monthly', value: 'monthly' },
    { label: 'company.hrSettings.frequencies.bimonthly', value: 'bimonthly' }
  ];

  readonly fiscalMonthOptions = [
    { label: 'company.hrSettings.months.january', value: 1 },
    { label: 'company.hrSettings.months.february', value: 2 },
    { label: 'company.hrSettings.months.march', value: 3 },
    { label: 'company.hrSettings.months.april', value: 4 },
    { label: 'company.hrSettings.months.may', value: 5 },
    { label: 'company.hrSettings.months.june', value: 6 },
    { label: 'company.hrSettings.months.july', value: 7 },
    { label: 'company.hrSettings.months.august', value: 8 },
    { label: 'company.hrSettings.months.september', value: 9 },
    { label: 'company.hrSettings.months.october', value: 10 },
    { label: 'company.hrSettings.months.november', value: 11 },
    { label: 'company.hrSettings.months.december', value: 12 }
  ];

  readonly paymentModeOptions = [
    { label: 'company.hrSettings.selectPaymentMode', value: null },
    { label: 'company.hrSettings.paymentModes.bankTransfer', value: 'virement' },
    { label: 'company.hrSettings.paymentModes.check', value: 'cheque' },
    { label: 'company.hrSettings.paymentModes.cash', value: 'especes' }
  ];

  readonly sectorOptions = [
    { label: 'company.hrSettings.selectSector', value: null },
    { label: 'company.hrSettings.sectors.agriculture', value: 'agriculture' },
    { label: 'company.hrSettings.sectors.industry', value: 'industry' },
    { label: 'company.hrSettings.sectors.commerce', value: 'commerce' },
    { label: 'company.hrSettings.sectors.services', value: 'services' },
    { label: 'company.hrSettings.sectors.tech', value: 'tech' },
    { label: 'company.hrSettings.sectors.construction', value: 'construction' },
    { label: 'company.hrSettings.sectors.transport', value: 'transport' },
    { label: 'company.hrSettings.sectors.tourism', value: 'tourism' },
    { label: 'company.hrSettings.sectors.health', value: 'health' },
    { label: 'company.hrSettings.sectors.education', value: 'education' },
    { label: 'company.hrSettings.sectors.finance', value: 'finance' },
    { label: 'company.hrSettings.sectors.other', value: 'other' }
  ];

  // Default form values
  private readonly defaultValues = {
    workingDays: [] as string[],
    standardHoursPerDay: 8,
    includeSaturdays: false,
    leaveAccrualRate: 1.5,
    currency: 'MAD',
    paymentFrequency: 'monthly',
    fiscalYearStartMonth: 1,
    defaultPaymentMode: null,
    sector: null,
    collectiveAgreement: '',
    cnssSpecificParameters: '',
    irSpecificParameters: ''
  };

  ngOnInit() {
    this.initForm();
    this.loadCompanyData();
    
    // Subscribe to context changes
    this.contextSub = this.contextService.contextChanged$.subscribe(() => {
      this.loadCompanyData();
    });
  }

  ngOnDestroy() {
    if (this.contextSub) {
      this.contextSub.unsubscribe();
    }
  }

  /** Check if a form field is invalid and should show error */
  isFieldInvalid(fieldName: string): boolean {
    const control = this.hrForm.get(fieldName);
    return !!(control?.invalid && (control.touched || this.formSubmitted));
  }

  private initForm() {
    this.hrForm = this.fb.group({
      workingDays: [this.defaultValues.workingDays, Validators.required],
      standardHoursPerDay: [this.defaultValues.standardHoursPerDay],
      includeSaturdays: [this.defaultValues.includeSaturdays, Validators.required],
      leaveAccrualRate: [this.defaultValues.leaveAccrualRate, Validators.required],
      currency: [this.defaultValues.currency, Validators.required],
      paymentFrequency: [this.defaultValues.paymentFrequency, Validators.required],
      fiscalYearStartMonth: [this.defaultValues.fiscalYearStartMonth, Validators.required],
      defaultPaymentMode: [this.defaultValues.defaultPaymentMode],
      sector: [this.defaultValues.sector],
      collectiveAgreement: [this.defaultValues.collectiveAgreement, Validators.maxLength(200)],
      cnssSpecificParameters: [this.defaultValues.cnssSpecificParameters, Validators.maxLength(500)],
      irSpecificParameters: [this.defaultValues.irSpecificParameters, Validators.maxLength(500)]
    });
  }

  private loadCompanyData() {
    this.loading.set(true);
    
    // Load company data first to get company ID
    this.companyService.getCompany().subscribe({
      next: (data) => {
        this.company.set(data);
        
        // Now load working calendar for this company to extract working days
        const companyId = Number(data.id);
        this.workingCalendarService.getByCompanyId(companyId).subscribe({
          next: (calendars) => {
            
            // Enrich calendars with hour properties for ngModel binding
            const enrichedCalendars = calendars.map(cal => {
              // Ensure startTime/endTime strings exist so updates include them even if user didn't edit hours
              const defaultStart = '09:00:00';
              const defaultEnd = '17:00:00';
              return {
                ...cal,
                startTime: cal.startTime ?? (cal.isWorkingDay ? defaultStart : undefined),
                endTime: cal.endTime ?? (cal.isWorkingDay ? defaultEnd : undefined),
                startTimeHour: (cal.startTime ? parseInt(cal.startTime.split(':')[0], 10) : 9),
                endTimeHour: (cal.endTime ? parseInt(cal.endTime.split(':')[0], 10) : 17)
              };
            });
            
            // Store the enriched calendars for display in template
            this.workingCalendars.set(enrichedCalendars);
            
            // Extract working days from calendar entries
            const workingDays = enrichedCalendars
              .filter(wc => wc.isWorkingDay)
              .map(wc => this.dayOfWeekToName(wc.dayOfWeek))
              .sort();
            
            // Patch form with company data including extracted working days
            this.patchFormWithCompanyData(data, workingDays);
            this.loading.set(false);
          },
          error: (err) => {
            console.warn('[HrSettingsTab] Error loading working calendar (will use HR params defaults):', err);
            // If working calendar fails, just use HR params
            this.patchFormWithCompanyData(data, []);
            this.loading.set(false);
          }
        });
      },
      error: (err) => {
        console.error('[HrSettingsTab] Error loading company data:', err);
        this.loading.set(false);
      }
    });
  }

  /**
   * Convert DayOfWeek number to day name
   */
  private dayOfWeekToName(dayOfWeek: number): string {
    const names = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday'];
    return names[dayOfWeek];
  }

  private dayNameToDayOfWeek(dayName: string): number {
    const names = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday'];
    return names.indexOf(dayName);
  }

  private patchFormWithCompanyData(data: Company, loadedWorkingDays: string[] = []) {
    if (!data.hrParameters) return;
    
    const hr = data.hrParameters as any;
    // Use loaded working days from WorkingCalendars, fall back to HR params, then default
    const workingDays = loadedWorkingDays.length > 0 
      ? loadedWorkingDays 
      : (hr.workingDays ?? this.defaultValues.workingDays);
    
    
    this.hrForm.patchValue({
      workingDays: workingDays,
      standardHoursPerDay: hr.standardHoursPerDay ?? this.defaultValues.standardHoursPerDay,
      includeSaturdays: hr.includeSaturdays ?? this.defaultValues.includeSaturdays,
      leaveAccrualRate: hr.leaveAccrualRate ?? this.defaultValues.leaveAccrualRate,
      currency: hr.currency ?? this.defaultValues.currency,
      paymentFrequency: hr.paymentFrequency ?? this.defaultValues.paymentFrequency,
      fiscalYearStartMonth: hr.fiscalYearStartMonth ?? this.defaultValues.fiscalYearStartMonth,
      defaultPaymentMode: hr.defaultPaymentMode ?? this.defaultValues.defaultPaymentMode,
      sector: hr.sector ?? this.defaultValues.sector,
      collectiveAgreement: hr.collectiveAgreement ?? this.defaultValues.collectiveAgreement,
      cnssSpecificParameters: hr.cnssSpecificParameters ?? this.defaultValues.cnssSpecificParameters,
      irSpecificParameters: hr.irSpecificParameters ?? this.defaultValues.irSpecificParameters
    });
  }

  /**
   * Get start hour for a specific day (0-23 or null if not working)
   */
  getStartHourForDay(dayOfWeek: number): number | null {
    const cal = this.workingCalendars().find(wc => wc.dayOfWeek === dayOfWeek);
    if (!cal || !cal.isWorkingDay || !cal.startTime) return null;
    // Extract hour from "HH:mm:ss" format
    const hour = parseInt(cal.startTime.split(':')[0], 10);
    return hour;
  }

  /**
   * Get end hour for a specific day (0-23 or null if not working)
   */
  getEndHourForDay(dayOfWeek: number): number | null {
    const cal = this.workingCalendars().find(wc => wc.dayOfWeek === dayOfWeek);
    if (!cal || !cal.isWorkingDay || !cal.endTime) return null;
    // Extract hour from "HH:mm:ss" format
    const hour = parseInt(cal.endTime.split(':')[0], 10);
    return hour;
  }

  /**
   * Update hours for a specific day
   */
  updateDayHours(dayOfWeek: number, startHour: number | null, endHour: number | null) {
    const cal = this.workingCalendars().find(wc => wc.dayOfWeek === dayOfWeek);
    if (cal) {
      if (startHour !== null) {
        cal.startTime = `${String(startHour).padStart(2, '0')}:00:00`;
      }
      if (endHour !== null) {
        cal.endTime = `${String(endHour).padStart(2, '0')}:00:00`;
      }
    }
  }

  private resetFormToCompanyData() {
    const data = this.company();
    if (data) {
      this.patchFormWithCompanyData(data);
      this.hrForm.markAsPristine();
      this.formSubmitted = false;
    }
  }

  onSubmit() {
    this.formSubmitted = true;
    this.hrForm.markAllAsTouched();

    if (this.hrForm.invalid) {
      console.warn('[HrSettingsTab] ❌ Form validation failed. Invalid fields detected.');
      console.warn('[HrSettingsTab] Invalid form controls:', Object.keys(this.hrForm.controls).filter(key => this.hrForm.get(key)?.invalid));
      this.showToast(
        'error',
        this.translate.instant('common.error'),
        this.translate.instant('company.hrSettings.messages.validationError')
      );
      this.scrollToFirstError();
      return;
    }

    const currentCompany = this.company();
    if (!currentCompany) {
      console.error('[HrSettingsTab] ❌ No company data available. Cannot save.');
      return;
    }

    this.loading.set(true);
    const formValues = this.hrForm.value;
    const companyId = Number(currentCompany.id);

    // Prepare HR params WITHOUT workingDays (those go to WorkingCalendarsController)
    const updatedHrParams: HRParameters = {
      ...currentCompany.hrParameters,
      workingDays: [], // Keep empty - actual working days are managed by WorkingCalendarsController
      standardHoursPerDay: formValues.standardHoursPerDay,
      includeSaturdays: formValues.includeSaturdays,
      leaveAccrualRate: formValues.leaveAccrualRate,
      currency: formValues.currency,
      paymentFrequency: formValues.paymentFrequency,
      fiscalYearStartMonth: formValues.fiscalYearStartMonth,
      defaultPaymentMode: formValues.defaultPaymentMode,
      sector: formValues.sector,
      collectiveAgreement: formValues.collectiveAgreement,
      cnssSpecificParameters: formValues.cnssSpecificParameters,
      irSpecificParameters: formValues.irSpecificParameters,
      annualLeaveDays: formValues.leaveAccrualRate * 12
    };

    // Create two parallel requests:
    // 1. Sync working calendar entries via WorkingCalendarsController (with per-day times)
    // 2. Update HR parameters via Companies controller
    // If no per-day calendars were loaded/edited, build a payload from the selected days
    const existingCalendars = this.workingCalendars();
    const calendarsToSync = (existingCalendars && existingCalendars.length > 0) ? existingCalendars : (() => {
      // Build calendars for all 7 days based on selectedDays and standard hours
      const startHour = 9;
      const endHour = 9 + (formValues.standardHoursPerDay ?? 8);
      return Array.from({ length: 7 }, (_, dayOfWeek) => {
        const dayName = this.dayOfWeekToName(dayOfWeek);
        const isWorkingDay = (formValues.workingDays || []).some((d: string) => d.toLowerCase() === dayName.toLowerCase());
        return {
          dayOfWeek,
          isWorkingDay,
          startTime: isWorkingDay ? `${String(startHour).padStart(2, '0')}:00:00` : undefined,
          endTime: isWorkingDay ? `${String(endHour).padStart(2, '0')}:00:00` : undefined
        };
      });
    })();

    // Ensure every calendar entry that is a working day includes startTime and endTime
    const normalizedCalendarsToSync = (calendarsToSync || []).map(cal => {
      const defaultStart = '09:00:00';
      const defaultEnd = (() => {
        const hours = formValues.standardHoursPerDay ?? 8;
        const endHour = 9 + Number(hours);
        return `${String(endHour).padStart(2, '0')}:00:00`;
      })();

      return {
        ...cal,
        startTime: cal.isWorkingDay ? (cal.startTime ?? defaultStart) : undefined,
        endTime: cal.isWorkingDay ? (cal.endTime ?? defaultEnd) : undefined
      };
    });

    const workingCalendarSync$ = this.workingCalendarService.syncWorkingDaysWithTimes(
      companyId,
      normalizedCalendarsToSync
    );

    const hrParamsUpdate$ = this.companyService.updateCompany({ 
      id: currentCompany.id,
      hrParameters: updatedHrParams 
    });

    // Execute both operations in parallel using forkJoin
    forkJoin([workingCalendarSync$, hrParamsUpdate$]).subscribe({
      next: ([calendarResult, companyResult]) => {
        
        this.company.set(companyResult);
        this.formSubmitted = false;
        this.hrForm.markAsPristine();
        this.showToast(
          'success',
          this.translate.instant('common.success'),
          this.translate.instant('company.hrSettings.messages.saveSuccess')
        );
        this.loading.set(false);
      },
      error: (error) => {
        console.error('[HrSettingsTab] ❌ ERROR in dual save flow:', error);
        console.error('[HrSettingsTab] Error details:', {
          status: error.status,
          message: error.message,
          responseBody: error.error,
          full: error
        });
        this.showToast(
          'error',
          this.translate.instant('common.error'),
          this.translate.instant('company.hrSettings.messages.saveError')
        );
        this.loading.set(false);
      }
    });
  }

  onCancel() {
    if (!this.hrForm.dirty) return;
    
    this.resetFormToCompanyData();
    this.showToast(
      'info',
      this.translate.instant('common.success'),
      this.translate.instant('company.hrSettings.messages.changesDiscarded')
    );
  }

  toggleWorkingDay(dayValue: string) {
    const currentDays = this.hrForm.get('workingDays')?.value || [];
    const updatedDays = currentDays.includes(dayValue)
      ? currentDays.filter((day: string) => day !== dayValue)
      : [...currentDays, dayValue];
    
    this.hrForm.patchValue({ workingDays: updatedDays });
    this.hrForm.get('workingDays')?.markAsTouched();
    
    // Update the corresponding calendar entry's isWorkingDay property
    const dayOfWeek = this.dayNameToDayOfWeek(dayValue);
    const updatedCalendars = this.workingCalendars().map(cal => {
      if (cal.dayOfWeek === dayOfWeek) {
        return { ...cal, isWorkingDay: updatedDays.includes(dayValue) };
      }
      return cal;
    });
    this.workingCalendars.set(updatedCalendars);
  }

  private showToast(severity: 'success' | 'error' | 'info', summary: string, detail: string) {
    this.messageService.add({ 
      severity, 
      summary, 
      detail,
      life: severity === 'error' ? 5000 : 4000
    });
  }

  getDayTranslationKey(dayOfWeek: number): string {
    return `workingCalendar.days.${this.dayOfWeekToName(dayOfWeek)}`;
  }

  private scrollToFirstError() {
    setTimeout(() => {
      const firstError = document.querySelector('[aria-invalid="true"]');
      if (firstError) {
        firstError.scrollIntoView({ behavior: 'smooth', block: 'center' });
        (firstError as HTMLElement).focus();
      }
    }, 100);
  }
}
