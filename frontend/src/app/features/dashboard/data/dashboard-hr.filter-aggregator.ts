import {
  DashboardFilterState,
  DashboardHrData,
  DashboardParityFilter,
  KpiMetric,
  ProgressRowModel,
  StatusPill
} from '../state/dashboard-hr.models';
import { DashboardHrRawContract, DashboardHrRawData, DashboardHrRawSalary } from './dashboard-hr-raw.models';

const DEPARTMENT_COLORS = ['#2563eb', '#22c55e', '#f97316', '#6366f1', '#7c3aed', '#0ea5e9'];
const HIERARCHY_COLORS = ['#ef4444', '#f97316', '#22c55e', '#14b8a6', '#6366f1'];

interface EnrichedEmployee {
  id: number;
  firstName: string;
  lastName: string;
  fullName: string;
  department: string;
  statusCode: string;
  genderCode: string;
  position: string;
  contractType: string;
}

function normalizeStatusToken(value: string): string {
  return (value || '').toLowerCase().replace(/[^a-z0-9]+/g, '');
}

function isActiveOrOnLeaveStatus(statusCode: string): boolean {
  const normalized = normalizeStatusToken(statusCode);

  if (!normalized) {
    return false;
  }

  // Reject inactive-like statuses first to avoid false positives like "inactif" containing "actif".
  if (
    normalized.includes('inactive') ||
    normalized.includes('inactif') ||
    normalized.includes('resign') ||
    normalized.includes('terminated') ||
    normalized.includes('departed') ||
    normalized.includes('left') ||
    normalized.includes('suspended') ||
    normalized.includes('retired') ||
    normalized.includes('archive')
  ) {
    return false;
  }

  return (
    normalized === 'active' ||
    normalized === 'actif' ||
    normalized === 'enabled' ||
    normalized.startsWith('active') ||
    normalized.startsWith('actif') ||
    normalized === 'onleave' ||
    normalized === 'onconge' ||
    normalized === 'statusleave' ||
    normalized.includes('onleave') ||
    normalized.includes('conge') ||
    normalized.includes('leave')
  );
}

function isAllTimeMonth(month: string): boolean {
  return month === 'all';
}

function round1(value: number): number {
  return Math.round(value * 10) / 10;
}

function formatPct(value: number): string {
  return `${round1(value)}%`;
}

function formatK(value: number): string {
  const inK = round1((value || 0) / 1000);
  return Number.isInteger(inK) ? `${inK.toFixed(0)} K` : `${inK.toFixed(1)} K`;
}

function formatMad(value: number): string {
  return Math.round(value || 0).toLocaleString('fr-FR');
}

function parseDate(value: string | null | undefined): Date | null {
  if (!value) {
    return null;
  }

  const parsed = new Date(`${value}T00:00:00`);
  return Number.isNaN(parsed.getTime()) ? null : parsed;
}

function parseMonthToDate(month: string): Date {
  const [y, m] = month.split('-').map(Number);
  if (!Number.isFinite(y) || !Number.isFinite(m) || m < 1 || m > 12) {
    const now = new Date();
    return new Date(now.getFullYear(), now.getMonth(), 1);
  }

  return new Date(y, m - 1, 1);
}

function toMonthLabelShort(month: string): string {
  const months = ['Jan', 'Fev', 'Mar', 'Avr', 'Mai', 'Juin', 'Juil', 'Aout', 'Sep', 'Oct', 'Nov', 'Dec'];
  const m = Number(month.split('-')[1]);
  return Number.isFinite(m) && m >= 1 && m <= 12 ? months[m - 1] : month;
}

function toMonthLabelLong(month: string): string {
  const months = ['Janvier', 'Fevrier', 'Mars', 'Avril', 'Mai', 'Juin', 'Juillet', 'Aout', 'Septembre', 'Octobre', 'Novembre', 'Decembre'];
  const [yRaw, mRaw] = month.split('-');
  const y = Number(yRaw);
  const m = Number(mRaw);
  if (!Number.isFinite(y) || !Number.isFinite(m) || m < 1 || m > 12) {
    return month;
  }

  return `${months[m - 1]} ${y}`;
}

