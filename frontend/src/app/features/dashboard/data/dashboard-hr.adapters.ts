import {
  BarChartConfig,
  DashboardHrData,
  KpiMetric,
  ProgressRowModel,
  StatusPill
} from '../state/dashboard-hr.models';

export interface DashboardEmployeeSummaryItemApi {
  id: string | number;
  firstName: string;
  lastName: string;
  position?: string;
  department?: string;
  status?: string;
  statuses?: string;
  startDate?: string;
  contractType?: string;
  manager?: string | null;
}

export interface DashboardEmployeeSummaryApi {
  totalEmployees?: number;
  activeEmployees?: number;
  employees?: DashboardEmployeeSummaryItemApi[];
}

export interface DashboardEmployeeBasicApi {
  id: number;
  genderId?: number | null;
}

export interface DashboardEmployeeSalaryApi {
  id: number;
  employeeId: number;
  baseSalary: number;
  effectiveDate: string;
  endDate?: string | null;
}

export interface DashboardFormDataApi {
  genders?: Array<{
    id: number;
    name?: string;
    nameFr?: string;
    nameEn?: string;
    code?: string;
  }>;
}

export interface DashboardLiveAdapterInput {
  summary: DashboardEmployeeSummaryApi;
  employeeBasics: DashboardEmployeeBasicApi[];
  salaries: DashboardEmployeeSalaryApi[];
  formData?: DashboardFormDataApi;
  now: Date;
}

interface NormalizedEmployee {
  id: number;
  firstName: string;
  lastName: string;
  fullName: string;
  position: string;
  department: string;
  statusRaw: string;
  startDate: Date | null;
  contractType: string;
}

const ACTIVE_STATUSES = new Set(['active', 'actif', 'enabled']);
const INACTIVE_STATUSES = new Set(['inactive', 'inactif', 'resigned', 'terminated', 'departed', 'left']);

const DEPARTMENT_COLORS = ['#2563eb', '#22c55e', '#f97316', '#6366f1', '#7c3aed', '#0ea5e9'];

function toSafeNumber(value: unknown, fallback = 0): number {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
}

function parseDate(value: string | null | undefined): Date | null {
  if (!value) {
    return null;
  }

  const parsed = new Date(value);
  return Number.isNaN(parsed.getTime()) ? null : parsed;
}

function monthKey(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  return `${y}-${m}`;
}

function toMonthLabel(date: Date): string {
  return date.toLocaleString('fr-FR', { month: 'short' }).replace('.', '');
}

function round1(value: number): number {
  return Math.round(value * 10) / 10;
}

function formatK(value: number): string {
  if (!Number.isFinite(value)) {
    return '0 K';
  }

  return `${round1(value / 1000)} K`;
}

function formatPct(value: number): string {
  return `${round1(value)}%`;
}

function normalizeEmployees(summary: DashboardEmployeeSummaryApi): NormalizedEmployee[] {
  const rawEmployees = summary.employees ?? [];

  return rawEmployees.map(item => {
    const id = toSafeNumber(item.id);
    const firstName = item.firstName ?? '';
    const lastName = item.lastName ?? '';

    return {
      id,
      firstName,
      lastName,
      fullName: `${firstName} ${lastName}`.trim(),
      position: item.position ?? 'Non assigne',
      department: item.department ?? 'Autres',
      statusRaw: String(item.statuses ?? item.status ?? '').trim().toLowerCase(),
      startDate: parseDate(item.startDate),
      contractType: item.contractType ?? 'N/A'
    };
  });
}

function getMonthRange(now: Date, count: number): Date[] {
  const result: Date[] = [];

  for (let index = count - 1; index >= 0; index -= 1) {
    result.push(new Date(now.getFullYear(), now.getMonth() - index, 1));
  }

  return result;
}

function buildEffectifSeries(employees: NormalizedEmployee[], totalEmployees: number, now: Date): BarChartConfig {
  const months = getMonthRange(now, 6);
  const hiresPerMonth = new Map<string, number>();

  for (const employee of employees) {
    if (!employee.startDate) {
      continue;
    }

    const key = monthKey(employee.startDate);
    hiresPerMonth.set(key, (hiresPerMonth.get(key) ?? 0) + 1);
  }

  const values = months.map((month, monthIndex) => {
    let hiresAfter = 0;

    for (let cursor = monthIndex + 1; cursor < months.length; cursor += 1) {
      hiresAfter += hiresPerMonth.get(monthKey(months[cursor])) ?? 0;
    }

    return Math.max(totalEmployees - hiresAfter, 0);
  });

  return {
    labels: months.map(toMonthLabel),
    values,
    datasetLabel: 'Effectif',
    color: '#2563eb',
    highlightLast: false,
    ySuggestedMax: 90,
    yTickStep: 10
  };
}

