import { Component, OnInit, inject, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { CheckboxModule } from 'primeng/checkbox';
import { ButtonModule } from 'primeng/button';
import { WorkingCalendarService } from '../../../../core/services/working-calendar.service';
import { CompanyContextService } from '../../../../core/services/companyContext.service';
import { WorkingCalendar, CreateWorkingCalendarRequest, UpdateWorkingCalendarRequest, WorkingDay } from '../../../../core/models/working-calendar.model';

@Component({
  selector: 'app-working-calendar',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    TranslateModule,
    ToastModule,
    CheckboxModule,
    ButtonModule
  ],
  templateUrl: './working-calendar.html',
  styleUrls: ['./working-calendar.css'],
  providers: [MessageService]
})
export class WorkingCalendarComponent implements OnInit {
  private readonly workingCalendarService = inject(WorkingCalendarService);
  private readonly companyContextService = inject(CompanyContextService);
  private readonly messageService = inject(MessageService);
  private readonly translateService = inject(TranslateService);
  private readonly fb = inject(FormBuilder);

  readonly workingCalendars = signal<WorkingCalendar[]>([]);
  readonly loading = signal<boolean>(false);
  readonly saving = signal<boolean>(false);

  // Editable copy of week days for the UI (mutable)
  readonly editableDays = signal<WorkingDay[]>([]);
  // Snapshot of original days to detect changes
  readonly originalEditableDays = signal<WorkingDay[]>([]);

  // Compute whether UI has unsaved changes
  readonly hasChanges = computed(() => {
    const orig = this.originalEditableDays();
    const cur = this.editableDays();
    if (orig.length !== cur.length) return true;
    for (let i = 0; i < cur.length; i++) {
      const a = orig[i];
      const b = cur[i];
      if (!a && b) return true;
      if (!b && a) return true;
      // compare key properties
      if ((a?.isWorkingDay ?? false) !== (b?.isWorkingDay ?? false)) return true;
      if ((a?.startTime ?? null) !== (b?.startTime ?? null)) return true;
      if ((a?.endTime ?? null) !== (b?.endTime ?? null)) return true;
    }
    return false;
  });

  // Reactive form to control the Save button disabled state reliably
  saveForm!: FormGroup;

  // Days of the week (0 = Sunday to 6 = Saturday)
  readonly daysOfWeek = computed<WorkingDay[]>(() => {
    const days: WorkingDay[] = [
      { dayOfWeek: 1, dayName: 'workingCalendar.days.monday', isWorkingDay: true, startTime: '09:00', endTime: '17:00' },
      { dayOfWeek: 2, dayName: 'workingCalendar.days.tuesday', isWorkingDay: true, startTime: '09:00', endTime: '17:00' },
      { dayOfWeek: 3, dayName: 'workingCalendar.days.wednesday', isWorkingDay: true, startTime: '09:00', endTime: '17:00' },
      { dayOfWeek: 4, dayName: 'workingCalendar.days.thursday', isWorkingDay: true, startTime: '09:00', endTime: '17:00' },
      { dayOfWeek: 5, dayName: 'workingCalendar.days.friday', isWorkingDay: true, startTime: '09:00', endTime: '17:00' },
      { dayOfWeek: 6, dayName: 'workingCalendar.days.saturday', isWorkingDay: false, startTime: null, endTime: null },
      { dayOfWeek: 0, dayName: 'workingCalendar.days.sunday', isWorkingDay: false, startTime: null, endTime: null }
    ];

    const calendars = this.workingCalendars();
    if (calendars.length > 0) {
      return days.map(day => {
        const existing = calendars.find(c => c.dayOfWeek === day.dayOfWeek);
        if (existing) {
          return {
            ...day,
            id: existing.id,
            isWorkingDay: existing.isWorkingDay,
            startTime: existing.startTime,
            endTime: existing.endTime
          };
        }
        return day;
      });
    }

    return days;
  });

  ngOnInit(): void {
    this.loadWorkingCalendars();
    // create reactive form and initialize save control; disabled will be managed by effect
    this.saveForm = this.fb.group({
      save: [{ value: null, disabled: !this.hasChanges() }]
    });

    // Keep the form control enabled/disabled in sync with hasChanges signal
    effect(() => {
      const changed = this.hasChanges();
      const ctrl = this.saveForm.get('save');
      if (!ctrl) return;
      if (changed) {
        ctrl.enable({ emitEvent: false });
      } else {
        ctrl.disable({ emitEvent: false });
      }
    });
  }

