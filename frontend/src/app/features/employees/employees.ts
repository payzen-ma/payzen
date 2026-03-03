import { Component, signal, computed, OnInit, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { ToastModule } from 'primeng/toast';
// Dialog/MultiSelect/Toast removed for quick-action
import { TagComponent } from '../../shared/components/tag/tag.component';
import { TagVariant } from '../../shared/components/tag/tag.types';
import { EmptyState } from '../../shared/components/empty-state/empty-state';
import { AvatarModule } from 'primeng/avatar';
import { BadgeModule } from 'primeng/badge';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { EmployeeService, Employee, EmployeeFilters, EmployeeStats, EmployeesResponse } from '@app/core/services/employee.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { DepartmentService } from '@app/core/services/department.service';
import { ContractTypeService } from '@app/core/services/contract-type.service';
import { MessageService } from 'primeng/api';
// Permission quick-action removed

@Component({
  selector: 'app-employees',
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    TranslateModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    SelectModule,
    ToastModule,
    TagComponent,
    EmptyState,
    AvatarModule,
    BadgeModule,
    IconFieldModule,
    InputIconModule
  ],
  providers: [MessageService],
  templateUrl: './employees.html',
  styleUrl: './employees.css'
})
export class EmployeesPage implements OnInit {
  readonly searchQuery = signal('');
  readonly selectedDepartment = signal<string | null>(null);
  readonly selectedStatus = signal<string | null>(null);

  readonly employees = signal<Employee[]>([]);
  readonly departments = signal<Array<{ label: string; value: string | null }>>([]);
  readonly statuses = signal<Array<{ label: string; value: string | null }>>([]);
  readonly contractTypes = signal<Array<{ label: string; value: string | null }>>([]);
  readonly isLoading = signal<boolean>(false);
  readonly error = signal<string | null>(null);
  readonly stats = signal<EmployeeStats>({
    total: 0,
    active: 0
  });

  // quick-role removed

  get searchQueryModel(): string {
    return this.searchQuery();
  }

  set searchQueryModel(value: string) {
    this.searchQuery.set(value);
  }

  get selectedDepartmentModel(): string | null {
    return this.selectedDepartment();
  }

  set selectedDepartmentModel(value: string | null) {
    this.selectedDepartment.set(value);
  }

  get selectedStatusModel(): string | null {
    return this.selectedStatus();
  }

  set selectedStatusModel(value: string | null) {
    this.selectedStatus.set(value);
  }

  get disableClearButton(): boolean {
    return (!this.searchQuery() && !this.selectedDepartment() && !this.selectedStatus()) || this.isLoading();
  }

  readonly statCards = [
    {
      label: 'employees.stats.total',
      accessor: (stats: EmployeeStats) => stats.total,
      icon: 'pi pi-users',
      iconColor: 'text-blue-500',
      valueClass: ''
    },
    {
      label: 'employees.stats.active',
      accessor: (stats: EmployeeStats) => stats.active,
      icon: 'pi pi-check-circle',
      iconColor: 'text-green-500',
      valueClass: 'text-success'
    }
  ];

  readonly filteredEmployees = computed(() => {
    let result = this.employees();

    // Filter by search query
    if (this.searchQuery()) {
      const query = this.searchQuery().toLowerCase();
      result = result.filter(emp =>
        (emp.firstName || '').toLowerCase().includes(query) ||
        (emp.lastName || '').toLowerCase().includes(query) ||
        (emp.position || '').toLowerCase().includes(query)
      );
    }

    // Filter by department
    if (this.selectedDepartment()) {
      const sel = (this.selectedDepartment() || '').toString().trim().toLowerCase();
      result = result.filter(emp => ((emp.department || '').toString().trim().toLowerCase() === sel));
    }

    // Filter by status: prefer backend raw code when available
    if (this.selectedStatus()) {
      const selStatus = (this.selectedStatus() || '').toString().trim().toLowerCase();
      result = result.filter(emp => {
        const raw = (emp as any).statusRaw ?? emp.status;
        return (raw || '').toString().trim().toLowerCase() === selStatus;
      });
    }

    return result;
  });

