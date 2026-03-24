import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map, switchMap, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { 
  Holiday, 
  CreateHolidayRequest, 
  UpdateHolidayRequest, 
  HolidayScope,
  HolidayCheckResponse 
} from '../models/holiday.model';
import { CompanyContextService } from './companyContext.service';

interface HolidayDto {
  id: number;
  nameFr: string;
  nameAr: string;
  nameEn: string;
  holidayDate: string;
  description?: string;
  companyId?: number;
  companyName?: string;
  countryId: number;
  countryName: string;
  scope: number;
  scopeDescription: string;
  holidayType: string;
  isMandatory: boolean;
  isPaid: boolean;
  isRecurring: boolean;
  recurrenceRule?: string;
  year?: number;
  affectPayroll: boolean;
  affectAttendance: boolean;
  isActive: boolean;
  createdAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class HolidayService {
  private readonly http = inject(HttpClient);
  private readonly companyContext = inject(CompanyContextService);
  private readonly apiUrl = `${environment.apiUrl}/holidays`;

  /**
   * Get all holidays with optional filters
   */
  getHolidays(filters?: {
    countryId?: number;
    companyId?: number;
    scope?: HolidayScope;
    year?: number;
    holidayType?: string;
    isActive?: boolean;
  }): Observable<Holiday[]> {
    let params = new HttpParams();
    
    if (filters) {
      if (filters.countryId !== undefined) {
        params = params.set('countryId', filters.countryId.toString());
      }
      if (filters.companyId !== undefined) {
        params = params.set('companyId', filters.companyId.toString());
      }
      if (filters.scope !== undefined) {
        params = params.set('scope', filters.scope.toString());
      }
      if (filters.year !== undefined) {
        params = params.set('year', filters.year.toString());
      }
      if (filters.holidayType) {
        params = params.set('holidayType', filters.holidayType);
      }
      if (filters.isActive !== undefined) {
        params = params.set('isActive', filters.isActive.toString());
      }
    }

    return this.http.get<HolidayDto[]>(this.apiUrl, { params }).pipe(
      map(holidays => holidays.map(dto => this.mapDtoToHoliday(dto)))
    );
  }

  /**
   * Get holidays for current company
   */
  getCompanyHolidays(year?: number): Observable<Holiday[]> {
    const companyId = this.companyContext.companyId();
    if (!companyId) {
      throw new Error('No company selected');
    }

    return this.getHolidays({
      companyId: parseInt(companyId, 10),
      year: year || new Date().getFullYear()
    });
  }

  /**
   * Get holiday by ID
   */
  getHolidayById(id: number): Observable<Holiday> {
    return this.http.get<HolidayDto>(`${this.apiUrl}/${id}`).pipe(
      map(dto => this.mapDtoToHoliday(dto))
    );
  }

  /**
   * Create a new holiday
   */
  createHoliday(request: CreateHolidayRequest): Observable<Holiday> {
    return this.http.post<any>(this.apiUrl, request).pipe(
      map(response => {
        // Handle null response or wrapped response from CreatedAtAction
        if (!response) {
          // If response is null, return a minimal holiday object
          return {
            ...request,
            id: 0 // Will be set by refresh
          } as Holiday;
        }
        const dto = response.value || response;
        return this.mapDtoToHoliday(dto);
      })
    );
  }

  /**
   * Update an existing holiday
   */
  updateHoliday(id: number, request: UpdateHolidayRequest): Observable<Holiday> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, request).pipe(
      switchMap(response => {
        // If API returns 204 No Content (null response), fetch the updated holiday
        if (!response) {
          return this.getHolidayById(id);
        }
        const dto = response.value || response;
        return of(this.mapDtoToHoliday(dto));
      })
    );
  }

  /**
   * Delete a holiday (soft delete)
   */
  deleteHoliday(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Check if a date is a holiday
   */
  checkHoliday(countryId: number, date: string, companyId?: number): Observable<HolidayCheckResponse> {
    let params = new HttpParams()
      .set('countryId', countryId.toString())
      .set('date', date);

    if (companyId !== undefined) {
      params = params.set('companyId', companyId.toString());
    }

    return this.http.get<HolidayCheckResponse>(`${this.apiUrl}/check`, { params });
  }

  /**
   * Get distinct holiday types
   */
  getHolidayTypes(countryId?: number): Observable<string[]> {
    let params = new HttpParams();
    if (countryId !== undefined) {
      params = params.set('countryId', countryId.toString());
    }

    return this.http.get<string[]>(`${this.apiUrl}/types`, { params });
  }

  /**
   * Map DTO to Holiday model
   */
  private mapDtoToHoliday(dto: HolidayDto): Holiday {
    return {
      id: dto.id,
      nameFr: dto.nameFr || '',
      nameAr: dto.nameAr || '',
      nameEn: dto.nameEn || '',
      holidayDate: dto.holidayDate,
      description: dto.description,
      companyId: dto.companyId,
      companyName: dto.companyName,
      countryId: dto.countryId,
      countryName: dto.countryName || '',
      scope: dto.scope as HolidayScope,
      scopeDescription: dto.scopeDescription || '',
      holidayType: dto.holidayType || '',
      isMandatory: dto.isMandatory,
      isPaid: dto.isPaid,
      isRecurring: dto.isRecurring,
      recurrenceRule: dto.recurrenceRule,
      year: dto.year,
      affectPayroll: dto.affectPayroll,
      affectAttendance: dto.affectAttendance,
      isActive: dto.isActive,
      createdAt: dto.createdAt
    };
  }
}
