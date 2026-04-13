import {
  DashboardHrData,
  DashboardHrPayload,
  DeclarationRow,
  KpiMetric,
  MovementHistoryRow,
  ProgressRowModel,
  StatusPill
} from '../state/dashboard-hr.models';

type EnumValue = string | number | undefined | null;

export interface DashboardHrApiDto {
  Meta: {
    CompanyId: number;
    CompanyName: string;
    Month: string;
    GeneratedAt: string;
  };
  VueGlobale: {
    Kpis: {
      EffectifTotal: number;
      MasseSalarialeMad: number;
      Turnover12mPct: number;
      Parite: {
        FemalePct: number;
        MalePct: number;
      };
    };
    EffectifEvolution6M: Array<{
      Month: string;
      Value: number;
    }>;
    RepartitionDepartement: Array<{
      Department: string;
      Count: number;
    }>;
  };
  MouvementsRh: {
    Summary: {
      Entrees: number;
      Sorties: number;
      SoldeNet: number;
      RetentionPct: number;
    };
    Rows: Array<{
      EmployeeName: string;
      Department: string;
      Position: string;
      ContractType: string;
      Date: string;
      Reason: string;
      MovementType: EnumValue;
    }>;
  };
  MasseSalariale: {
    Kpis: {
      BrutTotalMad: number;
      NetTotalMad: number;
      ChargesPatronalesMad: number;
      CoutTotalEmployeurMad: number;
    };
    Brut12m: Array<{
      Month: string;
      ValueMad: number;
    }>;
    RepartitionDepartement: Array<{
      Department: string;
      Employees: number;
      AmountMad: number;
      SharePct: number;
    }>;
  };
  PariteDiversite: {
    Kpis: {
      EffectifFemmes: number;
      EffectifHommes: number;
      EcartSalarialPct: number;
    };
    PariteDepartement: Array<{
      Department: string;
      FemaleCount: number;
      MaleCount: number;
      FemalePct: number;
    }>;
    PariteNiveauHierarchique: Array<{
      Level: string;
      Total: number;
      FemaleCount: number;
      FemalePct: number;
    }>;
  };
  ConformiteSociale: {
    Kpis: {
      CnssSalarialeMad: number;
      CnssPatronaleMad: number;
      AmoSalarialeMad: number;
      IrRetenuSourceMad: number;
    };
    Declarations: Array<{
      Label: string;
      AmountMad: number;
      Deadline: string;
      Status: EnumValue;
      Reference: string;
    }>;
  };
}

const DEPARTMENT_COLORS = ['#2563eb', '#22c55e', '#f97316', '#6366f1', '#7c3aed', '#0ea5e9'];
const HIERARCHY_COLORS = ['#ef4444', '#f97316', '#22c55e', '#14b8a6', '#6366f1'];

function round1(value: number): number {
  return Math.round(value * 10) / 10;
}

function formatPct(value: number): string {
  return `${round1(value)}%`;
}

function formatKMad(value: number): string {
  const inK = round1((value || 0) / 1000);
  return Number.isInteger(inK) ? `${inK.toFixed(0)} K` : `${inK.toFixed(1)} K`;
}

function formatMad(value: number): string {
  return Math.round(value || 0).toLocaleString('fr-FR');
}

function monthShort(month: string): string {
  const tokens = month.split('-');
  const monthIndex = Number(tokens[1]);
  const labels = ['Jan', 'Fev', 'Mar', 'Avr', 'Mai', 'Juin', 'Juil', 'Aout', 'Sep', 'Oct', 'Nov', 'Dec'];
  if (!Number.isFinite(monthIndex) || monthIndex < 1 || monthIndex > 12) {
    return month;
  }

  return labels[monthIndex - 1];
}

function monthLong(month: string): string {
  const tokens = month.split('-');
  const year = Number(tokens[0]);
  const monthIndex = Number(tokens[1]);
  const labels = [
    'Janvier',
    'Fevrier',
    'Mars',
    'Avril',
    'Mai',
    'Juin',
    'Juillet',
    'Aout',
    'Septembre',
    'Octobre',
    'Novembre',
    'Decembre'
  ];

  if (!Number.isFinite(monthIndex) || monthIndex < 1 || monthIndex > 12 || !Number.isFinite(year)) {
    return month;
  }

  return `${labels[monthIndex - 1]} ${year}`;
}