function toFrDate(dateIso: string): string {
  const parsed = parseDate(dateIso);
  return parsed ? parsed.toLocaleDateString('fr-FR') : '-';
}

function monthKey(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`;
}

function getMonthRange(baseMonth: string, count: number): string[] {
  const base = parseMonthToDate(baseMonth);
  const result: string[] = [];

  for (let i = count - 1; i >= 0; i -= 1) {
    const d = new Date(base.getFullYear(), base.getMonth() - i, 1);
    result.push(monthKey(d));
  }

  return result;
}

function monthStart(month: string): Date {
  return parseMonthToDate(month);
}

function monthEnd(month: string): Date {
  const start = monthStart(month);
  return new Date(start.getFullYear(), start.getMonth() + 1, 0);
}

function isSalaryActive(salary: DashboardHrRawSalary, month: string): boolean {
  const start = monthStart(month);
  const end = monthEnd(month);
  const effective = parseDate(salary.effectiveDate);
  const salaryEnd = parseDate(salary.endDate);

  if (!effective || effective > end) {
    return false;
  }

  if (salaryEnd && salaryEnd < start) {
    return false;
  }

  return true;
}

function statusPill(kind: 'entry' | 'exit'): StatusPill {
  return kind === 'entry'
    ? { label: 'Entree', severity: 'success' }
    : { label: 'Sortie', severity: 'danger' };
}

function declarationStatus(label: string): StatusPill {
  if (label === 'Soumis') {
    return { label, severity: 'success' };
  }
  if (label === 'En retard') {
    return { label, severity: 'danger' };
  }
  return { label, severity: 'warn' };
}

function resolveGenderBucket(code: string): DashboardParityFilter | null {
  const c = (code || '').toLowerCase();
  if (c.startsWith('f') || c.includes('fem') || c.includes('female') || c.includes('woman')) {
    return 'F';
  }
  if (c.startsWith('m') || c.includes('mas') || c.includes('male') || c.includes('man') || c.includes('hom')) {
    return 'H';
  }
  return null;
}

function classifyHierarchy(position: string): string {
  const value = (position || '').toLowerCase();
  if (value.includes('direction') || value.includes('directeur') || value.includes('director')) return 'Direction';
  if (value.includes('manager') || value.includes('chef') || value.includes('lead')) return 'Managers';
  if (value.includes('dev') || value.includes('engineer') || value.includes('analyst') || value.includes('finance') || value.includes('qa') || value.includes('rh')) return 'Cadres';
  return 'Employes';
}

function activeContractAtMonthEnd(contracts: DashboardHrRawContract[], selectedMonth: string): Map<number, DashboardHrRawContract> {
  if (isAllTimeMonth(selectedMonth)) {
    const map = new Map<number, DashboardHrRawContract>();
    for (const contract of contracts) {
      const cStart = parseDate(contract.startDate);
      if (!cStart) {
        continue;
      }
      const existing = map.get(contract.employeeId);
      const existingStart = parseDate(existing?.startDate)?.getTime() ?? 0;
      if (!existing || cStart.getTime() >= existingStart) {
        map.set(contract.employeeId, contract);
      }
    }
    return map;
  }

  const end = monthEnd(selectedMonth);
  const map = new Map<number, DashboardHrRawContract>();

  for (const contract of contracts) {
    const cStart = parseDate(contract.startDate);
    const cEnd = parseDate(contract.endDate);
    if (!cStart || cStart > end || (cEnd && cEnd < end)) {
      continue;
    }

    const existing = map.get(contract.employeeId);
    if (!existing || parseDate(existing.startDate)! < cStart) {
      map.set(contract.employeeId, contract);
    }
  }

  return map;
}

function buildEnrichedEmployees(raw: DashboardHrRawData, selectedMonth: string): EnrichedEmployee[] {
  const contractsByEmployee = activeContractAtMonthEnd(raw.contracts, selectedMonth);

  return raw.employees.map(employee => {
    const contract = contractsByEmployee.get(employee.id);
    return {
      id: employee.id,
      firstName: employee.firstName,
      lastName: employee.lastName,
      fullName: `${employee.firstName} ${employee.lastName}`.trim(),
      department: employee.department || 'Autres',
      statusCode: employee.statusCode || '',
      genderCode: employee.genderCode || '',
      position: contract?.position || 'Non assigne',
      contractType: contract?.contractType || 'N/A'
    };
  });
}

function applyFilters(employees: EnrichedEmployee[], filters: DashboardFilterState): EnrichedEmployee[] {
  const includesBothParity = filters.parity.includes('F') && filters.parity.includes('H');

  return employees.filter(employee => {
    if (!isActiveOrOnLeaveStatus(employee.statusCode)) {
      return false;
    }

    if (filters.departments.length > 0 && !filters.departments.includes(employee.department)) {
      return false;
    }
    if (filters.contractTypes.length > 0 && !filters.contractTypes.includes(employee.contractType)) {
      return false;
    }

    const bucket = resolveGenderBucket(employee.genderCode);
    if (filters.parity.length > 0 && bucket && !filters.parity.includes(bucket)) {
      return false;
    }
    if (filters.parity.length > 0 && bucket === null) {
      // Keep unknown gender rows when both F/H are selected (default parity state).
      return includesBothParity;
    }

    return true;
  });
}

function salaryByEmployeeForMonth(raw: DashboardHrRawData, filteredEmployeeIds: Set<number>, month: string): Map<number, number> {
  const map = new Map<number, number>();

  for (const salary of raw.salaries) {
    if (!filteredEmployeeIds.has(salary.employeeId)) {
      continue;
    }
    if (!isAllTimeMonth(month) && !isSalaryActive(salary, month)) continue;
    const existing = map.get(salary.employeeId);
    const currentDate = parseDate(salary.effectiveDate)?.getTime() ?? 0;
    if (existing === undefined) {
      map.set(salary.employeeId, salary.baseSalary || 0);
      continue;
    }
    const existingCandidate = raw.salaries.find(s => s.employeeId === salary.employeeId && (s.baseSalary || 0) === existing);
    const existingDate = parseDate(existingCandidate?.effectiveDate)?.getTime() ?? 0;
    if (currentDate >= existingDate) {
      map.set(salary.employeeId, salary.baseSalary || 0);
    }
  }

  return map;
}

function buildTrendFromDelta(delta: number, suffix = ''): { value: string; direction: 'up' | 'down' | 'flat' } {
  const direction: 'up' | 'down' | 'flat' = delta > 0 ? 'up' : delta < 0 ? 'down' : 'flat';
  const absolute = Math.abs(round1(delta));
  const signed = `${delta > 0 ? '+' : delta < 0 ? '-' : ''}${absolute}${suffix}`;
  return { value: signed, direction };
}

function buildTurnoverTrend(current: number, compare: number | null): { value: string; direction: 'up' | 'down' | 'flat'; context?: string } {
  if (compare === null) {
    return { value: 'stable', direction: 'flat' };
  }

  const delta = round1(current - compare);
  return {
    value: `${delta > 0 ? '+' : ''}${delta}%`,
    direction: delta > 0 ? 'up' : delta < 0 ? 'down' : 'flat',
    context: 'vs N-1'
  };
}

export function extractAvailableFilterOptions(raw: DashboardHrRawData, month: string): { departments: string[]; contractTypes: string[] } {
  const departments = [...new Set(raw.employees.map(employee => employee.department || 'Autres'))].sort((a, b) => a.localeCompare(b));
  const contractTypes = isAllTimeMonth(month)
    ? [...new Set(raw.contracts.map(contract => contract.contractType || 'N/A'))].sort((a, b) => a.localeCompare(b))
    : [...new Set(buildEnrichedEmployees(raw, month).map(employee => employee.contractType))].sort((a, b) => a.localeCompare(b));
  return { departments, contractTypes };
}

export function aggregateDashboardFromRaw(
  raw: DashboardHrRawData,
  filters: DashboardFilterState,
  compareRaw?: DashboardHrRawData | null
): DashboardHrData {
  const enriched = buildEnrichedEmployees(raw, filters.month);
  const filteredEmployees = applyFilters(enriched, filters);
  const filteredIds = new Set(filteredEmployees.map(employee => employee.id));
  const compareEnriched = compareRaw ? buildEnrichedEmployees(compareRaw, filters.compareMonth || compareRaw.meta.month) : [];
  const compareFiltered = compareRaw ? applyFilters(compareEnriched, filters) : [];
  const monthLong = isAllTimeMonth(filters.month) ? 'Toutes periodes' : toMonthLabelLong(filters.month);

  const totalEmployees = filteredEmployees.length;
  const femaleCount = filteredEmployees.filter(employee => resolveGenderBucket(employee.genderCode) === 'F').length;
  const maleCount = filteredEmployees.filter(employee => resolveGenderBucket(employee.genderCode) === 'H').length;
  const knownGender = Math.max(femaleCount + maleCount, 1);

  const grossMap = salaryByEmployeeForMonth(raw, filteredIds, filters.month);
  const gross = [...grossMap.values()].reduce((sum, value) => sum + value, 0);

  const inactiveStatuses = ['inactive', 'inactif', 'resigned', 'terminated', 'departed', 'left', 'fired', 'archive'];
  const inactiveCount = filteredEmployees.filter(employee => inactiveStatuses.some(token => employee.statusCode.toLowerCase().includes(token))).length;
  const turnover = totalEmployees > 0 ? (inactiveCount * 100) / totalEmployees : 0;
  const compareTurnover = compareRaw
    ? (() => {
      const count = compareFiltered.length;
      if (count === 0) return 0;
      const inactive = compareFiltered.filter(employee => inactiveStatuses.some(token => employee.statusCode.toLowerCase().includes(token))).length;
      return (inactive * 100) / count;
    })()
    : null;

  const timelineBaseMonth = isAllTimeMonth(filters.month) ? raw.meta.month : filters.month;
  const sixMonths = getMonthRange(timelineBaseMonth, 6);
  const effectifValues = sixMonths.map(month => {
    const end = monthEnd(month);
    const contracts = raw.contracts.filter(contract => {
      const start = parseDate(contract.startDate);
      const cEnd = parseDate(contract.endDate);
      return !!start && start <= end && (!cEnd || cEnd >= end) && filteredIds.has(contract.employeeId);
    });
    return new Set(contracts.map(contract => contract.employeeId)).size;
  });

  const departmentGroups = [...new Set(filteredEmployees.map(employee => employee.department))]
    .map(department => ({
      department,
      count: filteredEmployees.filter(employee => employee.department === department).length
    }))
    .sort((a, b) => b.count - a.count);

  const month12 = getMonthRange(timelineBaseMonth, 12);
  const monthlyGross = month12.map(month => {
    const values = salaryByEmployeeForMonth(raw, filteredIds, month);
    return [...values.values()].reduce((sum, value) => sum + value, 0);
  });
  const effectifDelta = effectifValues.length >= 2 ? effectifValues[effectifValues.length - 1] - effectifValues[effectifValues.length - 2] : 0;
  const previousGross = monthlyGross.length >= 2 ? monthlyGross[monthlyGross.length - 2] : 0;
  const grossTrendPct = previousGross > 0 ? ((gross - previousGross) * 100) / previousGross : 0;

  const movementContracts = raw.contracts.filter(contract => filteredIds.has(contract.employeeId));
  const entries = isAllTimeMonth(filters.month)
    ? movementContracts.filter(contract => !!parseDate(contract.startDate))
    : movementContracts.filter(contract => monthKey(parseDate(contract.startDate) || new Date(0)) === filters.month);
  const exits = isAllTimeMonth(filters.month)
    ? movementContracts.filter(contract => !!parseDate(contract.endDate || ''))
    : movementContracts.filter(contract => monthKey(parseDate(contract.endDate || '') || new Date(0)) === filters.month);

  const movementRows = [...entries.map(contract => ({ contract, kind: 'entry' as const })), ...exits.map(contract => ({ contract, kind: 'exit' as const }))]
    .sort((a, b) => (parseDate(b.kind === 'entry' ? b.contract.startDate : b.contract.endDate || '')?.getTime() || 0) - (parseDate(a.kind === 'entry' ? a.contract.startDate : a.contract.endDate || '')?.getTime() || 0))
    .slice(0, 50)
    .map(item => {
      const employee = filteredEmployees.find(e => e.id === item.contract.employeeId);
      return {
        employe: employee?.fullName || `Employee #${item.contract.employeeId}`,
        departement: employee?.department || 'Autres',
        poste: item.contract.position || 'Non assigne',
        type: item.contract.contractType || 'N/A',
        date: toFrDate(item.kind === 'entry' ? item.contract.startDate : item.contract.endDate || ''),
        motifNote: item.kind === 'entry' ? 'Nouvelle embauche' : 'Fin de contrat',
        mouvement: statusPill(item.kind)
      };
    });

  const retention = totalEmployees > 0 ? ((totalEmployees - exits.length) * 100) / totalEmployees : 0;

  const salaryByDepartment = departmentGroups.map(group => {
    const employeeIds = new Set(filteredEmployees.filter(employee => employee.department === group.department).map(employee => employee.id));
    const amount = [...salaryByEmployeeForMonth(raw, employeeIds, filters.month).values()].reduce((sum, value) => sum + value, 0);
    const ratio = gross > 0 ? (amount * 100) / gross : 0;
    return {
      label: `${group.department} (${group.count} pers.)`,
      rightLabel: `${formatK(amount)} MAD - ${Math.round(ratio)}%`,
      percent: Math.round(ratio),
      color: DEPARTMENT_COLORS[departmentGroups.findIndex(d => d.department === group.department) % DEPARTMENT_COLORS.length]
    } satisfies ProgressRowModel;
  });

  const parityDept = departmentGroups.map(group => {
    const list = filteredEmployees.filter(employee => employee.department === group.department);
    const f = list.filter(employee => resolveGenderBucket(employee.genderCode) === 'F').length;
    const h = list.filter(employee => resolveGenderBucket(employee.genderCode) === 'H').length;
    const total = Math.max(f + h, 1);
    return {
      label: group.department,
      rightLabel: `${f}F / ${h}H`,
      percent: Math.round((f * 100) / total),
      color: '#7c3aed'
    } satisfies ProgressRowModel;
  });

  const hierarchies = ['Direction', 'Managers', 'Cadres', 'Employes'];
  const parityHierarchy = hierarchies
    .map((level, index) => {
      const list = filteredEmployees.filter(employee => classifyHierarchy(employee.position) === level);
      if (list.length === 0) return null;
      const f = list.filter(employee => resolveGenderBucket(employee.genderCode) === 'F').length;
      const levelLabel = level === 'Employes' ? 'Employés' : level;
      return {
        label: `${levelLabel} (${list.length})`,
        rightLabel: `${Math.round((f * 100) / list.length)}% F`,
        percent: Math.round((f * 100) / list.length),
        color: HIERARCHY_COLORS[index % HIERARCHY_COLORS.length]
      } satisfies ProgressRowModel;
    })
    .filter((x): x is ProgressRowModel => x !== null);

  const cnssSalariale = gross * 0.0429;
  const cnssPatronale = gross * 0.2109;
  const amoSalariale = gross * 0.0226;
  const ir = gross * 0.161;

  const deadlineDate = new Date(monthEnd(filters.month).getFullYear(), monthEnd(filters.month).getMonth() + 1, 28);
  const irDeadline = new Date(monthEnd(filters.month).getFullYear(), monthEnd(filters.month).getMonth() + 1, 31);
  const now = new Date();

  const declarations = [
    {
      declaration: `Bordereau CNSS - ${monthLong}`,
      montantMad: formatMad(cnssSalariale + cnssPatronale),
      echeance: deadlineDate.toLocaleDateString('fr-FR'),
      statut: declarationStatus(now > deadlineDate ? 'En retard' : 'En attente'),
      reference: `CNSS-${filters.month.replace('-', '')}`
    },
    {
      declaration: `AMO employeur - ${monthLong}`,
      montantMad: formatMad(amoSalariale),
      echeance: deadlineDate.toLocaleDateString('fr-FR'),
      statut: declarationStatus(now > deadlineDate ? 'En retard' : 'En attente'),
      reference: `AMO-${filters.month.replace('-', '')}`
    },
    {
      declaration: `Versement IR DGI - ${monthLong}`,
      montantMad: formatMad(ir),
      echeance: irDeadline.toLocaleDateString('fr-FR'),
      statut: declarationStatus(now > irDeadline ? 'En retard' : 'Soumis'),
      reference: `IR-${filters.month.replace('-', '')}`
    }
  ];

  const effectifTrend = buildTrendFromDelta(effectifDelta);
  const grossTrend = buildTrendFromDelta(grossTrendPct, '%');

  const vueKpis: KpiMetric[] = [
    {
      label: 'Effectif total',
      value: String(totalEmployees),
      subLabel: `${filteredEmployees.length} actifs filtres`,
      trend: {
        ...effectifTrend,
        value: `${effectifTrend.value} ce mois`
      }
    },
    { label: 'Masse salariale', value: formatK(gross), subLabel: 'MAD / mois', trend: grossTrend },
    { label: 'Turnover (12M)', value: formatPct(turnover), subLabel: 'Base statuts employes', trend: buildTurnoverTrend(turnover, compareTurnover) },
    { label: 'Parite F/H', value: `${Math.round((femaleCount * 100) / knownGender)} / ${Math.round((maleCount * 100) / knownGender)}`, subLabel: '% Femmes / Hommes', trend: { value: 'stable', direction: 'flat' } }
  ];

  return {
    appTitle: 'Dashboard',
    appSubtitle: `${raw.meta.companyName} - ${monthLong}`,
    vueGlobale: {
      meta: {
        eyebrow: '',
        title: 'Vue Globale RH',
        badge: 'HOME',
        subtitle: `KPIs instantanes - Snapshot du mois en cours - ${monthLong}`,
        icon: 'pi pi-home'
      },
      kpis: vueKpis,
      effectifEvolution: {
        labels: sixMonths.map(toMonthLabelShort),
        values: effectifValues,
        datasetLabel: 'Effectif',
        color: '#2563eb',
        highlightLast: false,
        ySuggestedMax: 90,
        yTickStep: 10
      },
      repartitionDepartement: {
        centerLabel: String(totalEmployees),
        slices: departmentGroups.map((group, index) => ({
          label: group.department,
          value: group.count,
          color: DEPARTMENT_COLORS[index % DEPARTMENT_COLORS.length]
        }))
      }
    },
    mouvementsRh: {
      meta: {
        eyebrow: '',
        title: 'Mouvements RH',
        badge: 'Entrées / sorties',
        subtitle: `Historique des entrées et sorties — ${monthLong} — ${entries.length} entrées — ${exits.length} sorties`,
        icon: 'pi pi-refresh'
      },
      summary: [
        { label: 'Entrées ce mois', value: `+${entries.length}`, subLabel: 'Nouveaux contrats', accent: 'success' },
        { label: 'Sorties ce mois', value: `-${exits.length}`, subLabel: 'Fins de contrat', accent: 'danger' },
        { label: 'Solde net', value: `${entries.length - exits.length >= 0 ? '+' : ''}${entries.length - exits.length}`, subLabel: `Taux de rétention : ${formatPct(retention)}`, accent: entries.length - exits.length >= 0 ? 'success' : 'danger' }
      ],
      history: movementRows
    },
    masseSalariale: {
      meta: {
        eyebrow: '',
        title: 'Masse salariale',
        badge: 'Paie',
        subtitle: `Analyse des coûts salariaux — charges patronales incluses — ${monthLong}`,
        icon: 'pi pi-wallet'
      },
      kpis: [
        { label: 'Brut total', value: formatK(gross), subLabel: 'MAD' },
        { label: 'Net total verse', value: formatK(gross * 0.772), subLabel: 'MAD apres retenues' },
        { label: 'Charges patronales', value: formatK(gross * 0.216), subLabel: 'CNSS + AMO employeur' },
        { label: 'Coût total employeur', value: formatK(gross * 1.216), subLabel: 'MAD / mois' }
      ],
      masseBrute12Mois: (() => {
        const values = monthlyGross.map(value => round1(value / 1000));
        const maxK = values.length ? Math.max(...values) : 1;
        return {
          labels: month12.map(toMonthLabelShort),
          values,
          datasetLabel: 'Masse salariale brute',
          color: '#14b8a6',
          highlightLast: false,
          suffix: 'K MAD',
          ySuggestedMax: Math.max(100, Math.ceil(maxK / 20) * 20),
          yTickStep: 20
        };
      })(),
      repartitionDepartement: salaryByDepartment
    },
    pariteDiversite: {
      meta: {
        eyebrow: '',
        title: 'Parité & diversité',
        badge: 'Équité',
        subtitle: `Indicateurs d’équilibre femmes / hommes — ${monthLong}`,
        icon: 'pi pi-balance-scale'
      },
      kpis: [
        { label: 'Effectif femmes', value: String(femaleCount), subLabel: `${formatPct((femaleCount * 100) / knownGender)} de l’effectif`, accent: 'purple' },
        { label: 'Effectif hommes', value: String(maleCount), subLabel: `${formatPct((maleCount * 100) / knownGender)} de l’effectif`, accent: 'blue' },
        { label: 'Écart salarial moyen', value: 'N/A', subLabel: 'Calculé sur le sous-ensemble filtré', accent: 'danger' }
      ],
      pariteDepartement: parityDept,
      pariteNiveauHierarchique: parityHierarchy
    },
    conformiteSociale: {
      meta: {
        eyebrow: '',
        title: 'Conformité sociale',
        badge: 'CNSS — AMO — IR',
        subtitle: `État des déclarations et cotisations — ${monthLong}`,
        icon: 'pi pi-check-circle'
      },
      kpis: [
        { label: 'CNSS salariale', value: formatK(cnssSalariale), subLabel: 'MAD — taux 4,29 %' },
        { label: 'CNSS patronale', value: formatK(cnssPatronale), subLabel: 'MAD — taux 21,09 %' },
        { label: 'AMO (salariale)', value: formatK(amoSalariale), subLabel: 'MAD — taux 2,26 %' },
        { label: 'IR retenu à la source', value: formatK(ir), subLabel: 'MAD — estimation' }
      ],
      declarations
    }
  };
}

