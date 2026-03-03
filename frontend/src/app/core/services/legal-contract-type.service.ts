import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { LegalContractType } from '../models/legal-contract-type.model';
import { TranslateService } from '@ngx-translate/core';

export interface LegalContractTypeOption {
  id: number;
  label: string;
  value: number;
}

@Injectable({
  providedIn: 'root'
})
export class LegalContractTypeService {
  private http = inject(HttpClient);
  private translate = inject(TranslateService);
  private apiUrl = `${environment.apiUrl}/legal-contract-types`;

  getAll(): Observable<LegalContractType[]> {
    console.log('[LegalContractTypeService] Fetching from:', this.apiUrl);
    return this.http.get<any[]>(this.apiUrl).pipe(
      map(items => {
        console.log('[LegalContractTypeService] Received:', items);
        return (items || []).map(i => this.normalize(i));
      })
    );
  }

  getOptions(): Observable<LegalContractTypeOption[]> {
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

  private normalize(item: any): LegalContractType {
    return {
      id: item.Id ?? item.id ?? 0,
      code: item.Code ?? item.code ?? '',
      name: item.Name ?? item.name ?? '',
      isActive: item.IsActive ?? item.isActive ?? true
    };
  }

  private getLocalizedName(item: LegalContractType): string {
    return item.name;
  }
}