function toFrDate(date: string): string {
  if (!date) {
    return '-';
  }

  const parsed = new Date(`${date}T00:00:00`);
  if (Number.isNaN(parsed.getTime())) {
    return date;
  }

  return parsed.toLocaleDateString('fr-FR');
}

function movementStatus(value: EnumValue): StatusPill {
  const normalized = String(value ?? '').toUpperCase();
  const isEntry = normalized === 'ENTRY' || normalized === '1';

  return isEntry
    ? { label: 'Entree', severity: 'success' }
    : { label: 'Sortie', severity: 'danger' };
}

function declarationStatus(value: EnumValue): StatusPill {
  const normalized = String(value ?? '').toUpperCase();

  if (normalized === 'SUBMITTED' || normalized === '2') {
    return { label: 'Soumis', severity: 'success' };
  }

  if (normalized === 'PENDING' || normalized === '1') {
    return { label: 'En attente', severity: 'warn' };
  }

  if (normalized === 'OVERDUE' || normalized === '4') {
    return { label: 'En retard', severity: 'danger' };
  }

  if (normalized === 'REJECTED' || normalized === '3') {
    return { label: 'Rejete', severity: 'danger' };
  }

  return { label: 'A verifier', severity: 'info' };
}

function pick<T>(obj: unknown, pascal: string, camel: string): T {
  const source = obj as Record<string, unknown> | null | undefined;
  return (source?.[pascal] ?? source?.[camel]) as T;
}

function buildVueGlobaleKpis(vue: any, masse: any): KpiMetric[] {
  const vueKpis = pick<any>(vue, 'Kpis', 'kpis') ?? {};
  const parity = pick<any>(vueKpis, 'Parite', 'parite') ?? {};
  const salaryMonths = pick<any[]>(masse, 'Brut12m', 'brut12m') ?? [];
  const effectifEvolution = pick<any[]>(vue, 'EffectifEvolution6M', 'effectifEvolution6M') ?? [];
  const effectifValues = effectifEvolution.map(item => Number(pick<number>(item, 'Value', 'value') ?? 0));
  const effectifDelta = effectifValues.length >= 2 ? effectifValues[effectifValues.length - 1] - effectifValues[effectifValues.length - 2] : 0;

  const salaryValues = salaryMonths.map(item => Number(pick<number>(item, 'ValueMad', 'valueMad') ?? 0));
  const prevSalary = salaryValues.length >= 2 ? salaryValues[salaryValues.length - 2] : 0;
  const currSalary = salaryValues.length >= 1 ? salaryValues[salaryValues.length - 1] : 0;
  const salaryTrendPct = prevSalary > 0 ? ((currSalary - prevSalary) / prevSalary) * 100 : 0;

  return [
    {
      label: 'Effectif total',
      value: String(pick<number>(vueKpis, 'EffectifTotal', 'effectifTotal') ?? 0),
      subLabel: `${Math.max(effectifDelta, 0)} variation ce mois`,
      trend: {
        value: `${effectifDelta >= 0 ? '+' : ''}${effectifDelta}`,
        direction: effectifDelta >= 0 ? 'up' : 'down'
      }
    },
    {
      label: 'Masse salariale',
      value: formatKMad(Number(pick<number>(vueKpis, 'MasseSalarialeMad', 'masseSalarialeMad') ?? 0)),
      subLabel: 'MAD / mois',
      trend: {
        value: `${salaryTrendPct >= 0 ? '+' : ''}${formatPct(salaryTrendPct)}`,
        direction: salaryTrendPct >= 0 ? 'up' : 'down'
      }
    },
    {
      label: 'Turnover (12M)',
      value: formatPct(Number(pick<number>(vueKpis, 'Turnover12mPct', 'turnover12mPct') ?? 0)),
      subLabel: 'Base mouvements 12 mois',
      trend: {
        value: 'suivi annuel',
        direction: 'flat'
      }
    },
    {
      label: 'Parite F/H',
      value: `${Math.round(Number(pick<number>(parity, 'FemalePct', 'femalePct') ?? 0))} / ${Math.round(Number(pick<number>(parity, 'MalePct', 'malePct') ?? 0))}`,
      subLabel: '% Femmes / Hommes',
      trend: {
        value: 'stable',
        direction: 'flat'
      }
    }
  ];
}