export function normalizeRawApi(dto: any): DashboardHrRawData {
  const pick = <T>(obj: any, pascal: string, camel: string): T => (obj?.[pascal] ?? obj?.[camel]) as T;
  const metaDto = pick<any>(dto, 'Meta', 'meta') ?? {};
  const employeesDto = pick<any[]>(dto, 'Employees', 'employees') ?? [];
  const contractsDto = pick<any[]>(dto, 'Contracts', 'contracts') ?? [];
  const salariesDto = pick<any[]>(dto, 'Salaries', 'salaries') ?? [];

  return {
    meta: {
      companyId: Number(pick<number>(metaDto, 'CompanyId', 'companyId') ?? 0),
      companyName: String(pick<string>(metaDto, 'CompanyName', 'companyName') ?? 'Entreprise'),
      month: String(pick<string>(metaDto, 'Month', 'month') ?? ''),
      generatedAt: String(pick<string>(metaDto, 'GeneratedAt', 'generatedAt') ?? new Date().toISOString())
    },
    employees: employeesDto.map(item => ({
      id: Number(pick<number>(item, 'Id', 'id') ?? 0),
      firstName: String(pick<string>(item, 'FirstName', 'firstName') ?? ''),
      lastName: String(pick<string>(item, 'LastName', 'lastName') ?? ''),
      department: String(pick<string>(item, 'Department', 'department') ?? 'Autres'),
      statusCode: String(pick<string>(item, 'StatusCode', 'statusCode') ?? ''),
      genderCode: String(pick<string>(item, 'GenderCode', 'genderCode') ?? '')
    })),
    contracts: contractsDto.map(item => ({
      employeeId: Number(pick<number>(item, 'EmployeeId', 'employeeId') ?? 0),
      startDate: String(pick<string>(item, 'StartDate', 'startDate') ?? ''),
      endDate: pick<string | null>(item, 'EndDate', 'endDate') ?? null,
      position: String(pick<string>(item, 'Position', 'position') ?? 'Non assigne'),
      contractType: String(pick<string>(item, 'ContractType', 'contractType') ?? 'N/A')
    })),
    salaries: salariesDto.map(item => ({
      employeeId: Number(pick<number>(item, 'EmployeeId', 'employeeId') ?? 0),
      baseSalary: Number(pick<number>(item, 'BaseSalary', 'baseSalary') ?? 0),
      effectiveDate: String(pick<string>(item, 'EffectiveDate', 'effectiveDate') ?? ''),
      endDate: pick<string | null>(item, 'EndDate', 'endDate') ?? null
    }))
  };
}