  // Route prefix based on current context mode
  private readonly contextService = inject(CompanyContextService);
  private readonly departmentService = inject(DepartmentService);
  private readonly contractTypeService = inject(ContractTypeService);
  private readonly destroyRef = inject(DestroyRef);
  readonly routePrefix = computed(() => this.contextService.isExpertMode() ? '/expert' : '/app');

  constructor(
    private router: Router,
    private employeeService: EmployeeService,
    private translate: TranslateService
  ) {}

  ngOnInit(): void {
    this.initializeFilterDefaults();

    // Load data immediately
    this.loadEmployees();

    // Subscribe to context changes to reload data
    this.contextService.contextChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.loadEmployees();
      });

    this.translate.onLangChange
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.localizeFilterDefaults();
      });

    // Load status options from referential API (use includeInactive=true to get full list)
    this.employeeService.getStatuses(true).subscribe({
      next: (items) => {
        const opts = (items || []).map(i => ({ label: i.label, value: (i as any).value ?? String(i.id) }));
        this.statuses.set([
          { label: this.t('employees.filter.allStatuses'), value: null },
          ...opts
        ]);
        // debug current employees and how they'll map
        // If employees are already loaded, populate their statusName from the referential labels
        const current = this.employees();
          if (current && current.length) {
          const updated = current.map(emp => {
            const candidates = new Set<string>();
            candidates.add(this.normalizeStatusCode((emp as any).statusRaw ?? ''));
            candidates.add(this.normalizeStatusCode(emp.status ?? ''));
            candidates.add(this.normalizeStatusCode(emp.statusName ?? ''));
            const norm = this.normalizeStatusCode(emp.status ?? '');
            if (norm === 'active') candidates.add(this.normalizeStatusCode('actif'));
            if (norm === 'onleave' || norm === 'onleave' ) candidates.add(this.normalizeStatusCode('conge'));
            if (norm === 'inactive') candidates.add('inact');

            let match: any = null;
            for (const o of opts) {
              const anyO = o as any;
              const val = this.normalizeStatusCode(anyO.value ?? anyO.id ?? anyO.label ?? '');
              const lbl = this.normalizeStatusCode(o.label ?? '');
              // exact normalized match
              if (candidates.has(val) || candidates.has(lbl)) { match = o; break; }
              // contains match
              for (const c of Array.from(candidates)) {
                if (c && (val.includes(c) || lbl.includes(c))) { match = o; break; }
              }
              if (match) break;
            }

            return match ? { ...emp, statusName: match.label } : emp;
          });
          this.employees.set(updated);
        }
      },
      error: (err) => {
        console.error('Failed to load statuses from API', err);
      }
    });

    // Load company-specific contract types when context available
    const cid = this.contextService.companyId();
    if (cid) {
      const num = Number(cid);
      if (!Number.isNaN(num) && num) {
        this.contractTypeService.getByCompany(num).subscribe({
          next: (items) => {
            const opts = (items || []).map(i => ({ label: i.contractTypeName ?? i.contractTypeName, value: String(i.contractTypeName ?? i.id) }));
            this.contractTypes.set([
              { label: this.t('employees.filter.allContractTypes'), value: null },
              ...opts
            ]);
          },
          error: () => {}
        });
      }
    }
  }

  

  /**
   * Load employees from backend
   */
  loadEmployees(): void {
    this.isLoading.set(true);
    this.error.set(null);

    const filters: EmployeeFilters = {
      searchQuery: this.searchQuery() || undefined,
      department: this.selectedDepartment() || undefined,
      status: this.selectedStatus() || undefined,
      companyId: this.contextService.companyId() ?? undefined
    };

    this.employeeService.getEmployees(filters).subscribe({
      next: (response: EmployeesResponse) => {
        // Set employees
        const respEmployees = response.employees || [];
        // If we already have referential status options, enrich employees with statusName
        const opts = this.statuses();
        const enriched = (respEmployees || []).map(emp => {
          if (opts && opts.length > 0) {
            const code = String((emp as any).statusRaw ?? emp.status ?? '').toLowerCase();
            const match = opts.find(o => {
              const anyO = o as any;
              const val = String(anyO.value ?? anyO.id ?? anyO.label ?? '').toLowerCase();
              const lbl = String(o.label ?? '').toLowerCase();
              return val === code || lbl === code || val.includes(code) || lbl.includes(code);
            });
            return match ? { ...emp, statusName: match.label } : emp;
          }
          return emp;
        });
        this.employees.set(enriched);
        this.stats.set({ total: response.total, active: response.active });
        this.departments.set([
          { label: this.t('employees.filter.allDepartments'), value: null },
          ...this.buildDepartmentOptions(response.departments)
        ]);
        // If backend did not return departments (common for expert/company-specific endpoints),
        // fall back to fetching departments explicitly by companyId from the DepartmentService.
        if ((!response.departments || response.departments.length === 0) && this.contextService.companyId()) {
          this.departmentService.getByCompany(Number(this.contextService.companyId())).subscribe({
            next: (deps) => {
              this.departments.set([
                { label: this.t('employees.filter.allDepartments'), value: null },
                ...this.buildDepartmentOptions(deps.map(d => d.departementName))
              ]);
            },
            error: () => {
              // ignore - keep existing minimal options
            }
          });
        }
        // Status options are loaded from the referential API in ngOnInit; do not overwrite here.
        this.isLoading.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.message || this.t('employees.errors.loadFailed'));
        this.isLoading.set(false);
        console.error('Error loading employees:', err);
      }
    });
  }

  /**
   * Refresh employees when filters change
   */
  applyFilters(): void {
    this.loadEmployees();
  }

  getFullName(employee: Employee): string {
    return `${employee.firstName} ${employee.lastName}`;
  }

  getInitials(employee: Employee): string {
    return `${employee.firstName.charAt(0)}${employee.lastName.charAt(0)}`;
  }

  // Normalize a status/code/label for robust matching (remove non-alphanum, lowercase)
  private normalizeStatusCode(val: any): string {
    if (val === undefined || val === null) return '';
    return String(val).toLowerCase().replace(/[^a-z0-9]+/g, '');
  }

  getEmployeeStatusLabel(employee: Employee): string {
    const opts = this.statuses();
    if (opts && opts.length) {
      const code = this.normalizeStatusCode((employee as any).statusRaw ?? employee.status ?? employee.statusName ?? '');
      for (const o of opts) {
        const anyO = o as any;
        const val = this.normalizeStatusCode(anyO.value ?? anyO.id ?? anyO.label ?? '');
        const lbl = this.normalizeStatusCode(o.label ?? '');
        if (val === code || lbl === code || val.includes(code) || lbl.includes(code)) {
          return o.label;
        }
      }
    }
    return employee.statusName || this.getStatusLabel((employee as any).status ?? '');
  }

  getStatusSeverity(status: string): TagVariant {
    // Prefer API-provided status options to infer severity when possible
    try {
      const opts = this.statuses();
      if (opts && opts.length) {
        const found = opts.find(o => {
          const anyO = o as any;
          const val = anyO.value ?? String(anyO.id ?? anyO.value ?? '');
          return String(val).toLowerCase() === String(status).toLowerCase() || String(o.label).toLowerCase() === String(status).toLowerCase();
        });
        if (found) {
          const key = ((found as any).value ?? found.label).toString().toLowerCase();
          if (key.includes('active') || key.includes('actif') || key.includes('enabled')) return 'success';
          if (key.includes('leave') || key.includes('cong') || key.includes('abs') || key.includes('on_leave') || key.includes('on-leave')) return 'warning';
          if (key.includes('resign') || key.includes('retir') || key.includes('suspend') || key.includes('term') || key.includes('left') || key.includes('depart')) return 'danger';
        }
      }
    } catch (e) {
      // ignore and fall back
    }

    const severityMap: Record<string, TagVariant> = {
      active: 'success',
      on_leave: 'warning',
      inactive: 'danger'
    };
    return severityMap[status] || 'warning';
  }

  getStatusLabel(status: string): string {
    // Prefer API-provided status labels when available
    try {
      const opts = this.statuses();
      if (opts && opts.length) {
        const found = opts.find(o => {
          const anyO = o as any;
          const val = anyO.value ?? String(anyO.id ?? anyO.value ?? '');
          return String(val).toLowerCase() === String(status).toLowerCase() || String(o.label).toLowerCase() === String(status).toLowerCase();
        });
        if (found) return found.label;
      }
    } catch (e) {
      // fall back to hardcoded map
    }

    const labelMap: Record<string, string> = {
      active: this.t('employees.status.active'),
      on_leave: this.t('employees.status.onLeave'),
      inactive: this.t('employees.status.inactive'),
      retired: 'RETIRED'
    };
    return labelMap[status] || status;
  }

  getContractTypeVariant(type: string): TagVariant {
    const variantMap: Record<string, TagVariant> = {
      CDI: 'success',
      CDD: 'info',
      Stage: 'warning'
    };
    return variantMap[type] || 'default';
  }

  manageRolesForEmployee(employee: Employee, event?: Event): void {
    // quick-action removed
  }
  

  getContractTypeLabel(employee: Employee): string {
    try {
      const opts = this.contractTypes();
      if (opts && opts.length) {
        const code = (employee.contractType || '').toString().toLowerCase().replace(/[^a-z0-9]+/g, '');
        // If company exists but no contract types loaded, don't show any label
        const cid = this.contextService.companyId();
        if ((opts.length === 1 && (opts[0].value === null || opts[0].value === undefined)) && cid) {
          return '';
        }
        if (!code) return employee.contractType || '';
        for (const o of opts) {
          const val = (o.value ?? '').toString().toLowerCase().replace(/[^a-z0-9]+/g, '');
          const lbl = (o.label ?? '').toString().toLowerCase().replace(/[^a-z0-9]+/g, '');
          if (val === code || lbl === code || (code && (val.includes(code) || lbl.includes(code)))) return o.label || employee.contractType;
        }
      }
    } catch (e) {}
    return employee.contractType || '';
  }

  viewEmployee(employee: Employee) {
    this.router.navigate([`${this.routePrefix()}/employees`, employee.id]);
  }

  addEmployee() {
    this.router.navigate([`${this.routePrefix()}/employees`, 'create']);
  }

  clearFilters() {
    this.searchQuery.set('');
    this.selectedDepartment.set(null);
    this.selectedStatus.set(null);
    this.loadEmployees();
  }

  private buildDepartmentOptions(departments: string[] = []): Array<{ label: string; value: string | null }> {
    const uniqueDepartments = Array.from(new Set(departments.filter(Boolean)));
    return uniqueDepartments.map(dep => ({
      label: dep,
      value: dep
    }));
  }

  private buildStatusOptions(statuses: string[] = []): Array<{ label: string; value: string | null }> {
    const uniqueStatuses = Array.from(new Set(statuses.filter(Boolean)));
    return uniqueStatuses.map(status => ({
      label: this.getStatusLabel(status) || status,
      value: status
    }));
  }

  private t(key: string, params?: Record<string, unknown>): string {
    const translated = this.translate.instant(key, params);
    return typeof translated === 'string' ? translated : key;
  }

  private initializeFilterDefaults(): void {
    this.departments.set([{ label: this.t('employees.filter.allDepartments'), value: null }]);
    this.statuses.set([{ label: this.t('employees.filter.allStatuses'), value: null }]);
    this.contractTypes.set([{ label: this.t('employees.filter.allContractTypes'), value: null }]);
  }

  private localizeFilterDefaults(): void {
    this.departments.update(items => this.relabelDefaultOption(items, this.t('employees.filter.allDepartments')));
    this.statuses.update(items => this.relabelDefaultOption(items, this.t('employees.filter.allStatuses')));
    this.contractTypes.update(items => this.relabelDefaultOption(items, this.t('employees.filter.allContractTypes')));
  }

  private relabelDefaultOption(
    items: Array<{ label: string; value: string | null }>,
    label: string
  ): Array<{ label: string; value: string | null }> {
    if (!items.length) return [{ label, value: null }];
    return items.map((item, index) => (index === 0 && item.value === null ? { ...item, label } : item));
  }
}