export function mapDashboardHrApiToPayload(dto: DashboardHrApiDto): DashboardHrPayload {
  const meta = pick<any>(dto, 'Meta', 'meta') ?? {};
  const vue = pick<any>(dto, 'VueGlobale', 'vueGlobale') ?? {};
  const mouvements = pick<any>(dto, 'MouvementsRh', 'mouvementsRh') ?? {};
  const masse = pick<any>(dto, 'MasseSalariale', 'masseSalariale') ?? {};
  const parite = pick<any>(dto, 'PariteDiversite', 'pariteDiversite') ?? {};
  const conformite = pick<any>(dto, 'ConformiteSociale', 'conformiteSociale') ?? {};

  const month = String(pick<string>(meta, 'Month', 'month') ?? '');
  const monthLongLabel = monthLong(month);

  const mouvementsSummary = pick<any>(mouvements, 'Summary', 'summary') ?? {};
  const mouvementsRowsSource = pick<any[]>(mouvements, 'Rows', 'rows') ?? [];
  const mouvementsRows: MovementHistoryRow[] = mouvementsRowsSource.map(row => ({
    employe: String(pick<string>(row, 'EmployeeName', 'employeeName') ?? ''),
    departement: String(pick<string>(row, 'Department', 'department') ?? ''),
    poste: String(pick<string>(row, 'Position', 'position') ?? ''),
    type: String(pick<string>(row, 'ContractType', 'contractType') ?? ''),
    date: toFrDate(String(pick<string>(row, 'Date', 'date') ?? '')),
    motifNote: String(pick<string>(row, 'Reason', 'reason') ?? ''),
    mouvement: movementStatus(pick<EnumValue>(row, 'MovementType', 'movementType'))
  }));

  const masseDeptSource = pick<any[]>(masse, 'RepartitionDepartement', 'repartitionDepartement') ?? [];
  const masseRows: ProgressRowModel[] = masseDeptSource.map((row, index) => ({
    label: `${String(pick<string>(row, 'Department', 'department') ?? '')} (${Number(pick<number>(row, 'Employees', 'employees') ?? 0)} pers.)`,
    rightLabel: `${formatKMad(Number(pick<number>(row, 'AmountMad', 'amountMad') ?? 0))} MAD - ${Math.round(Number(pick<number>(row, 'SharePct', 'sharePct') ?? 0))}%`,
    percent: Math.round(Number(pick<number>(row, 'SharePct', 'sharePct') ?? 0)),
    color: DEPARTMENT_COLORS[index % DEPARTMENT_COLORS.length]
  }));

  const pariteDeptSource = pick<any[]>(parite, 'PariteDepartement', 'pariteDepartement') ?? [];
  const pariteDeptRows: ProgressRowModel[] = pariteDeptSource.map(row => ({
    label: String(pick<string>(row, 'Department', 'department') ?? ''),
    rightLabel: `${Number(pick<number>(row, 'FemaleCount', 'femaleCount') ?? 0)}F / ${Number(pick<number>(row, 'MaleCount', 'maleCount') ?? 0)}H`,
    percent: Math.round(Number(pick<number>(row, 'FemalePct', 'femalePct') ?? 0)),
    color: '#7c3aed'
  }));

  const pariteHierarchySource = pick<any[]>(parite, 'PariteNiveauHierarchique', 'pariteNiveauHierarchique') ?? [];
  const pariteHierarchyRows: ProgressRowModel[] = pariteHierarchySource.map((row, index) => ({
    label: `${String(pick<string>(row, 'Level', 'level') ?? '')} (${Number(pick<number>(row, 'Total', 'total') ?? 0)})`,
    rightLabel: `${Math.round(Number(pick<number>(row, 'FemalePct', 'femalePct') ?? 0))}% F`,
    percent: Math.round(Number(pick<number>(row, 'FemalePct', 'femalePct') ?? 0)),
    color: HIERARCHY_COLORS[index % HIERARCHY_COLORS.length]
  }));

  const declarationsSource = pick<any[]>(conformite, 'Declarations', 'declarations') ?? [];
  const declarations: DeclarationRow[] = declarationsSource.map(row => ({
    declaration: String(pick<string>(row, 'Label', 'label') ?? ''),
    montantMad: formatMad(Number(pick<number>(row, 'AmountMad', 'amountMad') ?? 0)),
    echeance: toFrDate(String(pick<string>(row, 'Deadline', 'deadline') ?? '')),
    statut: declarationStatus(pick<EnumValue>(row, 'Status', 'status')),
    reference: String(pick<string>(row, 'Reference', 'reference') ?? '')
  }));

  const pariteKpis = pick<any>(parite, 'Kpis', 'kpis') ?? {};
  const femaleCount = Number(pick<number>(pariteKpis, 'EffectifFemmes', 'effectifFemmes') ?? 0);
  const maleCount = Number(pick<number>(pariteKpis, 'EffectifHommes', 'effectifHommes') ?? 0);
  const knownCount = Math.max(femaleCount + maleCount, 1);
  const vueKpis = pick<any>(vue, 'Kpis', 'kpis') ?? {};
  const vueEffectif = pick<any[]>(vue, 'EffectifEvolution6M', 'effectifEvolution6M') ?? [];
  const vueRepartition = pick<any[]>(vue, 'RepartitionDepartement', 'repartitionDepartement') ?? [];
  const masseKpis = pick<any>(masse, 'Kpis', 'kpis') ?? {};
  const masse12m = pick<any[]>(masse, 'Brut12m', 'brut12m') ?? [];
  const conformiteKpis = pick<any>(conformite, 'Kpis', 'kpis') ?? {};

  const data: DashboardHrData = {
    appTitle: 'PayZen HR - Dashboard RH',
    appSubtitle: `Donnees dynamiques - ${String(pick<string>(meta, 'CompanyName', 'companyName') ?? 'Entreprise')} - ${monthLongLabel}`,
    vueGlobale: {
      meta: {
        eyebrow: '',
        title: 'Vue Globale RH',
        badge: 'Live',
        subtitle: `KPIs instantanes - ${monthLongLabel}`,
        icon: 'pi pi-home'
      },
      kpis: buildVueGlobaleKpis(vue, masse),
      effectifEvolution: {
        labels: vueEffectif.map(item => monthShort(String(pick<string>(item, 'Month', 'month') ?? ''))),
        values: vueEffectif.map(item => Number(pick<number>(item, 'Value', 'value') ?? 0)),
        datasetLabel: 'Effectif',
        color: '#2563eb',
        highlightLast: false,
        ySuggestedMax: 90,
        yTickStep: 10
      },
      repartitionDepartement: {
        centerLabel: String(pick<number>(vueKpis, 'EffectifTotal', 'effectifTotal') ?? 0),
        slices: vueRepartition.map((row, index) => ({
          label: String(pick<string>(row, 'Department', 'department') ?? ''),
          value: Number(pick<number>(row, 'Count', 'count') ?? 0),
          color: DEPARTMENT_COLORS[index % DEPARTMENT_COLORS.length]
        }))
      }
    },
    mouvementsRh: {
      meta: {
        eyebrow: '',
        title: 'Mouvements RH',
        badge: 'Live',
        subtitle: `Historique des entrées et sorties — ${monthLongLabel}`,
        icon: 'pi pi-refresh'
      },
      summary: [
        { label: 'Entrées ce mois', value: `+${Number(pick<number>(mouvementsSummary, 'Entrees', 'entrees') ?? 0)}`, subLabel: 'Nouveaux contrats', accent: 'success' },
        { label: 'Sorties ce mois', value: `-${Number(pick<number>(mouvementsSummary, 'Sorties', 'sorties') ?? 0)}`, subLabel: 'Fins de contrat', accent: 'danger' },
        { label: 'Solde net', value: `${Number(pick<number>(mouvementsSummary, 'SoldeNet', 'soldeNet') ?? 0) >= 0 ? '+' : ''}${Number(pick<number>(mouvementsSummary, 'SoldeNet', 'soldeNet') ?? 0)}`, subLabel: `Taux de rétention : ${formatPct(Number(pick<number>(mouvementsSummary, 'RetentionPct', 'retentionPct') ?? 0))}`, accent: Number(pick<number>(mouvementsSummary, 'SoldeNet', 'soldeNet') ?? 0) >= 0 ? 'success' : 'danger' }
      ],
      history: mouvementsRows
    },
    masseSalariale: {
      meta: {
        eyebrow: '',
        title: 'Masse salariale',
        badge: 'Live',
        subtitle: `Analyse des coûts salariaux — ${monthLongLabel}`,
        icon: 'pi pi-wallet'
      },
      kpis: [
        { label: 'Brut total', value: formatKMad(Number(pick<number>(masseKpis, 'BrutTotalMad', 'brutTotalMad') ?? 0)), subLabel: 'MAD' },
        { label: 'Net total versé', value: formatKMad(Number(pick<number>(masseKpis, 'NetTotalMad', 'netTotalMad') ?? 0)), subLabel: 'MAD après retenues' },
        { label: 'Charges patronales', value: formatKMad(Number(pick<number>(masseKpis, 'ChargesPatronalesMad', 'chargesPatronalesMad') ?? 0)), subLabel: 'CNSS + AMO employeur' },
        { label: 'Coût total employeur', value: formatKMad(Number(pick<number>(masseKpis, 'CoutTotalEmployeurMad', 'coutTotalEmployeurMad') ?? 0)), subLabel: 'MAD / mois' }
      ],
      masseBrute12Mois: (() => {
        const values = masse12m.map(item => round1(Number(pick<number>(item, 'ValueMad', 'valueMad') ?? 0) / 1000));
        const maxK = values.length ? Math.max(...values) : 1;
        return {
          labels: masse12m.map(item => monthShort(String(pick<string>(item, 'Month', 'month') ?? ''))),
          values,
          datasetLabel: 'Masse salariale brute',
          color: '#14b8a6',
          highlightLast: false,
          suffix: 'K MAD',
          ySuggestedMax: Math.max(100, Math.ceil(maxK / 20) * 20),
          yTickStep: 20
        };
      })(),
      repartitionDepartement: masseRows
    },
    pariteDiversite: {
      meta: {
        eyebrow: '',
        title: 'Parité & diversité',
        badge: 'Live',
        subtitle: `Indicateurs d’équilibre — ${monthLongLabel}`,
        icon: 'pi pi-balance-scale'
      },
      kpis: [
        { label: 'Effectif femmes', value: String(femaleCount), subLabel: `${formatPct((femaleCount * 100) / knownCount)} de l’effectif`, accent: 'purple' },
        { label: 'Effectif hommes', value: String(maleCount), subLabel: `${formatPct((maleCount * 100) / knownCount)} de l’effectif`, accent: 'blue' },
        { label: 'Écart salarial moyen', value: formatPct(Number(pick<number>(pariteKpis, 'EcartSalarialPct', 'ecartSalarialPct') ?? 0)), subLabel: 'Femmes vs hommes', accent: Number(pick<number>(pariteKpis, 'EcartSalarialPct', 'ecartSalarialPct') ?? 0) < 0 ? 'danger' : 'success' }
      ],
      pariteDepartement: pariteDeptRows,
      pariteNiveauHierarchique: pariteHierarchyRows
    },
    conformiteSociale: {
      meta: {
        eyebrow: '',
        title: 'Conformité sociale',
        badge: 'CNSS — AMO — IR',
        subtitle: `État des déclarations — ${monthLongLabel}`,
        icon: 'pi pi-check-circle'
      },
      kpis: [
        { label: 'CNSS salariale', value: formatKMad(Number(pick<number>(conformiteKpis, 'CnssSalarialeMad', 'cnssSalarialeMad') ?? 0)), subLabel: 'MAD — taux 4,29 %' },
        { label: 'CNSS patronale', value: formatKMad(Number(pick<number>(conformiteKpis, 'CnssPatronaleMad', 'cnssPatronaleMad') ?? 0)), subLabel: 'MAD — taux 21,09 %' },
        { label: 'AMO (salariale)', value: formatKMad(Number(pick<number>(conformiteKpis, 'AmoSalarialeMad', 'amoSalarialeMad') ?? 0)), subLabel: 'MAD — taux 2,26 %' },
        { label: 'IR retenu à la source', value: formatKMad(Number(pick<number>(conformiteKpis, 'IrRetenuSourceMad', 'irRetenuSourceMad') ?? 0)), subLabel: 'MAD — estimation' }
      ],
      declarations
    }
  };

  return {
    data,
    meta: {
      source: 'api',
      loadedAtIso: String(pick<string>(meta, 'GeneratedAt', 'generatedAt') ?? new Date().toISOString()),
      warnings: []
    }
  };
}
