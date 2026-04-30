import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  signal,
  OnInit
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { SkeletonModule } from 'primeng/skeleton';
import { MessageModule } from 'primeng/message';

import { CompanyContextService } from '@app/core/services/companyContext.service';
import { EmployeeService, Employee, EmployeesResponse } from '@app/core/services/employee.service';
import { TaxDashboardComponent } from './tax-dashboard.component';

/**
 * Page wrapper pour le dashboard fiscal IR.
 * Récupère l'employé via query param ?employeeId=X
 * ou liste les employés si aucun n'est sélectionné.
 */
@Component({
  selector: 'app-tax-dashboard-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    FormsModule,
    SelectModule,
    SkeletonModule,
    MessageModule,
    TaxDashboardComponent
  ],
  template: `
    <div class="tax-dashboard-page">

      <!-- Sélecteur d'employé -->
      <div class="employee-selector-bar">
        <label class="selector-label">
          <i class="pi pi-user"></i>
          Employé
        </label>
        <p-select
          [options]="employeeOptions()"
          [ngModel]="selectedEmployeeId()"
          (ngModelChange)="onEmployeeChange($event)"
          optionLabel="label"
          optionValue="value"
          placeholder="Sélectionnez un employé..."
          [filter]="true"
          filterBy="label"
          styleClass="employee-select"
          [appendTo]="'body'"
        />
      </div>

      @if (isLoadingEmployees()) {
        <div class="loading-state">
          <p-skeleton height="400px" borderRadius="12px" />
        </div>
      }

      @if (!isLoadingEmployees() && !selectedEmployee()) {
        <div class="no-employee-state">
          <p-message
            severity="info"
            text="Sélectionnez un employé pour afficher son tableau de bord fiscal."
          />
        </div>
      }

      @if (selectedEmployee(); as emp) {
        <app-tax-dashboard
          [employeeId]="+emp.id"
          [companyId]="companyId()"
          [employeeName]="emp.firstName + ' ' + emp.lastName"
        />
      }

    </div>
  `,
  styles: [`
    .tax-dashboard-page {
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
    }

    .employee-selector-bar {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      background: var(--surface-card, #fff);
      border-radius: var(--radius-lg, 12px);
      padding: 1rem 1.25rem;
      box-shadow: var(--shadow-md);
    }

    .selector-label {
      display: flex;
      align-items: center;
      gap: 0.4rem;
      font-weight: 600;
      font-size: 0.875rem;
      color: var(--surface-700);
      white-space: nowrap;
    }

    .selector-label .pi {
      color: var(--primary-600);
    }

    :host ::ng-deep .employee-select {
      min-width: 320px;
    }

    .loading-state,
    .no-employee-state {
      padding: 1rem 0;
    }
  `]
})
export class TaxDashboardPageComponent implements OnInit {
  private readonly contextService = inject(CompanyContextService);
  private readonly employeeService = inject(EmployeeService);
  private readonly route = inject(ActivatedRoute);

  readonly companyId = computed(() => Number(this.contextService.companyId() ?? 0));

  readonly employees = signal<Employee[]>([]);
  readonly isLoadingEmployees = signal(false);
  readonly selectedEmployeeId = signal<string | null>(null);

  readonly selectedEmployee = computed(() =>
    this.employees().find(e => e.id === this.selectedEmployeeId()) ?? null
  );

  readonly employeeOptions = computed(() =>
    this.employees().map(e => ({
      label: `${e.firstName} ${e.lastName}`,
      value: String(e.id)
    }))
  );

  ngOnInit(): void {
    this.loadEmployees();

    const paramId = this.route.snapshot.queryParamMap.get('employeeId');
    if (paramId) {
      this.selectedEmployeeId.set(paramId);
    }
  }

  private loadEmployees(): void {
    this.isLoadingEmployees.set(true);
    this.employeeService.getEmployees().subscribe({
      next: (response: EmployeesResponse) => {
        this.employees.set(response.employees);
        this.isLoadingEmployees.set(false);
      },
      error: () => {
        this.isLoadingEmployees.set(false);
      }
    });
  }

  onEmployeeChange(id: string): void {
    this.selectedEmployeeId.set(id);
  }
}
