import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, Subject, map, of, throwError, tap } from 'rxjs';
import { forkJoin } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Company, CompanyEvent, TaxRegime, CompanyCreateByExpertDto } from '../models/company.model';
import { AuthService } from './auth.service';
import { CompanyContextService } from './companyContext.service';

interface CityDto {
  id: number;
  cityName: string;
  countryId: number;
  countryName: string;
}

interface CompanyDto {
  id: number;
  companyName: string;
  companyAddress: string;
  cityName: string;
  countryName: string;
  cnssNumber: string;
  iceNumber: string;
  employeeCount?: number;
  totalEmployees?: number;
  rcNumber?: string;
  ifNumber?: string;
  ribNumber?: string;
  patente?: string;
  phoneNumber: string;
  email: string;
  createdAt: string;
  website?: string;
  taxRegime?: string;
  // Add other fields as needed based on backend response
}

interface CompanyUpdateDto {
  CompanyName?: string;
  Email?: string;
  PhoneNumber?: string;
  CompanyAddress?: string;
  CityName?: string;
  // Legal entity fields (admin-only)
  IceNumber?: string;
  RcNumber?: string;
  Patente?: string;
  CnssNumber?: string;
  IfNumber?: string;
  RibNumber?: string;
  TaxRegime?: string;
  WebsiteUrl?: string;
  LegalForm?: string;
}

  // Mapping configuration from frontend model to backend DTO
const COMPANY_FIELD_MAP: Partial<Record<keyof Company, keyof CompanyUpdateDto>> = {
  legalName: 'CompanyName',
  email: 'Email',
  phone: 'PhoneNumber',
  address: 'CompanyAddress',
  city: 'CityName',
  // Legal entity fields (admin-only)
  ice: 'IceNumber',
  rc: 'RcNumber',
  patente: 'Patente',
  cnss: 'CnssNumber',
  if: 'IfNumber',
  rib: 'RibNumber',
  legalForm: 'LegalForm',
  website: 'WebsiteUrl',
};

