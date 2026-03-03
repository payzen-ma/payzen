import { Injectable } from '@angular/core';
import { CimrConfig, CIMR_AL_KAMIL_RATES, CIMR_AL_MOUNASSIB_RATES } from '../../models/salary-package.model';

const STORAGE_KEY = 'payzen_default_cimr_config';

@Injectable({ providedIn: 'root' })
export class DefaultCimrConfigService {
  private cached: CimrConfig | null = null;

  getDefaultCimrConfig(): CimrConfig {
    if (this.cached) return { ...this.cached };
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (raw) {
        const parsed = JSON.parse(raw) as CimrConfig;
        if (parsed && typeof parsed.regime === 'string') {
          this.cached = this.normalize(parsed);
          return { ...this.cached };
        }
      }
    } catch {
      // ignore
    }
    const fallback: CimrConfig = { regime: 'NONE', employeeRate: 0, employerRate: 0 };
    this.cached = fallback;
    return { ...fallback };
  }

  setDefaultCimrConfig(config: CimrConfig): void {
    const normalized = this.normalize(config);
    this.cached = normalized;
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(normalized));
    } catch {
      // ignore
    }
  }

  private normalize(config: CimrConfig): CimrConfig {
    if (config.regime === 'NONE') {
      return { regime: 'NONE', employeeRate: 0, employerRate: 0 };
    }
    if (config.regime === 'AL_KAMIL') {
      const first = CIMR_AL_KAMIL_RATES[0];
      return {
        regime: 'AL_KAMIL',
        employeeRate: config.employeeRate ?? first.employeeRate,
        employerRate: config.employerRate ?? first.employerRate
      };
    }
    if (config.regime === 'AL_MOUNASSIB') {
      const first = CIMR_AL_MOUNASSIB_RATES[0];
      return {
        regime: 'AL_MOUNASSIB',
        employeeRate: config.employeeRate ?? first.employeeRate,
        employerRate: config.employerRate ?? first.employerRate
      };
    }
    return { regime: 'NONE', employeeRate: 0, employerRate: 0 };
  }
}
