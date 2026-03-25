import { Component, OnInit, computed, inject, signal, effect } from '@angular/core';
import { toSignal, takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';
import { forkJoin, of } from 'rxjs';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { InputFieldComponent } from '@app/shared/components/form-controls/input-field';
import { SelectFieldComponent } from '@app/shared/components/form-controls/select-field';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { JobPositionService } from '@app/core/services/job-position.service';
import { ContractTypeService } from '@app/core/services/contract-type.service';
import { AttendanceTypeService, AttendanceTypeLookupOption } from '@app/core/services/attendance-type.service';
import { EmployeeCategoryService, EmployeeCategoryLookupOption } from '@app/core/services/employee-category.service';
import { SalaryPackageService } from '@app/core/services/salary-package.service';
import { UserService } from '@app/core/services/user.service';
import {
  CityLookupOption,
  CreateEmployeeRequest,
  EmployeeFormData,
  EmployeeService,
  LookupOption,
  ManagerLookupOption
} from '@app/core/services/employee.service';
import { JobPosition } from '@app/core/models/job-position.model';
import { ContractType } from '@app/core/models/contract-type.model';
import { environment } from '@environments/environment';

@Component({
  selector: 'app-employee-create',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    TranslateModule,
    ButtonModule,
    InputTextModule,
    SelectModule,
    AutoCompleteModule,
    ToastModule,
    InputFieldComponent,
    SelectFieldComponent
  ],
  templateUrl: './employee-create.html',
  styleUrl: './employee-create.css',
  providers: [MessageService]
})
export class EmployeeCreatePage implements OnInit {
  readonly isLoading = signal<boolean>(true);
  readonly isSubmitting = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly salaryPackageOptions = signal<LookupOption[]>([]);
  readonly inviteRoleOptions = signal<LookupOption[]>([]);
  private readonly baseUrl = environment.apiUrl.replace('/api', '');
  private readonly emptyFormData: EmployeeFormData = {
    statuses: [],
    genders: [],
    educationLevels: [],
    maritalStatuses: [],
    nationalities: [],
    countries: [],
    cities: [],
    departments: [],
    jobPositions: [],
    contractTypes: [],
    potentialManagers: [],
    attendanceTypes: [],
    employeeCategories: []
  };
  readonly formData = signal<EmployeeFormData>(this.emptyFormData);
  private readonly fb = inject(FormBuilder);
  private readonly employeeService = inject(EmployeeService);
  private readonly salaryPackageService = inject(SalaryPackageService);
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly translate = inject(TranslateService);
  private readonly messageService = inject(MessageService);
  private readonly contextService = inject(CompanyContextService);
  private readonly userService = inject(UserService);
  private readonly jobPositionService = inject(JobPositionService);
  private readonly contractTypeService = inject(ContractTypeService);
  private readonly attendanceTypeService = inject(AttendanceTypeService);
  private readonly employeeCategoryService = inject(EmployeeCategoryService);

  // Route prefix based on current context mode
  readonly routePrefix = computed(() => this.contextService.isExpertMode() ? '/expert' : '/app');