  loadWorkingCalendars(): void {
    const companyId = this.companyContextService.companyId();
    if (!companyId) {
      this.messageService.add({
        severity: 'error',
        summary: this.translateService.instant('common.error'),
        detail: this.translateService.instant('workingCalendar.errors.noCompany')
      });
      return;
    }

    this.loading.set(true);
    this.workingCalendarService.getByCompanyId(Number(companyId)).subscribe({
      next: (calendars) => {
        this.workingCalendars.set(calendars);
            // Initialize editableDays from calendars (merge with defaults)
            const defaults = this.daysOfWeek();
            const merged = defaults.map(d => {
              const existing = calendars.find(c => c.dayOfWeek === d.dayOfWeek);
              if (existing) {
                return {
                  ...d,
                  id: existing.id,
                  isWorkingDay: existing.isWorkingDay,
                  startTime: existing.startTime,
                  endTime: existing.endTime
                } as WorkingDay;
              }
              return d;
            });
            this.editableDays.set(merged);
            // store original snapshot for change detection
            this.originalEditableDays.set(JSON.parse(JSON.stringify(merged)));
            this.loading.set(false);
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.translateService.instant('common.error'),
          detail: this.translateService.instant('workingCalendar.errors.loadFailed')
        });
        this.loading.set(false);
      }
    });
  }

  /**
   * Les champs utilisent ngModel sur des objets déjà dans le signal : muter une propriété
   * ne change pas la référence du signal, donc `hasChanges` ne se recalcule pas.
   * On ré-émet un tableau (shallow copy) pour forcer la détection.
   */
  refreshEditableDaysSignal(): void {
    this.editableDays.update((days) => [...days]);
  }

  onWorkingDayChange(day: WorkingDay): void {
    if (day.isWorkingDay && (!day.startTime || !day.endTime)) {
      day.startTime = '09:00';
      day.endTime = '17:00';
    }
    this.refreshEditableDaysSignal();
  }

  validateDay(day: WorkingDay): boolean {
    if (!day.isWorkingDay) {
      return true;
    }

    if (!day.startTime || !day.endTime) {
      this.messageService.add({
        severity: 'error',
        summary: this.translateService.instant('common.error'),
        detail: this.translateService.instant('workingCalendar.errors.timesRequired', { day: this.translateService.instant(day.dayName) })
      });
      return false;
    }

    if (day.startTime >= day.endTime) {
      this.messageService.add({
        severity: 'error',
        summary: this.translateService.instant('common.error'),
        detail: this.translateService.instant('workingCalendar.errors.invalidTimeRange', { day: this.translateService.instant(day.dayName) })
      });
      return false;
    }

    return true;
  }

  saveWorkingCalendar(): void {
    const companyId = this.companyContextService.companyId();
    if (!companyId) {
      return;
    }

    const days = this.editableDays();

    // Validate all working days
    for (const day of days) {
      if (!this.validateDay(day)) {
        return;
      }
    }

    this.saving.set(true);
    const operations: Array<Promise<void>> = [];

    days.forEach(day => {
      const request: CreateWorkingCalendarRequest | UpdateWorkingCalendarRequest = {
        companyId: Number(companyId),
        dayOfWeek: day.dayOfWeek,
        isWorkingDay: day.isWorkingDay,
        startTime: day.isWorkingDay ? (day.startTime ?? undefined) : undefined,
        endTime: day.isWorkingDay ? (day.endTime ?? undefined) : undefined
      };

      if (day.id) {
        // Update existing
        const operation = this.workingCalendarService.update(day.id, request).toPromise()
          .then(() => {});
        operations.push(operation);
      } else {
        // Create new
        const operation = this.workingCalendarService.create(request as CreateWorkingCalendarRequest).toPromise()
          .then(() => {});
        operations.push(operation);
      }
    });

    Promise.all(operations)
      .then(() => {
        this.messageService.add({
          severity: 'success',
          summary: this.translateService.instant('common.success'),
          detail: this.translateService.instant('workingCalendar.messages.saveSuccess')
        });
        this.loadWorkingCalendars();
      })
      .catch((error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.translateService.instant('common.error'),
          detail: this.translateService.instant('workingCalendar.errors.saveFailed')
        });
      })
      .finally(() => {
        this.saving.set(false);
      });
  }
}
