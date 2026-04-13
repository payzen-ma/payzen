import { Injectable } from '@angular/core';
import { Observable, of, catchError, map, shareReplay, tap } from 'rxjs';
import { PayrollReferentielService } from './payroll-referentiel.service';
import { LegalParameterDto } from '../../models/payroll-referentiel';
import { getLegalParameterType, LegalParameterType } from '../../models/payroll-referentiel/legal-parameter.model';

/**
 * Legacy hardcoded values (DEPRECATED - will be removed in Phase 4)
 * These are used as fallbacks when API fails
 * @deprecated Use API values instead
 */
const LEGACY_SMIG_2025 = 3111.39;
const LEGACY_CNSS_CEILING = 6000;

/**
 * @deprecated Use API values instead
 */
const LEGACY_CIMR_EMPLOYEE_RATE = 0.06;

/**
 * @deprecated Use API values instead
 */
const LEGACY_CIMR_EMPLOYER_RATE = 0.075;

/**
 * Interface for payroll data result
 */
export interface PayrollDataResult<T> {
  value: T;
  source: 'api' | 'cache' | 'fallback';
  lastUpdated?: Date;
}

/**
 * Salary Package Data Facade Service
 *
 * Provides a unified interface for accessing payroll referential data
 * with automatic fallback to hardcoded values if the API fails.
 *
 * This service:
 * - Fetches data from the API via PayrollReferentielService
 * - Caches API responses for performance
 * - Falls back to legacy hardcoded values if API is unavailable
 * - Reports the data source (api, cache, or fallback) for debugging
 *
 * @example
 * // Get SMIG value with automatic fallback
 * this.salaryDataService.getSmig().subscribe(result => {
 * });
 */
@Injectable({
  providedIn: 'root'
})
export class SalaryPackageDataService {

  // Cached observables for API results
  private smigCache$?: Observable<PayrollDataResult<number>>;
  private cnssCeilingCache$?: Observable<PayrollDataResult<number>>;
  private cimrEmployeeRateCache$?: Observable<PayrollDataResult<number>>;
  private cimrEmployerRateCache$?: Observable<PayrollDataResult<number>>;
  private allParametersCache$?: Observable<PayrollDataResult<Map<string, LegalParameterDto>>>;

  // Cache timestamps
  private cacheTimestamp: Date | null = null;
  private readonly CACHE_TTL_MS = 5 * 60 * 1000; // 5 minutes

  constructor(private payrollService: PayrollReferentielService) {}

  /**
   * Get the current SMIG value
   */
  getSmig(): Observable<PayrollDataResult<number>> {
    if (this.smigCache$ && !this.isCacheExpired()) {
      return this.smigCache$;
    }

    this.smigCache$ = this.payrollService.getAllLegalParameters().pipe(
      map((params: any) => {
        const smigParam = params.find((p: any) => getLegalParameterType(p.name) === LegalParameterType.SMIG && this.isCurrentlyActive(p));
        if (smigParam) {
          return {
            value: smigParam.value,
            source: 'api' as const,
            lastUpdated: new Date()
          };
        }
        throw new Error('SMIG parameter not found');
      }),
      catchError(err => {
        return of({
          value: LEGACY_SMIG_2025,
          source: 'fallback' as const,
          lastUpdated: new Date()
        });
      }),
      tap(() => this.updateCacheTimestamp()),
      shareReplay(1)
    );

    return this.smigCache$;
  }

  /**
   * Get the CNSS ceiling value
   */
  getCnssCeiling(): Observable<PayrollDataResult<number>> {
    if (this.cnssCeilingCache$ && !this.isCacheExpired()) {
      return this.cnssCeilingCache$;
    }

    this.cnssCeilingCache$ = this.payrollService.getAllLegalParameters().pipe(
      map((params: any) => {
        const ceilingParam = params.find((p: any) =>
          getLegalParameterType(p.name) === LegalParameterType.CNSS && p.name?.toLowerCase().includes('plafond') && this.isCurrentlyActive(p)
        ) || params.find((p: any) =>
          getLegalParameterType(p.name) === LegalParameterType.CNSS && this.isCurrentlyActive(p)
        );
        if (ceilingParam) {
          return {
            value: ceilingParam.value,
            source: 'api' as const,
            lastUpdated: new Date()
          };
        }
        throw new Error('CNSS ceiling parameter not found');
      }),
      catchError(err => {
        return of({
          value: LEGACY_CNSS_CEILING,
          source: 'fallback' as const,
          lastUpdated: new Date()
        });
      }),
      tap(() => this.updateCacheTimestamp()),
      shareReplay(1)
    );

    return this.cnssCeilingCache$;
  }

