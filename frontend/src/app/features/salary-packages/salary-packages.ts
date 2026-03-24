import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { TranslateModule, TranslateService } from '@ngx-translate/core';

import { CompanyContextService } from '@app/core/services/companyContext.service';
import { SalaryPackageService } from '@app/core/services/salary-package.service';
import { SalaryPackage, SalaryPackageStatus } from '@app/core/models/salary-package.model';
import { SalaryMetricCardComponent } from './components/salary-metric-card/salary-metric-card.component';
import { SalaryStatusChipComponent } from './components/salary-status-chip/salary-status-chip.component';

export type PackageAction = 'view' | 'edit' | 'publish' | 'duplicate' | 'delete' | 'new_version' | 'deprecate';

type ActionTone = 'primary' | 'neutral' | 'danger' | 'warning';
type ActiveTab = 'company' | 'official';

interface ActionConfig {
  key: PackageAction;
  label: string;
  tone: ActionTone;
}

@Component({
  selector: 'app-salary-packages',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    TranslateModule,
    SalaryMetricCardComponent,
    SalaryStatusChipComponent
  ],
  templateUrl: './salary-packages.html',
  styleUrl: './salary-packages.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class SalaryPackagesPage implements OnInit {
  private readonly router = inject(Router);
  private readonly salaryPackageService = inject(SalaryPackageService);
  private readonly contextService = inject(CompanyContextService);
  private readonly translate = inject(TranslateService);
  private readonly destroyRef = inject(DestroyRef);

  readonly searchQuery = signal('');
  readonly selectedStatus = signal<SalaryPackageStatus | null>(null);
  readonly selectedCategory = signal<string | null>(null);
  readonly activeTab = signal<ActiveTab>('company');

  readonly companyPackages = signal<SalaryPackage[]>([]);
  readonly officialTemplates = signal<SalaryPackage[]>([]);
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);

  readonly showCloneDialog = signal(false);
  readonly selectedTemplateToClone = signal<SalaryPackage | null>(null);
  readonly cloneName = signal('');

  readonly showConfirmDialog = signal(false);
  readonly confirmDialogTitle = signal('');
  readonly confirmDialogMessage = signal('');
  readonly confirmDialogAction = signal('');
  readonly confirmDialogType = signal<'danger' | 'success'>('danger');
  private confirmCallback: (() => void) | null = null;

  readonly categories = signal<Array<{ label: string; value: string | null }>>([
    { label: '', value: null }
  ]);

  readonly routePrefix = computed(() => this.contextService.isExpertMode() ? '/expert' : '/app');

  readonly stats = computed(() => {
    const company = this.companyPackages();
    const official = this.officialTemplates();
    return {
      totalCompany: company.length,
      draftCompany: company.filter((p) => p.status === 'draft').length,
      publishedCompany: company.filter((p) => p.status === 'published').length,
      deprecatedCompany: company.filter((p) => p.status === 'deprecated').length,
      totalOfficial: official.length
    };
  });

  readonly statusFilters = computed(() => ([
    { value: null as SalaryPackageStatus | null, label: this.t('salaryPackages.filters.all'), count: this.stats().totalCompany },
    { value: 'draft' as SalaryPackageStatus, label: this.t('salaryPackages.filters.draft'), count: this.stats().draftCompany },
    { value: 'published' as SalaryPackageStatus, label: this.t('salaryPackages.filters.published'), count: this.stats().publishedCompany },
    { value: 'deprecated' as SalaryPackageStatus, label: this.t('salaryPackages.filters.deprecated'), count: this.stats().deprecatedCompany }
  ]));

  readonly filteredCompanyPackages = computed(() => {
    let result = this.companyPackages();

    if (this.searchQuery()) {
      const query = this.searchQuery().toLowerCase();
      result = result.filter((pkg) =>
        pkg.name.toLowerCase().includes(query) ||
        (pkg.category || '').toLowerCase().includes(query) ||
        (pkg.code || '').toLowerCase().includes(query)
      );
    }

    if (this.selectedCategory()) {
      result = result.filter((pkg) => pkg.category === this.selectedCategory());
    }

    if (this.selectedStatus()) {
      result = result.filter((pkg) => pkg.status === this.selectedStatus());
    }

    return [...result].sort((a, b) => a.name.localeCompare(b.name));
  });

  readonly filteredOfficialTemplates = computed(() => {
    let result = this.officialTemplates();

    if (this.searchQuery()) {
      const query = this.searchQuery().toLowerCase();
      result = result.filter((pkg) =>
        pkg.name.toLowerCase().includes(query) ||
        (pkg.category || '').toLowerCase().includes(query) ||
        (pkg.code || '').toLowerCase().includes(query)
      );
    }

    return [...result].sort((a, b) => a.name.localeCompare(b.name));
  });

  readonly groupedOfficialTemplates = computed(() => {
    const grouped = this.filteredOfficialTemplates().reduce((acc, template) => {
      const category = template.category || this.t('salaryPackages.common.others');
      if (!acc[category]) {
        acc[category] = [];
      }
      acc[category].push(template);
      return acc;
    }, {} as Record<string, SalaryPackage[]>);

    return Object.entries(grouped)
      .map(([category, templates]) => ({
        category,
        templates
      }))
      .sort((a, b) => {
        if (a.category === this.t('salaryPackages.common.others')) {
          return 1;
        }

        if (b.category === this.t('salaryPackages.common.others')) {
          return -1;
        }

        return a.category.localeCompare(b.category);
      });
  });

  get searchQueryModel(): string {
    return this.searchQuery();
  }

  set searchQueryModel(value: string) {
    this.searchQuery.set(value);
  }

  get selectedCategoryModel(): string | null {
    return this.selectedCategory();
  }

  set selectedCategoryModel(value: string | null) {
    this.selectedCategory.set(value);
  }

  get cloneNameModel(): string {
    return this.cloneName();
  }

  set cloneNameModel(value: string) {
    this.cloneName.set(value);
  }

  ngOnInit(): void {
    this.localizeStaticOptions();
    this.loadData();

    this.contextService.contextChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.loadData();
      });

    this.translate.onLangChange
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.localizeStaticOptions());
  }

  loadData(): void {
    this.isLoading.set(true);
    this.error.set(null);

    this.salaryPackageService.getCompanyPackages(undefined, undefined).subscribe({
      next: (packages) => {
        this.companyPackages.set(packages);
        this.extractCategories(packages);
      },
      error: (err) => {
        console.error('Error loading company packages:', err);
        this.error.set(this.t('salaryPackages.errors.loadPackages'));
      }
    });

    this.salaryPackageService.getOfficialTemplates('published').subscribe({
      next: (templates) => {
        this.officialTemplates.set(templates);
        this.isLoading.set(false);
      },
      error: (err) => {
        console.error('Error loading official templates:', err);
        this.isLoading.set(false);
      }
    });
  }

  setActiveTab(tab: ActiveTab): void {
    this.activeTab.set(tab);
    if (tab === 'official') {
      this.selectedStatus.set(null);
      this.selectedCategory.set(null);
    }
  }

  applyStatusFilter(status: SalaryPackageStatus | null): void {
    this.activeTab.set('company');
    this.selectedStatus.set(status);
  }

  private extractCategories(packages: SalaryPackage[]): void {
    const uniqueCategories = Array.from(new Set(packages.map((p) => p.category).filter(Boolean)));

    this.categories.set([
      { label: this.t('salaryPackages.filters.allCategories'), value: null },
      ...uniqueCategories.map((category) => ({ label: category, value: category }))
    ]);
  }

  viewPackage(pkg: SalaryPackage): void {
    this.router.navigate([`${this.routePrefix()}/salary-packages`, pkg.id]);
  }

  createPackage(): void {
    this.router.navigate([`${this.routePrefix()}/salary-packages`, 'create']);
  }

  editPackage(pkg: SalaryPackage, event?: Event): void {
    event?.stopPropagation();
    this.router.navigate([`${this.routePrefix()}/salary-packages`, pkg.id, 'edit']);
  }

  getAvailableActions(pkg: SalaryPackage): ActionConfig[] {
    if (pkg.templateType === 'OFFICIAL' || pkg.isGlobalTemplate) {
      return [
        { key: 'view', label: this.t('salaryPackages.actions.view'), tone: 'neutral' },
        { key: 'duplicate', label: this.t('salaryPackages.actions.duplicate'), tone: 'primary' }
      ];
    }

    switch (pkg.status) {
      case 'draft':
        return [
          { key: 'edit', label: this.t('salaryPackages.actions.edit'), tone: 'primary' },
          { key: 'publish', label: this.t('salaryPackages.actions.publish'), tone: 'neutral' },
          { key: 'delete', label: this.t('salaryPackages.actions.delete'), tone: 'danger' }
        ];
      case 'published':
        return [
          { key: 'view', label: this.t('salaryPackages.actions.view'), tone: 'neutral' },
          { key: 'new_version', label: this.t('salaryPackages.actions.newVersion'), tone: 'primary' },
          { key: 'duplicate', label: this.t('salaryPackages.actions.duplicate'), tone: 'neutral' },
          { key: 'deprecate', label: this.t('salaryPackages.actions.deprecate'), tone: 'warning' }
        ];
      case 'deprecated':
        return [
          { key: 'view', label: this.t('salaryPackages.actions.view'), tone: 'neutral' },
          { key: 'duplicate', label: this.t('salaryPackages.actions.duplicate'), tone: 'neutral' }
        ];
      default:
        return [];
    }
  }

  getActionButtonClass(tone: ActionTone): string {
    const classes: Record<ActionTone, string> = {
      primary: 'btn btn-primary btn-sm',
      neutral: 'btn btn-secondary btn-sm',
      danger: 'btn btn-danger-outline btn-sm',
      warning: 'btn btn-warning-outline btn-sm'
    };

    return classes[tone];
  }

  handleAction(action: PackageAction, pkg: SalaryPackage, event: Event): void {
    event.stopPropagation();

    switch (action) {
      case 'view':
        this.viewPackage(pkg);
        break;
      case 'edit':
        this.editPackage(pkg);
        break;
      case 'publish':
        this.publishPackage(pkg);
        break;
      case 'duplicate':
        this.duplicatePackage(pkg);
        break;
      case 'delete':
        this.deletePackage(pkg);
        break;
      case 'new_version':
        this.createNewVersion(pkg);
        break;
      case 'deprecate':
        this.deprecatePackage(pkg);
        break;
    }
  }

  openCloneDialog(template: SalaryPackage, event?: Event): void {
    event?.stopPropagation();
    this.selectedTemplateToClone.set(template);
    this.cloneName.set(`${template.name} - ${this.t('salaryPackages.common.copy')}`);
    this.showCloneDialog.set(true);
  }

  closeCloneDialog(): void {
    this.showCloneDialog.set(false);
    this.selectedTemplateToClone.set(null);
    this.cloneName.set('');
  }

  confirmClone(): void {
    const template = this.selectedTemplateToClone();
    if (!template) {
      return;
    }

    const companyId = this.contextService.companyId();
    if (!companyId) {
      this.error.set(this.t('salaryPackages.errors.noCompanySelected'));
      return;
    }

    this.salaryPackageService.cloneTemplate(template.id, {
      companyId: Number(companyId),
      name: this.cloneName() || undefined
    }).subscribe({
      next: (cloned) => {
        this.closeCloneDialog();
        this.loadData();
        this.router.navigate([`${this.routePrefix()}/salary-packages`, cloned.id, 'edit']);
      },
      error: (err) => {
        console.error('Error cloning template:', err);
        this.error.set(this.t('salaryPackages.errors.cloneFailed'));
      }
    });
  }

  private openConfirmDialog(
    title: string,
    message: string,
    action: string,
    type: 'danger' | 'success',
    callback: () => void
  ): void {
    this.confirmDialogTitle.set(title);
    this.confirmDialogMessage.set(message);
    this.confirmDialogAction.set(action);
    this.confirmDialogType.set(type);
    this.confirmCallback = callback;
    this.showConfirmDialog.set(true);
  }

  closeConfirmDialog(): void {
    this.showConfirmDialog.set(false);
    this.confirmCallback = null;
  }

  executeConfirmAction(): void {
    if (this.confirmCallback) {
      this.confirmCallback();
    }
    this.closeConfirmDialog();
  }

  deletePackage(pkg: SalaryPackage, event?: Event): void {
    event?.stopPropagation();

    if (pkg.status !== 'draft') {
      this.error.set(this.t('salaryPackages.errors.onlyDraftDelete'));
      return;
    }

    this.openConfirmDialog(
      this.t('salaryPackages.dialogs.confirm.deleteTitle'),
      this.t('salaryPackages.dialogs.confirm.deleteMessage', { name: pkg.name }),
      this.t('salaryPackages.actions.delete'),
      'danger',
      () => {
        this.salaryPackageService.delete(pkg.id).subscribe({
          next: () => {
            this.loadData();
          },
          error: (err) => {
            console.error('Error deleting package:', err);
            this.error.set(this.t('salaryPackages.errors.deleteFailed'));
          }
        });
      }
    );
  }

  publishPackage(pkg: SalaryPackage, event?: Event): void {
    event?.stopPropagation();

    this.openConfirmDialog(
      this.t('salaryPackages.dialogs.confirm.publishTitle'),
      this.t('salaryPackages.dialogs.confirm.publishMessage', { name: pkg.name }),
      this.t('salaryPackages.actions.publish'),
      'success',
      () => {
        this.salaryPackageService.publish(pkg.id).subscribe({
          next: () => {
            this.loadData();
          },
          error: (err) => {
            console.error('Error publishing package:', err);
            this.error.set(this.t('salaryPackages.errors.publishFailed'));
          }
        });
      }
    );
  }

  duplicatePackage(pkg: SalaryPackage, event?: Event): void {
    event?.stopPropagation();

    this.salaryPackageService.duplicate(pkg.id, `${pkg.name} - ${this.t('salaryPackages.common.copy')}`).subscribe({
      next: (duplicated) => {
        this.loadData();
        this.router.navigate([`${this.routePrefix()}/salary-packages`, duplicated.id, 'edit']);
      },
      error: (err) => {
        console.error('Error duplicating package:', err);
        this.error.set(this.t('salaryPackages.errors.duplicateFailed'));
      }
    });
  }

  createNewVersion(pkg: SalaryPackage, event?: Event): void {
    event?.stopPropagation();

    if (pkg.status !== 'published') {
      this.error.set(this.t('salaryPackages.errors.onlyPublishedVersion'));
      return;
    }

    const nextVersion = (pkg.version || 1) + 1;
    this.openConfirmDialog(
      this.t('salaryPackages.dialogs.confirm.newVersionTitle'),
      this.t('salaryPackages.dialogs.confirm.newVersionMessage', { nextVersion, currentVersion: pkg.version || 1 }),
      this.t('salaryPackages.actions.createVersion'),
      'success',
      () => {
        this.salaryPackageService.createNewVersion(pkg.id).subscribe({
          next: (newVersion) => {
            this.loadData();
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

  deprecatePackage(pkg: SalaryPackage, event?: Event): void {
    event?.stopPropagation();

    if (pkg.status !== 'published') {
      this.error.set(this.t('salaryPackages.errors.onlyPublishedDeprecate'));
      return;
    }

    this.openConfirmDialog(
      this.t('salaryPackages.dialogs.confirm.deprecateTitle'),
      this.t('salaryPackages.dialogs.confirm.deprecateMessage', { name: pkg.name }),
      this.t('salaryPackages.actions.markDeprecated'),
      'danger',
      () => {
        this.salaryPackageService.deprecate(pkg.id).subscribe({
          next: () => {
            this.loadData();
          },
          error: (err) => {
            console.error('Error deprecating package:', err);
            this.error.set(this.t('salaryPackages.errors.deprecateFailed'));
          }
        });
      }
    );
  }

  clearFilters(): void {
    this.searchQuery.set('');
    this.selectedStatus.set(null);
    this.selectedCategory.set(null);
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
    if (!date) {
      return '-';
    }

    return new Date(date).toLocaleDateString('fr-FR', {
      day: '2-digit',
      month: 'short',
      year: 'numeric'
    });
  }

  private localizeStaticOptions(): void {
    this.categories.set([{ label: this.t('salaryPackages.filters.allCategories'), value: null }]);
  }

  private t(key: string, params?: Record<string, unknown>): string {
    const translated = this.translate.instant(key, params);
    return typeof translated === 'string' ? translated : key;
  }
}
