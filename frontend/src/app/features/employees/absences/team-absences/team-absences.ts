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
import { AbsenceService } from '@app/core/services/absence.service';
import { EmployeeService } from '@app/core/services/employee.service';
import { AuthService } from '@app/core/services/auth.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { Absence } from '@app/core/models/absence.model';
import { AbsenceType, AbsenceDurationType, CreateAbsenceRequest } from '@app/core/models/absence.model';
import { TranslateService } from '@ngx-translate/core';

interface EmployeeAbsenceSummary {
  employeeId: number;
  employeeName: string;
  totalAbsences: number;
  totalDays: number;
}

interface GrantAbsenceRequest extends CreateAbsenceRequest {
  employeeName?: string;
}

@Component({
  selector: 'app-team-absences',
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
    TooltipModule
  ],
  templateUrl: './team-absences.html',
  styleUrl: './team-absences.css'
})
export class TeamAbsencesComponent implements OnInit {
  private absenceService = inject(AbsenceService);
  private employeeService = inject(EmployeeService);
  private authService = inject(AuthService);
  private router = inject(Router);
  private contextService = inject(CompanyContextService);
  private translate = inject(TranslateService);

  employees = signal<EmployeeAbsenceSummary[]>([]);
  isLoading = signal(false);
  searchQuery = signal('');

  // Grant absence dialog state
  showGrantDialog = signal(false);
  grantRequest = signal<GrantAbsenceRequest | null>(null);

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

  readonly routePrefix = signal('/app');

  teamStats = signal({
    totalAbsences: 0,
    totalDays: 0
  });

  ngOnInit() {
    // Determine route prefix based on context
    if (this.contextService.isExpertMode()) {
      this.routePrefix.set('/expert');
    }
    // init translated option labels
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

    this.loadTeamAbsences();
  }

  openGrantDialog(employeeId: number, employeeName: string) {
    this.grantRequest.set({
      employeeId: Number(employeeId),
      employeeName: employeeName,
      absenceDate: '',
      durationType: 'FullDay',
      absenceType: 'JUSTIFIED',
      reason: ''
    });
    this.showGrantDialog.set(true);
  }

  submitGrant() {
    const req = this.grantRequest();
    if (!req) return;
    if (!req.absenceDate) return;

    if (req.durationType === 'HalfDay' && req.isMorning === undefined) {
      return;
    }
    if (req.durationType === 'Hourly' && (!req.startTime || !req.endTime)) {
      return;
    }

    const { employeeName, ...apiRequest } = req;

    this.absenceService.createAbsence(apiRequest).subscribe({
      next: () => {
        this.showGrantDialog.set(false);
        this.loadTeamAbsences();
      },
      error: (err) => {
        console.error('Failed to grant absence', err);
      }
    });
  }

  updateGrantField(field: keyof CreateAbsenceRequest, value: any) {
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
  }

  loadTeamAbsences() {
    this.isLoading.set(true);
    const user = this.authService.currentUser();
    
    if (!user || !user.employee_id) {
      this.isLoading.set(false);
      return;
    }

    // Load subordinates only
    this.employeeService.getSubordinates(user.employee_id).subscribe({
      next: (subordinates) => {
        const summaries: EmployeeAbsenceSummary[] = subordinates.map(emp => ({
          employeeId: Number(emp.id),
          employeeName: `${emp.firstName} ${emp.lastName}`,
          totalAbsences: 0,
          totalDays: 0
        }));

        this.employees.set(summaries);
        this.teamStats.set({ totalAbsences: 0, totalDays: 0 });

        // Load stats for each subordinate
        subordinates.forEach((emp, index) => {
          this.absenceService.getEmployeeAbsences(String(emp.id)).subscribe({
            next: (response) => {
              this.employees.update(current => {
                const updated = [...current];
                updated[index] = {
                  ...updated[index],
                  totalAbsences: response.stats?.totalAbsences ?? 0,
                  totalDays: response.stats?.totalDays ?? 0
                };
                return updated;
              });

              // Update team-wide stats
              this.teamStats.update(current => ({
                totalAbsences: current.totalAbsences + (response.stats?.totalAbsences ?? 0),
                totalDays: current.totalDays + (response.stats?.totalDays ?? 0)
              }));
            },
            error: (err) => console.error(`Failed to load stats for subordinate ${emp.id}`, err)
          });
        });

        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load subordinates', err);
        this.isLoading.set(false);
      }
    });
  }

  viewEmployeeAbsences(employeeId: string) {
    this.router.navigate([`${this.routePrefix()}/absences/employee`, employeeId]);
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
}
