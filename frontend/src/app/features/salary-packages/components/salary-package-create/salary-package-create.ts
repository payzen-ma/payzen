import { Component, OnInit, OnDestroy, inject, signal, computed, effect, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';

import { SalaryPackageService } from '@app/core/services/salary-package.service';
import { PayrollReferentielService } from '@app/core/services/payroll-referentiel.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';

import {
  SalaryPackage,
  SalaryPackageWriteRequest,
  SalaryPackageItemWriteRequest,
  SalaryPackageStatus,
  CimrRegime
} from '@app/core/models/salary-package.model';

import { ReferentielElementListDto } from '@app/core/models/payroll-referentiel.model';

@Component({
  selector: 'app-salary-package-create',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslateModule],
  templateUrl: './salary-package-create.html',
  styleUrl: './salary-package-create.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SalaryPackageCreateComponent implements OnInit, OnDestroy {
  private salaryPackageService = inject(SalaryPackageService);
  private referentielService = inject(PayrollReferentielService);
  private companyContextService = inject(CompanyContextService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private translate = inject(TranslateService);

  constructor() {
    effect(() => {
      this.packageName();
      this.baseSalary();
      this.selectedCategory();
      this.description();
      this.components();
      this.cimrRegime();
      this.cimrEmployeeRate();
      this.seniorityBonusEnabled();

      if (this.packageId()) {
        this.hasUnsavedChanges.set(true);
      }
    });
  }

  readonly packageId = signal<number | null>(null);
  readonly isEditMode = computed(() => this.packageId() !== null);

  readonly packageName = signal('');
  readonly selectedCategory = signal<string | null>(null);
  readonly description = signal('');
  readonly baseSalary = signal<number>(0);

  readonly components = signal<SalaryPackageItemWriteRequest[]>([]);

  readonly cimrRegime = signal<CimrRegime>('NONE');
  readonly cimrEmployeeRate = signal<number>(0);
  readonly cimrEmployerRate = computed(() => {
    const rate = this.cimrEmployeeRate();
    return Math.round(rate * 1.3 * 10000) / 10000;
  });

  readonly seniorityBonusEnabled = signal(true);

  readonly status = signal<SalaryPackageStatus>('draft');
  readonly validFrom = signal<string | null>(null);
  readonly validTo = signal<string | null>(null);

  readonly officialElements = signal<ReferentielElementListDto[]>([]);

  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly showCustomDialog = signal(false);

  readonly customName = signal('');
  readonly customAmount = signal<number>(0);
  readonly customIsSocial = signal(true);
  readonly customIsTaxable = signal(true);

  readonly validationErrors = signal<Record<string, string>>({});

  readonly routePrefix = computed(() => this.companyContextService.isExpertMode() ? '/expert' : '/app');

  readonly totalComponents = computed(() => this.components().length);
  readonly addedOfficialElementIds = computed(() => {
    return new Set(
      this.components()
        .map((component) => component.referentielElementId)
        .filter((id): id is number => typeof id === 'number')
    );
  });

  readonly estimatedGross = computed(() => {
    const base = this.baseSalary();
    const componentsTotal = this.components().reduce((sum, c) => sum + c.defaultValue, 0);
    return base + componentsTotal;
  });

  readonly estimatedCnssSalary = computed(() => Math.round(this.estimatedGross() * 0.0993 * 100) / 100);
  readonly estimatedCnssEmployer = computed(() => Math.round(this.estimatedGross() * 0.1346 * 100) / 100);

  readonly estimatedCimrSalary = computed(() => {
    if (this.cimrRegime() === 'NONE') return 0;

    const rate = this.cimrEmployeeRate();
    const base = this.cimrRegime() === 'AL_MOUNASSIB'
      ? Math.min(this.estimatedGross(), 6000)
      : this.estimatedGross();

    return Math.round(base * rate * 100) / 100;
  });

  readonly estimatedCimrEmployer = computed(() => {
    if (this.cimrRegime() === 'NONE') return 0;
    return Math.round(this.estimatedCimrSalary() * 1.3 * 100) / 100;
  });

  readonly canSaveDraft = computed(() => this.packageName().trim().length >= 3 && this.baseSalary() > 0);
  readonly canPublish = computed(() => this.canSaveDraft() && this.selectedCategory() !== null && this.components().length >= 1);
  readonly hasUnsavedChanges = signal(false);

  readonly packageNameError = computed(() => {
    const name = this.packageName().trim();
    if (name.length === 0) return this.t('salaryPackages.errors.nameRequired');
    if (name.length < 3) return this.t('salaryPackages.errors.nameMinLength');
    return '';
  });

  readonly baseSalaryError = computed(() => {
    const salary = this.baseSalary();
    if (salary === 0) return this.t('salaryPackages.errors.baseSalaryRequired');
    if (salary < 0) return this.t('salaryPackages.errors.baseSalaryPositive');
    return '';
  });

  readonly categoryError = computed(() => {
    if (!this.selectedCategory()) return this.t('salaryPackages.errors.categoryRequiredPublish');
    return '';
  });

  readonly componentsError = computed(() => {
    if (this.components().length === 0) return this.t('salaryPackages.errors.minOneComponent');
    return '';
  });

  categoryOptions: Array<{ label: string; value: string }> = [];
  private langChangeSub?: Subscription;

  readonly cimrRateOptions = computed(() => {
    const regime = this.cimrRegime();

    if (regime === 'AL_KAMIL') {
      return [0.03, 0.0375, 0.045, 0.0525, 0.06, 0.07, 0.075, 0.08, 0.085, 0.09, 0.095, 0.1];
    }

    if (regime === 'AL_MOUNASSIB') {
      return [0.06, 0.07, 0.08, 0.09, 0.1, 0.11, 0.12];
    }

    return [];
  });

  private getDefaultCimrRate(regime: CimrRegime): number {
    const options: Record<CimrRegime, number[]> = {
      NONE: [],
      AL_KAMIL: [0.03, 0.0375, 0.045, 0.0525, 0.06, 0.07, 0.075, 0.08, 0.085, 0.09, 0.095, 0.1],
      AL_MOUNASSIB: [0.06, 0.07, 0.08, 0.09, 0.1, 0.11, 0.12]
    };

    return options[regime][0] ?? 0;
  }

  onCimrRegimeChange(regime: CimrRegime | string): void {
    const nextRegime = regime as CimrRegime;
    this.cimrRegime.set(nextRegime);
    this.cimrEmployeeRate.set(this.getDefaultCimrRate(nextRegime));
    this.markAsChanged();
  }

  ngOnInit(): void {
    this.localizeStaticOptions();
    this.langChangeSub = this.translate.onLangChange.subscribe(() => this.localizeStaticOptions());

    this.loadReferentielElements();

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadExistingPackage(Number(id));
    }
  }

  ngOnDestroy(): void {
    this.langChangeSub?.unsubscribe();
  }

  loadReferentielElements(): void {
    this.isLoading.set(true);

    this.referentielService.getAllReferentielElements(false).subscribe({
      next: (elements) => {
        this.officialElements.set(elements.filter((e) => e.isActive));
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load referentiel elements', err);
        this.isLoading.set(false);
      }
    });
  }

  loadExistingPackage(id: number): void {
    this.isLoading.set(true);

    this.salaryPackageService.getById(id).subscribe({
      next: (pkg) => {
        this.populateFormFromPackage(pkg);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Failed to load package', err);
        this.isLoading.set(false);
      }
    });
  }

  populateFormFromPackage(pkg: SalaryPackage): void {
    this.packageId.set(pkg.id);
    this.packageName.set(pkg.name);
    this.selectedCategory.set(pkg.category);
    this.description.set(pkg.description || '');
    this.baseSalary.set(pkg.baseSalary);
    this.status.set(pkg.status);
    this.validFrom.set(pkg.validFrom || null);
    this.validTo.set(pkg.validTo || null);

    if (pkg.cimrConfig) {
      this.cimrRegime.set(pkg.cimrConfig.regime);
      this.cimrEmployeeRate.set(pkg.cimrConfig.employeeRate);
    }

    if (pkg.autoRules) {
      this.seniorityBonusEnabled.set(pkg.autoRules.seniorityBonusEnabled);
    }

    this.components.set(pkg.items.map((item) => ({
      id: item.id,
      payComponentId: item.payComponentId,
      referentielElementId: item.referentielElementId,
      label: item.label,
      defaultValue: item.defaultValue,
      sortOrder: item.sortOrder,
      type: item.type,
      isTaxable: item.isTaxable,
      isSocial: item.isSocial,
      isCIMR: item.isCIMR,
      isVariable: item.isVariable,
      exemptionLimit: item.exemptionLimit
    })));
  }

  onElementSelected(elementId: string): void {
    if (!elementId) return;

    const element = this.officialElements().find((e) => e.id === +elementId);
    if (element) {
      this.addOfficialElement(element);
    }
  }

  isOfficialElementAlreadyAdded(elementId: number): boolean {
    return this.addedOfficialElementIds().has(elementId);
  }

  addOfficialElement(element: ReferentielElementListDto): void {
    if (this.isOfficialElementAlreadyAdded(element.id)) {
      this.showError(this.t('salaryPackages.errors.officialComponentAlreadyAdded', { name: element.name }));
      return;
    }

    const newComponent: SalaryPackageItemWriteRequest = {
      referentielElementId: element.id,
      label: element.name,
      defaultValue: 0,
      sortOrder: this.components().length + 1,
      type: 'allowance',
      isTaxable: true,
      isSocial: true,
      isCIMR: false,
      isVariable: false,
      exemptionLimit: null
    };

    this.components.update((items) => [...items, newComponent]);
    this.hasUnsavedChanges.set(true);
  }

  openCustomDialog(): void {
    this.resetCustomDialog();
    this.showCustomDialog.set(true);
  }

  closeCustomDialog(): void {
    this.showCustomDialog.set(false);
  }

  createCustomComponent(data: { name: string; amount: number; isSocial: boolean; isTaxable: boolean }): void {
    const newComponent: SalaryPackageItemWriteRequest = {
      referentielElementId: null,
      label: data.name,
      defaultValue: data.amount,
      sortOrder: this.components().length + 1,
      type: 'allowance',
      isTaxable: data.isTaxable,
      isSocial: data.isSocial,
      isCIMR: false,
      isVariable: false,
      exemptionLimit: null
    };

    this.components.update((items) => [...items, newComponent]);
    this.hasUnsavedChanges.set(true);
    this.closeCustomDialog();
  }

  submitCustomComponent(): void {
    if (!this.customName().trim() || this.customAmount() < 0) return;

    this.createCustomComponent({
      name: this.customName().trim(),
      amount: this.customAmount(),
      isSocial: this.customIsSocial(),
      isTaxable: this.customIsTaxable()
    });
  }

  private resetCustomDialog(): void {
    this.customName.set('');
    this.customAmount.set(0);
    this.customIsSocial.set(true);
    this.customIsTaxable.set(true);
  }

  updateComponentAmount(index: number, newAmount: number): void {
    this.components.update((items) => {
      const updated = [...items];
      updated[index] = { ...updated[index], defaultValue: newAmount };
      return updated;
    });
    this.hasUnsavedChanges.set(true);
  }

  updateComponentFlag(index: number, field: 'isSocial' | 'isTaxable', value: boolean): void {
    this.components.update((items) => {
      const target = items[index];
      // Regulatory flags from predefined referentiel elements are read-only.
      if (!target || target.referentielElementId) {
        return items;
      }

      const updated = [...items];
      updated[index] = { ...updated[index], [field]: value };
      return updated;
    });
    this.hasUnsavedChanges.set(true);
  }

  removeComponent(index: number): void {
    this.components.update((items) => items.filter((_, i) => i !== index));
    this.hasUnsavedChanges.set(true);
  }

  validateForDraft(): boolean {
    const errors: Record<string, string> = {};

    if (this.packageNameError()) errors['packageName'] = this.packageNameError();
    if (this.baseSalaryError()) errors['baseSalary'] = this.baseSalaryError();

    this.validationErrors.set(errors);
    return Object.keys(errors).length === 0;
  }

  validateForPublish(): boolean {
    if (!this.validateForDraft()) return false;

    const errors = { ...this.validationErrors() };

    if (this.categoryError()) errors['category'] = this.categoryError();
    if (this.componentsError()) errors['components'] = this.componentsError();

    this.validationErrors.set(errors);
    return Object.keys(errors).length === 0;
  }

  showValidationErrors(): void {
    const messages = Object.values(this.validationErrors());
    if (messages.length > 0) {
      alert(this.t('salaryPackages.errors.validationTitle') + ':\n\n' + messages.join('\n'));
    }
  }

  showSuccess(message: string): void {
    alert(message);
  }

  showError(message: string): void {
    alert(message);
  }

  saveDraft(redirectToList = true): void {
    if (!this.validateForDraft()) {
      this.showValidationErrors();
      return;
    }

    this.persistDraft(redirectToList);
  }

  publish(): void {
    if (!this.validateForPublish()) {
      this.showValidationErrors();
      return;
    }

    const confirmed = confirm(
      this.t('salaryPackages.dialogs.confirm.publishReadonlyWarning')
    );

    if (!confirmed) return;

    if (this.hasUnsavedChanges()) {
      this.persistDraft(false, () => this.executePublish());
      return;
    }

    this.executePublish();
  }

  private executePublish(): void {
    if (!this.packageId()) {
      this.showError(this.t('salaryPackages.errors.saveBeforePublish'));
      return;
    }

    this.salaryPackageService.publish(this.packageId()!).subscribe({
      next: (pkg) => {
        this.showSuccess(this.t('salaryPackages.messages.publishedSuccess', { name: pkg.name }));

        setTimeout(() => {
          this.router.navigate([`${this.routePrefix()}/salary-packages`, pkg.id]);
        }, 500);
      },
      error: (err) => {
        console.error('Publish failed', err);

        const errorMsg = err?.error?.message || err?.message || this.t('salaryPackages.common.unknownError');
        this.showError(this.t('salaryPackages.errors.publishFailedWithReason', { errorMsg }));
      }
    });
  }

  private persistDraft(redirectToList: boolean, afterSave?: () => void): void {
    this.isSaving.set(true);

    const request: SalaryPackageWriteRequest = this.buildSaveRequest('draft');

    const save$ = this.packageId()
      ? this.salaryPackageService.update(this.packageId()!, request)
      : this.salaryPackageService.create(request);

    save$.subscribe({
      next: (pkg) => {
        this.packageId.set(pkg.id);
        this.isSaving.set(false);
        this.hasUnsavedChanges.set(false);
        this.validationErrors.set({});

        this.showSuccess(this.t('salaryPackages.messages.savedSuccess', { name: pkg.name }));
        afterSave?.();

        if (redirectToList) {
          this.router.navigate([`${this.routePrefix()}/salary-packages`]);
        }
      },
      error: (err) => {
        console.error('Save failed', err);
        this.isSaving.set(false);

        const errorMsg = err?.error?.message || err?.message || this.t('salaryPackages.common.unknownError');
        this.showError(this.t('salaryPackages.errors.saveFailedWithReason', { errorMsg }));
      }
    });
  }

  cancel(): void {
    if (this.hasUnsavedChanges()) {
      const confirmed = confirm(this.t('salaryPackages.dialogs.confirm.unsavedChanges'));
      if (!confirmed) return;
    }

    this.router.navigate([`${this.routePrefix()}/salary-packages`]);
  }

  markAsChanged(): void {
    this.hasUnsavedChanges.set(true);
  }

  private buildSaveRequest(status: SalaryPackageStatus): SalaryPackageWriteRequest {
    const companyId = this.companyContextService.companyId();

    return {
      name: this.packageName(),
      category: this.selectedCategory() || '',
      description: this.description() || null,
      baseSalary: this.baseSalary(),
      status,
      companyId: companyId ? Number(companyId) : null,
      templateType: 'COMPANY',
      regulationVersion: 'MA_2025',
      autoRules: {
        seniorityBonusEnabled: this.seniorityBonusEnabled(),
        ruleVersion: 'MA_2025'
      },
      cimrConfig: this.cimrRegime() !== 'NONE' ? {
        regime: this.cimrRegime(),
        employeeRate: this.cimrEmployeeRate(),
        employerRate: this.cimrEmployerRate(),
        customEmployerRate: null
      } : null,
      cimrRate: this.cimrEmployeeRate(),
      hasPrivateInsurance: false,
      validFrom: this.validFrom(),
      validTo: this.validTo(),
      items: this.components()
    };
  }

  private t(key: string, params?: Record<string, unknown>): string {
    const translated = this.translate.instant(key, params);
    return typeof translated === 'string' ? translated : key;
  }

  private localizeStaticOptions(): void {
    this.categoryOptions = [
      { label: this.t('salaryPackages.editor.categories.cadre'), value: 'Cadres' },
      { label: this.t('salaryPackages.editor.categories.nonCadre'), value: 'Non-cadres' },
      { label: this.t('salaryPackages.editor.categories.executive'), value: 'Dirigeants' }
    ];
  }
}
