import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { Company, CompanyCreateRequest, CompanyUpdateRequest, CompanyFormData, CompanyCreateResponse, PartialUpdateRequest } from '../models/company.model';

@Injectable({
  providedIn: 'root'
})
export class CompanyService {
  private baseUrl = 'http://localhost:5119';
  private apiUrl = `${this.baseUrl}/api/companies`;

  constructor(private http: HttpClient) {}

  /**
   * Transform API response from PascalCase to camelCase
   */
  private transformCompany(data: any): Company {
    // Normalize status to the union type 'active' | 'inactive'
    const statusVal: 'active' | 'inactive' = ((): 'active' | 'inactive' => {
      if (typeof data.IsActive === 'boolean') return data.IsActive ? 'active' : 'inactive';
      if (typeof data.isActive === 'boolean') return data.isActive ? 'active' : 'inactive';
      if (data.Status !== undefined && data.Status !== null) {
        const s = String(data.Status).toLowerCase();
        return s === 'inactive' ? 'inactive' : 'active';
      }
      return 'active';
    })();

    return {
      id: data.Id,
      companyName: data.CompanyName,
      isCabinetExpert: data.IsCabinetExpert,
      email: data.Email,
      phoneNumber: data.PhoneNumber,
      countryCode: data.CountryCode || '+212',
      cityName: data.CityName,
      countryName: data.CountryName,
      companyAddress : data.CompanyAddress,
      cnssNumber: data.CnssNumber,
      createdAt: data.CreatedAt,
      // Use normalized status
      status: statusVal,
      // Keep explicit isActive flag for frontend use when provided by API
      ...(typeof data.IsActive === 'boolean' ? { isActive: data.IsActive } : {}),
      ...(typeof data.isActive === 'boolean' ? { isActive: data.isActive } : {}),
      // Legal & Fiscal (optional)
      iceNumber: data.IceNumber || data.Ice || data.IceNum || data.ICE || data.ICE_Number || data.Ice_Number,
      ifNumber: data.IfNumber || data.IdentifiantFiscalNumber || data.TaxIdentifier || data.IdentifiantFiscal,
      rcNumber:  data.RcNumber,
      legalForm: data.LegalForm,
      foundingDate: data.FoundingDate,
      patentNumber: data.PatentNumber || data.PatenteNumber || data.Patent || data.patentNumber,
    };
  }

  /**
   * Transform partial update keys from camelCase (or French keys) to API PascalCase
   */
  private transformPartialUpdateToApiFormat(data: PartialUpdateRequest): any {
    const result: any = {};
    if ('companyName' in data && data.companyName !== undefined) result.CompanyName = data.companyName;
    if ('email' in data && data.email !== undefined) result.Email = data.email;
    if ('phoneNumber' in data && data.phoneNumber !== undefined) result.PhoneNumber = data.phoneNumber;
    if ('address' in data && data.address !== undefined) result.CompanyAddress = data.address;
    if ('cityName' in data && data.cityName !== undefined) result.CityName = data.cityName;
    if ('countryName' in data && data.countryName !== undefined) result.CountryName = data.countryName;
    if ('cnssNumber' in data && data.cnssNumber !== undefined) result.CnssNumber = data.cnssNumber;
    if ('status' in data && data.status !== undefined) result.Status = data.status;
    if ('isCabinetExpert' in data && data.isCabinetExpert !== undefined) result.IsCabinetExpert = data.isCabinetExpert;
    // Send camelCase `isActive` as backend expects { "isActive": true/false }
    if ('isActive' in data && data.isActive !== undefined) result.isActive = data.isActive;
    if ('licence' in data && data.licence !== undefined) result.Licence = data.licence;

    // Legal & Fiscal mappings (accept multiple frontend key variants)
    if ('iceNumber' in data && data.iceNumber !== undefined) result.IceNumber = data.iceNumber;
    if ('IceNumber' in (data as any) && (data as any).IceNumber !== undefined) result.IceNumber = (data as any).IceNumber;
    if ('identifiantFiscal' in data && data.identifiantFiscal !== undefined) result.IfNumber = data.identifiantFiscal;
    if ('ifNumber' in data && data.ifNumber !== undefined) result.IfNumber = data.ifNumber;
    if ('rcNumber' in data && data.rcNumber !== undefined) result.RcNumber = data.rcNumber;
    if ('formeJuridique' in data && data.formeJuridique !== undefined) result.LegalForm = data.formeJuridique;
    if ('foundingDate' in data && data.foundingDate !== undefined) result.FoundingDate = data.foundingDate;
    // Patent / Patente
    if ('patentNumber' in data && data.patentNumber !== undefined) result.PatentNumber = data.patentNumber;
    if ('patenteNumber' in (data as any) && (data as any).patenteNumber !== undefined) result.PatentNumber = (data as any).patenteNumber;

    return result;
  }

