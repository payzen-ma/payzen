import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '@environments/environment';
import { catchError, map, of } from 'rxjs';
import { DashboardHrApiDto, mapDashboardHrApiToPayload } from './dashboard-hr.api.mapper';
import { normalizeRawApi } from './dashboard-hr.filter-aggregator';
import { DashboardHrMockRepository } from './dashboard-hr.mock.repository';
import { DashboardHrRepository } from './dashboard-hr.repository';
import { DashboardHrQuery } from '../state/dashboard-hr.models';

@Injectable()
export class DashboardHrHttpRepository implements DashboardHrRepository {
  private readonly http = inject(HttpClient);
  private readonly fallback = inject(DashboardHrMockRepository);
  private readonly apiUrl = environment.apiUrl;

  getDashboardData(query: DashboardHrQuery) {
    return this.fetchFromApi(query).pipe(
      catchError(error =>
        this.fallback.getDashboardData(query).pipe(
          map(payload => ({
            ...payload,
            meta: {
              ...payload.meta,
              warnings: [
                ...payload.meta.warnings,
                'Mode fallback mock active: API dashboard indisponible.',
                error instanceof Error ? error.message : 'Erreur reseau inconnue.'
              ]
            }
          }))
        )
      )
    );
  }

  private fetchFromApi(query: DashboardHrQuery) {
    const params = this.buildParams(query);
    const headers = this.buildHeaders(query);

    return this.http
      .get<DashboardHrApiDto>(`${this.apiUrl}/dashboard/hr`, { params, headers })
      .pipe(
        map(response => mapDashboardHrApiToPayload(response))
      );
  }

  getDashboardRawData(query: DashboardHrQuery) {
    const params = this.buildParams(query);
    const headers = this.buildHeaders(query);
    return this.http
      .get<unknown>(`${this.apiUrl}/dashboard/hr/raw`, { params, headers })
      .pipe(map(response => normalizeRawApi(response)));
  }

  private buildParams(query: DashboardHrQuery): HttpParams {
    let params = new HttpParams();

    // Note: companyId is NOT sent as a query param — the backend reads it exclusively
    // from the X-Company-Id header (see ReadCompanyIdHeader() in DashboardControllers.cs).
    if (query.month) {
      params = params.set('month', query.month);
    }

    // Cache buster: prevents browser/CDN from serving stale responses after a context switch.
    params = params.set('_ts', Date.now().toString());

    return params;
  }

  private buildHeaders(query: DashboardHrQuery): HttpHeaders {
    let headers = new HttpHeaders();
    if (query.companyId) {
      headers = headers.set('X-Company-Id', String(query.companyId));
    }
    headers = headers.set('X-Role-Context', query.isExpertMode ? 'expert' : 'standard');
    return headers;
  }
}
