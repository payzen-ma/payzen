import { Component, signal, OnInit, inject, computed, HostListener, DestroyRef, effect } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { TabsModule } from 'primeng/tabs';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { InputNumberModule } from 'primeng/inputnumber';
import { TextareaModule } from 'primeng/textarea';
import { TagModule } from 'primeng/tag';
import { AvatarModule } from 'primeng/avatar';
import { FileUploadModule } from 'primeng/fileupload';
import { TimelineModule } from 'primeng/timeline';
import { SkeletonModule } from 'primeng/skeleton';
import { TooltipModule } from 'primeng/tooltip';
import { ChipModule } from 'primeng/chip';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { MultiSelectModule } from 'primeng/multiselect';
import { CheckboxModule } from 'primeng/checkbox';
import {
  EmployeeService,
  EmployeeFormData,
  LookupOption
} from '@app/core/services/employee.service';
import { JobPositionService } from '@app/core/services/job-position.service';
import { ContractTypeService } from '@app/core/services/contract-type.service';
import { EmployeeCategoryService } from '@app/core/services/employee-category.service';
import { FamilyService } from '@app/core/services/family.service';
import { JobPosition } from '@app/core/models/job-position.model';
import { ContractType } from '@app/core/models/contract-type.model';
import { Employee as EmployeeProfileModel, EmployeeEvent } from '@app/core/models/employee.model';
import { DraftService } from '@app/core/services/draft.service';
import { ChangeTracker, ChangeSet } from '@app/core/utils/change-tracker.util';
import { ChangeConfirmationDialog } from '@app/shared/components/change-confirmation-dialog/change-confirmation-dialog';
import { UnsavedChangesDialog } from '@app/shared/components/unsaved-changes-dialog/unsaved-changes-dialog';
import { CanComponentDeactivate } from '@app/core/guards/unsaved-changes.guard';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable, of, firstValueFrom, forkJoin } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { TagComponent } from '../../../shared/components/tag/tag.component';
import { TagVariant } from '../../../shared/components/tag/tag.types';
import { SpouseChildrenComponent } from '../family/spouse-children';

interface Document {
  type: string;
  name: string;
  uploadDate: string;
  status: 'uploaded' | 'missing';
  url?: string;
}

@Component({
  selector: 'app-employee-profile',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    TranslateModule,
    TabsModule,
    ButtonModule,
    InputTextModule,
    SelectModule,
    AutoCompleteModule,
    InputNumberModule,
    TextareaModule,
    TagComponent,
    AvatarModule,
    FileUploadModule,
    TimelineModule,
    SkeletonModule,
    TooltipModule,
    ChipModule,
    IconFieldModule,
    InputIconModule,
    MultiSelectModule,
    CheckboxModule,
    SpouseChildrenComponent,
    ChangeConfirmationDialog,
    UnsavedChangesDialog
  ],
  templateUrl: './employee-profile.html',
  styleUrl: './employee-profile.css'
})
export class EmployeeProfile implements OnInit, CanComponentDeactivate {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private employeeService = inject(EmployeeService);
  private draftService = inject(DraftService);
  private translate = inject(TranslateService);
  private jobPositionService = inject(JobPositionService);
  private contractTypeService = inject(ContractTypeService);
  private employeeCategoryService = inject(EmployeeCategoryService);
  private familyService = inject(FamilyService);
  private destroyRef = inject(DestroyRef);
  private contextService = inject(CompanyContextService);

  // Route prefix based on current context mode
  readonly routePrefix = computed(() => this.contextService.isExpertMode() ? '/expert' : '/app');

  private readonly AUTO_SAVE_DEBOUNCE = 800; // 800ms debounce for better UX
  private readonly ENTITY_TYPE = 'employee_profile';
  
  private isRestoringDraft = false;
  private lastSerializedEmployee = '';
  private autoSaveTimer: ReturnType<typeof setTimeout> | null = null;
  private originalEmployee: EmployeeProfileModel | null = null;
  private pendingNavigationResolver: ((result: boolean) => void) | null = null;
  private pendingCancel = false;

  private pendingTabTarget: string | null = null;
  private pendingDraftData: Partial<EmployeeProfileModel> | null = null;
  private pendingDraftTimestamp: Date | null = null;

  private readonly TAB_IDS = ['0', '1', '2', '3', '4', '5', '6', '7'] as const;
  private readonly TAB_FIELD_MAP: Record<string, (keyof EmployeeProfileModel)[]> = {
    '0': ['firstName', 'lastName', 'cin', 'maritalStatus', 'dateOfBirth', 'birthPlace'],
    '1': ['personalEmail', 'phone', 'address', 'countryId', 'countryName', 'city', 'addressLine1', 'addressLine2', 'zipCode'],
    '2': [], // Family - Spouse & Children managed by separate component
    '3': ['position', 'department', 'manager', 'contractType', 'status', 'startDate', 'endDate', 'probationPeriod'],
    '4': ['baseSalary', 'salaryComponents', 'paymentMethod'],
    '5': ['cnss', 'amo', 'cimr', 'cimrEmployeeRate', 'cimrCompanyRate', 'hasPrivateInsurance', 'privateInsuranceNumber', 'privateInsuranceRate', 'disableAmo', 'annualLeave'],
    '6': ['missingDocuments'],
    '7': ['events']
  };

  // UI State
  readonly activeTab = signal('0');
  readonly isEditMode = signal(false);
  readonly employeeId = signal<string | null>(null);
  readonly isLoadingProfile = signal(false);
  readonly isLoadingFormData = signal(false);
  readonly isSaving = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly saveError = signal<string | null>(null);
  
  // Draft & Change Tracking State
  readonly lastAutoSave = signal<Date | null>(null);
  readonly draftRestored = signal(false);
  readonly changeSet = signal<ChangeSet>(this.createEmptyChangeSet());
  readonly tabChangeSets = signal<Record<string, ChangeSet>>(this.createEmptyTabChangeSets());
  readonly unsavedTabs = computed(() =>
    Object.entries(this.tabChangeSets())
      .filter(([_, set]) => set.hasChanges)
      .map(([tab]) => tab)
  );
  
  // Dialog State
  readonly showConfirmDialog = signal(false);
  readonly showUnsavedDialog = signal(false);

