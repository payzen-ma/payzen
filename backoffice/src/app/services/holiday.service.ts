import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { HolidayReadDto, HolidayCreateDto, HolidayUpdateDto, HolidayScope } from '../models/holiday.model';

@Injectable({
  providedIn: 'root'
})
export class HolidayService {
  private baseUrl = 'http://localhost:5119';
  private apiUrl = `${this.baseUrl}/api/holidays`;
  // (No client-side translation provider used; backend should handle translations)

  constructor(private http: HttpClient) {}

  /**
   * Transform API response from PascalCase to camelCase
   */
  private transformHoliday(data: any): HolidayReadDto {
    if (!data) {
      throw new Error('Cannot transform null or undefined holiday data');
    }
    return {
      id: data.Id,
      nameFr: data.NameFr,
      nameAr: data.NameAr,
      nameEn: data.NameEn,
      holidayDate: data.HolidayDate,
      description: data.Description,
      companyId: data.CompanyId,
      companyName: data.CompanyName,
      countryId: data.CountryId,
      countryName: data.CountryName,
      scope: data.Scope,
      scopeDescription: data.ScopeDescription,
      holidayType: data.HolidayType,
      isMandatory: data.IsMandatory,
      isPaid: data.IsPaid,
      isRecurring: data.IsRecurring,
      recurrenceRule: data.RecurrenceRule,
      year: data.Year,
      affectPayroll: data.AffectPayroll,
      affectAttendance: data.AffectAttendance,
      // Backend uses soft-delete with DeletedAt timestamp; derive isActive accordingly
      isActive: (data.IsActive !== undefined) ? data.IsActive : !(data.DeletedAt),
      createdAt: data.CreatedAt || data.createdAt,
      // expose DeletedAt in case UI needs it
      deletedAt: data.DeletedAt
    };
  }

  /**
   * Transform camelCase to PascalCase for API request
   */
  private transformToApiFormat(data: HolidayCreateDto | HolidayUpdateDto): any {
    const result: any = {};
    
    if ('nameFr' in data && data.nameFr !== undefined) result.NameFr = data.nameFr;
    if ('nameAr' in data && data.nameAr !== undefined) result.NameAr = data.nameAr;
    if ('nameEn' in data && data.nameEn !== undefined) result.NameEn = data.nameEn;
    if ('holidayDate' in data && data.holidayDate !== undefined) result.HolidayDate = data.holidayDate;
    if ('description' in data && data.description !== undefined) result.Description = data.description;
    if ('companyId' in data) result.CompanyId = data.companyId;
    if ('countryId' in data && data.countryId !== undefined) result.CountryId = data.countryId;
    if ('scope' in data && data.scope !== undefined) result.Scope = data.scope;
    if ('holidayType' in data && data.holidayType !== undefined) result.HolidayType = data.holidayType;
    if ('isMandatory' in data && data.isMandatory !== undefined) result.IsMandatory = data.isMandatory;
    if ('isPaid' in data && data.isPaid !== undefined) result.IsPaid = data.isPaid;
    if ('isRecurring' in data && data.isRecurring !== undefined) result.IsRecurring = data.isRecurring;
    if ('recurrenceRule' in data && data.recurrenceRule !== undefined) result.RecurrenceRule = data.recurrenceRule;
    if ('year' in data && data.year !== undefined) result.Year = data.year;
    if ('affectPayroll' in data && data.affectPayroll !== undefined) result.AffectPayroll = data.affectPayroll;
    if ('affectAttendance' in data && data.affectAttendance !== undefined) result.AffectAttendance = data.affectAttendance;
    if ('isActive' in data && data.isActive !== undefined) result.IsActive = data.isActive;

    return result;
  }

  /**
   * Get all holidays (global and company-specific)
   */
  getAllHolidays(includeInactive: boolean = false): Observable<HolidayReadDto[]> {
    let params = new HttpParams();
    if (!includeInactive) {
      params = params.set('isActive', 'true');
    }
    
    return this.http.get<any[]>(this.apiUrl, { params }).pipe(
      map(holidays => holidays.map(h => this.transformHoliday(h)))
    );
  }

  /**
   * Get global holidays only (where CompanyId is null)
   */
  getGlobalHolidays(countryId?: number, includeInactive: boolean = false, year?: number): Observable<HolidayReadDto[]> {
    let params = new HttpParams();
    params = params.set('scope', HolidayScope.Global.toString());
    if (countryId) {
      params = params.set('countryId', countryId.toString());
    }
    if (year !== undefined && year !== null) {
      params = params.set('year', year.toString());
    }
    if (!includeInactive) {
      params = params.set('isActive', 'true');
    }

    return this.http.get<any[]>(this.apiUrl, { params }).pipe(
      map(holidays => holidays.map(h => this.transformHoliday(h)))
    );
  }

