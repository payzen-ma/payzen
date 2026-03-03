import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
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

  readonly workingCalendars = signal<WorkingCalendar[]>([]);
  readonly loading = signal<boolean>(false);
  readonly saving = signal<boolean>(false);

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
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading working calendars:', error);
        this.messageService.add({
          severity: 'error',
          summary: this.translateService.instant('common.error'),
          detail: this.translateService.instant('workingCalendar.errors.loadFailed')
        });
        this.loading.set(false);
      }
    });
  }

  onWorkingDayChange(day: WorkingDay): void {
    if (day.isWorkingDay && (!day.startTime || !day.endTime)) {
      day.startTime = '09:00';
      day.endTime = '17:00';
    }
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

    const days = this.daysOfWeek();
    
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
        console.error('Error saving working calendar:', error);
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
