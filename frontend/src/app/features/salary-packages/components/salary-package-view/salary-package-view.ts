import { Component, signal, computed, OnInit, OnDestroy, inject, effect, ViewChild } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { Subscription } from 'rxjs';

import { SalaryPackageService } from '@app/core/services/salary-package.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { PayrollReferentielService } from '@app/core/services/payroll-referentiel.service';
import {
  SalaryPackage,
  SalaryPackageItem,
  SalaryPackageWriteRequest,
  SalaryPackageItemWriteRequest,
  SalaryPackageStatus,
  SalaryComponentType,
  CimrConfig,
  CimrRegime,
  SalaryPackageAssignment
} from '@app/core/models/salary-package.model';
import { ElementRuleDto, ExemptionType } from '@app/core/models/payroll-referentiel.model';
import { CimrConfigCardComponent } from '../cimr-config-card/cimr-config-card.component';
import { AssignModalComponent } from '../assign-modal/assign-modal.component';
import { EndAssignmentModalComponent } from '../end-assignment-modal/end-assignment-modal.component';

@Component({
  selector: 'app-salary-package-view',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    TranslateModule,
    CimrConfigCardComponent,
    AssignModalComponent,
    EndAssignmentModalComponent
  ],
  templateUrl: './salary-package-view.html',
  styleUrl: './salary-package-view.css'
})
export class SalaryPackageViewComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly salaryPackageService = inject(SalaryPackageService);
  private readonly contextService = inject(CompanyContextService);
  private readonly payrollReferentielService = inject(PayrollReferentielService);
  private readonly translate = inject(TranslateService);

  readonly package = signal<SalaryPackage | null>(null);
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly error = signal<string | null>(null);

  // Edit mode state
  readonly viewMode = signal<'view' | 'edit'>('view');
  readonly hasUnsavedChanges = signal(false);

  // Edit form state
  readonly editName = signal('');
  readonly editBaseSalary = signal(0);
  readonly editCategory = signal('');
  readonly editDescription = signal('');
  readonly editComponents = signal<SalaryPackageItem[]>([]);
  readonly editCimrRegime = signal<CimrRegime>('NONE');
  readonly editCimrEmployeeRate = signal(0);
  readonly editCimrEmployerRate = signal(0);
  readonly editSeniorityBonusEnabled = signal(false);

  // Validation errors
  readonly validationErrors = signal<Record<string, string>>({});

  // Category options
  categoryOptions: Array<{ label: string; value: string }> = [];

  // CIMR regime options
  cimrRegimeOptions: Array<{ label: string; value: CimrRegime }> = [];
  private langChangeSub?: Subscription;

  // CIMR rate options based on regime
  readonly cimrRateOptions = computed(() => {
    const regime = this.editCimrRegime();

    if (regime === 'AL_KAMIL') {
      return [
        { label: '3,00%', value: 0.03 },
        { label: '3,75%', value: 0.0375 },
        { label: '4,50%', value: 0.045 },
        { label: '5,25%', value: 0.0525 },
        { label: '6,00%', value: 0.06 },
        { label: '7,00%', value: 0.07 },
        { label: '7,50%', value: 0.075 },
        { label: '8,00%', value: 0.08 },
        { label: '8,50%', value: 0.085 },
        { label: '9,00%', value: 0.09 },
        { label: '9,50%', value: 0.095 },
        { label: '10,00%', value: 0.10 }
      ];
    } else if (regime === 'AL_MOUNASSIB') {
      return [
        { label: '6%', value: 0.06 },
        { label: '7%', value: 0.07 },
        { label: '8%', value: 0.08 },
        { label: '9%', value: 0.09 },
        { label: '10%', value: 0.10 },
        { label: '11%', value: 0.11 },
        { label: '12%', value: 0.12 }
      ];
    }

    return [];
  });

  readonly routePrefix = computed(() => this.contextService.isExpertMode() ? '/expert' : '/app');

  constructor() {
    // Track changes to form fields
    effect(() => {
      if (this.viewMode() === 'edit') {
        this.editName();
        this.editBaseSalary();
        this.editCategory();
        this.editDescription();
        this.editComponents();
        this.editCimrRegime();
        this.editCimrEmployeeRate();
        this.editSeniorityBonusEnabled();
        this.hasUnsavedChanges.set(true);
      }
    }, { allowSignalWrites: true });
  }

  // Confirm dialog state
  readonly showConfirmDialog = signal(false);
  readonly confirmDialogTitle = signal('');
  readonly confirmDialogMessage = signal('');
  readonly confirmDialogAction = signal('');
  readonly confirmDialogType = signal<'danger' | 'info'>('info');
  private pendingAction: (() => void) | null = null;

  // Computed values
  readonly totalGrossSalary = computed(() => {
    const pkg = this.package();
    if (!pkg) return 0;

    const allowances = pkg.items
      .filter(i => i.type === 'allowance' || i.type === 'bonus' || i.type === 'benefit_in_kind')
      .reduce((sum, i) => sum + i.defaultValue, 0);

    return pkg.baseSalary + allowances;
  });

  readonly taxableAmount = computed(() => {
    const pkg = this.package();
    if (!pkg) return 0;

    return pkg.baseSalary + pkg.items
      .filter(i => i.isTaxable && (i.type === 'allowance' || i.type === 'bonus'))
      .reduce((sum, i) => sum + i.defaultValue, 0);
  });

  readonly socialAmount = computed(() => {
    const pkg = this.package();
    if (!pkg) return 0;

    return pkg.baseSalary + pkg.items
      .filter(i => i.isSocial && (i.type === 'allowance' || i.type === 'bonus'))
      .reduce((sum, i) => sum + i.defaultValue, 0);
  });

  readonly cimrConfig = computed((): CimrConfig => {
    const pkg = this.package();
    return pkg?.cimrConfig ?? { regime: 'NONE', employeeRate: 0, employerRate: 0 };
  });

  readonly canDirectEdit = computed(() => {
    const pkg = this.package();
    return pkg && pkg.status === 'draft' && pkg.templateType === 'COMPANY';
  });

  readonly canCreateVersion = computed(() => {
    const pkg = this.package();
    return pkg && pkg.status === 'published' && pkg.templateType === 'COMPANY';
  });

  readonly canPublish = computed(() => {
    const pkg = this.package();
    return pkg && pkg.status === 'draft' && pkg.templateType === 'COMPANY';
  });

  readonly canDelete = computed(() => {
    const pkg = this.package();
    return pkg && pkg.status === 'draft' && pkg.templateType === 'COMPANY';
  });

  readonly canDeprecate = computed(() => {
    const pkg = this.package();
    return pkg && pkg.status === 'published' && pkg.templateType === 'COMPANY';
  });

  readonly isReadOnly = computed(() => {
    return this.viewMode() === 'view' || !this.canDirectEdit();
  });

  // ============ Assignment State ============
  readonly assignments = signal<SalaryPackageAssignment[]>([]);
  readonly isLoadingAssignments = signal(false);
  readonly assignmentsError = signal<string | null>(null);
  readonly isAssignModalOpen = signal(false);
  readonly isEndAssignmentModalOpen = signal(false);
  readonly selectedAssignment = signal<SalaryPackageAssignment | null>(null);
  @ViewChild(AssignModalComponent) private assignModal?: AssignModalComponent;

  readonly activeAssignmentsCount = computed(() =>
    this.assignments().filter(a => !a.endDate).length
  );

  readonly showAssignmentsSection = computed(() =>
    this.package()?.status === 'published' &&
    this.package()?.templateType === 'COMPANY'
  );

  ngOnInit(): void {
    this.localizeStaticOptions();
    this.langChangeSub = this.translate.onLangChange.subscribe(() => this.localizeStaticOptions());

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadPackage(Number(id));
    } else {
      this.error.set(this.t('salaryPackages.errors.packageIdMissing'));
      this.isLoading.set(false);
    }
  }

  loadPackage(id: number): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.salaryPackageService.getById(id).subscribe({
      next: (pkg) => {
        this.package.set(pkg);
        this.applyReferentielRulesToPackageItems(pkg);
        this.isLoading.set(false);
        if (pkg.status === 'published' && pkg.templateType === 'COMPANY') {
          this.loadAssignments();
        }
      },
      error: (err) => {
        console.error('Error loading package:', err);
        this.error.set(this.t('salaryPackages.errors.loadPackage'));
        this.isLoading.set(false);
      }
    });
  }

  ngOnDestroy(): void {
    this.langChangeSub?.unsubscribe();
  }

  private applyReferentielRulesToPackageItems(pkg: SalaryPackage): void {
    const elementIds = Array.from(
      new Set(
        pkg.items
          .map(item => item.referentielElementId)
          .filter((id): id is number => typeof id === 'number')
      )
    );

    if (elementIds.length === 0) {
      return;
    }

    forkJoin(
      elementIds.map(id =>
        this.payrollReferentielService.getReferentielElementById(id).pipe(
          map(element => ({ id, rules: element.rules || [] })),
          catchError(() => of({ id, rules: [] as ElementRuleDto[] }))
        )
      )
    ).subscribe({
      next: (elementsWithRules) => {
        const rulesByElementId = new Map<number, ElementRuleDto[]>(
          elementsWithRules.map(entry => [entry.id, entry.rules])
        );

        const normalizedItems = pkg.items.map(item =>
          this.withRuleBasedFlags(item, rulesByElementId.get(item.referentielElementId ?? -1))
        );

        this.package.set({ ...pkg, items: normalizedItems });
      }
    });
  }

  private withRuleBasedFlags(
    item: SalaryPackageItem,
    rules: ElementRuleDto[] | undefined
  ): SalaryPackageItem {
    if (!item.referentielElementId) {
      return item;
    }

    const cnssRule = this.getCurrentRule(rules, (authorityName) => this.isCnssAuthority(authorityName));
    const irRule = this.getCurrentRule(rules, (authorityName) => this.isIrAuthority(authorityName));

    const isSocial = cnssRule ? this.isSubjectToContribution(cnssRule.exemptionType) : true;
    const isTaxable = irRule ? this.isSubjectToContribution(irRule.exemptionType) : true;

    return {
      ...item,
      isSocial,
      isTaxable
    };
  }

  private getCurrentRule(
    rules: ElementRuleDto[] | undefined,
    authorityMatcher: (authorityName: string | undefined) => boolean
  ): ElementRuleDto | undefined {
    if (!rules || rules.length === 0) {
      return undefined;
    }

    const matchingRules = rules.filter(rule => authorityMatcher(rule.authorityName));
    if (matchingRules.length === 0) {
      return undefined;
    }

    const now = new Date();
    const effectiveNow = matchingRules.filter((rule) => this.isRuleEffectiveNow(rule, now));

    const source = effectiveNow.length > 0 ? effectiveNow : matchingRules;
    return source
      .slice()
      .sort((a, b) => new Date(b.effectiveFrom).getTime() - new Date(a.effectiveFrom).getTime())[0];
  }

  private isRuleEffectiveNow(rule: ElementRuleDto, now: Date): boolean {
    const from = new Date(rule.effectiveFrom);
    const to = rule.effectiveTo ? new Date(rule.effectiveTo) : null;
    return from <= now && (!to || to >= now);
  }

  private isCnssAuthority(name: string | undefined): boolean {
    if (!name) return false;
    const authority = name.toLowerCase().trim();
    return authority === 'cnss' || authority.includes('sécurité sociale') || authority.includes('securite sociale');
  }

  private isIrAuthority(name: string | undefined): boolean {
    if (!name) return false;
    const authority = name.toLowerCase().trim();
    return authority === 'ir' || authority === 'dgi' || authority.includes('impot') || authority.includes('impôt') || authority.includes('revenu');
  }

  private isSubjectToContribution(exemptionType: ExemptionType): boolean {
    return exemptionType !== 'FULLY_EXEMPT';
  }

  goBack(): void {
    this.router.navigate([`${this.routePrefix()}/salary-packages`]);
  }

  editPackage(): void {
    const pkg = this.package();
    if (!pkg) return;

    // For draft packages, toggle edit mode
    if (pkg.status === 'draft' && pkg.templateType === 'COMPANY') {
      // Populate form with current values
      this.editName.set(pkg.name);
      this.editBaseSalary.set(pkg.baseSalary);
      this.editCategory.set(pkg.category || '');
      this.editDescription.set(pkg.description || '');
      this.editComponents.set([...pkg.items]);
      this.editCimrRegime.set(pkg.cimrConfig?.regime || 'NONE');
      this.editCimrEmployeeRate.set(pkg.cimrConfig?.employeeRate || 0);
      this.editCimrEmployerRate.set(pkg.cimrConfig?.employerRate || 0);
      this.editSeniorityBonusEnabled.set(pkg.autoRules?.seniorityBonusEnabled || false);

      this.hasUnsavedChanges.set(false);
      this.viewMode.set('edit');
    }
    // For published packages, create new version
    else if (pkg.status === 'published' && pkg.templateType === 'COMPANY') {
      this.createNewVersion();
    }
  }

  createNewVersion(): void {
    const pkg = this.package();
    if (!pkg || pkg.status !== 'published') return;

    const nextVersion = (pkg.version || 1) + 1;
    this.showConfirm(
      this.t('salaryPackages.dialogs.confirm.newVersionTitle'),
      this.t('salaryPackages.dialogs.confirm.newVersionMessage', { nextVersion, currentVersion: pkg.version || 1 }),
      this.t('salaryPackages.actions.createVersion'),
      'info',
      () => {
        this.salaryPackageService.createNewVersion(pkg.id).subscribe({
          next: (newVersion) => {
            // Navigate to the new version
            this.router.navigate([`${this.routePrefix()}/salary-packages`, newVersion.id]);
          },
          error: (err) => {
            console.error('Error creating version:', err);
            this.error.set(this.t('salaryPackages.errors.newVersionFailed'));
          }
        });
      }
    );
  }

  cancelEdit(): void {
    if (this.hasUnsavedChanges()) {
      this.showConfirm(
        this.t('salaryPackages.dialogs.confirm.cancelEditTitle'),
        this.t('salaryPackages.dialogs.confirm.cancelEditMessage'),
        this.t('common.cancel'),
        'danger',
        () => {
          this.viewMode.set('view');
          this.hasUnsavedChanges.set(false);
          // Reload package to discard changes
          const id = this.route.snapshot.paramMap.get('id');
          if (id) {
            this.loadPackage(Number(id));
          }
        }
      );
    } else {
      this.viewMode.set('view');
    }
  }

  saveChanges(): void {
    const pkg = this.package();
    if (!pkg) return;

    // Validate
    if (!this.validateForm()) {
      return;
    }

    this.isSaving.set(true);

    // Build update payload
    const request: SalaryPackageWriteRequest = {
      name: this.editName(),
      category: this.editCategory(),
      description: this.editDescription() || null,
      baseSalary: this.editBaseSalary(),
      status: pkg.status,
      companyId: pkg.companyId || null,
      templateType: pkg.templateType,
      regulationVersion: 'MA_2025',
      autoRules: {
        seniorityBonusEnabled: this.editSeniorityBonusEnabled(),
        ruleVersion: 'MA_2025'
      },
      cimrConfig: this.editCimrRegime() !== 'NONE' ? {
        regime: this.editCimrRegime(),
        employeeRate: this.editCimrEmployeeRate(),
        employerRate: this.editCimrEmployerRate(),
        customEmployerRate: null
      } : null,
      cimrRate: this.editCimrEmployeeRate(),
      hasPrivateInsurance: false,
      validFrom: pkg.validFrom,
      validTo: pkg.validTo,
      items: this.editComponents().map((item, index) => ({
        id: item.id,
        payComponentId: item.payComponentId,
        label: item.label,
        defaultValue: item.defaultValue,
        sortOrder: item.sortOrder ?? index + 1,
        type: item.type,
        isTaxable: item.isTaxable,
        isSocial: item.isSocial,
        isCIMR: item.isCIMR,
        isVariable: item.isVariable,
        exemptionLimit: item.exemptionLimit,
        referentielElementId: item.referentielElementId
      }))
    };

    this.salaryPackageService.update(pkg.id, request).subscribe({
      next: (updated) => {
        this.package.set(updated);
        this.isSaving.set(false);
        this.viewMode.set('view');
        this.hasUnsavedChanges.set(false);
        this.validationErrors.set({});
      },
      error: (err) => {
        console.error('Error saving package:', err);
        this.error.set(this.t('salaryPackages.errors.saveFailed'));
        this.isSaving.set(false);
      }
    });
  }

  validateForm(): boolean {
    const errors: Record<string, string> = {};

    if (!this.editName().trim()) {
      errors['name'] = this.t('salaryPackages.errors.nameRequired');
    }

    if (this.editBaseSalary() <= 0) {
      errors['baseSalary'] = this.t('salaryPackages.errors.baseSalaryPositive');
    }

    this.validationErrors.set(errors);
    return Object.keys(errors).length === 0;
  }

  // CIMR regime change handler
  onCimrRegimeChange(regime: CimrRegime): void {
    this.editCimrRegime.set(regime);
    if (regime === 'NONE') {
      this.editCimrEmployeeRate.set(0);
      this.editCimrEmployerRate.set(0);
    } else {
      // Set default rate based on regime
      const defaultRate = regime === 'AL_KAMIL' ? 0.06 : 0.08;
      this.editCimrEmployeeRate.set(defaultRate);
      this.editCimrEmployerRate.set(Math.round(defaultRate * 1.3 * 10000) / 10000);
    }
  }

  // CIMR employee rate change handler
  onCimrEmployeeRateChange(rate: number): void {
    this.editCimrEmployeeRate.set(rate);
    // Auto-calculate employer rate (coefficient 1.3)
    this.editCimrEmployerRate.set(Math.round(rate * 1.3 * 10000) / 10000);
  }

  // Component management
  removeComponent(index: number): void {
    const components = [...this.editComponents()];
    components.splice(index, 1);
    this.editComponents.set(components);
  }

  publishPackage(): void {
    const pkg = this.package();
    if (!pkg) return;

    this.showConfirm(
      this.t('salaryPackages.dialogs.confirm.publishTitle'),
      this.t('salaryPackages.dialogs.confirm.publishMessage', { name: pkg.name }),
      this.t('salaryPackages.actions.publish'),
      'info',
      () => {
        this.salaryPackageService.publish(pkg.id).subscribe({
          next: (updated) => {
            this.package.set(updated);
          },
          error: (err) => {
            console.error('Error publishing package:', err);
          }
        });
      }
    );
  }

  deletePackage(): void {
    const pkg = this.package();
    if (!pkg) return;

    this.showConfirm(
      this.t('salaryPackages.dialogs.confirm.deleteTitle'),
      this.t('salaryPackages.dialogs.confirm.deleteMessage', { name: pkg.name }),
      this.t('salaryPackages.actions.delete'),
      'danger',
      () => {
        this.salaryPackageService.delete(pkg.id).subscribe({
          next: () => {
            this.goBack();
          },
          error: (err) => {
            console.error('Error deleting package:', err);
          }
        });
      }
    );
  }

  duplicatePackage(): void {
    const pkg = this.package();
    if (!pkg) return;

    this.salaryPackageService.duplicate(pkg.id, `${pkg.name} - ${this.t('salaryPackages.common.copy')}`).subscribe({
      next: (duplicated) => {
        this.router.navigate([`${this.routePrefix()}/salary-packages`, duplicated.id]);
      },
      error: (err) => {
        console.error('Error duplicating package:', err);
        this.error.set(this.t('salaryPackages.errors.duplicateFailed'));
      }
    });
  }

  deprecatePackage(): void {
    const pkg = this.package();
    if (!pkg || pkg.status !== 'published') return;

    this.showConfirm(
      this.t('salaryPackages.dialogs.confirm.deprecateTitle'),
      this.t('salaryPackages.dialogs.confirm.deprecateMessage', { name: pkg.name }),
      this.t('salaryPackages.actions.markDeprecated'),
      'danger',
      () => {
        this.salaryPackageService.deprecate(pkg.id).subscribe({
          next: (updated) => {
            this.package.set(updated);
          },
          error: (err) => {
            console.error('Error deprecating package:', err);
            this.error.set(this.t('salaryPackages.errors.deprecateFailed'));
          }
        });
      }
    );
  }

  // Confirm dialog methods
  private showConfirm(
    title: string,
    message: string,
    action: string,
    type: 'danger' | 'info',
    onConfirm: () => void
  ): void {
    this.confirmDialogTitle.set(title);
    this.confirmDialogMessage.set(message);
    this.confirmDialogAction.set(action);
    this.confirmDialogType.set(type);
    this.pendingAction = onConfirm;
    this.showConfirmDialog.set(true);
  }

  confirmAction(): void {
    if (this.pendingAction) {
      this.pendingAction();
    }
    this.showConfirmDialog.set(false);
    this.pendingAction = null;
  }

  cancelConfirm(): void {
    this.showConfirmDialog.set(false);
    this.pendingAction = null;
  }

  // ============ Assignment Methods ============

  loadAssignments(): void {
    const packageId = this.package()?.id;
    if (!packageId) return;

    this.isLoadingAssignments.set(true);
    this.assignmentsError.set(null);

    this.salaryPackageService.getAssignments(packageId).subscribe({
      next: (assignments) => {
        this.assignments.set(
          assignments.sort((a, b) =>
            new Date(b.effectiveDate).getTime() - new Date(a.effectiveDate).getTime()
          )
        );
        this.isLoadingAssignments.set(false);
      },
      error: () => {
        this.assignmentsError.set(this.t('salaryPackages.errors.loadAssignments'));
        this.isLoadingAssignments.set(false);
      }
    });
  }

  openAssignModal(): void {
    this.isAssignModalOpen.set(true);
  }

  closeAssignModal(): void {
    this.isAssignModalOpen.set(false);
  }

  handleAssign(data: { employeeId: number; contractId: number; effectiveDate: string }): void {
    const packageId = this.package()?.id;
    if (!packageId) return;

    this.salaryPackageService.getAssignmentsByEmployee(data.employeeId).subscribe({
      next: (employeeAssignments) => {
        const activeAssignment = employeeAssignments.find(a => !a.endDate);

        if (activeAssignment) {
          this.assignModal?.setSubmitting(false);
          const samePackage = activeAssignment.salaryPackageId === packageId;
          const currentPackageName = activeAssignment.salaryPackageName || this.t('salaryPackages.common.anotherPackage');
          const employeeName = activeAssignment.employeeFullName || this.t('salaryPackages.common.thisEmployee');
          const message = samePackage
            ? this.t('salaryPackages.dialogs.confirm.replaceAssignmentSame', { employeeName })
            : this.t('salaryPackages.dialogs.confirm.replaceAssignmentOther', { employeeName, currentPackageName });

          this.showConfirm(
            this.t('salaryPackages.dialogs.confirm.replaceAssignmentTitle'),
            message,
            this.t('salaryPackages.actions.replace'),
            'danger',
            () => {
              this.assignModal?.setSubmitting(true);
              this.executeAssignment(data, packageId);
            }
          );
          return;
        }

        this.executeAssignment(data, packageId);
      },
      error: (error) => {
        console.error('Failed to verify existing employee assignment:', error);
        this.assignModal?.setError(this.t('salaryPackages.errors.verifyEmployeeAssignmentFailed'));
      }
    });
  }

  private executeAssignment(
    data: { employeeId: number; contractId: number; effectiveDate: string },
    packageId: number
  ): void {
    this.salaryPackageService.createAssignment({
      salaryPackageId: packageId,
      employeeId: data.employeeId,
      contractId: data.contractId,
      effectiveDate: data.effectiveDate
    }).subscribe({
      next: () => {
        this.closeAssignModal();
        this.loadAssignments();
      },
      error: (error) => {
        console.error('Failed to create assignment:', error);
        const message = error?.error?.Message || error?.error?.message || this.t('salaryPackages.errors.assignmentFailed');
        this.assignModal?.setError(message);
      }
    });
  }

  openEndAssignmentModal(assignment: SalaryPackageAssignment): void {
    this.selectedAssignment.set(assignment);
    this.isEndAssignmentModalOpen.set(true);
  }

  closeEndAssignmentModal(): void {
    this.isEndAssignmentModalOpen.set(false);
    this.selectedAssignment.set(null);
  }

  handleEndAssignment(data: { assignmentId: number; endDate: string }): void {
    this.salaryPackageService.endAssignment(data.assignmentId, { endDate: data.endDate })
      .subscribe({
        next: () => {
          this.closeEndAssignmentModal();
          this.loadAssignments();
        },
        error: (error) => {
          console.error('Failed to end assignment:', error);
        }
      });
  }

  // Helpers
  getStatusClasses(status: SalaryPackageStatus): string {
    const map: Record<SalaryPackageStatus, string> = {
      draft: 'bg-yellow-100 text-yellow-800',
      published: 'bg-green-100 text-green-800',
      deprecated: 'bg-red-100 text-red-800'
    };
    return map[status] || 'bg-gray-100 text-gray-800';
  }

  getStatusLabel(status: SalaryPackageStatus): string {
    const map: Record<SalaryPackageStatus, string> = {
      draft: this.t('salaryPackages.status.draft'),
      published: this.t('salaryPackages.status.published'),
      deprecated: this.t('salaryPackages.status.deprecated')
    };
    return map[status] || status;
  }

  getTypeLabel(type: SalaryComponentType): string {
    const map: Record<SalaryComponentType, string> = {
      base_salary: this.t('salaryPackages.view.types.baseSalary'),
      allowance: this.t('salaryPackages.view.types.allowance'),
      bonus: this.t('salaryPackages.view.types.bonus'),
      benefit_in_kind: this.t('salaryPackages.view.types.benefitInKind'),
      social_charge: this.t('salaryPackages.view.types.socialCharge')
    };
    return map[type] || type;
  }

  getTypeClasses(type: SalaryComponentType): string {
    const map: Record<SalaryComponentType, string> = {
      base_salary: 'bg-blue-100 text-blue-800',
      allowance: 'bg-green-100 text-green-800',
      bonus: 'bg-yellow-100 text-yellow-800',
      benefit_in_kind: 'bg-gray-100 text-gray-800',
      social_charge: 'bg-red-100 text-red-800'
    };
    return map[type] || 'bg-gray-100 text-gray-800';
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('fr-MA', {
      style: 'currency',
      currency: 'MAD',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(value);
  }

  formatDate(date: string | null | undefined): string {
    if (!date) return '-';
    return new Date(date).toLocaleDateString('fr-MA', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    });
  }

  private t(key: string, params?: Record<string, unknown>): string {
    const translated = this.translate.instant(key, params);
    return typeof translated === 'string' ? translated : key;
  }

  private localizeStaticOptions(): void {
    this.categoryOptions = [
      { label: this.t('salaryPackages.editor.categories.cadres'), value: 'Cadres' },
      { label: this.t('salaryPackages.editor.categories.nonCadres'), value: 'Non-cadres' },
      { label: this.t('salaryPackages.editor.categories.executive'), value: 'Dirigeants' }
    ];

    this.cimrRegimeOptions = [
      { label: this.t('salaryPackages.cimr.none'), value: 'NONE' },
      { label: 'Al Kamil', value: 'AL_KAMIL' },
      { label: this.t('salaryPackages.cimr.alMounassib'), value: 'AL_MOUNASSIB' }
    ];
  }
}