  /**
   * Get all companies
   */
  getAllCompanies(): Observable<Company[]> {
    return this.http.get<any[]>(this.apiUrl).pipe(
      map(companies => companies.map(c => this.transformCompany(c)))
    );
  }

  /**
   * Get company by ID
   */
  getCompanyById(id: number): Observable<Company> {
    return this.http.get<any>(`${this.apiUrl}/${id}`).pipe(
      map(c => this.transformCompany(c))
    );
  }

  /**
   * Search companies by term
   */
  searchCompanies(searchTerm: string): Observable<Company[]> {
    const params = new HttpParams().set('searchTerm', searchTerm);
    return this.http.get<any[]>('/api/company/search', { params }).pipe(
      map(companies => companies.map(c => this.transformCompany(c)))
    );
  }

  /**
   * Get all cabinet experts (accounting firms)
   */
  getCabinetsExperts(): Observable<Company[]> {
    return this.http.get<any[]>('/api/company/cabinets-experts').pipe(
      map(companies => companies.map(c => this.transformCompany(c)))
    );
  }

  /**
   * Create a new company
   */
  createCompany(company: CompanyCreateRequest): Observable<CompanyCreateResponse> {
    const payload: any = {
      CompanyName: company.companyName,
      CompanyEmail: company.email,
      CompanyPhoneNumber: company.phoneNumber,
      CountryPhoneCode: company.countryPhoneCode,
      CompanyAddress: company.companyAddress,
      CountryId: company.countryId,
      CityId: company.cityId,
      CityName: company.cityName,
      CnssNumber: company.cnssNumber,
      IsCabinetExpert: company.isCabinetExpert,
      // Admin account
      AdminFirstName: company.adminFirstName,
      AdminLastName: company.adminLastName,
      AdminEmail: company.adminEmail,
      AdminDateOfBirth: company.adminDateOfBirth,
      AdminPhone: company.adminPhone
    };

    return this.http.post<CompanyCreateResponse>(this.apiUrl, payload);
  }

  /**
   * Update an existing company
   */
  updateCompany(id: number, company: CompanyUpdateRequest): Observable<Company> {
    return this.http.put<any>(`${this.apiUrl}/${id}`, company).pipe(
      map(c => this.transformCompany(c))
    );
  }

  /**
   * PATCH an existing company
   */
  patchCompany(id: number, company: PartialUpdateRequest): Observable<Company> {
    const payload = this.transformPartialUpdateToApiFormat(company);
    return this.http.patch<any>(`${this.apiUrl}/${id}`, payload).pipe(
      map(c => this.transformCompany(c))
    );
  }
  /**
   * Delete a company
   */
  deleteCompany(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get form data (cities and countries)
   */
  getFormData(): Observable<CompanyFormData> {
    return this.http.get<any>(`${this.apiUrl}/form-data`).pipe(
      map(data => ({
        cities: (data.Cities || data.cities || []).map((city: any) => ({
          id: city.Id || city.id,
          name: city.CityName || city.cityName || city.name,
          countryId: city.CountryId || city.countryId,
          countryName: city.CountryName || city.countryName
        })),
        countries: (data.Countries || data.countries || []).map((country: any) => ({
          id: country.Id || country.id,
          name: country.CountryName || country.countryName || country.name,
          code: country.CountryCode || country.countryCode || country.code,
          phoneCode: country.CountryPhoneCode || country.countryPhoneCode || country.phoneCode,
          nameAr: country.CountryNameAr || country.countryNameAr
        }))
      }))
    );
  }
}