function groupByDepartment(employees: NormalizedEmployee[]): Array<{ department: string; count: number }> {
  const counts = new Map<string, number>();

  for (const employee of employees) {
    counts.set(employee.department, (counts.get(employee.department) ?? 0) + 1);
  }

  return [...counts.entries()]
    .map(([department, count]) => ({ department, count }))
    .sort((a, b) => b.count - a.count);
}

function resolveGenderIds(formData?: DashboardFormDataApi): { femaleIds: Set<number>; maleIds: Set<number> } {
  const femaleIds = new Set<number>();
  const maleIds = new Set<number>();

  for (const gender of formData?.genders ?? []) {
    const tokens = [gender.name, gender.nameFr, gender.nameEn, gender.code]
      .filter(Boolean)
      .join(' ')
      .toLowerCase();

    if (tokens.includes('fem') || tokens.includes('female') || tokens.includes('woman')) {
      femaleIds.add(gender.id);
    }

    if (tokens.includes('mas') || tokens.includes('male') || tokens.includes('man') || tokens.includes('hom')) {
      maleIds.add(gender.id);
    }
  }

  return { femaleIds, maleIds };
}

function splitGenderCounts(
  employees: NormalizedEmployee[],
  employeeBasics: DashboardEmployeeBasicApi[],
  formData?: DashboardFormDataApi
): {
  femaleCount: number;
  maleCount: number;
  knownCount: number;
  femaleByDepartment: Map<string, number>;
  maleByDepartment: Map<string, number>;
  femaleByHierarchy: Map<string, number>;
  totalByHierarchy: Map<string, number>;
} {
  const basicsById = new Map<number, DashboardEmployeeBasicApi>();

  for (const basic of employeeBasics) {
    basicsById.set(basic.id, basic);
  }

  const { femaleIds, maleIds } = resolveGenderIds(formData);

  const femaleByDepartment = new Map<string, number>();
  const maleByDepartment = new Map<string, number>();
  const femaleByHierarchy = new Map<string, number>();
  const totalByHierarchy = new Map<string, number>();

  let femaleCount = 0;
  let maleCount = 0;
  let knownCount = 0;

  for (const employee of employees) {
    const detail = basicsById.get(employee.id);
    const genderId = detail?.genderId ?? null;
    const hierarchy = classifyHierarchy(employee.position);

    totalByHierarchy.set(hierarchy, (totalByHierarchy.get(hierarchy) ?? 0) + 1);

    if (genderId == null) {
      continue;
    }

    if (femaleIds.has(genderId)) {
      femaleCount += 1;
      knownCount += 1;
      femaleByDepartment.set(employee.department, (femaleByDepartment.get(employee.department) ?? 0) + 1);
      femaleByHierarchy.set(hierarchy, (femaleByHierarchy.get(hierarchy) ?? 0) + 1);
      continue;
    }

    if (maleIds.has(genderId)) {
      maleCount += 1;
      knownCount += 1;
      maleByDepartment.set(employee.department, (maleByDepartment.get(employee.department) ?? 0) + 1);
    }
  }

  return {
    femaleCount,
    maleCount,
    knownCount,
    femaleByDepartment,
    maleByDepartment,
    femaleByHierarchy,
    totalByHierarchy
  };
}

function classifyHierarchy(position: string): string {
  const value = position.toLowerCase();

  if (value.includes('direction') || value.includes('director') || value.includes('directeur')) {
    return 'Direction';
  }

  if (value.includes('manager') || value.includes('chef') || value.includes('lead')) {
    return 'Managers';
  }

  if (
    value.includes('dev') ||
    value.includes('engineer') ||
    value.includes('analyst') ||
    value.includes('finance') ||
    value.includes('control') ||
    value.includes('qa')
  ) {
    return 'Cadres';
  }

  return 'Employes';
}