  @HostListener('window:beforeunload', ['$event'])
  handleBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.changeSet().hasChanges) {
      event.preventDefault();
      event.returnValue = '';
    }
  }
  readonly saveSuccess = signal<string | null>(null);
  readonly ariaMessage = signal('');

  readonly employee = signal<EmployeeProfileModel>(this.createEmptyEmployee());

  // Field labels for change tracking
  private readonly FIELD_LABELS: Record<string, string> = {
    firstName: 'First Name',
    lastName: 'Last Name',
    cin: 'National ID',
    maritalStatus: 'Marital Status',
    dateOfBirth: 'Date of Birth',
    birthPlace: 'Place of Birth',
    professionalEmail: 'Professional Email',
    personalEmail: 'Personal Email',
    phone: 'Phone',
    address: 'Address',
    countryId: 'Country',
    countryName: 'Country',
    city: 'City',
    addressLine1: 'Address Line 1',
    addressLine2: 'Address Line 2',
    zipCode: 'Zip Code',
    position: 'Position',
    department: 'Department',
    manager: 'Manager',
    contractType: 'Contract Type',
    status: 'Employment Status',
    startDate: 'Start Date',
    endDate: 'End Date',
    probationPeriod: 'Probation Period',
    baseSalary: 'Base Salary',
    salaryComponents: 'Salary Components',
    paymentMethod: 'Payment Method',
    cnss: 'CNSS',
    amo: 'AMO',
    cimr: 'CIMR',
    annualLeave: 'Annual Leave'
  };

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

  readonly documents = signal<Document[]>([
    {
      type: 'cin',
      name: 'CIN - Youssef Amrani.pdf',
      uploadDate: '2022-01-10',
      status: 'uploaded',
      url: '#'
    },
    {
      type: 'contract',
      name: 'Contrat CDI - Youssef Amrani.pdf',
      uploadDate: '2022-01-10',
      status: 'uploaded',
      url: '#'
    },
    {
      type: 'rib',
      name: 'RIB - Youssef Amrani.pdf',
      uploadDate: '2022-01-10',
      status: 'uploaded',
      url: '#'
    },
    {
      type: 'job_description',
      name: 'Fiche de poste',
      uploadDate: '',
      status: 'missing'
    }
  ]);

  readonly history = computed(() => this.employee().events || []);

  // History tab state
  readonly historySearchQuery = signal('');
  readonly historyFiltersExpanded = signal(false);
  readonly selectedHistoryTypes = signal<string[]>([]);
  
  // Computed filtered history
  readonly filteredHistory = computed(() => {
    let events = this.history();
    const query = this.historySearchQuery().toLowerCase();
    const types = this.selectedHistoryTypes();

    // Text search
    if (query) {
      events = events.filter(event =>
        event.title?.toLowerCase().includes(query) ||
        event.description?.toLowerCase().includes(query) ||
        event.modifiedBy?.name?.toLowerCase().includes(query)
      );
    }

    // Type filter
    if (types.length > 0) {
      events = events.filter(event => types.includes(event.type));
    }

    return events;
  });

  readonly historyTotalCount = computed(() => this.history().length);
  readonly historyResultCount = computed(() => this.filteredHistory().length);
  readonly hasActiveHistoryFilters = computed(() =>
    this.historySearchQuery() !== '' || this.selectedHistoryTypes().length > 0
  );

  readonly historyTypeOptions = [
    { label: 'Salary Change', value: 'salary_change' },
    { label: 'Salary Increase', value: 'salary_increase' },
    { label: 'Position Change', value: 'position_change' },
    { label: 'Address Updated', value: 'address_updated' },
    { label: 'General Update', value: 'general_update' },
    { label: 'Note', value: 'note' }
  ];

  readonly maritalStatusOptions: Array<{ id: number; label: string; value: EmployeeProfileModel['maritalStatus'] }> = [
    { id: 1, label: 'Single', value: 'single' },
    { id: 2, label: 'Married', value: 'married' },
    { id: 3, label: 'Divorced', value: 'divorced' },
    { id: 4, label: 'Widowed', value: 'widowed' }
  ];
  readonly maritalStatusMap: Record<string, string> = {
    'single': 'Célibataire',
    'married': 'Marié(e)',
    'divorced': 'Divorcé(e)',
    'widowed': 'Veuf(ve)'
  };

  readonly contractTypeOptions: Array<{ id: number; label: string; value: EmployeeProfileModel['contractType'] }> = [
    { id: 1, label: 'CDI', value: 'CDI' },
    { id: 2, label: 'CDD', value: 'CDD' },
    { id: 3, label: 'Stage', value: 'Stage' }
  ];

  readonly contractTypeSelectOptions = computed(() => {
    const key = 'employees.profile.position.contractTypePlaceholder';
    const translated = this.translate?.instant ? this.translate.instant(key) : null;
    const placeholderLabel = (translated && translated !== key) ? translated : 'Choisissez un type de contrat';
    const placeholder = { id: -1, label: placeholderLabel, value: null };
    const formItems = this.formData()?.contractTypes ?? [];
    // If company-specific contract types are available, return them
    if (formItems && formItems.length > 0) {
      return [placeholder, ...formItems.map((o: any, idx: number) => ({ id: o.id ?? idx, label: o.label, value: o.value ?? o.label }))];
    }

    // If a company context exists but there are no contract types for it, return only the placeholder
    const cid = this.contextService.companyId();
    if (cid !== null && cid !== undefined) {
      return [placeholder];
    }

    // No company context: fall back to static options
    return [placeholder, ...this.contractTypeOptions.map(o => ({ id: o.id, label: o.label, value: o.value ?? o.label }))];
  });

  readonly statusOptions = computed(() => {
    const opts = this.formData()?.statuses ?? [];
    if (!opts || opts.length === 0) {
      // Fallback to the previous hardcoded options when API data is not yet available
      return [
        { id: 1, label: 'Actif', value: 'active' as EmployeeProfileModel['status'] },
        { id: 2, label: 'En congé', value: 'on_leave' as EmployeeProfileModel['status'] },
        { id: 3, label: 'Inactif', value: 'inactive' as EmployeeProfileModel['status'] }
      ];
    }

    return opts.map((o, idx) => ({
      id: (o as any).id ?? idx,
      label: o.label,
      value: ((o as any).value ?? String((o as any).id ?? idx)) as EmployeeProfileModel['status']
    }));
  });

  readonly paymentMethodOptions: Array<{ id: number; label: string; value: EmployeeProfileModel['paymentMethod'] }> = [
    { id: 1, label: 'Virement bancaire', value: 'bank_transfer' },
    { id: 2, label: 'Chèque', value: 'check' },
    { id: 3, label: 'Espèces', value: 'cash' }
  ];
  readonly paymentMethodMap: Record<string, string> = {
    'bank_transfer': 'Virement bancaire',
    'check': 'Chèque',
    'cash': 'Espèces'
  };

  readonly totalSalary = computed(() => {
    const emp = this.employee();
    const componentsTotal = (emp.salaryComponents || []).reduce((sum, c) => sum + (Number(c.amount) || 0), 0);
    return (emp.baseSalary || 0) + componentsTotal;
  });

  constructor() {
    // Setup effect to track employee signal changes for auto-save and change detection
    effect(() => {
      const currentEmployee = this.employee();
      const isEdit = this.isEditMode();
      
      if (!isEdit || this.isRestoringDraft) {
        return;
      }
      
      const serialized = JSON.stringify(currentEmployee);
      
      // Track changes for confirmation dialog and per-tab state
      if (this.originalEmployee) {
        const changes = ChangeTracker.trackChanges(
          this.originalEmployee,
          currentEmployee,
          this.FIELD_LABELS,
          ['id', 'photo', 'missingDocuments', 'activeSalaryId']
        );
        this.changeSet.set(changes);
        this.updateTabChangeSets(changes);
      }

      if (!this.lastSerializedEmployee) {
        this.lastSerializedEmployee = serialized;
        return;
      }

      if (serialized === this.lastSerializedEmployee) {
        return;
      }

      this.lastSerializedEmployee = serialized;
      
      // Debounced auto-save
      if (this.autoSaveTimer) {
        clearTimeout(this.autoSaveTimer);
      }
      
      this.autoSaveTimer = setTimeout(() => {
        if (this.isEditMode() && !this.isRestoringDraft && this.employeeId()) {
          this.saveDraftForTab(this.activeTab());
          const savedAt = new Date();
          this.lastAutoSave.set(savedAt);
          this.announce(`Draft saved at ${savedAt.toLocaleTimeString()}`);
        }
      }, this.AUTO_SAVE_DEBOUNCE);
    });
  }

  ngOnInit() {
    this.route.params.subscribe(params => {
      if (params['id']) {
        this.employeeId.set(params['id']);
        this.loadEmployeeDetails(params['id']);
      }
    });

    this.draftService
      .onDraftUpdated()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ key, draft }) => {
        const id = this.employeeId();
        if (!id || !draft || !this.isDraftForCurrentEntity(key, id)) {
          return;
        }

        // Ignore updates originating from the same browser tab
        if (draft.metadata.tabId === this.draftService.getTabId()) {
          return;
        }

        this.pendingDraftData = { ...(this.pendingDraftData ?? {}), ...(draft.data as Partial<EmployeeProfileModel>) };
        const savedAt = new Date(draft.metadata.savedAt);
        this.pendingDraftTimestamp = savedAt;
        this.lastAutoSave.set(savedAt);
        this.draftRestored.set(true);
        this.announce('A newer draft is available from another tab.');
      });

    // When company context changes, refresh form lookup data (departments, job positions, contract types)
    this.contextService.contextChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.formData.set(this.emptyFormData);
        this.loadFormData();
      });

    // Also react directly to the companyId signal to ensure reload in all change paths
  }

  private loadEmployeeDetails(id: string): void {
    this.isLoadingProfile.set(true);
    this.loadError.set(null);
    
    forkJoin({
      details: this.employeeService.getEmployeeDetails(id),
      salary: this.employeeService.getEmployeeSalaryDetails(id),
      statuses: this.employeeService.getStatuses(false),
      spouse: this.familyService.getSpouse(id).pipe(catchError(() => of(null))),
      children: this.familyService.getChildren(id).pipe(catchError(() => of([]))),
      assignment: this.employeeService.getActivePackageAssignment(id).pipe(catchError(() => of(null)))
    }).subscribe({
      next: ({ details, salary, statuses, spouse, children, assignment }) => {
        console.log('[EmployeeProfile] loaded details, statuses from API:', statuses, 'currentLang:', this.translate?.currentLang);
        console.log('[EmployeeProfile] raw details for employee', id, ':', details);
        console.log('[EmployeeProfile] backend gender fields:', { GenderId: (details as any).GenderId, GenderName: (details as any).GenderName });
        this.isRestoringDraft = true;
        
        // Convert single spouse to array for consistency
        const spouses = spouse ? [spouse] : [];

        // Map backend PascalCase `details` to frontend camelCase employee shape
        const mappedDetails = this.mapBackendDetails(details);
        console.log('[EmployeeProfile] mapped details (with gender):', mappedDetails);

        // Merge salary components with IDs
        const mergedEmployee = {
          ...mappedDetails,
          activeSalaryId: salary.id,
          salaryComponents: salary.components.length > 0 ? salary.components : (mappedDetails.salaryComponents || []),
          spouses: spouses,
          children: children,
          assignedPackage: assignment
        } as EmployeeProfileModel;
        console.log('[EmployeeProfile] final mergedEmployee:', { genderId: mergedEmployee.genderId, genderName: mergedEmployee.genderName });

        // We'll set the employee and try to enrich its `statusName` from referential statuses.
        this.employee.set(mergedEmployee);
        console.debug('[EmployeeProfile] loaded details', { id: id, status: mergedEmployee.status });
        // Enrich statusName from referential statuses when available (robust matching)
        let finalEmployee = mergedEmployee;
        try {
          if (statuses && statuses.length) {
            const opts = statuses as any[];
            const normalize = (v: any) => (v === undefined || v === null) ? '' : String(v).toLowerCase().replace(/[^a-z0-9]+/g, '');
            const candidates = new Set<string>();
            candidates.add(normalize((mergedEmployee as any).statusRaw ?? ''));
            candidates.add(normalize(mergedEmployee.status ?? ''));
            candidates.add(normalize(mergedEmployee.statusName ?? ''));
            const norm = normalize(mergedEmployee.status ?? '');
            if (norm === 'active') candidates.add(normalize('actif'));
            if (norm === 'onleave') candidates.add(normalize('conge'));
            if (norm === 'inactive') candidates.add('inact');

            let match: any = null;
            for (const o of opts) {
              const anyO = o as any;
              const val = normalize(anyO.value ?? anyO.id ?? anyO.label ?? '');
              const lbl = normalize(o.label ?? '');
              if (candidates.has(val) || candidates.has(lbl)) { match = o; break; }
              for (const c of Array.from(candidates)) {
                if (c && (val.includes(c) || lbl.includes(c))) { match = o; break; }
              }
              if (match) break;
            }
            if (match) {
              finalEmployee = { ...mergedEmployee, statusName: match.label };
              this.employee.set(finalEmployee);
            }
          }
        } catch (e) {
          // ignore
        }
        // Ensure contractType is null when not provided so the select placeholder stays selected
        if (!finalEmployee.contractType || finalEmployee.contractType === '') {
          finalEmployee = { ...finalEmployee, contractType: null as any };
          this.employee.set(finalEmployee);
        }
        this.originalEmployee = { ...finalEmployee };
        this.lastSerializedEmployee = JSON.stringify(finalEmployee);
        // Load company-specific contract types when available so the contract select shows company items
        try {
          const compId = (finalEmployee as any).companyId ?? (finalEmployee as any).CompanyId ?? this.contextService.companyId();
          const cidNum = Number(compId);
          if (!Number.isNaN(cidNum) && cidNum) {
            this.contractTypeService.getByCompany(cidNum).subscribe({
              next: (items: ContractType[]) => {
                this.formData.update(f => ({ ...f, contractTypes: items.map((i: ContractType) => ({ id: i.id, label: i.contractTypeName, value: i.contractTypeName })) }));
              },
              error: () => {}
            });
            // Load employee categories
            this.employeeCategoryService.getLookupOptions(cidNum).subscribe({
              next: (categories) => {
                this.formData.update(f => ({ ...f, employeeCategories: categories }));
              },
              error: () => {}
            });
          }
        } catch (e) {
          // ignore
        }
        this.resetChangeTracking();
        
        // Check for existing draft
        this.restoreDraftIfAvailable();
        
        this.isLoadingProfile.set(false);
        
        setTimeout(() => {
          this.isRestoringDraft = false;
        }, 100);
      },
      error: (err) => {
        console.error('Failed to load employee details', err);
        this.loadError.set(this.translate.instant('employees.profile.loadError'));
        this.isLoadingProfile.set(false);
      }
    });
  }

  getFullName(): string {
    return `${this.employee().firstName} ${this.employee().lastName}`.trim();
  }

  getInitials(): string {
    const emp = this.employee();
    const firstInitial = emp.firstName?.charAt(0) || '';
    const lastInitial = emp.lastName?.charAt(0) || '';
    return `${firstInitial}${lastInitial}`.toUpperCase();
  }

  getStatusSeverity(): TagVariant {
    const emp = this.employee();
    // Prefer referential match
    try {
      const opts = this.formData()?.statuses ?? [];
      const norm = (s: any) => (s === undefined || s === null) ? '' : String(s).toLowerCase().replace(/[^a-z0-9]+/g, '');
      const code = norm((emp as any).statusRaw ?? emp.status ?? emp.statusName ?? '');
      for (const o of opts) {
        const anyO = o as any;
        const val = norm(anyO.value ?? anyO.id ?? anyO.label ?? '');
        const lbl = norm(o.label ?? '');
        if (val === code || lbl === code || val.includes(code) || lbl.includes(code)) {
          const key = (anyO.value ?? anyO.label ?? '').toString().toLowerCase();
          if (key.includes('active') || key.includes('actif') || key.includes('enabled')) return 'success';
          if (key.includes('leave') || key.includes('cong') || key.includes('abs')) return 'warning';
          return 'danger';
        }
      }
    } catch (e) {}

    const status = this.employee().status;
    if (status === 'active') return 'success';
    if (status === 'on_leave') return 'warning';
    return 'danger';
  }

  getStatusLabel(): string {
    const emp = this.employee();
    // If referential label is present on the employee, use it
    if (emp.statusName && emp.statusName.trim()) return emp.statusName;

    // Otherwise try to resolve from formData statuses
    try {
      const opts = this.formData()?.statuses ?? [];
      const norm = (s: any) => (s === undefined || s === null) ? '' : String(s).toLowerCase().replace(/[^a-z0-9]+/g, '');
      const code = norm((emp as any).statusRaw ?? emp.status ?? emp.statusName ?? '');
      for (const o of opts) {
        const anyO = o as any;
        const val = norm(anyO.value ?? anyO.id ?? anyO.label ?? '');
        const lbl = norm(o.label ?? '');
        if (val === code || lbl === code || val.includes(code) || lbl.includes(code)) {
          return o.label;
        }
      }
    } catch (e) {}

    const status = this.employee().status;
    if (status === 'active') return 'Actif';
    if (status === 'on_leave') return 'En congé';
    return 'Inactif';
  }

  getMaritalStatusLabel(): string {
    return this.maritalStatusMap[this.employee().maritalStatus] || '';
  }

  getPaymentMethodLabel(): string {
    return this.paymentMethodMap[this.employee().paymentMethod] || '';
  }

  getCountryLabel(): string {
    const countryId = this.employee().countryId;
    if (!countryId) return '-';
    const country = this.formData().countries.find(c => c.id === countryId);
    return country?.label || '-';
  }

  getGenderLabel(): string {
    const gid = this.employee().genderId;
    if (!gid && gid !== 0) return this.employee().genderName || '-';
    const g = (this.formData().genders || []).find((x: any) => x.id === gid);
    return g?.label || this.employee().genderName || '-';
  }

  getContractTypeLabel(): string {
    const emp = this.employee();
    // Prefer contractTypes from formData (company-specific)
    try {
      const opts = this.formData()?.contractTypes ?? [];
      if (opts && opts.length) {
        const norm = (v: any) => (v === undefined || v === null) ? '' : String(v).toLowerCase().replace(/[^a-z0-9]+/g, '');
        const code = norm(emp.contractType ?? '');
        if (!code) return emp.contractType || '';
        for (const o of opts) {
          const anyO = o as any;
          const val = norm(anyO.value ?? anyO.id ?? anyO.label ?? '');
          const lbl = norm(o.label ?? '');
          if (val === code || lbl === code || (code && (val.includes(code) || lbl.includes(code)))) {
            return (o as any).label ?? String(o.label ?? emp.contractType);
          }
        }
      }
    } catch (e) {
      // ignore
    }

    return emp.contractType || '-';
  }

  getEmployeeCategoryLabel(): string {
    const emp = this.employee();
    if (emp.employeeCategoryName) return emp.employeeCategoryName;
    const categoryId = emp.employeeCategoryId;
    if (!categoryId) return '-';
    const category = this.formData().employeeCategories?.find(c => c.id === categoryId);
    return category?.label || '-';
  }

  // Map backend PascalCase response to frontend Employee fields
  private mapBackendDetails(d: any): Partial<EmployeeProfileModel> {
    if (!d) return {};
    const out: any = {};
    out.id = d.Id != null ? String(d.Id) : (d.id ?? undefined);
    out.firstName = d.FirstName ?? d.firstName;
    out.lastName = d.LastName ?? d.lastName;
    out.cin = d.CinNumber ?? d.Cin ?? d.cin;
    out.maritalStatus = (d.MaritalStatus ?? d.MaritalStatusName ?? d.maritalStatus) as any;
    out.dateOfBirth = d.DateOfBirth ?? d.dateOfBirth;
    out.birthPlace = d.BirthPlace ?? d.birthPlace;
    out.professionalEmail = d.Email ?? d.ProfessionalEmail ?? d.professionalEmail;
    out.personalEmail = d.PersonalEmail ?? d.personalEmail;
    out.phone = d.Phone ?? d.phone;
    out.address = d.Address ?? d.address;
    // Address individual fields
    out.addressLine1 = d.AddressLine1 ?? d.addressLine1;
    out.addressLine2 = d.AddressLine2 ?? d.addressLine2;
    out.zipCode = d.ZipCode ?? d.zipCode;
    out.city = d.City ?? d.city;
    out.cityId = d.CityId ?? d.cityId;
    out.countryName = d.CountryName ?? d.countryName;
    out.countryId = d.CountryId ?? d.countryId;
    out.position = d.JobPositionName ?? d.Position ?? d.position;
    out.department = d.DepartmentName ?? d.department ?? (d.departments ?? '')
    out.manager = d.ManagerName ?? d.Manager ?? d.manager;
    out.contractType = d.ContractTypeName ?? d.ContractType ?? d.contractType;
    out.startDate = d.ContractStartDate ?? d.StartDate ?? d.startDate;
    out.endDate = d.ContractEndDate ?? d.EndDate ?? d.endDate;
    out.probationPeriod = d.ProbationPeriod ?? d.probationPeriod;
    out.baseSalary = d.BaseSalary ?? d.baseSalary ?? 0;
    out.salaryComponents = d.SalaryComponents ?? d.salaryComponents ?? [];
    out.statusName = d.StatusName ?? d.statusName ?? d.Status ?? d.status;
    out.status = (d.StatusCode ?? d.Status ?? d.status) as any;
    out.missingDocuments = d.MissingDocuments ?? d.missingDocuments ?? 0;
    out.companyId = d.CompanyId ?? d.CompanyId ?? d.companyId;
    out.userId = d.UserId ?? d.userId;
    out.createdAt = d.CreatedAt ?? d.createdAt;
    out.updatedAt = d.UpdatedAt ?? d.updatedAt;
    out.events = d.Events ?? d.events ?? [];
    // Gender normalization
    out.genderId = d.GenderId ?? d.GenderID ?? d.genderId ?? null;
    out.genderName = d.GenderName ?? d.Gender ?? d.genderName ?? null;
    // Employee category
    out.employeeCategoryId = d.CategoryId ?? d.categoryId ?? d.employeeCategoryId ?? null;
    out.employeeCategoryName = d.CategoryName ?? d.categoryName ?? d.employeeCategoryName ?? null;
    // Legal information fields (CNSS, AMO, CIMR, Insurance)
    out.cnss = d.Cnss ?? d.cnss ?? d.CNSS;
    out.amo = d.Amo ?? d.amo ?? d.AMO;
    out.cimr = d.Cimr ?? d.cimr ?? d.CIMR;
    out.cimrEmployeeRate = d.CimrEmployeeRate ?? d.cimrEmployeeRate;
    out.cimrCompanyRate = d.CimrCompanyRate ?? d.cimrCompanyRate;
    out.hasPrivateInsurance = d.HasPrivateInsurance ?? d.hasPrivateInsurance ?? false;
    out.privateInsuranceNumber = d.PrivateInsuranceNumber ?? d.privateInsuranceNumber;
    out.privateInsuranceRate = d.PrivateInsuranceRate ?? d.privateInsuranceRate;
    out.disableAmo = d.DisableAmo ?? d.disableAmo ?? false;
    out.annualLeave = d.AnnualLeave ?? d.annualLeave ?? 0;
    out.paymentMethod = d.PaymentMethod ?? d.SalaryPaymentMethod ?? d.paymentMethod ?? d.salaryPaymentMethod;
    // keep any other camelCase properties present
    return out;
  }

  // Helper to update employee signal (triggers effect)
  updateField<K extends keyof EmployeeProfileModel>(field: K, value: EmployeeProfileModel[K]): void {
    this.saveError.set(null);
    this.employee.set({ ...this.employee(), [field]: value });
  }

  toggleEditMode(): void {
    if (!this.isEditMode()) {
      // Entering edit mode
      console.debug('[EmployeeProfile] entering edit mode, current status:', this.employee().status);
      this.loadFormData();
      this.lastSerializedEmployee = JSON.stringify(this.employee());
      this.resetChangeTracking();
    } else {
      // Exiting edit mode - cancel changes
      this.cancel();
      return;
    }
    this.isEditMode.update(v => !v);
    console.log('[EmployeeProfile] isEditMode now', this.isEditMode());
    this.saveError.set(null);
    this.saveSuccess.set(null);
  }

  loadFormData(): void {
    if (this.formData().statuses.length > 0) {
      return;
    }
    this.isLoadingFormData.set(true);
    // Load both form lookups and statuses referential
    forkJoin({
      form: this.employeeService.getEmployeeFormData(),
      statuses: this.employeeService.getStatuses(false)
    }).subscribe({
        next: ({ form, statuses }) => {
          // Merge statuses into formData (convert to LookupOption[] expected shape)
          const merged = { ...form, statuses } as EmployeeFormData;
          this.formData.set(merged);
          this.isLoadingFormData.set(false);
          console.debug('[EmployeeProfile] formData.statuses:', merged.statuses);
          console.debug('[EmployeeProfile] current employee status (signal):', this.employee()?.status, this.employee()?.statusRaw, this.employee()?.statusName);

          // Fallbacks: if jobPositions or contractTypes are missing, load them by companyId
          const companyIdRaw = this.contextService.companyId();
          if (companyIdRaw !== null && companyIdRaw !== undefined) {
            const companyIdNum = Number(companyIdRaw as any);
            if (!Number.isNaN(companyIdNum)) {
              if (!merged.jobPositions || merged.jobPositions.length === 0) {
                this.jobPositionService.getByCompany(companyIdNum).subscribe({
                  next: (items: JobPosition[]) => this.formData.update(f => ({ ...f, jobPositions: items.map((i: JobPosition) => ({ id: i.id, label: i.name })) })),
                  error: () => {}
                });
              }
              if (!merged.contractTypes || merged.contractTypes.length === 0) {
                this.contractTypeService.getByCompany(companyIdNum).subscribe({
                  next: (items: ContractType[]) => this.formData.update(f => ({ ...f, contractTypes: items.map((i: ContractType) => ({ id: i.id, label: i.contractTypeName, value: i.contractTypeName })) })),
                  error: () => {}
                });
              }
              // Load company-scoped employee categories so edit forms have the lookup available
              this.employeeCategoryService.getLookupOptions(companyIdNum).subscribe({
                next: (categories) => this.formData.update(f => ({ ...f, employeeCategories: categories })),
                error: () => {}
              });
            }
          }
        },
      error: (err) => {
        console.error('Error loading form data or statuses', err);
        // fallback: try to at least set form data
        this.employeeService.getEmployeeFormData().subscribe({
          next: (data) => this.formData.set(data),
          error: () => {}
        });
        this.isLoadingFormData.set(false);
      }
    });
  }



  private restoreDraftIfAvailable(): void {
    const latestDraft = this.getLatestDraft();
    if (!latestDraft) {
      return;
    }

    this.pendingDraftData = latestDraft.data;
    this.pendingDraftTimestamp = latestDraft.savedAt;
    this.draftRestored.set(true);
    this.lastAutoSave.set(latestDraft.savedAt);
  }

  applyDraft(): void {
    const draftData = this.pendingDraftData ?? this.getLatestDraft()?.data;
    if (!draftData) {
      return;
    }

    this.isRestoringDraft = true;
    this.employee.set({ ...this.employee(), ...draftData });
    this.lastSerializedEmployee = JSON.stringify(this.employee());
    this.draftRestored.set(false);
    this.pendingDraftData = null;
    if (this.pendingDraftTimestamp) {
      this.lastAutoSave.set(this.pendingDraftTimestamp);
    }
    this.pendingDraftTimestamp = null;
    
    setTimeout(() => {
      this.isRestoringDraft = false;
      this.recomputeChangeTracking();
    }, 150);
  }

  dismissDraft(): void {
    this.clearDraftsForAllTabs();
    this.draftRestored.set(false);
    this.pendingDraftData = null;
    this.pendingDraftTimestamp = null;
    this.lastAutoSave.set(null);
  }

  readonly filteredCountries = signal<any[]>([]);
  readonly filteredCities = signal<any[]>([]);

  searchCountry(event: any) {
    this.employeeService.searchCountries(event.query).subscribe(data => {
      this.filteredCountries.set(data);
    });
  }

  searchCity(event: any) {
    this.employeeService.searchCities(event.query).subscribe(data => {
      this.filteredCities.set(data);
    });
  }

  selectedCountry = computed(() => {
    const emp = this.employee();
    if (emp.countryId && emp.countryName) {
      return { id: emp.countryId, label: emp.countryName };
    }
    return emp.countryName || '';
  });

  selectedCity = computed(() => {
    const emp = this.employee();
    if (emp.cityId && emp.city) {
      return { id: emp.cityId, label: emp.city };
    }
    return emp.city || '';
  });

  onCountryChange(value: any) {
    if (typeof value === 'string') {
      this.updateField('countryName', value);
      this.updateField('countryId', undefined);
    } else if (value && typeof value === 'object') {
      this.updateField('countryName', value.label);
      this.updateField('countryId', value.id);
    } else {
      this.updateField('countryName', undefined);
      this.updateField('countryId', undefined);
    }
  }

  onCityChange(value: any) {
    if (typeof value === 'string') {
      this.updateField('city', value);
      this.updateField('cityId', undefined);
    } else if (value && typeof value === 'object') {
      this.updateField('city', value.label);
      this.updateField('cityId', value.id);
    } else {
      this.updateField('city', undefined);
      this.updateField('cityId', undefined);
    }
  }

  addSalaryComponent() {
    const current = this.employee().salaryComponents || [];
    this.updateField('salaryComponents', [...current, { type: '', amount: 0 }]);
  }

  removeSalaryComponent(index: number) {
    const current = this.employee().salaryComponents || [];
    const updated = [...current];
    updated.splice(index, 1);
    this.updateField('salaryComponents', updated);
  }

  updateSalaryComponent(index: number, field: 'type' | 'amount', value: any) {
    const current = this.employee().salaryComponents || [];
    const updated = [...current];
    updated[index] = { ...updated[index], [field]: value };
    this.updateField('salaryComponents', updated);
  }

  onCimrEmployeeRateChange(value: number | null): void {
    this.updateField('cimrEmployeeRate', value);
    
    // Calculate company rate automatically: employee rate + 30% of employee rate
    // Example: if employee rate is 10%, company rate = 10% + (10% * 0.30) = 13%
    if (value !== null && value !== undefined && !isNaN(value)) {
      const companyRate = value * 1.30;
      this.updateField('cimrCompanyRate', companyRate);
    } else {
      this.updateField('cimrCompanyRate', null);
    }
  }

  // Save workflow with confirmation
  saveWithConfirmation(): void {
    if (!this.changeSet().hasChanges) {
      this.saveSuccess.set(this.translate.instant('employees.profile.noChanges'));
      setTimeout(() => this.saveSuccess.set(null), 2000);
      return;
    }
    
    this.showConfirmDialog.set(true);
  }

  confirmSave(): void {
    this.showConfirmDialog.set(false);
    this.performSave();
  }

  private async performSave(): Promise<void> {
    if (!this.originalEmployee || !this.employeeId()) {
      return;
    }

    this.isSaving.set(true);
    this.saveError.set(null);

    try {
      // Handle new Country creation
      let currentCountryId = this.employee().countryId;
      const currentCountryName = this.employee().countryName;
      
      if (!currentCountryId && currentCountryName) {
        try {
           const newCountry = await firstValueFrom(this.employeeService.createCountry(currentCountryName));
           if (newCountry) {
             this.updateField('countryId', newCountry.id);
             currentCountryId = newCountry.id;
           }
        } catch (e) {
           console.error('Failed to create country', e);
        }
      }

      // Handle new City creation
      let currentCityId = this.employee().cityId;
      const currentCityName = this.employee().city;

      if (!currentCityId && currentCityName) {
        try {
           console.log('Creating country with name:', currentCountryName);
           console.log('Creating city with name:', currentCityName, 'and countryId:', currentCountryId);
           const newCity = await firstValueFrom(this.employeeService.createCity(currentCityName, currentCountryId));
           if (newCity) {
             this.updateField('cityId', newCity.id);
             currentCityId = newCity.id;
           }
        } catch (e) {
           console.error('Failed to create city', e);
        }
      }

      const patch = ChangeTracker.generatePatch(
        this.originalEmployee,
        this.employee(),
        ['id', 'photo', 'missingDocuments', 'salaryComponents', 'activeSalaryId']
      );

      if (Object.keys(patch).length > 0) {
        // backend expects categoryId (Employee model uses CategoryId)
        if ((patch as any).employeeCategoryId !== undefined) {
          (patch as any).categoryId = (patch as any).employeeCategoryId;
          delete (patch as any).employeeCategoryId;
        }

        await firstValueFrom(
          this.employeeService.patchEmployeeProfile(this.employeeId()!, patch)
        );
      }

      // Handle Salary Components
      const salaryDetails = await firstValueFrom(this.employeeService.getEmployeeSalaryDetails(this.employeeId()!));
      const newActiveSalaryId = salaryDetails.id;
      const oldActiveSalaryId = this.employee().activeSalaryId;

      if (newActiveSalaryId) {
        const currentComponents = this.employee().salaryComponents || [];
        const componentPromises = [];

        if (newActiveSalaryId !== oldActiveSalaryId) {
           // New salary created. Add all components to it.
           for (const c of currentComponents) {
             componentPromises.push(firstValueFrom(this.employeeService.addSalaryComponent({
               employeeSalaryId: newActiveSalaryId,
               componentType: c.type,
               amount: c.amount,
               effectiveDate: new Date().toISOString()
             })));
           }
        } else {
          // Same salary. Diff changes.
          const originalComponents = this.originalEmployee?.salaryComponents || [];
          
          const newComponents = currentComponents.filter(c => !c.id);
          
          const modifiedComponents = currentComponents.filter(c => {
            if (!c.id) return false;
            const original = originalComponents.find(o => o.id === c.id);
            return original && (original.type !== c.type || original.amount !== c.amount);
          });

          const deletedComponents = originalComponents.filter(o => 
            o.id && !currentComponents.find(c => c.id === o.id)
          );

          for (const c of newComponents) {
            componentPromises.push(firstValueFrom(this.employeeService.addSalaryComponent({
              employeeSalaryId: newActiveSalaryId,
              componentType: c.type,
              amount: c.amount,
              effectiveDate: new Date().toISOString()
            })));
          }

          for (const c of modifiedComponents) {
            componentPromises.push(firstValueFrom(this.employeeService.updateSalaryComponent(c.id!, {
              id: c.id,
              employeeSalaryId: newActiveSalaryId,
              componentType: c.type,
              amount: c.amount,
              effectiveDate: new Date().toISOString()
            })));
          }

          for (const c of deletedComponents) {
            componentPromises.push(firstValueFrom(this.employeeService.deleteSalaryComponent(c.id!)));
          }
        }

        if (componentPromises.length > 0) {
          await Promise.all(componentPromises);
        }
      }

      // Handle Spouses changes
      const currentSpouses = this.employee().spouses || [];
      const originalSpouses = this.originalEmployee?.spouses || [];
      
      // Find new spouses (no id or temporary id > 1000000000000)
      const newSpouses = currentSpouses.filter(s => !s.id || s.id > 1000000000000);
      
      // Find updated spouses
      const updatedSpouses = currentSpouses.filter(s => {
        if (!s.id || s.id > 1000000000000) return false;
        const original = originalSpouses.find(o => o.id === s.id);
        return original && JSON.stringify(s) !== JSON.stringify(original);
      });
      
      // Find deleted spouses
      const deletedSpouses = originalSpouses.filter(o => 
        o.id && !currentSpouses.find(s => s.id === o.id)
      );

      const spousePromises = [];
      
      for (const spouse of newSpouses) {
        spousePromises.push(firstValueFrom(this.employeeService.createSpouse(this.employeeId()!, spouse)));
      }
      
      for (const spouse of updatedSpouses) {
        spousePromises.push(firstValueFrom(this.employeeService.updateSpouse(this.employeeId()!, spouse)));
      }
      
      for (const spouse of deletedSpouses) {
        spousePromises.push(firstValueFrom(this.employeeService.deleteSpouse(this.employeeId()!)));
      }
      
      if (spousePromises.length > 0) {
        await Promise.all(spousePromises);
      }

      // Handle Children changes
      const currentChildren = this.employee().children || [];
      const originalChildren = this.originalEmployee?.children || [];
      
      // Find new children (no id or temporary id > 1000000000000)
      const newChildren = currentChildren.filter(c => !c.id || c.id > 1000000000000);
      
      // Find updated children
      const updatedChildren = currentChildren.filter(c => {
        if (!c.id || c.id > 1000000000000) return false;
        const original = originalChildren.find(o => o.id === c.id);
        return original && JSON.stringify(c) !== JSON.stringify(original);
      });
      
      // Find deleted children
      const deletedChildren = originalChildren.filter(o => 
        o.id && !currentChildren.find(c => c.id === o.id)
      );

      const childPromises = [];
      
      for (const child of newChildren) {
        childPromises.push(firstValueFrom(this.employeeService.createChild(this.employeeId()!, child)));
      }
      
      for (const child of updatedChildren) {
        childPromises.push(firstValueFrom(this.employeeService.updateChild(this.employeeId()!, child.id!, child)));
      }
      
      for (const child of deletedChildren) {
        childPromises.push(firstValueFrom(this.employeeService.deleteChild(this.employeeId()!, child.id!)));
      }
      
      if (childPromises.length > 0) {
        await Promise.all(childPromises);
      }

      this.saveSuccess.set(this.translate.instant('employees.profile.saveSuccess'));
      this.clearDraftsForAllTabs();
      this.isEditMode.set(false);
      
      // Reload to refresh state
      this.loadEmployeeDetails(this.employeeId()!);
      
      this.resolveAfterSave(true);
      
    } catch (err) {
      console.error('Save failed', err);
      this.saveError.set(this.translate.instant('employees.profile.saveError'));
      this.resolveAfterSave(false);
    } finally {
      this.isSaving.set(false);
    }
  }

  // Navigation guard implementation
  canDeactivate(): Observable<boolean> {
    if (!this.changeSet().hasChanges) {
      return of(true);
    }
    
    return new Observable(observer => {
      this.pendingNavigationResolver = (result: boolean) => {
        observer.next(result);
        observer.complete();
      };
      this.showUnsavedDialog.set(true);
    });
  }

  // Unsaved changes dialog handlers
  onUnsavedDialogSave(): void {
    this.showUnsavedDialog.set(false);
    this.performSave();
  }

  onUnsavedDialogDiscard(): void {
    this.showUnsavedDialog.set(false);
    this.revertToOriginal();
    this.clearDraftsForAllTabs();
    this.resetChangeTracking();
    this.draftRestored.set(false);
    if (this.pendingCancel) {
      this.isEditMode.set(false);
    }
    this.resolveAfterSave(true);
  }

  onUnsavedDialogCancel(): void {
    this.showUnsavedDialog.set(false);
    this.resolveAfterSave(false);
  }

  cancel(): void {
    if (this.hasFormChanges()) {
      this.pendingCancel = true;
      this.showUnsavedDialog.set(true);
      return;
    }

    this.revertToOriginal();
    this.clearDraftsForAllTabs();
    this.resetChangeTracking();
    this.isEditMode.set(false);
    this.saveError.set(null);
    this.pendingTabTarget = null;
    this.pendingNavigationResolver = null;
    this.draftRestored.set(false);
    this.pendingCancel = false;
  }

  goBack(): void {
    this.router.navigate([`${this.routePrefix()}/employees`]);
  }

  // Navigate to salary packages page
  navigateToSalaryPackages(): void {
    const packageId = this.employee().assignedPackage?.salaryPackageId;
    this.router.navigate([`${this.routePrefix()}/salary-packages`], {
      queryParams: packageId ? { highlight: packageId } : {}
    });
  }

  // Navigate to assign package (go to packages list)
  navigateToAssignPackage(): void {
    this.router.navigate([`${this.routePrefix()}/salary-packages`]);
  }

  uploadDocument(event: any, documentType: string): void {
    // TODO: Handle document upload
    console.log('Uploading document:', documentType, event.files);
  }

  downloadDocument(doc: Document) {
    // TODO: Download document
    console.log('Downloading:', doc);
  }

  deleteDocument(doc: Document) {
    // TODO: Delete document
    console.log('Deleting:', doc);
  }

  getEventIcon(type: string): string {
    const iconMap: Record<string, string> = {
      salary_increase: 'pi pi-dollar',
      salary_change: 'pi pi-dollar',
      position_change: 'pi pi-briefcase',
      address_updated: 'pi pi-map-marker',
      general_update: 'pi pi-user-edit',
      note: 'pi pi-file-edit'
    };
    return iconMap[type] || 'pi pi-circle';
  }

  getEventColor(type: string): string {
    const colorMap: Record<string, string> = {
      salary_increase: 'text-green-600',
      salary_change: 'text-green-600',
      position_change: 'text-blue-600',
      address_updated: 'text-orange-600',
      general_update: 'text-purple-600',
      note: 'text-gray-600'
    };
    return colorMap[type] || 'text-gray-600';
  }

  // Enhanced history tab methods
  getEventSeverityClass(type: string): string {
    const classes: Record<string, string> = {
      salary_increase: 'text-green-600 bg-green-50 border-green-200',
      salary_change: 'text-green-600 bg-green-50 border-green-200',
      position_change: 'text-blue-600 bg-blue-50 border-blue-200',
      address_updated: 'text-amber-600 bg-amber-50 border-amber-200',
      general_update: 'text-purple-600 bg-purple-50 border-purple-200',
      note: 'text-gray-600 bg-gray-50 border-gray-200'
    };
    return classes[type] || 'text-gray-600 bg-gray-50 border-gray-200';
  }

  formatRelativeTime(dateStr: string): string {
    const date = new Date(dateStr);
    const now = new Date();
    const seconds = Math.round((now.getTime() - date.getTime()) / 1000);
    const minutes = Math.round(seconds / 60);
    const hours = Math.round(minutes / 60);
    const days = Math.round(hours / 24);

    if (seconds < 60) return this.translate.instant('common.time.justNow');
    if (minutes < 60) return this.translate.instant('common.time.minutesAgo', { count: minutes });
    if (hours < 24) return this.translate.instant('common.time.hoursAgo', { count: hours });
    if (days < 7) return this.translate.instant('common.time.daysAgo', { count: days });
    return this.formatHistoryDate(dateStr);
  }

  formatHistoryDate(dateStr: string): string {
    const date = new Date(dateStr);
    return new Intl.DateTimeFormat('fr-FR', {
      dateStyle: 'medium',
      timeStyle: 'short'
    }).format(date);
  }

  getHistoryTooltipText(dateStr: string): string {
    return this.formatHistoryDate(dateStr);
  }

  onHistorySearch(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.historySearchQuery.set(target.value);
  }

  onHistoryTypeChange(types: string[]): void {
    this.selectedHistoryTypes.set(types);
  }

  clearHistoryFilters(): void {
    this.historySearchQuery.set('');
    this.selectedHistoryTypes.set([]);
  }

  removeHistoryFilter(filterType: 'searchQuery' | 'eventType'): void {
    if (filterType === 'searchQuery') {
      this.historySearchQuery.set('');
    } else if (filterType === 'eventType') {
      this.selectedHistoryTypes.set([]);
    }
  }

  toggleHistoryFilters(): void {
    this.historyFiltersExpanded.update(val => !val);
  }

  hasFormChanges(): boolean {
    return this.changeSet().hasChanges;
  }

  onTabChange(nextTab: string | number | undefined): void {
    const targetTab = String(nextTab ?? this.activeTab());

    if (!this.isEditMode() || !this.hasFormChanges()) {
      this.activeTab.set(targetTab);
      return;
    }

    if (targetTab === this.activeTab()) {
      return;
    }

    this.pendingTabTarget = targetTab;
    this.showUnsavedDialog.set(true);
  }

  private handleSaveSuccess(updated: EmployeeProfileModel): void {
    this.originalEmployee = { ...updated };
    this.employee.set({ ...updated });
    this.lastSerializedEmployee = JSON.stringify(updated);
    this.resetChangeTracking();
    this.clearDraftsForAllTabs();
    this.isSaving.set(false);
    this.saveSuccess.set(this.translate.instant('employees.profile.saveSuccess'));
    this.isEditMode.set(false);
    this.draftRestored.set(false);
    this.pendingDraftData = null;
    this.pendingDraftTimestamp = null;
    this.announce('Profile saved successfully.');
    this.resolveAfterSave(true);

    setTimeout(() => this.saveSuccess.set(null), 3000);
  }

  private resolveAfterSave(success: boolean): void {
    if (success && this.pendingTabTarget) {
      this.activeTab.set(this.pendingTabTarget);
    }

    if (this.pendingNavigationResolver) {
      this.pendingNavigationResolver(success);
    }

    this.pendingTabTarget = null;
    this.pendingNavigationResolver = null;
    this.pendingCancel = false;
  }

  private revertToOriginal(): void {
    if (!this.originalEmployee) {
      return;
    }

    this.isRestoringDraft = true;
    this.employee.set({ ...this.originalEmployee });
    this.lastSerializedEmployee = JSON.stringify(this.originalEmployee);
    setTimeout(() => {
      this.isRestoringDraft = false;
    }, 100);
  }

  private resetChangeTracking(): void {
    this.changeSet.set(this.createEmptyChangeSet());
    this.tabChangeSets.set(this.createEmptyTabChangeSets());
  }

  private recomputeChangeTracking(): void {
    if (!this.originalEmployee || !this.isEditMode()) {
      return;
    }

    const changes = ChangeTracker.trackChanges(
      this.originalEmployee,
      this.employee(),
      this.FIELD_LABELS,
      ['id', 'photo', 'missingDocuments']
    );

    this.changeSet.set(changes);
    this.updateTabChangeSets(changes);
  }

  private updateTabChangeSets(changeSet: ChangeSet): void {
    const perTab = this.createEmptyTabChangeSets();

    changeSet.changes.forEach(change => {
      const tabId = this.getTabForField(change.field);
      const target = perTab[tabId];

      target.changes.push(change);
      target.modifiedFields.push(change.field);
      target.changeCount = target.changes.length;
      target.hasChanges = target.changeCount > 0;
    });

    this.tabChangeSets.set(perTab);
  }

  private getTabForField(field: string): string {
    return this.TAB_IDS.find(tabId => this.TAB_FIELD_MAP[tabId].includes(field as keyof EmployeeProfileModel)) ?? '0';
  }

  private createEmptyChangeSet(): ChangeSet {
    return { changes: [], hasChanges: false, modifiedFields: [], changeCount: 0 };
  }

  private createEmptyTabChangeSets(): Record<string, ChangeSet> {
    return this.TAB_IDS.reduce((acc, tabId) => {
      acc[tabId] = this.createEmptyChangeSet();
      return acc;
    }, {} as Record<string, ChangeSet>);
  }

  private saveDraftForTab(tabId: string): void {
    const id = this.employeeId();
    if (!id) {
      return;
    }

    const draftKey = this.getDraftKeyForTab(tabId);
    this.draftService.saveDraft(draftKey, id, this.employee());
  }

  private getDraftKeyForTab(tabId: string): string {
    return `${this.ENTITY_TYPE}_tab_${tabId}`;
  }

  private getLatestDraft(): { data: EmployeeProfileModel; savedAt: Date } | null {
    const id = this.employeeId();
    if (!id) {
      return null;
    }

    let latest: { data: EmployeeProfileModel; savedAt: Date } | null = null;

    this.TAB_IDS.forEach(tabId => {
      const draft = this.draftService.loadDraft<EmployeeProfileModel>(this.getDraftKeyForTab(tabId), id);
      if (draft) {
        const savedAt = new Date(draft.metadata.savedAt);
        if (!latest || savedAt > latest.savedAt) {
          latest = { data: draft.data, savedAt };
        }
      }
    });

    return latest;
  }

  private clearDraftsForAllTabs(): void {
    const id = this.employeeId();
    if (!id) {
      return;
    }

    this.TAB_IDS.forEach(tabId => {
      this.draftService.clearDraft(this.getDraftKeyForTab(tabId), id);
    });
  }

  private isDraftForCurrentEntity(key: string, id: string): boolean {
    return key.includes(`${this.ENTITY_TYPE}_tab_`) && key.endsWith(`_${id}`);
  }

  private announce(message: string): void {
    this.ariaMessage.set(message);
  }

  private createEmptyEmployee(): EmployeeProfileModel {
    return {
      id: '',
      firstName: '',
      lastName: '',
      photo: undefined,
      cin: '',
      maritalStatus: 'single',
      dateOfBirth: '',
      birthPlace: '',
      professionalEmail: '',
      personalEmail: '',
      phone: '',
      address: '',
      countryId: undefined,
      countryName: '',
      city: '',
      addressLine1: '',
      addressLine2: '',
      zipCode: '',
      position: '',
      department: '',
      manager: '',
      contractType: 'CDI',
      startDate: '',
      endDate: undefined,
      probationPeriod: '',
      exitReason: undefined,
      baseSalary: 0,
      salaryComponents: [],
      paymentMethod: 'bank_transfer',
      cnss: '',
      amo: '',
      cimr: undefined,
      annualLeave: 0,
      status: 'active',
      missingDocuments: 0,
      spouses: [],
      children: []
    };
  }
}
