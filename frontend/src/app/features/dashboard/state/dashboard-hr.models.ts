export type DashboardTabId =
  | 'vue-globale'
  | 'mouvements-rh'
  | 'masse-salariale'
  | 'parite-diversite'
  | 'conformite-sociale';

export type TrendDirection = 'up' | 'down' | 'flat';

export type KpiAccent = 'default' | 'success' | 'danger' | 'info' | 'purple' | 'blue';

export interface KpiTrend {
  value: string;
  direction: TrendDirection;
  context?: string;
}

export interface KpiMetric {
  label: string;
  value: string;
  subLabel?: string;
  trend?: KpiTrend;
  accent?: KpiAccent;
}

export interface SectionMeta {
  eyebrow?: string;
  title: string;
  badge?: string;
  subtitle: string;
  icon?: string;
}

export interface BarChartConfig {
  labels: string[];
  values: number[];
  datasetLabel: string;
  color?: string;
  highlightLast?: boolean;
  horizontal?: boolean;
  suffix?: string;
  /** Axe Y (effectif) : plafond suggéré (ex. 90 pour échelle 0–90). */
  ySuggestedMax?: number;
  /** Pas entre graduations Y (ex. 10). */
  yTickStep?: number;
}

export interface DonutSlice {
  label: string;
  value: number;
  color: string;
}

export interface DonutChartConfig {
  centerLabel?: string;
  slices: DonutSlice[];
}

export interface ProgressRowModel {
  label: string;
  rightLabel: string;
  percent: number;
  color: string;
}

export type StatusSeverity = 'success' | 'warn' | 'danger' | 'info' | 'neutral';

export interface StatusPill {
  label: string;
  severity: StatusSeverity;
}

export interface MovementHistoryRow {
  employe: string;
  departement: string;
  poste: string;
  type: string;
  date: string;
  motifNote: string;
  mouvement: StatusPill;
}

export interface DeclarationRow {
  declaration: string;
  montantMad: string;
  echeance: string;
  statut: StatusPill;
  reference: string;
}

/** Pagination bas de page (maquette Figma). */
export interface VueGlobaleFooterNav {
  current: number;
  total: number;
}

export interface VueGlobaleData {
  meta: SectionMeta;
  kpis: KpiMetric[];
  effectifEvolution: BarChartConfig;
  repartitionDepartement: DonutChartConfig;
  /** Affichage optionnel type « 2 / 93 » avec flèches. */
  footerNav?: VueGlobaleFooterNav | null;
}

export interface MouvementsRhData {
  meta: SectionMeta;
  summary: KpiMetric[];
  history: MovementHistoryRow[];
}

export interface MasseSalarialeData {
  meta: SectionMeta;
  kpis: KpiMetric[];
  masseBrute12Mois: BarChartConfig;
  repartitionDepartement: ProgressRowModel[];
}

export interface PariteDiversiteData {
  meta: SectionMeta;
  kpis: KpiMetric[];
  pariteDepartement: ProgressRowModel[];
  pariteNiveauHierarchique: ProgressRowModel[];
}

export interface ConformiteSocialeData {
  meta: SectionMeta;
  kpis: KpiMetric[];
  declarations: DeclarationRow[];
}

export interface DashboardHrData {
  appTitle: string;
  appSubtitle: string;
  vueGlobale: VueGlobaleData;
  mouvementsRh: MouvementsRhData;
  masseSalariale: MasseSalarialeData;
  pariteDiversite: PariteDiversiteData;
  conformiteSociale: ConformiteSocialeData;
}

export interface DashboardHrQuery {
  companyId: string | null;
  isExpertMode: boolean;
  isClientView: boolean;
  month: string | null;
  compareMonth: string | null;
}

export type DashboardParityFilter = 'F' | 'H';

export interface DashboardFilterState {
  departments: string[];
  contractTypes: string[];
  parity: DashboardParityFilter[];
  month: string;
  compareMonth: string | null;
}

export interface DashboardHrLoadMeta {
  source: 'api' | 'mock';
  loadedAtIso: string;
  warnings: string[];
}

export interface DashboardHrPayload {
  data: DashboardHrData;
  meta: DashboardHrLoadMeta;
}

export interface DashboardTab {
  id: DashboardTabId;
  label: string;
  icon: string;
}