function toStatus(rawStatus: string): 'active' | 'inactive' | 'other' {
  if (ACTIVE_STATUSES.has(rawStatus)) {
    return 'active';
  }

  if (INACTIVE_STATUSES.has(rawStatus)) {
    return 'inactive';
  }

  if (rawStatus.includes('active') && !rawStatus.startsWith('in')) {
    return 'active';
  }

  if (rawStatus.includes('leave') || rawStatus.includes('cong')) {
    return 'other';
  }

  if (rawStatus) {
    return 'inactive';
  }

  return 'other';
}

function isSalaryActiveForMonth(salary: DashboardEmployeeSalaryApi, monthStart: Date, monthEnd: Date): boolean {
  const effectiveDate = parseDate(salary.effectiveDate);
  const endDate = parseDate(salary.endDate ?? null);

  if (!effectiveDate) {
    return false;
  }

  if (effectiveDate > monthEnd) {
    return false;
  }

  if (endDate && endDate < monthStart) {
    return false;
  }

  return true;
}

function buildSalaryLookup(
  salaries: DashboardEmployeeSalaryApi[],
  employeeIds: Set<number>,
  monthStart: Date,
  monthEnd: Date
): Map<number, number> {
  const filtered = salaries.filter(salary => employeeIds.has(salary.employeeId));
  const grouped = new Map<number, DashboardEmployeeSalaryApi[]>();

  for (const salary of filtered) {
    const list = grouped.get(salary.employeeId) ?? [];
    list.push(salary);
    grouped.set(salary.employeeId, list);
  }

  const result = new Map<number, number>();

  for (const [employeeId, employeeSalaries] of grouped.entries()) {
    const matches = employeeSalaries
      .filter(salary => isSalaryActiveForMonth(salary, monthStart, monthEnd))
      .sort((a, b) => {
        const aDate = parseDate(a.effectiveDate)?.getTime() ?? 0;
        const bDate = parseDate(b.effectiveDate)?.getTime() ?? 0;
        return bDate - aDate;
      });

    if (matches.length > 0) {
      result.set(employeeId, toSafeNumber(matches[0].baseSalary));
    }
  }

  return result;
}

function buildSalarySeries(
  salaries: DashboardEmployeeSalaryApi[],
  employees: NormalizedEmployee[],
  now: Date
): { config: BarChartConfig; currentGross: number; monthlyGross: number[]; byEmployeeCurrent: Map<number, number> } {
  const months = getMonthRange(now, 12);
  const employeeIds = new Set<number>(employees.map(employee => employee.id));

  const monthlyGross = months.map(month => {
    const monthStart = new Date(month.getFullYear(), month.getMonth(), 1);
    const monthEnd = new Date(month.getFullYear(), month.getMonth() + 1, 0);
    const monthlyLookup = buildSalaryLookup(salaries, employeeIds, monthStart, monthEnd);

    let total = 0;

    for (const value of monthlyLookup.values()) {
      total += value;
    }

    return total;
  });

  const currentMonthStart = new Date(now.getFullYear(), now.getMonth(), 1);
  const currentMonthEnd = new Date(now.getFullYear(), now.getMonth() + 1, 0);
  const byEmployeeCurrent = buildSalaryLookup(salaries, employeeIds, currentMonthStart, currentMonthEnd);

  let currentGross = 0;
  for (const value of byEmployeeCurrent.values()) {
    currentGross += value;
  }

  const valuesInK = monthlyGross.map(value => Math.round(value / 1000));
  const maxK = valuesInK.length ? Math.max(...valuesInK) : 1;

  return {
    config: {
      labels: months.map(toMonthLabel),
      values: valuesInK,
      datasetLabel: 'Masse salariale brute',
      color: '#14b8a6',
      highlightLast: false,
      suffix: 'K MAD',
      ySuggestedMax: Math.max(100, Math.ceil(maxK / 20) * 20),
      yTickStep: 20
    },
    currentGross,
    monthlyGross,
    byEmployeeCurrent
  };
}

