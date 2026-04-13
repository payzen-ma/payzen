import { DestroyRef, Injectable, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { take } from 'rxjs';
import { CompanyContextService } from '@app/core/services/companyContext.service';
import { DashboardHrRepository } from '../data/dashboard-hr.repository';
import { DashboardHrData, DashboardFilterState, DashboardHrQuery, DashboardTab, DashboardTabId } from './dashboard-hr.models';
import { DashboardHrRawData } from '../data/dashboard-hr-raw.models';
import { aggregateDashboardFromRaw, extractAvailableFilterOptions } from '../data/dashboard-hr.filter-aggregator';

const DASHBOARD_TABS: DashboardTab[] = [
  { id: 'vue-globale', label: 'Vue Globale RH', icon: 'pi pi-home' },
  { id: 'mouvements-rh', label: 'Mouvements RH', icon: 'pi pi-refresh' },
  { id: 'masse-salariale', label: 'Masse Salariale', icon: 'pi pi-wallet' },
  { id: 'parite-diversite', label: 'Parité & diversité', icon: 'pi pi-users' },
  { id: 'conformite-sociale', label: 'Conformité sociale', icon: 'pi pi-check-circle' }
];

function isDashboardTabId(value: string): value is DashboardTabId {
  return DASHBOARD_TABS.some(tab => tab.id === value);
}

@Injectable()
export class DashboardHrStore {
  private readonly repository = inject(DashboardHrRepository);
  private readonly contextService = inject(CompanyContextService);
  private readonly destroyRef = inject(DestroyRef);

  readonly data = signal<DashboardHrData | null>(null);
  readonly rawData = signal<DashboardHrRawData | null>(null);
  readonly compareRawByMonth = signal<Record<string, DashboardHrRawData>>({});
  readonly activeTab = signal<DashboardTabId>('vue-globale');
  readonly isLoading = signal<boolean>(true);
  readonly isRefreshing = signal<boolean>(false);
  readonly error = signal<string | null>(null);
  readonly warnings = signal<string[]>([]);
  readonly dataSource = signal<'api' | 'mock'>('mock');
  readonly loadedAtIso = signal<string | null>(null);
  readonly availableDepartments = signal<string[]>([]);
  readonly availableContractTypes = signal<string[]>([]);

  readonly filters = signal<DashboardFilterState>({
    departments: [],
    contractTypes: [],
    parity: ['F', 'H'],
    month: this.defaultMonth(),
    compareMonth: null
  });

  readonly tabs = signal<DashboardTab[]>(DASHBOARD_TABS);
  readonly monthOptions = signal<Array<{ label: string; value: string }>>(this.buildMonthOptions(24));
  readonly compareMonthOptions = computed(() => [
    { label: 'Aucune comparaison', value: null as string | null },
    ...this.monthOptions()
  ]);

  readonly appTitle = computed(() => this.data()?.appTitle ?? 'PayZen HR');
  readonly appSubtitle = computed(() => this.data()?.appSubtitle ?? 'Dashboards RH');

  readonly vueGlobaleData = computed(() => this.data()?.vueGlobale ?? null);
  readonly mouvementsRhData = computed(() => this.data()?.mouvementsRh ?? null);
  readonly masseSalarialeData = computed(() => this.data()?.masseSalariale ?? null);
  readonly pariteDiversiteData = computed(() => this.data()?.pariteDiversite ?? null);
  readonly conformiteSocialeData = computed(() => this.data()?.conformiteSociale ?? null);

  readonly hasWarnings = computed(() => this.warnings().length > 0);
  readonly loadedAtLabel = computed(() => {
    const value = this.loadedAtIso();
    if (!value) {
      return '';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return value;
    }

    return date.toLocaleString('fr-FR');
  });

  constructor() {
    this.load({ hard: true });

    this.contextService.contextChanged$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.load({ hard: false }));

    // Recompute materialized data snapshot whenever raw or filters change.
    effect(() => {
      const raw = this.rawData();
      const filters = this.filters();
      const fallbackCompareMonth = this.isAllTimeMonth(filters.month) ? null : this.previousYearMonth(filters.month);
      const compareMonth = filters.compareMonth ?? fallbackCompareMonth;
      const compare = compareMonth ? this.compareRawByMonth()[compareMonth] ?? null : null;
      if (!raw) {
        return;
      }
      this.data.set(aggregateDashboardFromRaw(raw, filters, compare));
    }, { allowSignalWrites: true });
  }

  load(options: { hard: boolean }): void {
    if (options.hard) {
      this.isLoading.set(true);
    } else {
      this.isRefreshing.set(true);
    }

    this.error.set(null);

    this.repository
      .getDashboardRawData(this.buildQuery())
      .pipe(take(1))
      .subscribe({
        next: raw => {
          this.rawData.set(raw);
          this.loadedAtIso.set(raw.meta.generatedAt);
          this.warnings.set([]);
          this.dataSource.set('api');
          const optionsSet = extractAvailableFilterOptions(raw, this.filters().month);
          this.availableDepartments.set(optionsSet.departments);
          this.availableContractTypes.set(optionsSet.contractTypes);
          this.syncFilterDefaults(optionsSet.departments, optionsSet.contractTypes);
          this.isLoading.set(false);
          this.isRefreshing.set(false);

          const compareMonth = this.isAllTimeMonth(this.filters().month)
            ? null
            : this.filters().compareMonth ?? this.previousYearMonth(this.filters().month);
          if (compareMonth) {
            this.ensureCompareLoaded(compareMonth);
          }
        },
        error: error => {
          this.repository
            .getDashboardData(this.buildQuery())
            .pipe(take(1))
            .subscribe({
              next: payload => {
                this.data.set(payload.data);
                this.warnings.set([
                  ...payload.meta.warnings,
                  'Raw dataset indisponible, fallback sur payload agrege.',
                  error instanceof Error ? error.message : 'Erreur reseau inconnue.'
                ]);
                this.dataSource.set(payload.meta.source);
                this.loadedAtIso.set(payload.meta.loadedAtIso);
                this.isLoading.set(false);
                this.isRefreshing.set(false);
              },
              error: () => {
                this.error.set('Impossible de charger le dashboard RH dynamique.');
                this.isLoading.set(false);
                this.isRefreshing.set(false);
              }
            });
        }
      });
  }

  setActiveTab(value: string): void {
    if (isDashboardTabId(value)) {
      this.activeTab.set(value);
    }
  }

  setDepartments(values: string[]): void {
    this.filters.update(current => ({ ...current, departments: values ?? [] }));
  }

  setContractTypes(values: string[]): void {
    this.filters.update(current => ({ ...current, contractTypes: values ?? [] }));
  }

  setParity(values: Array<'F' | 'H'>): void {
    this.filters.update(current => ({ ...current, parity: values ?? ['F', 'H'] }));
  }

  setMonth(value: string): void {
    const compare = this.filters().compareMonth;
    const allTime = this.isAllTimeMonth(value);
    this.filters.update(current => ({
      ...current,
      month: value,
      compareMonth: allTime || compare === value ? null : compare
    }));
    this.load({ hard: false });
  }

  setCompareMonth(value: string | null): void {
    this.filters.update(current => ({ ...current, compareMonth: value }));
    if (value) {
      this.ensureCompareLoaded(value);
    }
  }

  resetFilters(): void {
    const departments = this.availableDepartments();
    const contractTypes = this.availableContractTypes();
    const month = this.filters().month;
    this.filters.set({
      departments: [...departments],
      contractTypes: [...contractTypes],
      parity: ['F', 'H'],
      month,
      compareMonth: null
    });
    const compare = this.filters().compareMonth;
    if (compare) {
      this.ensureCompareLoaded(compare);
    }
  }

  private buildQuery(): DashboardHrQuery {
    const selectedMonth = this.filters().month;
    return {
      companyId: this.contextService.companyId(),
      isExpertMode: this.contextService.isExpertMode(),
      isClientView: this.contextService.isClientView(),
      month: this.isAllTimeMonth(selectedMonth) ? null : selectedMonth,
      compareMonth: this.filters().compareMonth
    };
  }

  private syncFilterDefaults(departments: string[], contractTypes: string[]): void {
    this.filters.update(current => ({
      ...current,
      departments: current.departments.length > 0 ? current.departments.filter(item => departments.includes(item)) : [...departments],
      contractTypes: current.contractTypes.length > 0 ? current.contractTypes.filter(item => contractTypes.includes(item)) : [...contractTypes],
      parity: current.parity.length > 0 ? current.parity : ['F', 'H']
    }));
  }

  private ensureCompareLoaded(month: string): void {
    if (this.compareRawByMonth()[month] || !month) {
      return;
    }

    this.repository
      .getDashboardRawData({
        ...this.buildQuery(),
        month
      })
      .pipe(take(1))
      .subscribe({
        next: raw => {
          this.compareRawByMonth.update(current => ({ ...current, [month]: raw }));
        },
        error: () => {
          this.warnings.update(items => [...items, `Comparaison ${month} indisponible.`]);
        }
      });
  }

  private defaultMonth(): string {
    const now = new Date();
    return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
  }

  private previousYearMonth(month: string): string {
    const [yRaw, mRaw] = month.split('-').map(Number);
    const date = Number.isFinite(yRaw) && Number.isFinite(mRaw) ? new Date(yRaw - 1, mRaw - 1, 1) : new Date();
    return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
  }

  private isAllTimeMonth(month: string): boolean {
    return month === 'all';
  }

  private buildMonthOptions(count: number): Array<{ label: string; value: string }> {
    const base = new Date();
    const labels = ['Jan', 'Fev', 'Mar', 'Avr', 'Mai', 'Juin', 'Juil', 'Aout', 'Sep', 'Oct', 'Nov', 'Dec'];
    const values: Array<{ label: string; value: string }> = [
      { value: 'all', label: 'Toutes periodes' }
    ];

    for (let i = 0; i < count; i += 1) {
      const date = new Date(base.getFullYear(), base.getMonth() - i, 1);
      const month = `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
      values.push({
        value: month,
        label: `${labels[date.getMonth()]} ${date.getFullYear()}`
      });
    }

    return values;
  }
}
