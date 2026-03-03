// Time-series metric point models for dashboard charts
export interface MetricPoint {
  date: string; // ISO date (YYYY-MM-DD or full ISO)
  value: number;
}

export interface UsageMetrics {
  points: MetricPoint[]; // active companies / employees over time
  unit?: 'count' | 'sessions' | 'percent';
}

export interface RevenueMetricPoint extends MetricPoint {
  revenue: number;
}

export interface RevenueMetrics {
  points: RevenueMetricPoint[];
  currency?: string;
}
