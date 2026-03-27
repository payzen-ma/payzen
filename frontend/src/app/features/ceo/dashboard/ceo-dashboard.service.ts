import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { environment } from '@environments/environment';

@Injectable({
  providedIn: 'root'
})
export class CeoDashboardService {
  private http = inject(HttpClient);

  getCeoDashboardData(parity?: 'ALL' | 'F' | 'M', fromMonth?: string, toMonth?: string): Observable<any> {
    const params = new URLSearchParams();
    if (parity && parity !== 'ALL') params.set('parity', parity);
    if (fromMonth) params.set('fromMonth', fromMonth);
    if (toMonth) params.set('toMonth', toMonth);
    const query = params.toString();
    const url = `${environment.apiUrl}/DashboardCeo/GetCeoDashboardData${query ? `?${query}` : ''}`;

    return this.http.get<any>(url).pipe(
      catchError(() => of({
        kpis: [],
        evolutionChart: [],
        departments: [],
        payIndicators: [],
        alerts: []
      }))
    );
  }
}