@Injectable({
  providedIn: 'root'
})
export class CompanyService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private contextService = inject(CompanyContextService);
  private apiUrl = `${environment.apiUrl}`;

  // Subject to notify when company data is updated
  private companyUpdated$ = new Subject<void>();
  // Public observable for components to subscribe to
  readonly onCompanyUpdate$ = this.companyUpdated$.asObservable();

  getManagedCompanies(): Observable<Company[]> {
    // Use the expert's cabinet/company id to request only companies managed by that expert
    const expertId = this.contextService.currentContext()?.cabinetId || this.authService.currentUser()?.companyId;
    if (!expertId) {
      return of([]);
    }

    return this.http.get<CompanyDto[]>(`${this.apiUrl}/companies/managedby/${expertId}`).pipe(
      map(dtos => dtos.map(dto => this.mapDtoToCompany(dto)))
    );
  }

  getCompany(): Observable<Company> {
    // Prioritize context company ID (for Expert Client View or Standard Context)
    // Fallback to Auth User's company ID (Legacy/Direct access)
    const companyId = this.contextService.companyId() || this.authService.currentUser()?.companyId;
    
    if (!companyId) {
      // Fallback or error if no company ID is available
      return of({} as Company); 
    }

    return this.http.get<CompanyDto>(`${this.apiUrl}/companies/${companyId}`).pipe(
      map(dto => this.mapDtoToCompany(dto))
    );
  }

  /**
   * Fetch employee count for a specific company by calling the employee summary
   * endpoint while forcing `X-Company-Id` header to the target company.
   * This is a fallback approach when the managed-by endpoint doesn't include counts.
   */
  getCompanyEmployeeCount(companyId: string): Observable<number> {
    if (!companyId) return of(0);

    const url = `${this.apiUrl}/employee/summary`;
    const params = new HttpParams().set('companyId', companyId);
    const headers = { 'X-Company-Id': String(companyId) };

    return this.http.get<any>(url, { params, headers }).pipe(
      map(res => res?.totalEmployees ?? 0)
    );
  }

  updateCompany(company: Partial<Company>): Observable<Company> {
    const companyId = company.id || this.contextService.companyId() || this.authService.currentUser()?.companyId;
    
    if (!companyId) {
      console.log('UpdateCompany: No company ID found');
      return throwError(() => new Error('Company ID is required for update'));
    }

    const updateDto = this.mapCompanyToUpdateDto(company);
    // If hrParameters are provided in the partial company, include them in the payload
    if ((company as any).hrParameters) {
      (updateDto as any).hrParameters = (company as any).hrParameters;
    }

    // Debug: log the final payload to help trace why HR params may not be persisted
    console.debug('[CompanyService] updateCompany payload:', { companyId, updateDto, rawCompany: company });
    const url = `${this.apiUrl}/companies/${companyId}`;

    return this.http.patch<CompanyDto>(url, updateDto).pipe(
      map(dto => {
        const mappedCompany = this.mapDtoToCompany(dto);
        // Notify subscribers that company data has been updated
        this.companyUpdated$.next();
        return mappedCompany;
      })
    );
  }

  /**
   * Maps frontend Company model to backend CompanyUpdateDto
   * Only includes fields that are present in the partial Company object
   */
  private mapCompanyToUpdateDto(company: Partial<Company>): CompanyUpdateDto {
    const updateDto: CompanyUpdateDto = {};

    // Iterate over the field mapping and include only fields present in the input
    (Object.keys(COMPANY_FIELD_MAP) as Array<keyof typeof COMPANY_FIELD_MAP>).forEach(frontendKey => {
      if (frontendKey in company) {
        const backendKey = COMPANY_FIELD_MAP[frontendKey];
        if (backendKey) {
          updateDto[backendKey] = company[frontendKey] as string;
        }
      }
    });

    return updateDto;
  }

  createCompanyByExpert(companyData: CompanyCreateByExpertDto): Observable<any> {
    return this.http.post(`${this.apiUrl}/companies/create-by-expert`, companyData);
  }

  searchCities(query: string): Observable<string[]> {
    // The backend /api/cities returns all cities. We filter them client-side.
    return this.http.get<CityDto[]>(`${this.apiUrl}/cities`).pipe(
      map(cities => {
        if (!query) return cities.map(c => c.cityName);
        const lowerQuery = query.toLowerCase();
        return cities
          .filter(c => c.cityName.toLowerCase().includes(lowerQuery))
          .map(c => c.cityName);
      })
    );
  }

  updateLogo(file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    // Endpoint to be confirmed, keeping as is for now
    return this.http.post(`${this.apiUrl}/company/logo`, formData);
  }

  getCompanyHistory(): Observable<CompanyEvent[]> {
    const companyId = this.authService.currentUser()?.companyId;
    if (!companyId) {
      return of([]);
    }

    return this.http.get<any[]>(`${this.apiUrl}/companies/${companyId}/history`).pipe(
      map(events => events.map(event => this.mapEventDto(event)))
    );
  }

  private mapEventDto(dto: any): CompanyEvent {
    return {
      type: dto.type || dto.eventType || 'general_update',
      title: dto.title || dto.eventTitle || '',
      date: dto.date || dto.eventDate || '',
      description: dto.description || dto.eventDescription || '',
      details: dto.details || {},
      modifiedBy: dto.modifiedBy ? {
        name: dto.modifiedBy.name || dto.modifiedBy.userName || '',
        role: dto.modifiedBy.role || dto.modifiedBy.userRole || ''
      } : undefined,
      timestamp: dto.timestamp || dto.createdAt || new Date().toISOString()
    };
  }

  private mapDtoToCompany(dto: CompanyDto): Company {
    console.debug('[CompanyService] mapping CompanyDto:', dto);
    // Resolve tax regime and legal form from possible backend fields
    const rawTax = (dto as any).taxRegime || (dto as any).TaxRegime || (dto as any).tax_regime || '';
    let resolvedTax: TaxRegime = TaxRegime.IS;
    if (rawTax) {
      const up = String(rawTax).toUpperCase();
      if (up.includes('IR')) resolvedTax = TaxRegime.IR;
      else if (up.includes('AUTO')) resolvedTax = TaxRegime.AUTO_ENTREPRENEUR;
      else resolvedTax = TaxRegime.IS;
    }

    const legalForm = (dto as any).legalForm || (dto as any).LegalForm || (dto as any).legal_form || '';

    return {
      id: dto.id.toString(),
      legalName: (dto as any).companyName || (dto as any).legalName || (dto as any).name || String(dto.id),
      ice: dto.iceNumber,
      rc: dto.rcNumber,
      cnss: dto.cnssNumber,
      if: dto.ifNumber,
      rib: dto.ribNumber,
      patente: (dto as any).patente || (dto as any).Patente || (dto as any).patent || (dto as any).patenteNumber || '',
      address: dto.companyAddress,
      city: dto.cityName,
      country: dto.countryName || 'Maroc', // Default if missing
      email: dto.email,
      phone: dto.phoneNumber,
      website: (dto as any).website || (dto as any).websiteUrl || (dto as any).WebsiteUrl || '',
      legalForm: legalForm,
      // Map other fields with defaults
      postalCode: '',
      taxRegime: resolvedTax,
      fiscalYear: new Date().getFullYear(),
      employeeCount: (dto as any).employeeCount ?? (dto as any).totalEmployees ?? 0,
      hrParameters: {
        workingDays: [],
        workingHoursPerDay: 8,
        workingHoursPerWeek: 44,
        leaveCalculationMode: 'working_days',
        absenceCalculationMode: 'hourly',
        annualLeaveDays: 18,
        publicHolidays: [],
        probationPeriodDays: 90,
        noticePeriodDays: 30
      },
      documents: {
        cnss_attestation: null,
        amo: null,
        logo: null,
        rib: null,
        other: []
      },
      isActive: true,
      createdAt: new Date(dto.createdAt),
      updatedAt: new Date(dto.createdAt)
    };
  }
}