function buildMovementHistory(employees: NormalizedEmployee[]): {
  summary: KpiMetric[];
  rows: Array<{
    employe: string;
    departement: string;
    poste: string;
    type: string;
    date: string;
    motifNote: string;
    mouvement: StatusPill;
  }>;
} {
  const datedEmployees = employees.filter(employee => employee.startDate !== null);
  const latestStartDate = datedEmployees
    .map(employee => employee.startDate as Date)
    .sort((a, b) => b.getTime() - a.getTime())[0] ?? new Date();

  const latestYear = latestStartDate.getFullYear();
  const latestMonth = latestStartDate.getMonth();

  const entriesThisMonth = datedEmployees.filter(employee => {
    const date = employee.startDate as Date;
    return date.getFullYear() === latestYear && date.getMonth() === latestMonth;
  });

  const inactiveEmployees = employees.filter(employee => toStatus(employee.statusRaw) === 'inactive');

  const sortiesThisMonth = inactiveEmployees.slice(0, Math.max(1, Math.floor(inactiveEmployees.length * 0.3)));

  const entryRows = entriesThisMonth.map(employee => ({
    employe: employee.fullName,
    departement: employee.department,
    poste: employee.position,
    type: employee.contractType,
    date: employee.startDate ? employee.startDate.toLocaleDateString('fr-FR') : '-',
    motifNote: 'Recrutement',
    mouvement: { label: 'Entree', severity: 'success' as const }
  }));

  const sortieRows = sortiesThisMonth.map(employee => ({
    employe: employee.fullName,
    departement: employee.department,
    poste: employee.position,
    type: employee.contractType,
    date: '-',
    motifNote: 'Statut inactif detecte',
    mouvement: { label: 'Sortie', severity: 'danger' as const }
  }));

  const rows = [...entryRows, ...sortieRows].slice(0, 12);

  const entries = entryRows.length;
  const sorties = sortieRows.length;
  const net = entries - sorties;
  const retention = employees.length > 0 ? ((employees.length - sorties) / employees.length) * 100 : 0;

  return {
    summary: [
      { label: 'Entrees ce mois', value: `+${entries}`, subLabel: `Nouveaux contrats: ${entries}`, accent: 'success' },
      { label: 'Sorties ce mois', value: `-${sorties}`, subLabel: 'Sorties estimees a partir des statuts', accent: 'danger' },
      { label: 'Solde net', value: net >= 0 ? `+${net}` : `${net}`, subLabel: `Taux de retention: ${formatPct(retention)}`, accent: net >= 0 ? 'success' : 'danger' }
    ],
    rows
  };
}

