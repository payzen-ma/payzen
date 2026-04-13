import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { DatePickerModule } from 'primeng/datepicker';
import { ToastModule } from 'primeng/toast';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { EmployeeService } from '@app/core/services/employee.service';
import { ActivatedRoute, Router } from '@angular/router';
import { OvertimeService } from '@app/core/services/overtime.service';
import { MessageService } from 'primeng/api';
import { CreateOvertimeRequest, OvertimeType } from '@app/core/models/overtime.model';

@Component({
  selector: 'app-hr-overtime',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, TableModule, ButtonModule, DialogModule, InputTextModule, DatePickerModule, ToastModule, TranslateModule],
  providers: [MessageService],
  templateUrl: './hr-employees.html',
  styleUrls: ['./hr-employees.css']
})
export class HrEmployeesComponent implements OnInit {
  private readonly employeeService = inject(EmployeeService);
  private readonly overtimeService = inject(OvertimeService);
  private readonly fb = inject(FormBuilder);
  private readonly message = inject(MessageService);
  private readonly translate = inject(TranslateService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly employees = signal<any[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly showDialog = signal<boolean>(false);
  readonly selectedEmployee = signal<any | null>(null);

  overtimeForm = this.fb.group({
    overtimeDate: [null, Validators.required],
    overtimeType: [OvertimeType.Standard, Validators.required],
    startTime: [''],
    endTime: [''],
    reason: ['']
  });

  ngOnInit(): void {
    this.loadEmployees();

    // If navigated with a query param employeeId, fetch that employee and open declare dialog
    const q = this.route.snapshot.queryParamMap.get('employeeId');
    if (q) {
      const id = String(q);
      this.employeeService.getEmployeeById(id).subscribe({
        next: (emp) => {
          if (emp) {
            this.openDeclare(emp as any);
            // remove query param to avoid reopening when navigating back
            this.router.navigate([], { queryParams: { employeeId: null }, queryParamsHandling: 'merge' });
          }
        },
        error: (err) => console.error('Error loading employee by id', err)
      });
    }
  }

  loadEmployees(): void {
    this.isLoading.set(true);
    this.employeeService.getEmployees().subscribe({
      next: (resp) => {
        this.employees.set(resp.employees || []);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
      }
    });
  }

  openDeclare(employee: any): void {
    this.selectedEmployee.set(employee);
    this.overtimeForm.reset({ overtimeType: OvertimeType.Standard, startTime: '', endTime: '', reason: '' });
    this.showDialog.set(true);
  }

  closeDialog(): void {
    this.showDialog.set(false);
    this.selectedEmployee.set(null);
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
    if (this.overtimeForm.invalid) {
      this.overtimeForm.markAllAsTouched();
      return;
    }
    const emp = this.selectedEmployee();
    if (!emp) return;
    const v = this.overtimeForm.value;
    // Build CreateOvertimeRequest according to model: map fields to entryMode and employeeComment
    // This form only handles HoursRange entry mode with start/end times
    const entryModeNumeric = 1; // HoursRange

    const payload: CreateOvertimeRequest = {
      employeeId: Number(emp.id),
      overtimeDate: this.formatDateLocal(v.overtimeDate),
      entryMode: entryModeNumeric,
      startTime: v.startTime ? String(v.startTime).slice(0,5) : undefined,
      endTime: v.endTime ? String(v.endTime).slice(0,5) : undefined,
      employeeComment: v.reason || undefined
    };

    this.overtimeService.createOvertime(payload).subscribe({
      next: () => {
        this.message.add({ severity: 'success', summary: this.translate.instant('common.success'), detail: this.translate.instant('overtime.messages.createSuccess') });
        this.closeDialog();
      },
      error: (err) => {
        this.message.add({ severity: 'error', summary: this.translate.instant('common.error'), detail: this.translate.instant('overtime.errors.createFailed') });
      }
    });
  }

  /** Navigate to employee profile page */
  viewEmployee(employee: any): void {
    const id = employee?.id ?? employee?.Id ?? employee;
    if (!id) return;
    this.router.navigate(['/app/employees', id]);
  }
}