  /**
   * Get the CIMR employee contribution rate
   */
  getCimrEmployeeRate(): Observable<PayrollDataResult<number>> {
    if (this.cimrEmployeeRateCache$ && !this.isCacheExpired()) {
      return this.cimrEmployeeRateCache$;
    }

    this.cimrEmployeeRateCache$ = this.payrollService.getAllLegalParameters().pipe(
      map((params: any) => {
        const rateParam = params.find((p: any) =>
          getLegalParameterType(p.name) === LegalParameterType.CIMR && (p.name?.toLowerCase().includes('salarié') || p.name?.toLowerCase().includes('employee')) && this.isCurrentlyActive(p)
        ) || params.find((p: any) => getLegalParameterType(p.name) === LegalParameterType.CIMR && this.isCurrentlyActive(p));
        if (rateParam) {
          return {
            value: rateParam.value,
            source: 'api' as const,
            lastUpdated: new Date()
          };
        }
        throw new Error('CIMR employee rate parameter not found');
      }),
      catchError(err => {
        return of({
          value: LEGACY_CIMR_EMPLOYEE_RATE,
          source: 'fallback' as const,
          lastUpdated: new Date()
        });
      }),
      tap(() => this.updateCacheTimestamp()),
      shareReplay(1)
    );

    return this.cimrEmployeeRateCache$;
  }

  /**
   * Get the CIMR employer contribution rate
   */
  getCimrEmployerRate(): Observable<PayrollDataResult<number>> {
    if (this.cimrEmployerRateCache$ && !this.isCacheExpired()) {
      return this.cimrEmployerRateCache$;
    }

    this.cimrEmployerRateCache$ = this.payrollService.getAllLegalParameters().pipe(
      map((params: any) => {
        const rateParam = params.find((p: any) =>
          getLegalParameterType(p.name) === LegalParameterType.CIMR && (p.name?.toLowerCase().includes('employeur') || p.name?.toLowerCase().includes('employer')) && this.isCurrentlyActive(p)
        ) || params.find((p: any) => getLegalParameterType(p.name) === LegalParameterType.CIMR && this.isCurrentlyActive(p));
        if (rateParam) {
          return {
            value: rateParam.value,
            source: 'api' as const,
            lastUpdated: new Date()
          };
        }
        throw new Error('CIMR employer rate parameter not found');
      }),
      catchError(err => {
        return of({
          value: LEGACY_CIMR_EMPLOYER_RATE,
          source: 'fallback' as const,
          lastUpdated: new Date()
        });
      }),
      tap(() => this.updateCacheTimestamp()),
      shareReplay(1)
    );

    return this.cimrEmployerRateCache$;
  }

  /**
   * Get all legal parameters as a map keyed by name
   */
  getAllParameters(): Observable<PayrollDataResult<Map<string, LegalParameterDto>>> {
    if (this.allParametersCache$ && !this.isCacheExpired()) {
      return this.allParametersCache$;
    }

    this.allParametersCache$ = this.payrollService.getAllLegalParameters().pipe(
      map((params: any) => {
        const paramMap = new Map<string, LegalParameterDto>();
        params.forEach((p: any) => {
          if (this.isCurrentlyActive(p) && p.name) {
            paramMap.set(p.name, p);
          }
        });

        return {
          value: paramMap,
          source: 'api' as const,
          lastUpdated: new Date()
        };
      }),
      catchError(err => {
        return of({
          value: new Map<string, LegalParameterDto>(),
          source: 'fallback' as const,
          lastUpdated: new Date()
        });
      }),
      tap(() => this.updateCacheTimestamp()),
      shareReplay(1)
    );

    return this.allParametersCache$;
  }

  /**
   * Get a specific parameter value by name
   */
  getParameterValue(name: string, fallbackValue: number): Observable<PayrollDataResult<number>> {
    return this.payrollService.getAllLegalParameters().pipe(
      map((params: any) => {
        const param = params.find((p: any) => p.name === name && this.isCurrentlyActive(p));
        if (param) {
          return {
            value: param.value,
            source: 'api' as const,
            lastUpdated: new Date()
          };
        }
        throw new Error(`Parameter ${name} not found`);
      }),
      catchError(err => {
        return of({
          value: fallbackValue,
          source: 'fallback' as const,
          lastUpdated: new Date()
        });
      })
    );
  }

  /**
   * Clear all caches to force refresh from API
   */
  clearCache(): void {
    this.smigCache$ = undefined;
    this.cnssCeilingCache$ = undefined;
    this.cimrEmployeeRateCache$ = undefined;
    this.cimrEmployerRateCache$ = undefined;
    this.allParametersCache$ = undefined;
    this.cacheTimestamp = null;
  }

  /**
   * Check if a legal parameter is currently active
   */
  private isCurrentlyActive(param: LegalParameterDto): boolean {
    const now = new Date();
    const effectiveFrom = new Date(param.effectiveFrom);
    const effectiveTo = param.effectiveTo ? new Date(param.effectiveTo) : null;

    return effectiveFrom <= now && (effectiveTo === null || effectiveTo >= now);
  }

  /**
   * Check if cache is expired
   */
  private isCacheExpired(): boolean {
    if (!this.cacheTimestamp) return true;
    return (Date.now() - this.cacheTimestamp.getTime()) > this.CACHE_TTL_MS;
  }

  /**
   * Update cache timestamp
   */
  private updateCacheTimestamp(): void {
    this.cacheTimestamp = new Date();
  }
}