export function buildDashboardFromLiveData(input: DashboardLiveAdapterInput): { data: DashboardHrData; warnings: string[] } {
  const warnings: string[] = [];

  const employees = normalizeEmployees(input.summary);
  const totalEmployees = toSafeNumber(input.summary.totalEmployees, employees.length);
  const activeEmployees = toSafeNumber(
    input.summary.activeEmployees,
    employees.filter(employee => toStatus(employee.statusRaw) === 'active').length
  );

  const departmentGroups = groupByDepartment(employees);
  const donutSlices = departmentGroups.map((group, index) => ({
    label: group.department,
    value: group.count,
    color: DEPARTMENT_COLORS[index % DEPARTMENT_COLORS.length]
  }));

  const effectifSeries = buildEffectifSeries(employees, totalEmployees, input.now);
  const movement = buildMovementHistory(employees);

  const salarySeries = buildSalarySeries(input.salaries, employees, input.now);

  if (salarySeries.currentGross <= 0) {
    warnings.push('Aucune masse salariale exploitable depuis API.');
  }

  const gross = salarySeries.currentGross;
  const net = gross * 0.772;
  const employerCharges = gross * 0.216;
  const totalCost = gross + employerCharges;

  const currentSalaryByDepartment = new Map<string, number>();

  for (const employee of employees) {
    const employeeSalary = salarySeries.byEmployeeCurrent.get(employee.id) ?? 0;
    currentSalaryByDepartment.set(
      employee.department,
      (currentSalaryByDepartment.get(employee.department) ?? 0) + employeeSalary
    );
  }

  const salaryDepartmentRows: ProgressRowModel[] = departmentGroups.map((department, index) => {
    const amount = currentSalaryByDepartment.get(department.department) ?? 0;
    const ratio = gross > 0 ? (amount / gross) * 100 : 0;

    return {
      label: `${department.department} (${department.count} pers.)`,
      rightLabel: `${formatK(amount)} MAD - ${Math.round(ratio)}%`,
      percent: Math.round(ratio),
      color: DEPARTMENT_COLORS[index % DEPARTMENT_COLORS.length]
    };
  });

  const genderStats = splitGenderCounts(employees, input.employeeBasics, input.formData);

  if (genderStats.knownCount === 0) {
    warnings.push('Parite non disponible: genre absent sur les donnees employees.');
  }

  const femaleCount = genderStats.femaleCount;
  const maleCount = genderStats.maleCount;
  const knownGenderCount = Math.max(genderStats.knownCount, 1);
  const femalePct = (femaleCount / knownGenderCount) * 100;
  const malePct = (maleCount / knownGenderCount) * 100;

  const parityDeptRows: ProgressRowModel[] = departmentGroups.map((group, index) => {
    const female = genderStats.femaleByDepartment.get(group.department) ?? 0;
    const male = genderStats.maleByDepartment.get(group.department) ?? 0;
    const totalKnown = Math.max(female + male, 1);

    return {
      label: group.department,
      rightLabel: `${female}F / ${male}H`,
      percent: Math.round((female / totalKnown) * 100),
      color: '#7c3aed'
    };
  });

  const hierarchyOrder = ['Direction', 'Managers', 'Cadres', 'Employes'] as const;
  const hierarchyDisplayLabel = (key: string): string =>
    key === 'Employes' ? 'Employés' : key;

  const hierarchyRows: ProgressRowModel[] = hierarchyOrder
    .map((label, index) => {
      const total = genderStats.totalByHierarchy.get(label) ?? 0;
      if (total === 0) {
        return null;
      }

      const female = genderStats.femaleByHierarchy.get(label) ?? 0;
      const percent = Math.round((female / Math.max(total, 1)) * 100);
      const colors = ['#ef4444', '#f97316', '#22c55e', '#14b8a6'];

      return {
        label: `${hierarchyDisplayLabel(label)} (${total})`,
        rightLabel: `${percent}% F`,
        percent,
        color: colors[index]
      };
    })
    .filter((value): value is ProgressRowModel => value !== null);

  const inactiveEmployees = employees.filter(employee => toStatus(employee.statusRaw) === 'inactive').length;
  const turnover = totalEmployees > 0 ? (inactiveEmployees / totalEmployees) * 100 : 0;

  const now = input.now;
  const monthLabel = now.toLocaleString('fr-FR', { month: 'long', year: 'numeric' });

  const cnssSalariale = gross * 0.0429;
  const cnssPatronale = gross * 0.2109;
  const amoSalariale = gross * 0.0226;
  const irSource = gross * 0.161;

  const conformityRows = [
    {
      declaration: `Bordereau CNSS - ${monthLabel}`,
      montantMad: Math.round(cnssSalariale + cnssPatronale).toLocaleString('fr-FR'),
      echeance: new Date(now.getFullYear(), now.getMonth() + 1, 28).toLocaleDateString('fr-FR'),
      statut: { label: 'En attente', severity: 'warn' as const },
      reference: `CNSS-${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
    },
    {
      declaration: `AMO employeur - ${monthLabel}`,
      montantMad: Math.round(amoSalariale).toLocaleString('fr-FR'),
      echeance: new Date(now.getFullYear(), now.getMonth() + 1, 28).toLocaleDateString('fr-FR'),
      statut: { label: 'En attente', severity: 'warn' as const },
      reference: `AMO-${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
    },
    {
      declaration: `Versement IR DGI - ${monthLabel}`,
      montantMad: Math.round(irSource).toLocaleString('fr-FR'),
      echeance: new Date(now.getFullYear(), now.getMonth() + 1, 31).toLocaleDateString('fr-FR'),
      statut: { label: 'A verifier', severity: 'info' as const },
      reference: `IR-${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
    }
  ];

  const data: DashboardHrData = {
    appTitle: 'PayZen HR - Dashboard RH',
    appSubtitle: `Donnees dynamiques - ${totalEmployees} employes - ${monthLabel}`,
    vueGlobale: {
      meta: {
        title: 'Vue Globale RH',
        badge: 'Live',
        subtitle: `KPIs instantanés - ${monthLabel}`,
        icon: 'pi pi-home'
      },
      kpis: [
        {
          label: 'Effectif total',
          value: String(totalEmployees),
          subLabel: `${activeEmployees} actifs`,
          trend: {
            value: `${effectifSeries.values[effectifSeries.values.length - 1] - effectifSeries.values[effectifSeries.values.length - 2] >= 0 ? '+' : ''}${
              effectifSeries.values[effectifSeries.values.length - 1] - effectifSeries.values[effectifSeries.values.length - 2]
            }`,
            direction:
              effectifSeries.values[effectifSeries.values.length - 1] - effectifSeries.values[effectifSeries.values.length - 2] >= 0
                ? 'up'
                : 'down'
          }
        },
        {
          label: 'Masse salariale',
          value: formatK(gross),
          subLabel: 'MAD / mois',
          trend: {
            value: formatPct(
              salarySeries.monthlyGross[salarySeries.monthlyGross.length - 2] > 0
                ? ((salarySeries.monthlyGross[salarySeries.monthlyGross.length - 1] -
                    salarySeries.monthlyGross[salarySeries.monthlyGross.length - 2]) /
                    salarySeries.monthlyGross[salarySeries.monthlyGross.length - 2]) *
                    100
                : 0
            ),
            direction:
              salarySeries.monthlyGross[salarySeries.monthlyGross.length - 1] >=
              salarySeries.monthlyGross[salarySeries.monthlyGross.length - 2]
                ? 'up'
                : 'down'
          }
        },
        {
          label: 'Turnover (12M)',
          value: formatPct(turnover),
          subLabel: 'Base statuts employes',
          trend: {
            value: inactiveEmployees > 0 ? `${inactiveEmployees} inactifs` : 'stable',
            direction: inactiveEmployees > 0 ? 'down' : 'flat'
          }
        },
        {
          label: 'Parité F/H',
          value: `${Math.round(femalePct)} / ${Math.round(malePct)}`,
          subLabel: '% Femmes / Hommes',
          trend: {
            value: 'stable',
            direction: 'flat'
          }
        }
      ],
      effectifEvolution: effectifSeries,
      repartitionDepartement: {
        centerLabel: String(totalEmployees),
        slices: donutSlices
      }
    },
    mouvementsRh: {
      meta: {
        title: 'Mouvements RH',
        badge: 'Live',
        subtitle: `Historique des entrées et sorties — ${monthLabel}`,
        icon: 'pi pi-refresh'
      },
      summary: movement.summary,
      history: movement.rows
    },
    masseSalariale: {
      meta: {
        title: 'Masse salariale',
        badge: 'Live',
        subtitle: `Analyse des coûts salariaux — ${monthLabel}`,
        icon: 'pi pi-wallet'
      },
      kpis: [
        { label: 'Brut total', value: formatK(gross), subLabel: 'MAD' },
        { label: 'Net total versé', value: formatK(net), subLabel: 'MAD après retenues' },
        { label: 'Charges patronales', value: formatK(employerCharges), subLabel: 'CNSS + AMO employeur' },
        { label: 'Coût total employeur', value: formatK(totalCost), subLabel: 'MAD / mois' }
      ],
      masseBrute12Mois: salarySeries.config,
      repartitionDepartement: salaryDepartmentRows
    },
    pariteDiversite: {
      meta: {
        title: 'Parité & diversité',
        badge: 'Live',
        subtitle: `Indicateurs d’équilibre — ${monthLabel}`,
        icon: 'pi pi-balance-scale'
      },
      kpis: [
        {
          label: 'Effectif femmes',
          value: String(femaleCount),
          subLabel: `${formatPct(femalePct)} de l’effectif connu`,
          accent: 'purple'
        },
        {
          label: 'Effectif hommes',
          value: String(maleCount),
          subLabel: `${formatPct(malePct)} de l’effectif connu`,
          accent: 'blue'
        },
        {
          label: 'Écart salarial moyen',
          value: 'N/A',
          subLabel: 'Nécessite un endpoint rémunération comparée',
          accent: 'danger'
        }
      ],
      pariteDepartement: parityDeptRows,
      pariteNiveauHierarchique: hierarchyRows
    },
    conformiteSociale: {
      meta: {
        title: 'Conformité sociale',
        badge: 'Live',
        subtitle: `Estimations CNSS / AMO / IR — ${monthLabel}`,
        icon: 'pi pi-check-circle'
      },
      kpis: [
        { label: 'CNSS salariale', value: formatK(cnssSalariale), subLabel: 'MAD — taux 4,29 %' },
        { label: 'CNSS patronale', value: formatK(cnssPatronale), subLabel: 'MAD — taux 21,09 %' },
        { label: 'AMO (salariale)', value: formatK(amoSalariale), subLabel: 'MAD — taux 2,26 %' },
        { label: 'IR retenu à la source', value: formatK(irSource), subLabel: 'MAD — estimation' }
      ],
      declarations: conformityRows
    }
  };

  if (data.pariteDiversite.pariteDepartement.length === 0) {
    warnings.push('Parite par departement indisponible.');
  }

  return { data, warnings };
}