  /**
   * Get holidays by company
   */
  getHolidaysByCompany(companyId: number, includeInactive: boolean = false): Observable<HolidayReadDto[]> {
    let params = new HttpParams();
    params = params.set('companyId', companyId.toString());
    if (!includeInactive) {
      params = params.set('isActive', 'true');
    }
    
    return this.http.get<any[]>(this.apiUrl, { params }).pipe(
      map(holidays => holidays.map(h => this.transformHoliday(h)))
    );
  }

  /**
   * Get holiday by ID
   */
  getHolidayById(id: number): Observable<HolidayReadDto> {
    return this.http.get<any>(`${this.apiUrl}/${id}`).pipe(
      map(h => this.transformHoliday(h))
    );
  }

  /**
   * Create a new holiday
   */
  createHoliday(holiday: HolidayCreateDto): Observable<HolidayReadDto> {
    const apiData = this.transformToApiFormat(holiday);
    return this.http.post<any>(this.apiUrl, apiData).pipe(
      map(h => {
        console.log('createHoliday: raw response=', h, 'request=', apiData);
        if (!h) {
          // Some backends return 201 with empty body. Return a best-effort DTO based on the request.
          return {
            id: 0,
            nameFr: apiData.NameFr || '',
            nameAr: apiData.NameAr || '',
            nameEn: apiData.NameEn || '',
            holidayDate: apiData.HolidayDate,
            description: apiData.Description,
            companyId: apiData.CompanyId,
            countryId: apiData.CountryId,
            countryName: '',
            scope: apiData.Scope ?? 0,
            scopeDescription: '',
            holidayType: apiData.HolidayType || '',
            isMandatory: apiData.IsMandatory ?? true,
            isPaid: apiData.IsPaid ?? true,
            isRecurring: apiData.IsRecurring ?? false,
            recurrenceRule: apiData.RecurrenceRule,
            year: apiData.Year,
            affectPayroll: apiData.AffectPayroll ?? true,
            affectAttendance: apiData.AffectAttendance ?? true,
            isActive: apiData.IsActive ?? true,
            createdAt: new Date().toISOString(),
            deletedAt: null
          } as HolidayReadDto;
        }
        return this.transformHoliday(h);
      })
    );
  }

  /**
   * Update an existing holiday
   */
  updateHoliday(id: number, holiday: HolidayUpdateDto): Observable<void> {
    const apiData = this.transformToApiFormat(holiday);
    return this.http.put<void>(`${this.apiUrl}/${id}`, apiData);
  }

  /**
   * Delete a holiday
   */
  deleteHoliday(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get holidays for a specific year
   */
  getHolidaysByYear(year: number, countryId?: number): Observable<HolidayReadDto[]> {
    let params = new HttpParams();
    params = params.set('year', year.toString());
    if (countryId) {
      params = params.set('countryId', countryId.toString());
    }
    
    return this.http.get<any[]>(this.apiUrl, { params }).pipe(
      map(holidays => holidays.map(h => this.transformHoliday(h)))
    );
  }

  /**
   * Check if a specific date is a holiday
   */
  checkHoliday(countryId: number, date: string, companyId?: number): Observable<any> {
    let params = new HttpParams();
    params = params.set('countryId', countryId.toString());
    params = params.set('date', date);
    if (companyId) {
      params = params.set('companyId', companyId.toString());
    }
    
    return this.http.get<any>(`${this.apiUrl}/check`, { params });
  }

  /**
   * Get distinct holiday types
   */
  getHolidayTypes(countryId?: number): Observable<string[]> {
    let params = new HttpParams();
    if (countryId) {
      params = params.set('countryId', countryId.toString());
    }
    
    return this.http.get<string[]>(`${this.apiUrl}/types`, { params });
  }

  /**
   * Fetch holidays from Calendarific public API (client-side call)
   * Note: this method calls the external API directly from the frontend and
   * therefore requires an API key to be provided by the client.
   */
  fetchExternalHolidays(apiKey: string, countryCode: string, year: number): Observable<any[]> {
    const url = 'https://calendarific.com/api/v2/holidays';
    let params = new HttpParams()
      .set('api_key', apiKey)
      .set('country', countryCode)
      .set('year', year.toString());

    // Debugging logs: external URL, params and full response
    console.log('fetchExternalHolidays: url=', url, 'params=', params.toString());

    return this.http.get<any>(url, { params }).pipe(
      map(r => {
        console.log('fetchExternalHolidays: raw response=', r);
        return r && r.response && r.response.holidays ? r.response.holidays : [];
      })
    );
  }

  /**
   * Quick client-side translation using LibreTranslate public endpoint.
   * Note: this is intended for quick imports / dev only. For production use a server-side proxy and paid provider.
   */
  // removed client-side LibreTranslate usage — translations should come from backend or external API
}
