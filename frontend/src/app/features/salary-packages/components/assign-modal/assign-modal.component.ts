import { Component, signal, computed, output, input, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '@environments/environment';
import { SalaryPackage } from '@app/core/models/salary-package.model';
import { EmployeeService, Employee } from '@app/core/services/employee.service';

interface EmployeeContract {
  id: number;
  type: string;
  startDate: string;
  endDate?: string | null;
}

@Component({
  selector: 'app-assign-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './assign-modal.component.html'
})
export class AssignModalComponent implements OnInit {
  private readonly employeeService = inject(EmployeeService);
  private readonly http = inject(HttpClient);
  private readonly translate = inject(TranslateService);
  private readonly baseUrl = environment.apiUrl.replace('/api', '');

  // Inputs
  package = input.required<SalaryPackage>();
  isOpen = input.required<boolean>();

  // Outputs
  close = output<void>();
  assign = output<{ employeeId: number; contractId: number; effectiveDate: string }>();

  // State
  employees = signal<Employee[]>([]);
  isLoadingEmployees = signal(true);
  selectedEmployeeId = signal<number | null>(null);
  activeContract = signal<EmployeeContract | null>(null);
  isLoadingContract = signal(false);
  effectiveDate = signal<string>(this.getTodayString());
  isSubmitting = signal(false);
  errorMessage = signal<string | null>(null);

  canSubmit = computed(() =>
    this.selectedEmployeeId() !== null &&
    this.activeContract() !== null &&
    this.effectiveDate() !== '' &&
    !this.isSubmitting() &&
    !this.isLoadingContract()
  );

  ngOnInit() {
    this.loadEmployees();
  }

  loadEmployees() {
    this.isLoadingEmployees.set(true);
    this.employeeService.getEmployees().subscribe({
      next: (response) => {
        this.employees.set(response.employees.filter(e => e.status === 'active'));
        this.isLoadingEmployees.set(false);
      },
      error: () => {
        this.errorMessage.set(this.t('salaryPackages.assignModal.errors.loadEmployees'));
        this.isLoadingEmployees.set(false);
      }
    });
  }

  onEmployeeChange(employeeId: string) {
    const id = parseInt(employeeId, 10);
    if (isNaN(id)) {
      this.selectedEmployeeId.set(null);
      this.activeContract.set(null);
      return;
    }

    this.selectedEmployeeId.set(id);
    this.errorMessage.set(null);
    this.isLoadingContract.set(true);
    this.activeContract.set(null);

    // Fetch employee contracts from backend
    this.http.get<any[]>(`${this.baseUrl}/api/employee-contracts/employee/${id}`).subscribe({
      next: (contracts) => {
        // Find the active contract (no end date or end date in the future)
        const now = new Date();
        const active = contracts.find(c => {
          const endDate = c.EndDate ?? c.endDate;
          return !endDate || new Date(endDate) > now;
        });

        if (active) {
          this.activeContract.set({
            id: active.Id ?? active.id,
            type: active.ContractTypeName ?? active.contractTypeName ?? active.ContractType ?? active.contractType ?? 'CDI',
            startDate: active.StartDate ?? active.startDate,
            endDate: active.EndDate ?? active.endDate ?? null
          });
        } else {
          this.errorMessage.set(this.t('salaryPackages.assignModal.errors.noActiveContract'));
        }
        this.isLoadingContract.set(false);
      },
      error: () => {
        this.errorMessage.set(this.t('salaryPackages.assignModal.errors.loadContract'));
        this.isLoadingContract.set(false);
      }
    });
  }

  onSubmit() {
    if (!this.canSubmit()) return;

    const contract = this.activeContract();
    const employeeId = this.selectedEmployeeId();
    if (!contract || !employeeId) return;

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    this.assign.emit({
      employeeId,
      contractId: contract.id,
      effectiveDate: this.effectiveDate()
    });
  }

  setSubmitting(value: boolean) {
    this.isSubmitting.set(value);
  }

  setError(message: string) {
    this.errorMessage.set(message);
    this.isSubmitting.set(false);
  }

  onClose() {
    this.close.emit();
    this.resetForm();
  }

  resetForm() {
    this.selectedEmployeeId.set(null);
    this.activeContract.set(null);
    this.effectiveDate.set(this.getTodayString());
    this.isSubmitting.set(false);
    this.errorMessage.set(null);
  }

  getTodayString(): string {
    return new Date().toISOString().split('T')[0];
  }

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('fr-MA', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('fr-MA', {
      style: 'currency',
      currency: 'MAD',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(value);
  }

  private t(key: string, params?: Record<string, unknown>): string {
    const translated = this.translate.instant(key, params);
    return typeof translated === 'string' ? translated : key;
  }
}