  readonly employeeForm = this.fb.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    // Rôle utilisé pour envoyer l'invitation (sans mot de passe, activation via Entra/Google)
    inviteRoleId: [null as number | null, Validators.required],
    cinNumber: ['', Validators.required],
    birthDate: ['', Validators.required],
    phone: ['', Validators.required],
    phoneCountryId: [null as number | null, Validators.required],
    statusId: [null, Validators.required],
    genderId: [null],
    educationLevelId: [null],
    maritalStatusId: [null],
    nationalityId: [null],
    countryId: [null as number | null],
    cityId: [null],
    addressLine1: [''],
    addressLine2: [''],
    zipCode: [''],
    departmentId: [null as number | null, Validators.required],
    jobPositionId: [null as number | null, Validators.required],
    contractTypeId: [null, Validators.required],
    managerId: [null],
    startDate: ['', Validators.required],
    salary: [null, Validators.min(0)],
    salaryHourly: [null, Validators.min(0)],
    salaryEffectiveDate: [null as string | null],
    salaryPackageId: [null as number | null],
    attendanceTypeId: [null],
    employeeCategoryId: [null]
  });

  readonly selectedCountryId = toSignal(this.employeeForm.controls.countryId.valueChanges, {
    initialValue: null
  });

  constructor() {
    this.employeeForm.controls.countryId.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        this.employeeForm.controls.cityId.setValue(null);
      });
  }

  readonly phoneCode = computed(() => {
    const phoneCountryId = this.employeeForm.controls.phoneCountryId.value;
    return this.formData().countries.find(country => country.id === phoneCountryId)?.phoneCode ?? '';
  });

  readonly cityOptions = computed<CityLookupOption[]>(() => {
    const countryId = this.selectedCountryId();
    const cities = this.formData().cities;
    if (!countryId) {
      return [];
    }
    return cities.filter(city => city.countryId === Number(countryId));
  });

  readonly filteredDepartments = signal<LookupOption[]>([]);
  readonly filteredJobPositions = signal<LookupOption[]>([]);
  readonly selectedDepartment = signal<LookupOption | string | null>(null);
  readonly selectedJobPosition = signal<LookupOption | string | null>(null);

  searchDepartment(event: any) {
    const companyIdRaw = this.contextService.companyId();
    const companyId = companyIdRaw !== null && companyIdRaw !== undefined ? Number(companyIdRaw) : undefined;
    this.employeeService.searchDepartments(event.query, companyId).subscribe(data => {
      this.filteredDepartments.set(data);
    });
  }

  searchJobPosition(event: any) {
    const companyIdRaw = this.contextService.companyId();
    const companyId = companyIdRaw !== null && companyIdRaw !== undefined ? Number(companyIdRaw) : undefined;
    this.employeeService.searchJobPositions(event.query, companyId).subscribe(data => {
      this.filteredJobPositions.set(data);
    });
  }

  private resolveLookupId(value: any, options: LookupOption[] | undefined): number | null {
    if (value == null) {
      return null;
    }
    if (typeof value === 'object') {
      const raw = value.id ?? value.Id;
      if (raw != null && raw !== '') {
        const n = Number(raw);
        return Number.isNaN(n) ? null : n;
      }
      const label = (value.label ?? value.Label ?? '').toString().trim().toLowerCase();
      if (label && options?.length) {
        const hit = options.find((o) => o.label.trim().toLowerCase() === label);
        return hit?.id != null ? Number(hit.id) : null;
      }
      return null;
    }
    return null;
  }

  onDepartmentChange(value: any) {
    this.selectedDepartment.set(value);
    const id = this.resolveLookupId(value, this.formData().departments);
    if (id != null) {
      this.employeeForm.controls.departmentId.setValue(id);
    } else {
      this.employeeForm.controls.departmentId.setValue(null);
    }
  }

  onJobPositionChange(value: any) {
    this.selectedJobPosition.set(value);
    const id = this.resolveLookupId(value, this.formData().jobPositions);
    if (id != null) {
      this.employeeForm.controls.jobPositionId.setValue(id);
    } else {
      this.employeeForm.controls.jobPositionId.setValue(null);
    }
  }

  ngOnInit(): void {
    this.loadFormData();
    this.loadInviteRoles();

    // Reload form lookups when the selected company changes
    this.contextService.contextChanged$
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        this.formData.set(this.emptyFormData);
        this.loadFormData();
      });
  }

  loadFormData(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.employeeService.getEmployeeFormData().subscribe({
      next: (data) => {
        this.formData.set(data);
        if (data.countries.length) {
          const defaultCountry = this.findDefaultCountry(data.countries);
          if (!this.employeeForm.controls.phoneCountryId.value) {
            this.employeeForm.controls.phoneCountryId.setValue(defaultCountry?.id ?? data.countries[0].id);
          }
          if (!this.employeeForm.controls.countryId.value) {
            this.employeeForm.controls.countryId.setValue(defaultCountry?.id ?? data.countries[0].id);
          }
        }
        this.isLoading.set(false);
        // Load attendance types
        this.attendanceTypeService.getLookupOptions().subscribe({
          next: (types) => this.formData.update(f => ({ ...f, attendanceTypes: types })),
          error: (err) => console.error('Failed to load attendance types', err)
        });
        // Load employee categories
        const companyIdRaw = this.contextService.companyId();
        if (companyIdRaw !== null && companyIdRaw !== undefined) {
          const companyIdNum = Number(companyIdRaw as any);
          if (!Number.isNaN(companyIdNum)) {
            this.employeeCategoryService.getLookupOptions(companyIdNum).subscribe({
              next: (categories) => this.formData.update(f => ({ ...f, employeeCategories: categories })),
              error: (err) => console.error('Failed to load employee categories', err)
            });
          }
        }
        // Fallbacks: if backend didn't return company-specific job positions or contract types,
        // load them explicitly using companyId from context (expert mode).
        if (companyIdRaw !== null && companyIdRaw !== undefined) {
          const companyIdNum = Number(companyIdRaw as any);
          if (!Number.isNaN(companyIdNum)) {
            if (!data.jobPositions || data.jobPositions.length === 0) {
              this.jobPositionService.getByCompany(companyIdNum).subscribe({
                next: (items: JobPosition[]) => this.formData.update(f => ({ ...f, jobPositions: items.map((i: JobPosition) => ({ id: i.id, label: i.name })) })),
                error: () => {}
              });
            }
            if (!data.contractTypes || data.contractTypes.length === 0) {
              this.contractTypeService.getByCompany(companyIdNum).subscribe({
                next: (items: ContractType[]) => this.formData.update(f => ({ ...f, contractTypes: items.map((i: ContractType) => ({ id: i.id, label: i.contractTypeName })) })),
                error: () => {}
              });
            }
          }
        }
        this.loadSalaryPackages();
      },
      error: (err) => {
        console.error('Error loading employee form data', err);
        this.errorMessage.set(err.error?.message || this.translate.instant('employees.create.error'));
        this.isLoading.set(false);
      }
    });
  }

  private loadInviteRoles(): void {
    const ALLOWED_ROLES = ['Employee', 'Manager', 'RH'];
    this.userService.getRoles().subscribe({
      next: (roles) => {
        const options: LookupOption[] = (roles ?? []).map(r => ({
          id: Number(r.id),
          label: String(r.name ?? r.code ?? r.id)
        }))
        .filter(r => ALLOWED_ROLES.includes(r.label));
        this.inviteRoleOptions.set(options);

        // Par défaut : rôle "Employee" si on le trouve, sinon le premier rôle disponible.
        const employeeRole = options.find(o => o.label.toLowerCase().includes('employee'));
        const defaultId = employeeRole?.id ?? options[0]?.id ?? null;
        if (defaultId != null) {
          this.employeeForm.controls.inviteRoleId.setValue(defaultId);
        }
      },
      error: (err) => {
        console.error('Failed to load roles for invitation', err);
      }
    });
  }

  submit(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const departmentValue = this.selectedDepartment();
    const jobPositionValue = this.selectedJobPosition();
    const rawCompanyId = this.contextService.companyId();
    const companyId =
      rawCompanyId !== null && rawCompanyId !== undefined ? Number(rawCompanyId) : null;

    const needsNewDepartment =
      typeof departmentValue === 'string' &&
      departmentValue.trim().length > 0 &&
      this.employeeForm.controls.departmentId.value == null;
    const needsNewJobPosition =
      typeof jobPositionValue === 'string' &&
      jobPositionValue.trim().length > 0 &&
      this.employeeForm.controls.jobPositionId.value == null;

    const proceedAfterMissing = () => {
      if (this.employeeForm.invalid) {
        this.employeeForm.markAllAsTouched();
        this.isSubmitting.set(false);
        return;
      }
      this.submitEmployee();
    };

    if (needsNewDepartment || needsNewJobPosition) {
      if (!companyId || Number.isNaN(companyId)) {
        this.employeeForm.markAllAsTouched();
        return;
      }
      this.isSubmitting.set(true);
      this.createMissingEntities(departmentValue, jobPositionValue, companyId).subscribe({
        next: () => proceedAfterMissing(),
        error: (err: any) => {
          console.error('Error creating department/job position', err);
          this.isSubmitting.set(false);
          this.errorMessage.set(this.translate.instant('employees.create.error'));
        }
      });
      return;
    }

    if (this.employeeForm.invalid) {
      this.employeeForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.submitEmployee();
  }

  private createMissingEntities(departmentValue: any, jobPositionValue: any, companyId: number | null): any {
    const createObservables: any[] = [];

    if (typeof departmentValue === 'string' && departmentValue.trim() && companyId) {
      createObservables.push(
        this.employeeService.createDepartment(departmentValue.trim(), companyId).pipe(
          map((created) => {
            const id = created.id != null ? Number(created.id) : null;
            this.employeeForm.controls.departmentId.setValue(id);
            return created;
          })
        )
      );
    }

    if (typeof jobPositionValue === 'string' && jobPositionValue.trim() && companyId) {
      createObservables.push(
        this.employeeService.createJobPosition(jobPositionValue.trim(), companyId).pipe(
          map((created) => {
            const id = created.id != null ? Number(created.id) : null;
            this.employeeForm.controls.jobPositionId.setValue(id);
            return created;
          })
        )
      );
    }

    return createObservables.length > 0 
      ? forkJoin(createObservables)
      : of([]);
  }

  private submitEmployee(): void {
    if (this.employeeForm.invalid) {
      this.employeeForm.markAllAsTouched();
      this.isSubmitting.set(false);
      return;
    }

    const value = this.employeeForm.value;
    const rawCompanyId = this.contextService.companyId();
    const companyId =
      rawCompanyId !== null && rawCompanyId !== undefined
        ? Number(rawCompanyId as any)
        : null;

    const selectedPhoneCode = this.phoneCode();
    const selectedSalaryPackageId = value.salaryPackageId ? Number(value.salaryPackageId) : null;
    const inviteRoleId = value.inviteRoleId ? Number(value.inviteRoleId) : null;
    const payload: CreateEmployeeRequest = {
      firstName: value.firstName ?? '',
      lastName: value.lastName ?? '',
      email: value.email ?? '',
      phone: [selectedPhoneCode, value.phone].filter(Boolean).join(' ').trim(),
      dateOfBirth: value.birthDate ?? '',
      cinNumber: value.cinNumber || null,
      statusId: Number(value.statusId),
      inviteRoleId: inviteRoleId,
      genderId: value.genderId ? Number(value.genderId) : null,
      educationLevelId: value.educationLevelId ? Number(value.educationLevelId) : null,
      maritalStatusId: value.maritalStatusId ? Number(value.maritalStatusId) : null,
      nationalityId: value.nationalityId ? Number(value.nationalityId) : null,
      countryId: value.countryId ? Number(value.countryId) : null,
      cityId: value.cityId ? Number(value.cityId) : null,
      countryPhoneCode: selectedPhoneCode || null,
      addressLine1: value.addressLine1 || null,
      addressLine2: value.addressLine2 || null,
      zipCode: value.zipCode || null,
      departementId: value.departmentId ? Number(value.departmentId) : null,
      jobPositionId: value.jobPositionId ? Number(value.jobPositionId) : null,
      contractTypeId: value.contractTypeId ? Number(value.contractTypeId) : null,
      managerId: value.managerId ? Number(value.managerId) : null,
      startDate: value.startDate || null,
      salary: value.salary != null ? Number(value.salary) : null,
      salaryHourly: value.salaryHourly != null ? Number(value.salaryHourly) : null,
      salaryEffectiveDate: value.salaryEffectiveDate || null,
      cnssNumber: null,
      cimrNumber: null,
      attendanceTypeId: value.attendanceTypeId ? Number(value.attendanceTypeId) : null,
      employeeCategoryId: value.employeeCategoryId ? Number(value.employeeCategoryId) : null,
      companyId: companyId && !Number.isNaN(companyId) ? companyId : null
    };

    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.employeeService.createEmployeeRecord(payload).subscribe({
      next: (createdEmployee) => {
        const employeeId = this.extractCreatedEmployeeId(createdEmployee);

        if (!selectedSalaryPackageId) {
          this.handleEmployeeCreationSuccess();
          return;
        }

        if (!employeeId) {
          this.handleEmployeeCreationSuccess(true);
          return;
        }

        this.fetchActiveContractId(employeeId).subscribe({
          next: (contractId) => {
            this.salaryPackageService.createAssignment({
              salaryPackageId: selectedSalaryPackageId,
              employeeId,
              contractId,
              effectiveDate: payload.salaryEffectiveDate || payload.startDate || this.getTodayDate()
            }).subscribe({
              next: () => this.handleEmployeeCreationSuccess(),
              error: (assignmentError) => {
                console.error('Error assigning salary package on employee create', assignmentError);
                this.handleEmployeeCreationSuccess(true);
              }
            });
          },
          error: (contractError) => {
            console.error('Error loading active contract for salary package assignment', contractError);
            this.handleEmployeeCreationSuccess(true);
          }
        });
      },
      error: (err) => {
        console.error('Error creating employee', err);
        this.isSubmitting.set(false);
        const errorText = err.error?.message || this.translate.instant('employees.create.error');
        this.errorMessage.set(errorText);
        this.messageService.add({
          severity: 'error',
          summary: this.translate.instant('employees.create.errorTitle'),
          detail: errorText
        });
      }
    });
  }

  cancel(): void {
    this.router.navigate([`${this.routePrefix()}/employees`]);
  }

  retryLoad(): void {
    this.loadFormData();
  }

  isInvalid(controlName: string): boolean {
    const control = this.employeeForm.get(controlName);
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  trackByOption(_: number, option: { id: number }): number {
    return option.id;
  }

  trackByManager(_: number, manager: ManagerLookupOption): number {
    return manager.id;
  }

  /**
   * Finds the default country from the list.
   * Returns "Maroc" if found (case-insensitive), otherwise returns the first country.
   */
  private findDefaultCountry(countries: { id: number; label: string }[]): { id: number; label: string } | null {
    if (!countries.length) {
      return null;
    }
    const morocco = countries.find(c => c.label.toLowerCase() === 'maroc');
    return morocco ?? countries[0];
  }

  private loadSalaryPackages(): void {
    this.salaryPackageService.getCompanyPackages(undefined, 'published').subscribe({
      next: (packages) => {
        const options = packages
          .map(pkg => ({
            id: pkg.id,
            label: `${pkg.name} (v${pkg.version})`
          }))
          .sort((a, b) => a.label.localeCompare(b.label));
        this.salaryPackageOptions.set(options);
      },
      error: (error) => {
        console.error('Failed to load salary packages for employee create form', error);
        this.salaryPackageOptions.set([]);
      }
    });
  }

  private fetchActiveContractId(employeeId: number) {
    return this.http.get<any[]>(`${this.baseUrl}/api/employee-contracts/employee/${employeeId}`)
      .pipe(
        map((contracts) => {
          const now = new Date();
          const activeContract = (contracts || []).find(c => {
            const endDate = c.EndDate ?? c.endDate;
            return !endDate || new Date(endDate) > now;
          });
          const id = Number(activeContract?.Id ?? activeContract?.id);
          if (!Number.isFinite(id) || id <= 0) {
            throw new Error('No active contract found');
          }
          return id;
        })
      );
  }

  private extractCreatedEmployeeId(createdEmployee: any): number | null {
    const id = Number(createdEmployee?.id ?? createdEmployee?.Id);
    return Number.isFinite(id) && id > 0 ? id : null;
  }

  private getTodayDate(): string {
    return new Date().toISOString().split('T')[0];
  }

  private handleEmployeeCreationSuccess(showSalaryPackageWarning = false): void {
    this.isSubmitting.set(false);
    this.successMessage.set(this.translate.instant('employees.create.success'));
    this.messageService.add({
      severity: 'success',
      summary: this.translate.instant('employees.create.successTitle'),
      detail: this.translate.instant('employees.create.success')
    });

    if (showSalaryPackageWarning) {
      this.messageService.add({
        severity: 'warn',
        summary: this.translate.instant('employees.create.salaryPackageAssignWarningTitle'),
        detail: this.translate.instant('employees.create.salaryPackageAssignWarning')
      });
    }

    this.employeeForm.reset();
    const defaultCountry = this.findDefaultCountry(this.formData().countries);
    if (defaultCountry) {
      this.employeeForm.controls.phoneCountryId.setValue(defaultCountry.id);
    }
    setTimeout(() => this.router.navigate([`${this.routePrefix()}/employees`]), 800);
  }
}
