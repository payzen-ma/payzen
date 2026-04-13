import { CommonModule } from '@angular/common';
import { Component, computed, DestroyRef, effect, HostListener, inject, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { CanComponentDeactivate } from '@app/core/guards/unsaved-changes.guard';
import { ContractType } from '@app/core/models/contract-type.model';
import { Child, Employee as EmployeeProfileModel, Spouse } from '@app/core/models/employee.model';
import { JobPosition } from '@app/core/models/job-position.model';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { ContractTypeService } from '@app/core/services/contract-type.service';
import { DraftService } from '@app/core/services/draft.service';
import { EmployeeCategoryService } from '@app/core/services/employee-category.service';
import {
  EmployeeFormData,
  EmployeeService,
  LookupOption,
  NonImposableOption
} from '@app/core/services/employee.service';
import { FamilyService } from '@app/core/services/family.service';
import { JobPositionService } from '@app/core/services/job-position.service';
import { ToastService } from '@app/core/services/toast.service';
import { ChangeSet, ChangeTracker } from '@app/core/utils/change-tracker.util';
import { ChangeConfirmationDialog } from '@app/shared/components/change-confirmation-dialog/change-confirmation-dialog';
import { UnsavedChangesDialog } from '@app/shared/components/unsaved-changes-dialog/unsaved-changes-dialog';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { AvatarModule } from 'primeng/avatar';
import { ButtonModule } from 'primeng/button';
import { CheckboxModule } from 'primeng/checkbox';
import { ChipModule } from 'primeng/chip';
import { FileUploadModule } from 'primeng/fileupload';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MultiSelectModule } from 'primeng/multiselect';
import { SelectModule } from 'primeng/select';
import { SkeletonModule } from 'primeng/skeleton';
import { TabsModule } from 'primeng/tabs';
import { TextareaModule } from 'primeng/textarea';
import { TimelineModule } from 'primeng/timeline';
import { TooltipModule } from 'primeng/tooltip';
import { firstValueFrom, forkJoin, Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { TagComponent } from '../../../shared/components/tag/tag.component';
import { TagVariant } from '../../../shared/components/tag/tag.types';
import { SpouseChildrenComponent } from '../family/spouse-children';

interface Document {
  id?: number;
  type: string;
  name: string;
  uploadDate: string;
  status: 'uploaded' | 'missing';
  filePath?: string;
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
  private toastService = inject(ToastService);

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

  private pendingDraftData: Partial<EmployeeProfileModel> | null = null;
  private pendingDraftTimestamp: Date | null = null;

  private readonly TAB_IDS = ['0', '1', '2', '3', '4', '5', '6', '7'] as const;
  private readonly TAB_FIELD_MAP: Record<string, (keyof EmployeeProfileModel)[]> = {
    '0': ['firstName', 'lastName', 'cin', 'maritalStatus', 'dateOfBirth'],
    '1': ['personalEmail', 'phone', 'address', 'countryId', 'countryName', 'city', 'addressLine1', 'addressLine2', 'zipCode'],
    '2': [], // Family - Spouse & Children managed by separate component
    '3': [
      'position',
      'jobPositionId',
      'department',
      'departementId',
      'manager',
      'contractType',
      'contractTypeId',
      'employeeCategoryId',
      'status',
      'startDate',
      'endDate',
      'probationPeriod'
    ],
    '4': ['baseSalary', 'baseSalaryHourly', 'salaryEffectiveDate', 'salaryComponents', 'paymentMethod'],
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

  // Primes non imposables (liste statique miroir du backend, disponible immédiatement)
  readonly nonImposableOptions = signal<NonImposableOption[]>(this.employeeService.getNonImposableComponents());

  // Liste pour le dropdown : primes du catalogue + option "Autre (saisie libre)"
  readonly nonImposableWithCustom = computed(() => [
    ...this.nonImposableOptions(),
    { code: '__AUTRE__', label: '— Autre (saisie libre)' }
  ]);
  // Field labels for change tracking
  private readonly FIELD_LABELS: Record<string, string> = {
    firstName: 'First Name',
    lastName: 'Last Name',
    cin: 'National ID',
    maritalStatus: 'Marital Status',
    dateOfBirth: 'Date of Birth',
    //birthPlace: 'Place of Birth',
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
    baseSalaryHourly: 'Hourly Salary',
    salaryEffectiveDate: 'Salary Effective Date',
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

  private readonly documentTemplates: Array<{ type: string; name: string }> = [
    { type: 'cin', name: 'CIN' },
    { type: 'contract', name: 'Contrat de travail' },
    { type: 'rib', name: 'RIB' },
    { type: 'job_description', name: 'Fiche de poste' }
  ];
  readonly documents = signal<Document[]>(this.buildDocumentCards([]));

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

  private _historyTypeOptions = computed(() => {
    const types = Array.from(new Set(this.history().map(e => e.type).filter(Boolean)));
    const humanize = (t: string) => {
      return t.replace(/([a-z0-9])([A-Z])/g, '$1 $2')
        .replace(/_/g, ' ')
        .replace(/\b\w/g, ch => ch.toUpperCase());
    };

    return types.map(t => {
      const key = `employees.history.types.${t}`;
      const translated = this.translate.instant(key);
      const label = (translated && translated !== key) ? translated : humanize(t);
      return { label, value: t };
    });
  });

  // Expose as plain array for template consumption
  get historyTypeOptions(): { label: any; value: string }[] {
    try {
      return this._historyTypeOptions();
    } catch {
      return [];
    }
  }

  private _maritalStatusOptions = computed(() => {
    const formItems = this.formData()?.maritalStatuses ?? [];
    const lang = (this.translate?.currentLang || 'fr').toLowerCase();
    const cap = (s: string) => s ? s.charAt(0).toUpperCase() + s.slice(1) : s;
    const capLang = cap(lang);
    const getLabel = (o: any) => {
      if (!o) return '';
      return (
        o[`name${capLang}`] ??
        o[`Name${capLang}`] ??
        o.label ??
        o.name ??
        o.Name ??
        o.NameFr ??
        o.NameEn ??
        o.NameAr ??
        o.nameFr ??
        o.nameEn ??
        o.nameAr ??
        String(o.id ?? '')
      );
    };

    // Map IDs to canonical lowercase codes
    const idToCodeMap: { [key: number]: EmployeeProfileModel['maritalStatus'] } = {
      1: 'single',
      2: 'married',
      3: 'divorced',
      4: 'widowed'
    };

    if (formItems && formItems.length > 0) {
      const normalizeMaritalCode = (raw: unknown, idFallback: unknown): EmployeeProfileModel['maritalStatus'] => {
        const rawNorm = String(raw ?? '').trim().toLowerCase();
        const idFromRaw = Number(rawNorm);
        if (rawNorm && !Number.isNaN(idFromRaw) && idToCodeMap[idFromRaw]) {
          return idToCodeMap[idFromRaw];
        }

        const idNum = Number(idFallback);
        if (!Number.isNaN(idNum) && idToCodeMap[idNum]) {
          return idToCodeMap[idNum];
        }

        switch (rawNorm) {
          case 'single':
          case 'married':
          case 'divorced':
          case 'widowed':
            return rawNorm;
          default:
            return 'single';
        }
      };

      return formItems.map((o: any, idx: number) => {
        const id = o.id ?? idx;
        return {
          id,
          label: getLabel(o),
          value: normalizeMaritalCode(o.code ?? o.value, id)
        };
      });
    }

    return [
      { id: 1, label: this.translate.instant('employees.profile.maritalStatus.single'), value: 'single' as EmployeeProfileModel['maritalStatus'] },
      { id: 2, label: this.translate.instant('employees.profile.maritalStatus.married'), value: 'married' as EmployeeProfileModel['maritalStatus'] },
      { id: 3, label: this.translate.instant('employees.profile.maritalStatus.divorced'), value: 'divorced' as EmployeeProfileModel['maritalStatus'] },
      { id: 4, label: this.translate.instant('employees.profile.maritalStatus.widowed'), value: 'widowed' as EmployeeProfileModel['maritalStatus'] }
    ];
  });

  // Expose as a plain array for templates expecting `any[]`
  get maritalStatusOptions(): { id: any; label: any; value: EmployeeProfileModel['maritalStatus'] }[] {
    try {
      return this._maritalStatusOptions();
    } catch {
      return [];
    }
  }

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

    // Refresh localized lookup labels (genders, statuses, etc.) when language changes
    this.translate.onLangChange
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        // Force reload so labels are mapped for the new language
        this.loadFormData(true);
      });

    // Also react directly to the companyId signal to ensure reload in all change paths
  }

  private loadEmployeeDetails(id: string): void {
    this.isLoadingProfile.set(true);
    this.loadError.set(null);

    forkJoin({
      details: this.employeeService.getEmployeeDetails(id),
      salary: this.employeeService.getEmployeeSalaryDetails(id),
      documents: this.employeeService.getDocuments(id).pipe(catchError(() => of([]))),
      statuses: this.employeeService.getStatuses(false),
      form: this.employeeService.getEmployeeFormData(),
      spouse: this.familyService.getSpouse(id).pipe(catchError(() => of(null))),
      children: this.familyService.getChildren(id).pipe(catchError(() => of([]))),
      assignment: this.employeeService.getActivePackageAssignment(id).pipe(catchError(() => of(null)))
    }).subscribe({
      next: ({ details, salary, documents, statuses, form, spouse, children, assignment }) => {
        // L'historique formaté (y compris filtrage RH / non-RH) vient de GET .../details → Events
        this.isRestoringDraft = true;

        // API Payzen : GET .../spouse renvoie un tableau JSON ; ne pas envelopper une 2e fois.
        // JSON PascalCase (Id, FirstName…) : normaliser pour que les filtres new/update utilisent `id`.
        const spouses = this.normalizeSpousesFromApi(spouse);

        // Map backend PascalCase `details` to frontend camelCase employee shape
        const mappedDetails = this.mapBackendDetails(details, form);
        this.documents.set(this.buildDocumentCards(documents));

        // If lookup form data returned, populate formData so labels (eg. genders) are localized
        if (form) {
          this.formData.set(form);
        }

        // Merge salary components with IDs
        const mergedEmployee = {
          ...mappedDetails,
          activeSalaryId: salary.id ?? (salary as any).Id,
          salaryComponents: salary.components.length > 0 ? salary.components : (mappedDetails.salaryComponents || []),
          spouses: spouses,
          children: this.normalizeChildrenFromApi(children),
          assignedPackage: assignment
        } as EmployeeProfileModel;

        // We'll set the employee and try to enrich its `statusName` from referential statuses.
        this.employee.set(mergedEmployee);
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
              error: () => { }
            });
            // Load employee categories
            this.employeeCategoryService.getLookupOptions(cidNum).subscribe({
              next: (categories) => {
                this.formData.update(f => ({ ...f, employeeCategories: categories }));
              },
              error: () => { }
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
    } catch (e) { }

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
    } catch (e) { }

    const status = this.employee().status;
    if (status === 'active') return 'Actif';
    if (status === 'on_leave') return 'En congé';
    return 'Inactif';
  }

  getMaritalStatusLabel(): string {
    const ms = this.employee().maritalStatus;

    if (!ms) {
      return '-';
    }
    try {
      const opts = this.maritalStatusOptions;
      const found = (opts || []).find((o: any) => {
        return o.value === ms || String(o.id) === String(ms) || String(o.value) === String(ms);
      });
      return found?.label || '-';
    } catch {
      return '-';
    }
  }

  getPaymentMethodLabel(): string {
    return this.paymentMethodMap[this.employee().paymentMethod] || '';
  }

  getCurrentCountryPhoneCode(): string {
    const emp = this.employee();
    if (emp.countryPhoneCode && emp.countryPhoneCode.trim()) return emp.countryPhoneCode;
    const country = (this.formData().countries || []).find(c => Number(c.id) === Number(emp.countryId));
    return country?.phoneCode ?? '';
  }

  onCountryPhoneCodeChange(value: string): void {
    const raw = String(value ?? '').trim();
    const digits = raw.replace(/\D/g, '').slice(0, 4);
    const normalized = digits ? `+${digits}` : '';
    this.updateField('countryPhoneCode', normalized as any);
  }

  onPhoneLocalChange(value: string): void {
    const sanitized = String(value ?? '').replace(/\D/g, '').slice(0, 9);
    this.updateField('phone', sanitized as any);
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
  private mapBackendDetails(d: any, form?: any): Partial<EmployeeProfileModel> {
    if (!d) return {};
    const out: any = {};
    out.id = d.Id != null ? String(d.Id) : (d.id ?? undefined);
    out.firstName = d.FirstName ?? d.firstName;
    out.lastName = d.LastName ?? d.lastName;
    out.cin = d.CinNumber ?? d.Cin ?? d.cin;

    // Handle maritalStatus: convert MaritalStatusName to code (minuscules pour le frontend)
    let maritalStatusCode = 'single'; // Valeur par défaut

    // Si on a directement le code du backend (MAJUSCULES)
    if (d.MaritalStatus || d.maritalStatus) {
      const code = (d.MaritalStatus ?? d.maritalStatus);
      maritalStatusCode = code.toLowerCase();
    }
    // Si on a le nom, chercher le code correspondant
    else if (d.MaritalStatusName) {
      const statusName = d.MaritalStatusName.toLowerCase().trim();
      const normalizedName = statusName.replace(/[éè()]/g, '');

      // Mapping français (normalisé) -> code (minuscules)
      const frenchToCodeMap: { [key: string]: string } = {
        'celibataire': 'single',
        'marie': 'married',
        'divorce': 'divorced',
        'veufveuve': 'widowed'
      };

      maritalStatusCode = frenchToCodeMap[normalizedName] ?? 'single';

      // Sinon, chercher dans les options du formulaire
      if (maritalStatusCode === 'single' && form?.maritalStatuses) {
        const matchedStatus = form.maritalStatuses.find((ms: any) => {
          const label = (ms.nameFr ?? ms.name ?? ms.label ?? '').toLowerCase().trim();
          return label === statusName;
        });
        if (matchedStatus?.code) {
          maritalStatusCode = (matchedStatus.code).toLowerCase();
        }
      }
    }

    out.maritalStatus = maritalStatusCode as any;
    out.dateOfBirth = d.DateOfBirth ?? d.dateOfBirth;
    //out.birthPlace = d.BirthPlace ?? d.birthPlace;
    out.professionalEmail = d.Email ?? d.ProfessionalEmail ?? d.professionalEmail;
    out.personalEmail = d.PersonalEmail ?? d.personalEmail;
    out.countryName = d.CountryName ?? d.countryName;
    out.countryId = d.CountryId ?? d.countryId;
    const countryFromForm = (form?.countries ?? []).find((c: any) => Number(c.id) === Number(out.countryId));
    const rawPhone = String(d.Phone ?? d.phone ?? '').trim();
    let resolvedCountryPhoneCode =
      d.CountryPhoneCode ??
      d.countryPhoneCode ??
      countryFromForm?.phoneCode ??
      countryFromForm?.CountryPhoneCode ??
      '';

    // Fallback: infer country code from stored full phone (ex: +212671642688)
    if (!resolvedCountryPhoneCode && rawPhone.startsWith('+')) {
      const countryCodes = (form?.countries ?? [])
        .map((c: any) => String(c.phoneCode ?? c.CountryPhoneCode ?? '').trim())
        .filter((code: string) => !!code && code.startsWith('+'))
        .sort((a: string, b: string) => b.length - a.length);

      const matched = countryCodes.find((code: string) => rawPhone.startsWith(code));
      if (matched) {
        resolvedCountryPhoneCode = matched;
      } else {
        // Last fallback when referential is unavailable:
        // infer country code from full number and fixed local length (9 digits).
        const digits = rawPhone.replace(/\D/g, '');
        if (digits.length > 9) {
          resolvedCountryPhoneCode = `+${digits.slice(0, digits.length - 9)}`;
        } else {
          resolvedCountryPhoneCode = '';
        }
      }
    }

    if (resolvedCountryPhoneCode && !out.countryId) {
      const matchedCountry = (form?.countries ?? []).find((c: any) => {
        const code = String(c.phoneCode ?? c.CountryPhoneCode ?? '').trim();
        return code === resolvedCountryPhoneCode;
      });
      if (matchedCountry?.id) {
        out.countryId = Number(matchedCountry.id);
        out.countryName = out.countryName || matchedCountry.label || matchedCountry.countryName || matchedCountry.CountryName;
      }
    }

    let localPhone = rawPhone;
    if (resolvedCountryPhoneCode && rawPhone.startsWith(resolvedCountryPhoneCode)) {
      localPhone = rawPhone.slice(String(resolvedCountryPhoneCode).length);
    }
    localPhone = localPhone.replace(/\D/g, '');
    if (localPhone.length > 9) {
      localPhone = localPhone.slice(-9);
    }
    out.countryPhoneCode = resolvedCountryPhoneCode;
    out.phone = localPhone;
    out.address = d.Address ?? d.address;
    // Address individual fields
    out.addressLine1 = d.AddressLine1 ?? d.addressLine1;
    out.addressLine2 = d.AddressLine2 ?? d.addressLine2;
    out.zipCode = d.ZipCode ?? d.zipCode;
    out.city = d.City ?? d.city;
    out.cityId = d.CityId ?? d.cityId;
    out.position = this.normalizeUnassignedText(d.JobPositionName ?? d.Position ?? d.position);
    out.jobPositionId = d.JobPositionId ?? d.jobPositionId ?? undefined;
    out.department = d.DepartmentName ?? d.department ?? (d.departments ?? '');
    out.departementId = d.DepartementId ?? d.departementId ?? undefined;
    out.manager = d.ManagerName ?? d.Manager ?? d.manager;
    out.contractType = d.ContractTypeName ?? d.ContractType ?? d.contractType;
    out.contractTypeId = d.ContractTypeId ?? d.contractTypeId ?? undefined;
    out.startDate = d.ContractStartDate ?? d.StartDate ?? d.startDate;
    out.endDate = d.ContractEndDate ?? d.EndDate ?? d.endDate;
    out.probationPeriod = d.ProbationPeriod ?? d.probationPeriod;
    out.baseSalary = d.BaseSalary ?? d.baseSalary ?? 0;
    out.baseSalaryHourly = d.BaseSalaryHourly ?? d.baseSalaryHourly ?? 0;
    out.salaryEffectiveDate = d.salaryEffectiveDate ?? d.SalaryEffectiveDate ?? null;
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
      this.loadFormData();
      this.lastSerializedEmployee = JSON.stringify(this.employee());
      this.resetChangeTracking();
    } else {
      // Exiting edit mode - cancel changes
      this.cancel();
      return;
    }
    this.isEditMode.update(v => !v);
    this.saveError.set(null);
    this.saveSuccess.set(null);
  }

  loadFormData(force: boolean = false): void {
    if (!force && this.formData().statuses.length > 0) {
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

        // Fallbacks: if jobPositions or contractTypes are missing, load them by companyId
        const companyIdRaw = this.contextService.companyId();
        if (companyIdRaw !== null && companyIdRaw !== undefined) {
          const companyIdNum = Number(companyIdRaw as any);
          if (!Number.isNaN(companyIdNum)) {
            if (!merged.jobPositions || merged.jobPositions.length === 0) {
              this.jobPositionService.getByCompany(companyIdNum).subscribe({
                next: (items: JobPosition[]) => this.formData.update(f => ({ ...f, jobPositions: items.map((i: JobPosition) => ({ id: i.id, label: i.name })) })),
                error: () => { }
              });
            }
            if (!merged.contractTypes || merged.contractTypes.length === 0) {
              this.contractTypeService.getByCompany(companyIdNum).subscribe({
                next: (items: ContractType[]) => this.formData.update(f => ({ ...f, contractTypes: items.map((i: ContractType) => ({ id: i.id, label: i.contractTypeName, value: i.contractTypeName })) })),
                error: () => { }
              });
            }
            // Load company-scoped employee categories so edit forms have the lookup available
            this.employeeCategoryService.getLookupOptions(companyIdNum).subscribe({
              next: (categories) => this.formData.update(f => ({ ...f, employeeCategories: categories })),
              error: () => { }
            });
          }
        }
      },
      error: (err) => {
        // fallback: try to at least set form data
        this.employeeService.getEmployeeFormData().subscribe({
          next: (data) => this.formData.set(data),
          error: () => { }
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
  readonly filteredDepartments = signal<LookupOption[]>([]);
  readonly filteredJobPositions = signal<LookupOption[]>([]);
  readonly pendingNewDepartment = signal<string | null>(null);
  readonly pendingNewJobPosition = signal<string | null>(null);

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
      this.updateField('countryPhoneCode', '');
    } else if (value && typeof value === 'object') {
      this.updateField('countryName', value.label);
      this.updateField('countryId', value.id);
      const selected = (this.formData().countries || []).find(c => Number(c.id) === Number(value.id));
      this.updateField('countryPhoneCode', selected?.phoneCode ?? '');
    } else {
      this.updateField('countryName', undefined);
      this.updateField('countryId', undefined);
      this.updateField('countryPhoneCode', '');
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

  searchDepartment(event: any) {
    const raw = this.employee().companyId ?? this.contextService.companyId();
    const companyId = raw ? Number(raw) : undefined;
    this.employeeService.searchDepartments(event.query, companyId).subscribe(data => {
      this.filteredDepartments.set(data);
    });
  }

  selectedDepartment = computed(() => {
    const emp = this.employee();
    return emp.department || '';
  });

  onDepartmentChange(value: any) {
    const emp = this.employee();
    if (typeof value === 'string') {
      this.employee.set({ ...emp, department: value, departementId: undefined });
      this.pendingNewDepartment.set(value);
    } else if (value && typeof value === 'object') {
      const id = value.id != null && Number(value.id) > 0 ? Number(value.id) : undefined;
      this.employee.set({ ...emp, department: value.label, departementId: id });
      this.formData.update(f => ({
        ...f,
        departments: [...(f.departments || []).filter(d => d.id !== id), { id: value.id, label: value.label }]
      }));
      this.pendingNewDepartment.set(null);
    } else {
      this.employee.set({ ...emp, department: '', departementId: undefined });
      this.pendingNewDepartment.set(null);
    }
    this.saveError.set(null);
  }

  searchJobPosition(event: any) {
    const raw = this.employee().companyId ?? this.contextService.companyId();
    const companyId = raw ? Number(raw) : undefined;
    this.employeeService.searchJobPositions(event.query, companyId).subscribe(data => {
      this.filteredJobPositions.set(data);
    });
  }

  private normalizeUnassignedText(value: unknown): string {
    const text = String(value ?? '').trim();
    if (!text) return '';

    const normalized = text
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .replace(/\s+/g, ' ')
      .trim();

    return normalized === 'non assigne' || normalized === 'non assignee' ? '' : text;
  }

  selectedJobPosition = computed(() => {
    const emp = this.employee();
    return this.normalizeUnassignedText(emp.position);
  });

  onJobPositionChange(value: any) {
    const emp = this.employee();
    if (typeof value === 'string') {
      this.employee.set({ ...emp, position: value, jobPositionId: undefined });
      this.pendingNewJobPosition.set(value);
    } else if (value && typeof value === 'object') {
      const id = value.id != null && Number(value.id) > 0 ? Number(value.id) : undefined;
      this.employee.set({ ...emp, position: value.label, jobPositionId: id });
      this.formData.update(f => ({
        ...f,
        jobPositions: [...(f.jobPositions || []).filter(j => j.id !== id), { id: value.id, label: value.label }]
      }));
      this.pendingNewJobPosition.set(null);
    } else {
      this.employee.set({ ...emp, position: '', jobPositionId: undefined });
      this.pendingNewJobPosition.set(null);
    }
    this.saveError.set(null);
  }

  onContractTypeChange(value: string | null): void {
    const emp = this.employee();
    const opts = this.contractTypeSelectOptions();
    const match = opts.find(
      o =>
        (o.value === value || o.label === value) &&
        typeof (o as { id?: number }).id === 'number' &&
        (o as { id: number }).id > 0
    ) as { id: number } | undefined;
    this.employee.set({
      ...emp,
      contractType: (value ?? '') as EmployeeProfileModel['contractType'],
      contractTypeId: match?.id
    });
    this.saveError.set(null);
  }

  addNonImposableComponent() {
    const current = this.employee().salaryComponents || [];
    this.updateField('salaryComponents', [...current, { type: '', amount: 0, isTaxable: false }]);
  }

  addImposableComponent() {
    const current = this.employee().salaryComponents || [];
    this.updateField('salaryComponents', [...current, { type: '', amount: 0, isTaxable: true }]);
  }

  removeSalaryComponent(index: number) {
    const current = this.employee().salaryComponents || [];
    const updated = [...current];
    updated.splice(index, 1);
    this.updateField('salaryComponents', updated);
  }

  updateSalaryComponent(index: number, field: 'type' | 'amount' | 'isTaxable', value: any) {
    const current = this.employee().salaryComponents || [];
    const updated = [...current];
    const finalValue = field === 'isTaxable' ? Boolean(value) : value;
    updated[index] = { ...updated[index], [field]: finalValue };
    this.updateField('salaryComponents', updated);
  }

  onNewNonImposableSelected(index: number, label: string | null) {
    const current = this.employee().salaryComponents || [];
    const updated = [...current];
    updated[index] = { ...updated[index], type: label ?? '', isTaxable: false };
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

  /**
   * Le POST /api/countries exige CountryCode + CountryPhoneCode ; ne pas créer depuis le profil.
   * On résout l’ID via le référentiel (GET) quand l’API détail n’a renvoyé que le nom.
   */
  private async resolveCountryIdFromName(name: string | undefined, existing?: number): Promise<number | undefined> {
    if (existing != null && existing > 0) return existing;
    const trimmed = name?.trim();
    if (!trimmed) return undefined;
    try {
      const list = await firstValueFrom(this.employeeService.searchCountries(trimmed));
      const norm = (s: string) => s.toLowerCase().trim();
      const n = norm(trimmed);
      const exact = list.find(c => norm(c.label) === n);
      if (exact) return exact.id;
      const loose = list.find(c => norm(c.label).includes(n) || n.includes(norm(c.label)));
      return loose?.id;
    } catch {
      return undefined;
    }
  }

  private async resolveCityIdFromName(
    cityName: string | undefined,
    countryId: number | undefined,
    existing?: number
  ): Promise<number | undefined> {
    if (existing != null && existing > 0) return existing;
    const trimmed = cityName?.trim();
    if (!trimmed || countryId == null || countryId < 1) return undefined;
    try {
      const list = await firstValueFrom(this.employeeService.searchCities(trimmed));
      const norm = (s: string) => s.toLowerCase().trim();
      const n = norm(trimmed);
      const inCountry = list.filter(c => c.countryId === countryId);
      const pool = inCountry.length > 0 ? inCountry : list;
      const exact = pool.find(c => norm(c.label) === n);
      if (exact) return exact.id;
      const loose = pool.find(c => norm(c.label).includes(n) || n.includes(norm(c.label)));
      if (loose) return loose.id;

      // Ville absente du référentiel: créer automatiquement à la sauvegarde.
      const created = await firstValueFrom(this.employeeService.createCity(trimmed, countryId));

      this.formData.update(f => ({
        ...f,
        cities: [...(f.cities || []).filter(c => c.id !== created.id), created]
      }));

      return created.id;
    } catch {
      return undefined;
    }
  }

  /** Map libellés UI (poste, service, type de contrat) vers les IDs attendus par l’API PATCH employé. */
  private augmentProfilePatchForApi(patch: Record<string, unknown>): Record<string, unknown> {
    const out: Record<string, unknown> = { ...patch };
    const fd = this.formData();
    const norm = (v: unknown) =>
      v === undefined || v === null ? '' : String(v).toLowerCase().replace(/[^a-z0-9]+/g, '');

    const resolveJobPositionId = (label: string): number | undefined => {
      const n = label.trim().toLowerCase();
      if (!n) return undefined;
      for (const pool of [fd.jobPositions || [], this.filteredJobPositions()]) {
        const jp = pool.find(j => String(j.label).trim().toLowerCase() === n);
        if (jp?.id != null && Number(jp.id) > 0) return Number(jp.id);
      }
      return undefined;
    };

    const resolveDepartementId = (name: string): number | undefined => {
      const n = name.trim().toLowerCase();
      if (!n) return undefined;
      for (const pool of [fd.departments || [], this.filteredDepartments()]) {
        const d = pool.find(x => String(x.label).trim().toLowerCase() === n);
        if (d?.id != null && Number(d.id) > 0) return Number(d.id);
      }
      return undefined;
    };

    if ('jobPositionId' in out) {
      const id = Number(out['jobPositionId']);
      if (id > 0) {
        delete out['position'];
      } else {
        delete out['jobPositionId'];
      }
    }
    if ('position' in out && !('jobPositionId' in out)) {
      const label = String(out['position'] ?? '').trim();
      const resolved = label ? resolveJobPositionId(label) : undefined;
      if (resolved != null) out['jobPositionId'] = resolved;
      delete out['position'];
    }

    if ('departementId' in out) {
      const id = Number(out['departementId']);
      if (id > 0) {
        delete out['department'];
      } else {
        delete out['departementId'];
      }
    }
    if ('department' in out && !('departementId' in out)) {
      const name = String(out['department'] ?? '').trim();
      const resolved = name ? resolveDepartementId(name) : undefined;
      if (resolved != null) out['departementId'] = resolved;
      delete out['department'];
    }

    if ('contractTypeId' in out) {
      const id = Number(out['contractTypeId']);
      if (id > 0) {
        delete out['contractType'];
      } else {
        delete out['contractTypeId'];
      }
    }
    if ('contractType' in out && !('contractTypeId' in out)) {
      const code = norm(out['contractType']);
      let ctId: number | undefined;
      const pool = [...(fd.contractTypes || []), ...this.contractTypeOptions];
      for (const o of pool) {
        const anyO = o as { id?: number; value?: unknown; label?: string };
        const val = norm(anyO.value ?? anyO.id ?? '');
        const lbl = norm(o.label ?? '');
        if (code && (val === code || lbl === code || (code.length > 0 && (val.includes(code) || lbl.includes(code))))) {
          ctId = typeof (o as { id: number }).id === 'number' ? (o as { id: number }).id : anyO.id;
          break;
        }
      }
      if (ctId != null && ctId > 0) out['contractTypeId'] = ctId;
      delete out['contractType'];
    }

    if ('phone' in out) {
      const localPhone = String(out['phone'] ?? '').replace(/\D/g, '');
      if (localPhone.length === 9) {
        out['phone'] = localPhone;
        let countryCode = String(out['countryPhoneCode'] ?? '').trim();
        if (!countryCode) {
          countryCode = String(this.employee().countryPhoneCode ?? '').trim();
        }
        if (!countryCode) {
          const countryId = Number(out['countryId'] ?? this.employee().countryId ?? 0);
          const country = (fd.countries || []).find(c => Number(c.id) === countryId);
          countryCode = country?.phoneCode ?? '';
        }
        if (countryCode) {
          out['countryPhoneCode'] = countryCode;
        }
      }
    }

    // Handle maritalStatus: convert string code to maritalStatusId (integer)
    if ('maritalStatus' in out) {
      const msCode = String(out['maritalStatus'] ?? '').toLowerCase().trim();
      if (msCode) {
        let msId: number | undefined;
        const maritalStatusOptions = this.maritalStatusOptions;
        for (const option of maritalStatusOptions) {
          if (String(option.value ?? '').toLowerCase().trim() === msCode) {
            msId = Number(option.id);
            break;
          }
        }
        if (msId != null && msId > 0) {
          out['maritalStatusId'] = msId;
        }
      }
      delete out['maritalStatus'];
    }

    return out;
  }

  private async performSave(): Promise<void> {
    if (!this.originalEmployee || !this.employeeId()) {
      return;
    }

    this.isSaving.set(true);
    this.saveError.set(null);

    try {
      let currentCountryId = this.employee().countryId;
      const resolvedCountryId = await this.resolveCountryIdFromName(this.employee().countryName, currentCountryId);
      if (resolvedCountryId != null) {
        currentCountryId = resolvedCountryId;
        this.updateField('countryId', resolvedCountryId);
      }

      let currentCityId = this.employee().cityId;
      const resolvedCityId = await this.resolveCityIdFromName(this.employee().city, currentCountryId, currentCityId);
      if (resolvedCityId != null) {
        currentCityId = resolvedCityId;
        this.updateField('cityId', resolvedCityId);
      }

      // Handle new Department creation
      const newDeptName = this.pendingNewDepartment();
      if (newDeptName) {
        try {
          const companyIdRaw = this.employee().companyId ?? this.contextService.companyId();
          const companyId = companyIdRaw ? Number(companyIdRaw) : 0;
          if (companyId) {
            const dep = await firstValueFrom(this.employeeService.createDepartment(newDeptName, companyId));
            this.formData.update(f => ({
              ...f,
              departments: [...(f.departments || []).filter(d => d.id !== dep.id), dep]
            }));
            this.employee.update(emp => ({ ...emp, departementId: dep.id }));
          }
          this.pendingNewDepartment.set(null);
        } catch (e: any) {
          // If already exists (409), ignore; otherwise log
          if (e?.status !== 409) alert('Failed to create department');
          this.pendingNewDepartment.set(null);
        }
      }

      // Handle new Job Position creation
      const newPositionName = this.pendingNewJobPosition();
      if (newPositionName) {
        try {
          const companyIdRaw = this.employee().companyId ?? this.contextService.companyId();
          const companyId = companyIdRaw ? Number(companyIdRaw) : 0;
          if (companyId) {
            const jp = await firstValueFrom(this.employeeService.createJobPosition(newPositionName, companyId));
            this.formData.update(f => ({
              ...f,
              jobPositions: [...(f.jobPositions || []).filter(j => j.id !== jp.id), jp]
            }));
            this.employee.update(emp => ({ ...emp, jobPositionId: jp.id }));
          }
          this.pendingNewJobPosition.set(null);
        } catch (e: any) {
          if (e?.status !== 409) alert('Failed to create job position');
          this.pendingNewJobPosition.set(null);
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

        const apiPatch = this.augmentProfilePatchForApi(patch as Record<string, unknown>) as Partial<EmployeeProfileModel>;

        await firstValueFrom(
          this.employeeService.patchEmployeeProfile(this.employeeId()!, apiPatch)
        );
      }

      // Handle Salary Components
      const salaryDetails = await firstValueFrom(this.employeeService.getEmployeeSalaryDetails(this.employeeId()!));
      const newActiveSalaryId = salaryDetails.id;
      const oldActiveSalaryId = this.employee().activeSalaryId;

      // Sans ligne salaire active, id vaut 0 — `if (newActiveSalaryId)` serait faux (0 est falsy en JS).
      if (newActiveSalaryId > 0) {
        const currentComponents = this.employee().salaryComponents || [];
        const componentPromises = [];

        if (newActiveSalaryId !== oldActiveSalaryId) {
          // New salary created. Add all components to it.
          for (const c of currentComponents) {
            componentPromises.push(firstValueFrom(this.employeeService.addSalaryComponent({
              employeeSalaryId: newActiveSalaryId,
              componentType: c.type,
              amount: c.amount,
              isTaxable: c.isTaxable ?? true,
              effectiveDate: new Date().toISOString(),
            })));
          }
        } else {
          // Same salary. Diff changes.
          const originalComponents = this.originalEmployee?.salaryComponents || [];

          const newComponents = currentComponents.filter(c => !c.id);

          const modifiedComponents = currentComponents.filter(c => {
            if (!c.id) return false;
            const original = originalComponents.find(o => o.id === c.id);
            return original && (original.type !== c.type || original.amount !== c.amount || (original.isTaxable ?? true) !== (c.isTaxable ?? true));
          });

          const deletedComponents = originalComponents.filter(o =>
            o.id && !currentComponents.find(c => c.id === o.id)
          );

          for (const c of newComponents) {
            componentPromises.push(firstValueFrom(this.employeeService.addSalaryComponent({
              employeeSalaryId: newActiveSalaryId,
              componentType: c.type,
              amount: c.amount,
              isTaxable: c.isTaxable ?? true,
              effectiveDate: new Date().toISOString()
            })));
          }

          for (const c of modifiedComponents) {
            componentPromises.push(firstValueFrom(this.employeeService.updateSalaryComponent(c.id!, {
              id: c.id,
              employeeSalaryId: newActiveSalaryId,
              componentType: c.type,
              amount: c.amount,
              isTaxable: c.isTaxable ?? true,
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

      // Id : le backend peut renvoyer PascalCase (Id) et GET conjoint un tableau — utiliser relationId().
      const newSpouses = currentSpouses.filter(s => {
        const rid = this.relationId(s);
        return !rid || rid > 1000000000000;
      });

      const updatedSpouses = currentSpouses.filter(s => {
        const rid = this.relationId(s);
        if (!rid || rid > 1000000000000) return false;
        const original = originalSpouses.find(o => this.relationId(o) === rid);
        return !!original && JSON.stringify(s) !== JSON.stringify(original);
      });

      const deletedSpouses = originalSpouses.filter(o => {
        const oid = this.relationId(o);
        return oid != null && !currentSpouses.find(s => this.relationId(s) === oid);
      });

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

      const newChildren = currentChildren.filter(c => {
        const rid = this.relationId(c);
        return !rid || rid > 1000000000000;
      });

      const updatedChildren = currentChildren.filter(c => {
        const rid = this.relationId(c);
        if (!rid || rid > 1000000000000) return false;
        const original = originalChildren.find(o => this.relationId(o) === rid);
        return !!original && JSON.stringify(c) !== JSON.stringify(original);
      });

      const deletedChildren = originalChildren.filter(o => {
        const oid = this.relationId(o);
        return oid != null && !currentChildren.find(c => this.relationId(c) === oid);
      });

      const childPromises = [];

      for (const child of newChildren) {
        childPromises.push(firstValueFrom(this.employeeService.createChild(this.employeeId()!, child)));
      }

      for (const child of updatedChildren) {
        childPromises.push(firstValueFrom(this.employeeService.updateChild(this.employeeId()!, this.relationId(child)!, child)));
      }

      for (const child of deletedChildren) {
        childPromises.push(firstValueFrom(this.employeeService.deleteChild(this.employeeId()!, this.relationId(child)!)));
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
      const errorMessage = this.extractErrorMessage(err, this.translate.instant('employees.profile.saveError'));
      this.saveError.set(errorMessage);
      this.toastService.error(errorMessage);
      this.resolveAfterSave(false);
    } finally {
      this.isSaving.set(false);
    }
  }

  /**
   * Extract a human-readable error message from various API error shapes
   */
  private extractErrorMessage(error: any, defaultMessage?: string): string {
    try {
      const candidates = [
        error?.error?.Message,
        error?.error?.message,
        error?.error?.Error,
        error?.error?.error,
        error?.error?.details,
        error?.error?.Details,
        error?.Message,
        error?.message,
        typeof error === 'string' ? error : null,
        error?.statusText
      ];

      for (const candidate of candidates) {
        if (candidate !== null && candidate !== undefined && String(candidate).trim() !== '') {
          return String(candidate).trim();
        }
      }

      // Fallback based on HTTP status
      if (error?.status) {
        const status = error.status;
        if (status === 400) return 'Requête invalide. Vérifiez vos données.';
        if (status === 401) return 'Authentification requise.';
        if (status === 403) return 'Accès refusé. Vérifiez vos permissions.';
        if (status === 404) return 'Ressource non trouvée.';
        if (status === 409) return 'Conflit. Veuillez réessayer.';
        if (status === 422) return 'Données invalides. Vérifiez votre saisie.';
        if (status === 429) return 'Trop de tentatives. Veuillez réessayer plus tard.';
        if (status === 500) return 'Erreur serveur. Veuillez réessayer.';
        if (status === 503) return 'Service temporairement indisponible.';
      }

      return defaultMessage || 'Une erreur est survenue. Veuillez réessayer.';
    } catch {
      return defaultMessage || 'Une erreur est survenue. Veuillez réessayer.';
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

  private buildDocumentCards(items: any[]): Document[] {
    const uploadedByType = new Map<string, any>();
    for (const it of items || []) {
      const type = String(it?.documentType ?? it?.DocumentType ?? '').trim().toLowerCase();
      if (!type) continue;
      const current = uploadedByType.get(type);
      const currentDate = current ? new Date(current.createdAt ?? current.CreatedAt ?? 0).getTime() : 0;
      const nextDate = new Date(it?.createdAt ?? it?.CreatedAt ?? 0).getTime();
      if (!current || nextDate >= currentDate) {
        uploadedByType.set(type, it);
      }
    }

    const cards: Document[] = this.documentTemplates.map(t => {
      const u = uploadedByType.get(t.type);
      if (!u) {
        return { type: t.type, name: t.name, uploadDate: '', status: 'missing' };
      }
      return {
        id: Number(u.id ?? u.Id),
        type: t.type,
        name: String(u.name ?? u.Name ?? t.name),
        uploadDate: String(u.createdAt ?? u.CreatedAt ?? ''),
        status: 'uploaded',
        filePath: String(u.filePath ?? u.FilePath ?? '')
      };
    });

    // Keep additional uploaded document types not in default templates visible in UI.
    for (const [type, u] of uploadedByType.entries()) {
      if (cards.some(c => c.type === type)) continue;
      cards.push({
        id: Number(u.id ?? u.Id),
        type,
        name: String(u.name ?? u.Name ?? type),
        uploadDate: String(u.createdAt ?? u.CreatedAt ?? ''),
        status: 'uploaded',
        filePath: String(u.filePath ?? u.FilePath ?? '')
      });
    }

    return cards;
  }

  private refreshDocuments(): void {
    const id = this.employeeId();
    if (!id) return;
    this.employeeService.getDocuments(id).subscribe({
      next: docs => this.documents.set(this.buildDocumentCards(docs)),
      error: () => { }
    });
  }

  uploadDocument(event: any, documentType: string): void {
    const id = this.employeeId();
    const file: File | undefined = event?.files?.[0];
    if (!id || !file) return;

    this.employeeService.uploadDocument(id, documentType, file).subscribe({
      next: () => {
        this.saveSuccess.set(this.translate.instant('employees.profile.saveSuccess'));
        setTimeout(() => this.saveSuccess.set(null), 1500);
        this.refreshDocuments();
      },
      error: (err) => {
        const errorMessage = this.extractErrorMessage(err, this.translate.instant('employees.profile.saveError'));
        this.saveError.set(errorMessage);
        this.toastService.error(errorMessage);
        setTimeout(() => this.saveError.set(null), 2500);
      }
    });
  }

  downloadDocument(doc: Document) {
    const id = this.employeeId();
    if (!id || !doc.id) return;

    this.employeeService.downloadDocument(id, doc.id).subscribe({
      next: blob => {
        const url = URL.createObjectURL(blob);
        const a = window.document.createElement('a');
        a.href = url;
        a.download = doc.name || `document-${doc.id}`;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: (err) => {
        const errorMessage = this.extractErrorMessage(err, this.translate.instant('employees.profile.saveError'));
        this.saveError.set(errorMessage);
        this.toastService.error(errorMessage);
        setTimeout(() => this.saveError.set(null), 2500);
      }
    });
  }

  deleteDocument(doc: Document) {
    const id = this.employeeId();
    if (!id || !doc.id) return;
    this.employeeService.deleteDocument(id, doc.id).subscribe({
      next: () => this.refreshDocuments(),
      error: (err) => {
        const errorMessage = this.extractErrorMessage(err, this.translate.instant('employees.profile.saveError'));
        this.saveError.set(errorMessage);
        this.toastService.error(errorMessage);
        setTimeout(() => this.saveError.set(null), 2500);
      }
    });
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
    const date = this.parseHistoryDate(dateStr);
    if (!date) return '-';
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
    const date = this.parseHistoryDate(dateStr);
    if (!date) return '-';
    return new Intl.DateTimeFormat('fr-FR', {
      dateStyle: 'medium',
      timeStyle: 'short'
    }).format(date);
  }

  private parseHistoryDate(dateInput: unknown): Date | null {
    if (dateInput === null || dateInput === undefined) return null;
    const raw = String(dateInput).trim();
    if (!raw) return null;
    const date = new Date(raw);
    return Number.isNaN(date.getTime()) ? null : date;
  }

  // Return a raw backend title when it's meaningful and not generic.
  getHistoryRawTitle(event: any): string | null {
    if (!event) return null;
    const rawTitle = (event.title ?? event.EventTitle ?? event.Name ?? '').toString().trim();
    if (!rawTitle) return null;

    // Don't use as raw title a value that looks like an i18n key (e.g. employees.history.titles.child_added).
    if (/^[a-z0-9_.]+\.[a-z0-9_.]+$/i.test(rawTitle) || rawTitle.startsWith('employees.history.')) {
      return null;
    }

    const specificTitles = [
      /modification\s+de\s+l'email/i,
      /modification\s+du\s+numéro\s+cnss/i,
      /modification\s+du\s+numéro\s+cimr/i,
      /ajout\s+d'adresse/i,
      /nouveau\s+contrat/i,
      /enfant\s+ajouté/i,
      /email\s+modifié/i,
      /prénom\s+modifié/i,
      /statut\s+modifié/i
    ];

    if (specificTitles.some(p => p.test(rawTitle))) return rawTitle;

    const genericPatterns: RegExp[] = [
      /^modification$/i,
      /^general(?:\s|_)?update$/i,
      /^update$/i,
      /^general_update$/i,
      /^metadata\s*update$/i
    ];

    const isGeneric = genericPatterns.some(p => p.test(rawTitle));
    if (!isGeneric) return rawTitle;

    return null;
  }

  // Return a translation key for the event; template applies the translate pipe so it updates when language files load.
  getHistoryTitleKey(event: any): string {
    const type = event.type ?? event.eventName ?? event.EventName ?? event.Event ?? event.EventType;
    if (!type) return 'audit.genericModification';

    const normalized = String(type)
      .replace(/([a-z0-9])([A-Z])/g, '$1_$2')
      .replace(/\s+/g, '_')
      .toLowerCase();

    return `employees.history.titles.${normalized}`;
  }

  /** Returns the translated history event title, with fallbacks so the raw i18n key is never shown. */
  getHistoryTitle(event: any): string {
    const key = this.getHistoryTitleKey(event);
    const normalized = key.replace(/^.*\.([^.]+)$/, '$1');
    let translated = this.translate.instant(key);
    if (translated === key) {
      translated = this.translate.instant(`employees.profile.history.titles.${normalized}`);
    }
    if (translated === key || translated === `employees.profile.history.titles.${normalized}`) {
      return this.getHistoryTitleFallback(normalized);
    }
    return translated;
  }

  /** Humanized fallback by current language when translation key is missing. */
  private getHistoryTitleFallback(normalizedKey: string): string {
    const isFr = (this.translate.currentLang || this.translate.defaultLang || '').startsWith('fr');
    const fallbacksFr: Record<string, string> = {
      child_added: 'Enfant ajouté',
      email_changed: 'Email modifié',
      cnss_changed: 'CNSS mis à jour',
      cimr_changed: 'CIMR mis à jour',
      user_account_created: 'Compte utilisateur créé',
      salary_created: 'Salaire créé',
      employee_created: 'Employé créé',
      address_created: 'Adresse ajoutée',
      contract_created: 'Contrat créé',
      general_update: 'Modification',
      firstname_changed: 'Prénom modifié',
      status_changed: 'Statut modifié',
      role_assigned: 'Rôle attribué',
      role_revoked: 'Rôle révoqué',
      role_removed: 'Rôle retiré'
    };
    if (isFr && fallbacksFr[normalizedKey]) return fallbacksFr[normalizedKey];
    return normalizedKey.replace(/_/g, ' ').replace(/\b\w/g, c => c.toUpperCase());
  }

  getHistoryTooltipText(dateStr: string | undefined | null): string {
    return this.formatHistoryDate(dateStr ?? '');
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
    this.activeTab.set(targetTab);
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
    if (this.pendingNavigationResolver) {
      this.pendingNavigationResolver(success);
    }

    this.pendingNavigationResolver = null;
    this.pendingCancel = false;
  }

  private revertToOriginal(): void {
    if (!this.originalEmployee) {
      return;
    }

    this.pendingNewDepartment.set(null);
    this.pendingNewJobPosition.set(null);
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
    const orig = this.originalEmployee;
    const editMode = this.isEditMode?.();
    if (!orig || !editMode) {
      return;
    }

    const changes = ChangeTracker.trackChanges(
      orig,
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

  /** Identifiant stable pour conjoint/enfant (camelCase ou PascalCase API .NET). */
  private relationId(x: any): number | undefined {
    if (!x || typeof x !== 'object') return undefined;
    const v = x.id ?? x.Id;
    if (v == null || v === '') return undefined;
    const n = Number(v);
    return Number.isFinite(n) ? n : undefined;
  }

  /**
   * GET .../spouse renvoie souvent un tableau ; éviter [[{...}]].
   * Normaliser les champs pour le reste du profil (camelCase).
   */
  private normalizeSpousesFromApi(spouse: any): Spouse[] {
    if (spouse == null) return [];
    const rawList = Array.isArray(spouse) ? spouse : [spouse];
    return rawList.filter(Boolean).map(s => this.normalizeOneSpouse(s));
  }

  private normalizeOneSpouse(s: any): Spouse {
    return {
      ...s,
      id: s.id ?? s.Id,
      employeeId: s.employeeId ?? s.EmployeeId,
      firstName: s.firstName ?? s.FirstName,
      lastName: s.lastName ?? s.LastName,
      dateOfBirth: s.dateOfBirth ?? s.DateOfBirth,
      genderId: s.genderId ?? s.GenderId ?? null,
      genderName: s.genderName ?? s.GenderName ?? null,
      cinNumber: s.cinNumber ?? s.CinNumber ?? null,
      marriageDate: s.marriageDate ?? s.MarriageDate ?? null,
      isDependent: s.isDependent ?? s.IsDependent ?? false
    };
  }

  private normalizeChildrenFromApi(children: any): Child[] {
    if (!children || !Array.isArray(children)) return [];
    return children.filter(Boolean).map(c => ({
      ...c,
      id: c.id ?? c.Id,
      employeeId: c.employeeId ?? c.EmployeeId,
      firstName: c.firstName ?? c.FirstName,
      lastName: c.lastName ?? c.LastName,
      dateOfBirth: c.dateOfBirth ?? c.DateOfBirth,
      genderId: c.genderId ?? c.GenderId ?? null,
      genderName: c.genderName ?? c.GenderName ?? null,
      isDependent: c.isDependent ?? c.IsDependent ?? true,
      isStudent: c.isStudent ?? c.IsStudent ?? false
    }));
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
      //birthPlace: '',
      professionalEmail: '',
      personalEmail: '',
      phone: '',
      countryPhoneCode: '',
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
