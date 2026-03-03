import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { StateEmploymentProgram } from '../models/state-employment-program.model';
import { TranslateService } from '@ngx-translate/core';

export interface StateEmploymentProgramOption {
  id: number;
  label: string;
  value: number;
}

@Injectable({
  providedIn: 'root'
})
export class StateEmploymentProgramService {
  private http = inject(HttpClient);
  private translate = inject(TranslateService);
  private apiUrl = `${environment.apiUrl}/state-employment-programs`;

  getAll(): Observable<StateEmploymentProgram[]> {
    console.log('[StateEmploymentProgramService] Fetching from:', this.apiUrl);
    return this.http.get<any[]>(this.apiUrl).pipe(
      map(items => {
        console.log('[StateEmploymentProgramService] Received:', items);
        return (items || []).map(i => this.normalize(i));
      })
    );
  }

  getOptions(): Observable<StateEmploymentProgramOption[]> {
    return this.getAll().pipe(
      map(items => items
        .filter(i => i.isActive)
        .map(i => ({
          id: i.id,
          label: this.getLocalizedName(i),
          value: i.id
        }))
      )
    );
  }

  private normalize(item: any): StateEmploymentProgram {
    return {
      id: item.Id ?? item.id ?? 0,
      code: item.Code ?? item.code ?? '',
      name: item.Name ?? item.name ?? '',
      isActive: item.IsActive ?? item.isActive ?? true
    };
  }

  private getLocalizedName(item: StateEmploymentProgram): string {
    return item.name;
  }
}
