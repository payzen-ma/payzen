import { HttpClient, HttpParams } from '@angular/common/http';
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

    return this.http
      .get<DashboardHrApiDto>(`${this.apiUrl}/dashboard/hr`, { params })
      .pipe(
        map(response => mapDashboardHrApiToPayload(response))
      );
  }

  getDashboardRawData(query: DashboardHrQuery) {
    const params = this.buildParams(query);
    return this.http
      .get<unknown>(`${this.apiUrl}/dashboard/hr/raw`, { params })
      .pipe(map(response => normalizeRawApi(response)));
  }

  private buildParams(query: DashboardHrQuery): HttpParams {
    let params = new HttpParams();

    if (query.companyId) {
      params = params.set('companyId', query.companyId);
    }

    if (query.month) {
      params = params.set('month', query.month);
    }

    return params;
  }
}
