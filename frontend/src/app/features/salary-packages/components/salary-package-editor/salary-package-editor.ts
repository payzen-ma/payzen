import { Component, signal, computed, OnInit, inject, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { Subscription } from 'rxjs';

import { SalaryPackageService } from '@app/core/services/salary-package.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { PayrollReferentielService } from '@app/core/services/payroll-referentiel.service';
import {
  ReferentielElementListDto,
  ReferentielElementDto,
  ElementRuleDto,
  ExemptionType
} from '@app/core/models/payroll-referentiel.model';
import {
  SalaryPackage,
  SalaryPackageItem,
  SalaryPackageWriteRequest,
  SalaryPackageItemWriteRequest,
  SalaryComponentType,
  PayComponent,
  CimrRegime,
  CimrConfig,
  CIMR_AL_KAMIL_RATES,
  CIMR_AL_MOUNASSIB_RATES,
  calculateCimrEmployerRate
} from '@app/core/models/salary-package.model';
import { CimrConfigCardComponent } from '../cimr-config-card/cimr-config-card.component';

@Component({
  selector: 'app-salary-package-editor',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    ReactiveFormsModule,
    TranslateModule,
    CimrConfigCardComponent
  ],
  templateUrl: './salary-package-editor.html',
  styleUrl: './salary-package-editor.css'
})
export class SalaryPackageEditorComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly fb = inject(FormBuilder);
  private readonly salaryPackageService = inject(SalaryPackageService);
  private readonly contextService = inject(CompanyContextService);
  private readonly payrollReferentielService = inject(PayrollReferentielService);
  private readonly translate = inject(TranslateService);

  readonly isEditMode = signal(false);
  readonly packageId = signal<number | null>(null);
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly error = signal<string | null>(null);
  readonly currentPackage = signal<SalaryPackage | null>(null);

  readonly routePrefix = computed(() => this.contextService.isExpertMode() ? '/expert' : '/app');

  // Read-only detection for global templates
  readonly isGlobalTemplate = computed(() => {
    const pkg = this.currentPackage();
    return pkg?.companyId === null;
  });

  readonly isReadOnly = computed(() => {
    const pkg = this.currentPackage();
    // Global templates (companyId null) are always read-only
    return pkg?.companyId === null;
  });

  readonly isClonedTemplate = computed(() => {
    const pkg = this.currentPackage();
    return pkg?.sourceTemplateId !== null && pkg?.sourceTemplateId !== undefined;
  });

  readonly sourceTemplateName = computed(() => {
    const pkg = this.currentPackage();
    return pkg?.sourceTemplateName || '';
  });

  // Pay components catalog (kept for potential future use)
  readonly payComponents = signal<PayComponent[]>([]);

  // Referentiel elements for dropdown
  readonly referentielElements = signal<ReferentielElementListDto[]>([]);
  readonly filteredReferentielElements = signal<ReferentielElementListDto[]>([]);
  readonly loadingElements = signal(false);
  readonly activeDropdownIndex = signal<number | null>(null);
  elementSearchTerm = '';

  // Click outside handler reference for cleanup
  private clickOutsideHandler = this.onClickOutsideDropdown.bind(this);

  // CIMR configuration
  cimrConfig: CimrConfig = { regime: 'NONE', employeeRate: 0, employerRate: 0 };

  // Form
  form!: FormGroup;

  // Options
  categoryOptions: Array<{ label: string; value: string }> = [];

  componentTypeOptions: Array<{ label: string; value: SalaryComponentType }> = [];
  private langChangeSub?: Subscription;

  readonly totalGrossSalary = computed(() => {
    if (!this.form) return 0;
    const baseSalary = this.form.get('baseSalary')?.value || 0;
    const items = this.itemsArray.controls;
    const itemsTotal = items.reduce((sum, ctrl) => {
      const type = ctrl.get('type')?.value;
      if (type !== 'social_charge') {
        return sum + (ctrl.get('defaultValue')?.value || 0);
      }
      return sum;
    }, 0);
    return baseSalary + itemsTotal;
  });

  get itemsArray(): FormArray {
    return this.form.get('items') as FormArray;
  }

  ngOnInit(): void {
    this.localizeStaticOptions();
    this.langChangeSub = this.translate.onLangChange.subscribe(() => this.localizeStaticOptions());

    this.initForm();
    this.loadPayComponents();
    this.loadReferentielElements();

    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'create') {
      this.isEditMode.set(true);
      this.packageId.set(Number(id));
      this.loadPackage(Number(id));
    } else {
      this.isLoading.set(false);
    }
  }

  ngOnDestroy(): void {
    document.removeEventListener('click', this.clickOutsideHandler);
    this.langChangeSub?.unsubscribe();
  }

  private initForm(): void {
    this.form = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(3)]],
      category: ['', Validators.required],
      description: [''],
      baseSalary: [0, [Validators.required, Validators.min(0)]],

      // Effective dates
      validFrom: [null],
      validTo: [null],

      // Items
      items: this.fb.array([])
    });
  }

  onCimrConfigChange(config: CimrConfig): void {
    this.cimrConfig = config;
  }

  private loadPackage(id: number): void {
    this.salaryPackageService.getById(id).subscribe({
      next: (pkg) => {
        this.currentPackage.set(pkg);
        this.populateForm(pkg);
        
        // Disable form if it's a global template (read-only)
        if (this.isReadOnly()) {
          this.form.disable();
        }
        
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading package:', err);
        this.error.set(this.t('salaryPackages.errors.loadPackage'));
        this.isLoading.set(false);
      }
    });
  }

  private populateForm(pkg: SalaryPackage): void {
    this.form.patchValue({
      name: pkg.name,
      category: pkg.category,
      description: pkg.description || '',
      baseSalary: pkg.baseSalary,
      validFrom: pkg.validFrom ? this.formatDateForInput(pkg.validFrom) : null,
      validTo: pkg.validTo ? this.formatDateForInput(pkg.validTo) : null
    });

    // Set CIMR config
    if (pkg.cimrConfig) {
      this.cimrConfig = { ...pkg.cimrConfig };
    } else {
      this.cimrConfig = { regime: 'NONE', employeeRate: 0, employerRate: 0 };
    }

    // Clear and populate items
    this.itemsArray.clear();
    pkg.items.forEach(item => this.addItemToForm(item));
  }

  private loadPayComponents(): void {
    this.salaryPackageService.getPayComponents(undefined, true).subscribe({
      next: (components) => {
        this.payComponents.set(components);
      },
      error: (err) => {
        console.error('Error loading pay components:', err);
      }
    });
  }

  private loadReferentielElements(): void {
    this.loadingElements.set(true);
    this.payrollReferentielService.getAllReferentielElements().subscribe({
      next: (elements) => {
        this.referentielElements.set(elements);
        this.filteredReferentielElements.set([...elements]);
        this.loadingElements.set(false);
      },
      error: (err) => {
        console.error('Error loading referentiel elements:', err);
        this.referentielElements.set([]);
        this.filteredReferentielElements.set([]);
        this.loadingElements.set(false);
      }
    });
  }

  // ============ Element Dropdown Methods ============

  toggleElementDropdown(index: number): void {
    if (this.activeDropdownIndex() === index) {
      this.closeElementDropdown();
    } else {
      this.activeDropdownIndex.set(index);
      this.elementSearchTerm = '';
      this.filterElements();

      // Defer adding click-outside listener
      setTimeout(() => {
        document.addEventListener('click', this.clickOutsideHandler);
      }, 100);
    }
  }

  closeElementDropdown(): void {
    this.activeDropdownIndex.set(null);
    this.elementSearchTerm = '';
    document.removeEventListener('click', this.clickOutsideHandler);
  }

  private onClickOutsideDropdown(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.element-dropdown-container')) {
      this.closeElementDropdown();
    }
  }

  filterElements(): void {
    const elements = this.referentielElements();
    if (!this.elementSearchTerm.trim()) {
      this.filteredReferentielElements.set([...elements]);
    } else {
      const term = this.elementSearchTerm.toLowerCase();
      this.filteredReferentielElements.set(
        elements.filter(el =>
          el.name.toLowerCase().includes(term) ||
          el.categoryName.toLowerCase().includes(term)
        )
      );
    }
  }

  selectReferentielElement(itemIndex: number, element: ReferentielElementListDto): void {
    const itemGroup = this.itemsArray.at(itemIndex);
    if (!itemGroup) return;

    // Update form control values
    itemGroup.patchValue({
      label: element.name,
      referentielElementId: element.id,
      referentielElementCode: element.name,
      isConvergence: element.isConvergence,
      type: 'allowance'
    });

    // Load full element details with rules and apply them
    this.loadElementRulesAndApply(itemIndex, element.id);
    this.closeElementDropdown();
  }

  private loadElementRulesAndApply(itemIndex: number, elementId: number): void {
    this.payrollReferentielService.getReferentielElementById(elementId).subscribe({
      next: (fullElement) => {
        const itemGroup = this.itemsArray.at(itemIndex);
        if (!itemGroup) return;

        // Find CNSS and IR rules
        const cnssRule = fullElement.rules.find(r => this.isCnssAuthority(r.authorityName));
        const irRule = fullElement.rules.find(r => this.isIrAuthority(r.authorityName));

        // Apply rules to form
        this.applyReferentielRules(itemGroup, cnssRule, irRule);
      },
      error: (err) => {
        console.error('Failed to load element rules:', err);
        // Apply defaults if fetch fails
        const itemGroup = this.itemsArray.at(itemIndex);
        if (itemGroup) {
          itemGroup.patchValue({
            isTaxable: true,
            isSocial: true,
            isCIMR: true,
            isVariable: true
          });
        }
      }
    });
  }

  private isCnssAuthority(name: string | undefined): boolean {
    if (!name) return false;
    const n = name.toLowerCase();
    return n === 'cnss' || n.includes('sécurité sociale') || n.includes('securite sociale');
  }

  private isIrAuthority(name: string | undefined): boolean {
    if (!name) return false;
    const n = name.toLowerCase();
    return n === 'ir' || n === 'dgi' || n.includes('impôt') || n.includes('impot') || n.includes('revenu');
  }

  private applyReferentielRules(itemGroup: any, cnssRule?: ElementRuleDto, irRule?: ElementRuleDto): void {
    let isSocial = true;
    let isTaxable = true;

    // Apply CNSS rule
    if (cnssRule) {
      isSocial = this.interpretExemption(cnssRule.exemptionType);
    }

    // Apply IR rule
    if (irRule) {
      isTaxable = this.interpretExemption(irRule.exemptionType);
    }

    itemGroup.patchValue({
      isTaxable,
      isSocial,
      isCIMR: true, // Usually subject
      isVariable: true // Allowances are typically variable
    });
  }

  private interpretExemption(exemptionType: ExemptionType): boolean {
    switch (exemptionType) {
      case 'FULLY_EXEMPT':
        return false;
      case 'FULLY_SUBJECT':
        return true;
      default:
        // CAPPED, FORMULA, etc. - partially subject
        return true;
    }
  }

  // Get selected element for an item (for display)
  getSelectedElement(itemIndex: number): ReferentielElementListDto | null {
    const itemGroup = this.itemsArray.at(itemIndex);
    if (!itemGroup) return null;

    const elementId = itemGroup.get('referentielElementId')?.value;
    if (!elementId) return null;

    return this.referentielElements().find(el => el.id === elementId) || null;
  }

  // Item management
  addItemToForm(item?: Partial<SalaryPackageItem>): void {
    const itemGroup = this.fb.group({
      id: [item?.id || null],
      payComponentId: [item?.payComponentId || null],
      label: [item?.label || '', Validators.required],
      defaultValue: [item?.defaultValue || 0, [Validators.required, Validators.min(0)]],
      type: [item?.type || 'allowance', Validators.required],
      isTaxable: [item?.isTaxable ?? true],
      isSocial: [item?.isSocial ?? true],
      isCIMR: [item?.isCIMR ?? false],
      isVariable: [item?.isVariable ?? false],
      exemptionLimit: [item?.exemptionLimit || null],
      // Referentiel element fields
      referentielElementId: [item?.referentielElementId || null],
      referentielElementCode: [item?.referentielElementCode || null],
      isConvergence: [item?.isConvergence]
    });

    this.itemsArray.push(itemGroup);
  }

  removeItem(index: number): void {
    this.itemsArray.removeAt(index);
  }

  moveItemUp(index: number): void {
    if (index > 0) {
      const items = this.itemsArray;
      const current = items.at(index);
      items.removeAt(index);
      items.insert(index - 1, current);
    }
  }

  moveItemDown(index: number): void {
    if (index < this.itemsArray.length - 1) {
      const items = this.itemsArray;
      const current = items.at(index);
      items.removeAt(index);
      items.insert(index + 1, current);
    }
  }

  // Navigation
  goBack(): void {
    this.router.navigate([`${this.routePrefix()}/salary-packages`]);
  }

  // Save
  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    const request = this.buildRequest();

    const operation = this.isEditMode()
      ? this.salaryPackageService.update(this.packageId()!, request)
      : this.salaryPackageService.create(request);

    operation.subscribe({
      next: (pkg) => {
        this.isSaving.set(false);
        this.router.navigate([`${this.routePrefix()}/salary-packages`, pkg.id]);
      },
      error: (err) => {
        console.error('Error saving package:', err);
        this.isSaving.set(false);
      }
    });
  }

  private buildRequest(): SalaryPackageWriteRequest {
    const formValue = this.form.value;

    const items: SalaryPackageItemWriteRequest[] = formValue.items.map((item: any, index: number) => ({
      id: item.id || undefined,
      payComponentId: item.payComponentId,
      label: item.label,
      defaultValue: item.defaultValue,
      sortOrder: index + 1,
      type: item.type,
      isTaxable: item.isTaxable,
      isSocial: item.isSocial,
      isCIMR: item.isCIMR,
      isVariable: item.isVariable,
      exemptionLimit: item.exemptionLimit,
      referentielElementId: item.referentielElementId || null
    }));

    const request: SalaryPackageWriteRequest = {
      name: formValue.name,
      category: formValue.category,
      description: formValue.description || null,
      baseSalary: formValue.baseSalary,
      status: 'draft',
      companyId: this.contextService.companyId() ? Number(this.contextService.companyId()) : undefined,
      templateType: 'COMPANY',
      validFrom: formValue.validFrom || null,
      validTo: formValue.validTo || null,
      items
    };

    // Add CIMR config if not NONE
    if (this.cimrConfig.regime !== 'NONE') {
      request.cimrConfig = { ...this.cimrConfig };
    }

    return request;
  }

  // Helpers
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

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('fr-MA', {
      style: 'currency',
      currency: 'MAD',
      minimumFractionDigits: 0,
      maximumFractionDigits: 0
    }).format(value);
  }

  private formatDateForInput(dateString: string): string {
    // Convert ISO date string to YYYY-MM-DD format for HTML date input
    const date = new Date(dateString);
    return date.toISOString().split('T')[0];
  }

  private t(key: string, params?: Record<string, unknown>): string {
    const translated = this.translate.instant(key, params);
    return typeof translated === 'string' ? translated : key;
  }

  private localizeStaticOptions(): void {
    this.categoryOptions = [
      { label: this.t('salaryPackages.editor.categories.cadre'), value: 'Cadre' },
      { label: this.t('salaryPackages.editor.categories.nonCadre'), value: 'Non-Cadre' },
      { label: this.t('salaryPackages.editor.categories.employee'), value: 'Employé' },
      { label: this.t('salaryPackages.editor.categories.worker'), value: 'Ouvrier' },
      { label: this.t('salaryPackages.editor.categories.intern'), value: 'Stagiaire' },
      { label: this.t('salaryPackages.editor.categories.other'), value: 'Autre' }
    ];

    this.componentTypeOptions = [
      { label: this.t('salaryPackages.view.types.allowance'), value: 'allowance' },
      { label: this.t('salaryPackages.view.types.bonus'), value: 'bonus' },
      { label: this.t('salaryPackages.view.types.benefitInKind'), value: 'benefit_in_kind' },
      { label: this.t('salaryPackages.view.types.socialCharge'), value: 'social_charge' }
    ];
  }
}
