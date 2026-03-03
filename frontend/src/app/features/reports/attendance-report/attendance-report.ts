import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { TranslateModule } from '@ngx-translate/core';
import { EmployeeService, Employee } from '@app/core/services/employee.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '@environments/environment';
import { forkJoin, map } from 'rxjs';

interface ReportRow {
  id: string;
  name: string;
  position?: string;
  department?: string;
  totalWorkedHours?: number; // in hours
  totalWorkedDays?: number;
  totalBreakMinutes?: number;
}

@Component({
  selector: 'app-attendance-report',
  standalone: true,
  imports: [CommonModule, ButtonModule, CardModule, TableModule, InputTextModule, TranslateModule],
  templateUrl: './attendance-report.html',
  styleUrls: ['./attendance-report.css']
})
export class AttendanceReportPage implements OnInit {
  private readonly employeeService = inject(EmployeeService);
  private readonly http = inject(HttpClient);

  readonly employees = signal<Employee[]>([]);
  readonly reportRows = signal<ReportRow[]>([]);
  readonly isLoading = signal(false);

  // simple date range signals
  readonly startDate = signal<Date | null>(null);
  readonly endDate = signal<Date | null>(null);

  ngOnInit(): void {
    // default range: last 7 days
    const end = new Date();
    const start = new Date();
    start.setDate(end.getDate() - 7);
    this.startDate.set(start);
    this.endDate.set(end);

    this.loadEmployees();
  }

  setStartDateFromInput(value: string | null): void {
    if (!value) { this.startDate.set(null); return; }
    const d = new Date(value);
    this.startDate.set(isNaN(d.getTime()) ? null : d);
  }

  setEndDateFromInput(value: string | null): void {
    if (!value) { this.endDate.set(null); return; }
    const d = new Date(value);
    this.endDate.set(isNaN(d.getTime()) ? null : d);
  }

  private loadEmployees(): void {
    this.employeeService.getEmployees().subscribe({
      next: (res) => {
        console.log('👥 Loaded Employees:', res.employees);
        this.employees.set(res.employees || []);
      },
      error: (err) => {
        console.error('Failed to load employees', err);
        this.employees.set([]);
      }
    });
  }

  private formatHours(totalHours?: number): string {
    if (totalHours == null) return '-';
    const hours = Math.floor(totalHours);
    const minutes = Math.round((totalHours - hours) * 60);
    return `${hours}h${String(minutes).padStart(2, '0')}`;
  }

  generateReport(): void {
    const start = this.startDate();
    const end = this.endDate();
    if (!start || !end) return;

    const startStr = start.toISOString().slice(0,10);
    const endStr = end.toISOString().slice(0,10);

    const list = this.employees();
    if (!list.length) {
      this.reportRows.set([]);
      return;
    }

    this.isLoading.set(true);

    console.log('🔍 Generating report for employees:', list.map(e => ({ employeeId: e.id, userId: e.userId, name: `${e.firstName} ${e.lastName}` })));

    const requests = list.map(emp => {
      // Use userId because backend stores userId in the attendance employeeId field
      const idToUse = emp.userId || emp.id;
      console.log(`API Call for ${emp.firstName} ${emp.lastName}: /employee-attendance/employee/${idToUse}`);
      return this.http.get<any[]>(`${environment.apiUrl}/employee-attendance/employee/${idToUse}?startDate=${startStr}&endDate=${endStr}`)
        .pipe(map(records => ({ emp, records })));
    });

    forkJoin(requests).subscribe({
      next: (results: Array<{ emp: Employee; records: any[] }>) => {
        console.log('📊 API Response - All Employee Attendance Records:', results);
        const rows: ReportRow[] = results.map(r => {
          const totalWorked = (r.records || []).reduce((acc, x) => acc + (Number(x.workedHours ?? x.WorkedHours ?? 0) || 0), 0);
          console.log("Calculating totals for employee:", r.emp.id, r.emp.firstName, r.emp.lastName);
          console.log("Total Worked Hours Calculation:", r.records, "=>", totalWorked);
          const totalBreaks = (r.records || []).reduce((acc, x) => acc + (Number(x.breakMinutesApplied ?? x.BreakMinutesApplied ?? 0) || 0), 0);
          const totalWorkedDays = (r.records || []).reduce((acc, x) => {
            const worked = Number(x.workedHours ?? x.WorkedHours ?? 0);
            const hasCheckIn = !!(x.checkIn ?? x.CheckIn);
            return acc + ((worked > 0 || hasCheckIn) ? 1 : 0);
          }, 0);

          return {
            id: String(r.emp.id),
            name: `${r.emp.firstName} ${r.emp.lastName}`,
            position: r.emp.position,
            department: r.emp.department,
            totalWorkedHours: totalWorked,
            totalWorkedDays: totalWorkedDays,
            totalBreakMinutes: totalBreaks
          };
        });
        console.log('📋 Mapped Report Rows:', rows);
        this.reportRows.set(rows);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to generate report', err);
        this.reportRows.set([]);
        this.isLoading.set(false);
      }
    });
  }

  // small helper for template
  displayWorked(row: ReportRow): string {
    return this.formatHours(row.totalWorkedHours);
  }
}
