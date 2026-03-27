import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, switchMap, startWith, tap } from 'rxjs/operators';
import { TranslateService } from '@ngx-translate/core';
import { environment } from '@environments/environment';
import { Employee as EmployeeProfileModel, EmployeeSalaryPackageAssignment } from '@app/core/models/employee.model';
import { CompanyContextService } from '@app/core/services/companyContext.service';

// ... [Keep all your existing interfaces: Employee, EmployeeFilters, etc.] ...
export interface Employee {
  id: string;
  firstName: string;
  lastName: string;
  position: string;
  department: string;
  status?: string;
  // raw status code returned by backend (e.g. 'ACTIVE', 'RESIGNED')
  statusRaw?: string;
  // localized label coming from backend (NameFr/NameEn/NameAr)
  statusName?: string;
  // Role information (preserve raw role fields from backend)
  roleName?: any;
  role?: any;
  roles?: any;
  startDate: string;
  missingDocuments: number;
  contractType: string;
  manager?: string;
  userId?: string | number;
}

export interface EmployeeFilters {
  searchQuery?: string;
  department?: string;
  status?: string;
  contractType?: string;
  companyId?: string | number;
  page?: number;
  limit?: number;
}

export interface EmployeesResponse {
  employees: Employee[];
  total: number;
  active: number;
  departments: string[];
  statuses: LookupOption[];
}

export interface EmployeeStats {
  total: number;
  active: number;
}

// ... [Keep other interfaces: LookupOption, EmployeeFormData, etc.] ...
// ... [Keep interfaces: LookupResponseItem, CountryResponseItem, etc.] ...
interface LookupResponseItem {
  id: number;
  name: string;
}

interface CountryResponseItem {
  id: number;
  countryName: string;
  countryPhoneCode: string;
}

interface CityResponseItem {
  id: number;
  cityName: string;
  countryId: number;
  countryName: string;
}

interface DepartementResponseItem {
  id: number;
  departementName: string;
  companyId: number;
}

interface JobPositionResponseItem {
  id: number;
  name: string;
  companyId: number;
}

interface ContractTypeResponseItem {
  id: number;
  contractTypeName: string;
  companyId: number;
}

interface PotentialManagerResponseItem {
  id: number;
  firstName: string;
  lastName: string;
  fullName: string;
  departementName: string;
}

interface EmployeeFormDataResponse {
  statuses?: LookupResponseItem[];
  genders?: LookupResponseItem[];
  educationLevels?: LookupResponseItem[];
  maritalStatuses?: LookupResponseItem[];
  nationalities?: LookupResponseItem[];
  countries?: CountryResponseItem[];
  cities?: CityResponseItem[];
  departements?: DepartementResponseItem[];
  jobPositions?: JobPositionResponseItem[];
  contractTypes?: ContractTypeResponseItem[];
  potentialManagers?: PotentialManagerResponseItem[];
  employeeCategories?: { id?: number; Id?: number; name?: string; Name?: string }[];
}

export interface LookupOption {
  id: number;
  label: string;
  value?: string;
}

export interface CountryLookupOption extends LookupOption {
  phoneCode: string;
}

export interface CityLookupOption extends LookupOption {
  countryId: number;
  countryName: string;
}

export interface ManagerLookupOption extends LookupOption {
  departmentName: string;
}

export interface EmployeeFormData {
  statuses: LookupOption[];
  genders: LookupOption[];
  educationLevels: LookupOption[];
  maritalStatuses: LookupOption[];
  nationalities: LookupOption[];
  countries: CountryLookupOption[];
  cities: CityLookupOption[];
  departments: LookupOption[];
  jobPositions: LookupOption[];
  contractTypes: LookupOption[];
  potentialManagers: ManagerLookupOption[];
  attendanceTypes?: LookupOption[];
  employeeCategories?: LookupOption[];
}

export interface CreateEmployeeRequest {
  firstName: string;
  lastName: string;
  cinNumber?: string | null;
  dateOfBirth: string;
  phone: string;
  email: string;
  statusId: number;
  // Rôle utilisé pour envoyer une invitation d’activation (sans mot de passe)
  inviteRoleId?: number | null;
  genderId?: number | null;
  educationLevelId?: number | null;
  maritalStatusId?: number | null;
  nationalityId?: number | null;
  countryId?: number | null;
  cityId?: number | null;
  countryPhoneCode?: string | null;
  addressLine1?: string | null;
  addressLine2?: string | null;
  zipCode?: string | null;
  departementId?: number | null;
  jobPositionId?: number | null;
  contractTypeId?: number | null;
  managerId?: number | null;
  startDate?: string | null;
  salary?: number | null;
  salaryHourly?: number | null;
  salaryEffectiveDate?: string | null;
  cnssNumber?: string | null;
  cimrNumber?: string | null;
  attendanceTypeId?: number | null;
  employeeCategoryId?: number | null;
  companyId?: number | null;
}

interface DashboardEmployee {
  id: string | number;
  firstName: string;
  lastName: string;
  position: string;
  department: string;
  status: string;
  startDate: string;
  missingDocuments: number;
  contractType: string;
  manager?: string | null;
}

interface DashboardEmployeesResponse {
  totalEmployees: number;
  activeEmployees: number;
  employees: DashboardEmployee[];
  departments?: string[];
  statuses?: string[];
}

interface EmployeeAddressResponse {
  addressLine1?: string;
  addressLine2?: string;
  zipCode?: string;
  cityId?: number;
  cityName?: string;
  countryId?: number;
  countryName?: string;
}

interface SalaryComponentResponse {
  componentName: string;
  amount: number;
  isTaxable?: boolean;
  IsTaxable?: boolean; // Backend returns PascalCase
}

export interface NonImposableOption {
  code: string;
  label: string;
}

interface BackendEventResponse {
  type: string;
  title: string;
  date: string;
  description: string;
  details?: any;
  modifiedBy?: {
    name: string;
    role: string;
  };
  timestamp: string;
}

interface EmployeeDetailsResponse {
  id: string | number;
  firstName: string;
  lastName: string;
  cinNumber: string;
  maritalStatusName: string;
  dateOfBirth: string;
  statusName: string;
  email: string;
  phone: string | number;
  countryPhoneCode?: string | null;
  address?: EmployeeAddressResponse;
  jobPositionId?: number | null;
  jobPositionName: string;
  contractTypeId?: number | null;
  departementId?: number | null;
  departments?: string | null;
  department?: string;
  departmentName?: string;
  managerName?: string | null;
  contractStartDate: string;
  contractTypeName: string;
  baseSalary: number;
  baseSalaryHourly?: number | null;
  salaryEffectiveDate?: string | null;
  SalaryEffectiveDate?: string | null;
  salaryComponents?: SalaryComponentResponse[];
  totalSalary?: number;
  cnss?: string | number;
  amo?: string | number;
  cimr?: string | number;
  cimrEmployeeRate?: number | null;
  cimrCompanyRate?: number | null;
  hasPrivateInsurance?: boolean;
  privateInsuranceNumber?: string | null;
  privateInsuranceRate?: number | null;
  disableAmo?: boolean;
  createdAt?: string;
  updatedAt?: string;
  companyId?: string | number;
  userId?: string | number;
  missingDocuments?: number;
  salaryPaymentMethod?: string;
  annualLeave?: number;
  probationPeriod?: string;
  CategoryName?: string;
  events?: BackendEventResponse[];
  Events?: BackendEventResponse[];
}

// ===== Sage Import interfaces =====
export interface SageImportCreatedItem {
  id: number;
  fullName: string;
  matricule?: number;
  email: string;
}

export interface SageImportError {
  row: number;
  fullName?: string;
  message: string;
}

export interface SageImportResult {
  totalProcessed: number;
  successCount: number;
  failedCount: number;
  created: SageImportCreatedItem[];
  updated: SageImportCreatedItem[];
  errors: SageImportError[];
}

@Injectable({
  providedIn: 'root'
})
export class EmployeeService {
  private readonly EMPLOYEE_URL = `${environment.apiUrl}/employee`;
  // Fallback when company context isn't established yet:
  // this endpoint derives company from the authenticated user and returns both counts + list.
  private readonly EMPLOYEE_DASHBOARD_URL = `${environment.apiUrl}/dashboard/employees`;
  private readonly EMPLOYEES_URL = `${environment.apiUrl}/employees`;

  private readonly contextService = inject(CompanyContextService);
  private readonly translate = inject(TranslateService);

  constructor(private http: HttpClient) {}

  private buildFilterParams(filters?: EmployeeFilters): HttpParams {
    let params = new HttpParams();
    if (!filters) return params;

    if (filters.searchQuery) params = params.set('search', filters.searchQuery);
    if (filters.department) params = params.set('department', filters.department);
    if (filters.status) params = params.set('status', filters.status);
    if (filters.contractType) params = params.set('contractType', filters.contractType);
    if (filters.page) params = params.set('page', filters.page.toString());
    if (filters.limit) params = params.set('limit', filters.limit.toString());

    return params;
  }

  /**
   * Récupère l'employé lié à l'utilisateur authentifié.
   * Utilisé comme fallback quand employee_id n'est pas dans le token.
   */
  getMyEmployee(): Observable<{ employeeId: number; firstName: string; lastName: string }> {
    return this.http.get<any>(`${this.EMPLOYEE_URL}/me`).pipe(
      map((response) => {
        const employeeId = Number(
          response?.employeeId ??
          response?.EmployeeId ??
          response?.id ??
          response?.Id ??
          0
        );

        return {
          employeeId,
          firstName: String(response?.firstName ?? response?.FirstName ?? ''),
          lastName: String(response?.lastName ?? response?.LastName ?? '')
        };
      })
    );
  }

  /**
   * Get all employees with optional filters.
   * INTELLIGENT ROUTING FIX:
   * 1. If a specific companyId is requested, use /api/employee/company/{id}
   * 2. Otherwise fallback to /api/dashboard/employees (derived from current user)
   */
  getEmployees(filters?: EmployeeFilters): Observable<EmployeesResponse> {
    // Prefer company-specific endpoint when companyId is provided (from filters or current context)
    const companyId = filters?.companyId ?? this.contextService.companyId();
    if (companyId) {
      const url = `${this.EMPLOYEE_URL}/company/${companyId}`;
      return this.http.get<any>(url).pipe(
        map(response => {
          // Backend may return PascalCase (e.g. Employees, TotalEmployees) or camelCase.
          if (!response) return this.mapArrayToEmployeesResponse([]);

          // If the response is a plain array
          if (Array.isArray(response)) {
            return this.mapArrayToEmployeesResponse(response);
          }

          // Normalize possible Dashboard response shapes (PascalCase / camelCase)
          const employeesArray = response.employees ?? response.Employees ?? [];
          const totalEmployees = response.totalEmployees ?? response.TotalEmployees ?? employeesArray.length;
          const activeEmployees = response.activeEmployees ?? response.ActiveEmployees ?? 0;
          const departments = response.departments ?? response.Departements ?? response.Departments ?? [];
          const statuses = response.statuses ?? response.Statuses ?? response.statuses ?? [];

          const normalized: DashboardEmployeesResponse = {
            totalEmployees,
            activeEmployees,
            employees: employeesArray,
            departments,
            statuses
          };

          return this.mapDashboardEmployeesResponse(normalized);
        })
      );
    }

    const params = this.buildFilterParams(filters);
    return this.http
      .get<DashboardEmployeesResponse>(this.EMPLOYEE_DASHBOARD_URL, { params })
      .pipe(map(response => this.mapDashboardEmployeesResponse(response)));
  }

  /**
   * Returns an observable that re-fetches employees whenever the company context changes.
   * Components can subscribe to this to automatically refresh when the selected company changes.
   */
  watchEmployees(filters?: EmployeeFilters): Observable<EmployeesResponse> {
    const ctxChanged = (this.contextService as any).contextChanged$;
    if (!ctxChanged || typeof ctxChanged.pipe !== 'function') {
      return this.getEmployees(filters);
    }
    return (ctxChanged as Observable<any>).pipe(
      startWith(null),
      switchMap(() => this.getEmployees(filters))
    );
  }

  /**
   * Helper to map the raw array from /api/employee/company/{id} to our standard response format
   */
  private mapArrayToEmployeesResponse(list: any[]): EmployeesResponse {
    // Map the raw items to our Employee model
    const employees = list.map(item => this.mapDashboardEmployee(item));

    // Calculate derived stats client-side since this endpoint doesn't return them
    const total = employees.length;
    const active = employees.filter(e => e.status === 'active').length;
    
    // Extract unique values for filter dropdowns
    const departments = Array.from(new Set(employees.map(e => e.department).filter(Boolean)));
    const statusCandidates = employees.map(e => e.statusRaw ?? e.status).filter(Boolean);
    const uniqueStatuses = Array.from(new Set(statusCandidates));
    const statuses = uniqueStatuses.map((s, idx) => ({ id: idx, label: String(s), value: String(s) } as LookupOption));

    return {
      employees,
      total,
      active,
      departments,
      statuses
    };
  }

  getEmployeeById(id: string): Observable<Employee> {
    return this.http.get<Employee>(`${this.EMPLOYEE_URL}/${id}`);
  }

  /**
   * Get the employee record for the currently authenticated user
   * Endpoint: GET /api/employee/current
   */
  getCurrentEmployee(): Observable<Employee> {
    return this.http.get<Employee>(`${this.EMPLOYEE_URL}/current`);
  }

  /**
   * Get subordinates for a manager
   */
  getSubordinates(managerId: string | number): Observable<Employee[]> {
    return this.http.get<any[]>(`${this.EMPLOYEE_URL}/manager/${managerId}/subordinates`).pipe(
      map(subordinates => {
        if (!Array.isArray(subordinates)) return [];
        return subordinates.map(sub => ({
          id: String(sub.id || sub.Id),
          firstName: sub.firstName || sub.FirstName || '',
          lastName: sub.lastName || sub.LastName || '',
          position: sub.position || sub.Position || sub.jobPositionName || sub.JobPositionName || '',
          department: sub.department || sub.Department || sub.departementName || sub.DepartementName || '',
          status: sub.status || sub.Status || 'ACTIVE',
          startDate: sub.startDate || sub.StartDate || '',
          missingDocuments: sub.missingDocuments || sub.MissingDocuments || 0,
          contractType: sub.contractType || sub.ContractType || sub.contractTypeName || sub.ContractTypeName || '',
          manager: sub.manager || sub.Manager || '',
          userId: sub.userId || sub.UserId
        }));
      })
    );
  }

  getEmployeeDetails(id: string): Observable<EmployeeProfileModel> {
    return this.http
      .get<EmployeeDetailsResponse>(`${this.EMPLOYEE_URL}/${id}/details`)
      .pipe(map(response => {      
        return this.mapEmployeeDetailsResponse(response);
      }));
  }

  /**
   * Fetch employee history/events separately
   */
  getEmployeeHistory(id: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.EMPLOYEE_URL}/${id}/history`).pipe(
      tap((events: any[]) => {
        events.forEach((event: any, index: number) => {
          
        });
      })
    );
  }

  getEmployeeFormData(): Observable<EmployeeFormData> {
    const companyId = this.contextService.companyId();
    let params = new HttpParams();
    
    if (companyId) {
      params = params.set('companyId', String(companyId));
    }

    return this.http
      .get<EmployeeFormDataResponse>(`${this.EMPLOYEE_URL}/form-data`, { params })
      .pipe(map(response => this.mapEmployeeFormDataResponse(response)));
  }

  /**
   * Fetch statuses from referential endpoint
   * Backend endpoint: GET /api/statuses?includeInactive={bool}
   */
  getStatuses(includeInactive: boolean = false): Observable<LookupOption[]> {
    let params = new HttpParams();
    if (includeInactive) params = params.set('includeInactive', 'true');

    return this.http.get<any[]>(`${environment.apiUrl}/statuses`, { params }).pipe(
      map(items => {
        return (items || []).map(i => {
            const it: any = i;
            const label = this.getLocalizedLabel(it);
            const rawVal = it.Code ?? it.code ?? it.Id ?? it.id ?? '';
            const value = String(rawVal).toLowerCase();
            return {
              id: it.Id ?? it.id,
              label,
              value: value
            } as LookupOption & { value: string };
            });
      })
    );
  }

  private getLocalizedLabel(it: any): string {
    const lang = (this.translate?.currentLang || (this.translate?.getBrowserLang && this.translate.getBrowserLang()) || 'fr').toString().toLowerCase();
    const suffix = lang.startsWith('fr') ? 'Fr' : lang.startsWith('ar') ? 'Ar' : 'En';
    return (
      it[`Name${suffix}`] ??
      it[`name${suffix}`] ??
      it.Name ??
      it.name ??
      it.NameEn ??
      it.nameEn ??
      it.NameFr ??
      it.nameFr ??
      it.NameAr ??
      it.nameAr ??
      it.Code ??
      it.code ??
      String(it.id ?? it.Id ?? '')
    );
  }

  createEmployee(employee: Partial<Employee>): Observable<Employee> {
    return this.http.post<Employee>(this.EMPLOYEE_URL, employee);
  }

  createEmployeeRecord(payload: CreateEmployeeRequest): Observable<any> {
    const body: any = { ...payload };
    if (payload.dateOfBirth) {
      body.DateOfBirth = this.formatForDateInput(payload.dateOfBirth);
      delete body.dateOfBirth;
      delete body.birthdate;
    }
    if (payload.startDate) {
      body.StartDate = this.formatForDateInput(payload.startDate);
      // keep camelCase startDate removed to avoid duplication
      delete body.startDate;
    }
    // Ensure category field matches backend expectation (categoryId)
    if ((payload as any).employeeCategoryId !== undefined && (payload as any).employeeCategoryId !== null) {
      body.categoryId = (payload as any).employeeCategoryId;
      delete body.employeeCategoryId;
    }
    return this.http.post<any>(this.EMPLOYEE_URL, body);
  }

  updateEmployee(id: string, employee: Partial<Employee>): Observable<Employee> {
    return this.http.put<Employee>(`${this.EMPLOYEE_URL}/${id}`, employee);
  }

  patchEmployeeProfile(id: string, payload: Partial<EmployeeProfileModel>): Observable<EmployeeProfileModel> {
    const body: any = { ...payload };
    if ((payload as any).dateOfBirth) {
      body.dateOfBirth = this.formatForDateInput((payload as any).dateOfBirth);
      delete body.birthdate;
    }
    if ((payload as any).startDate !== undefined && (payload as any).startDate !== null && (payload as any).startDate !== '') {
      body.contractStartDate = this.formatForDateInput((payload as any).startDate);
      delete body.startDate;
    }
    if ((payload as any).cin !== undefined) {
      body.cinNumber = (payload as any).cin;
      delete body.cin;
    }
    const pe = (payload as any).professionalEmail;
    const pers = (payload as any).personalEmail;
    if (pe !== undefined || pers !== undefined) {
      if (pe !== undefined && pe !== null && String(pe).trim() !== '') body.email = String(pe).trim();
      else if (pers !== undefined && pers !== null) body.email = String(pers).trim();
      delete body.professionalEmail;
      delete body.personalEmail;
    }
    if ((payload as any).baseSalary !== undefined && (payload as any).baseSalary !== null) {
      body.salary = Number((payload as any).baseSalary);
      delete body.baseSalary;
    }
    return this.http
      .patch<EmployeeDetailsResponse>(`${this.EMPLOYEE_URL}/${id}`, body)
      .pipe(map(response => this.mapEmployeeDetailsResponse(response)));
  }

  deleteEmployee(id: string): Observable<void> {
    return this.http.delete<void>(`${this.EMPLOYEE_URL}/${id}`);
  }

  uploadDocument(employeeId: string, documentType: string, file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('type', documentType);
    return this.http.post(`${this.EMPLOYEE_URL}/${employeeId}/documents/upload`, formData);
  }

  getDocuments(employeeId: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.EMPLOYEE_URL}/${employeeId}/documents`);
  }

  deleteDocument(employeeId: string, documentId: number): Observable<void> {
    return this.http.delete<void>(`${this.EMPLOYEE_URL}/${employeeId}/documents/${documentId}`);
  }

  downloadDocument(employeeId: string, documentId: number): Observable<Blob> {
    return this.http.get(`${this.EMPLOYEE_URL}/${employeeId}/documents/${documentId}/download`, {
      responseType: 'blob'
    });
  }

  getEmployeeSalaryDetails(employeeId: string): Observable<{ id: number, components: any[] }> {
    return this.http.get<any[]>(`${environment.apiUrl}/employee-salaries/employee/${employeeId}`).pipe(
      map(salaries => {
        const list = salaries ?? [];
        // API Payzen : JSON PascalCase (EndDate, Id) — ne pas utiliser seulement endDate/id en camelCase.
        return list.find((s: any) => {
          const end = s.endDate ?? s.EndDate;
          return end == null || end === '';
        });
      }),
      switchMap(activeSalary => {
        if (!activeSalary) return of({ id: 0, components: [] });
        const sid = Number(activeSalary.id ?? activeSalary.Id ?? 0);
        if (!Number.isFinite(sid) || sid <= 0) return of({ id: 0, components: [] });
        return this.http.get<any[]>(`${environment.apiUrl}/employee-salary-components/salary/${sid}`).pipe(
          map(components => ({
            id: sid,
            components: (components ?? []).map((c: any) => ({
              id: c.id ?? c.Id,
              employeeSalaryId: c.employeeSalaryId ?? c.EmployeeSalaryId,
              type: c.componentType ?? c.ComponentType,
              amount: c.amount ?? c.Amount,
              isTaxable: c.isTaxable ?? c.IsTaxable ?? true
            }))
          }))
        );
      })
    );
  }

  addSalaryComponent(component: any): Observable<any> {
    return this.http.post(`${environment.apiUrl}/employee-salary-components`, component);
  }

  /**
   * Importe des employés en masse depuis un fichier CSV Sage Paie.
   * POST /api/employee/import-sage
   */
  importFromSage(file: File, companyId?: number, month?: number, year?: number, preview?: boolean): Observable<SageImportResult> {
    const formData = new FormData();
    formData.append('file', file, file.name);
    const params = new URLSearchParams();
    if (companyId) params.set('companyId', companyId.toString());
    if (month) params.set('month', month.toString());
    if (year) params.set('year', year.toString());
    if (preview) params.set('preview', 'true');
    let url = `${this.EMPLOYEE_URL}/import-sage`;
    const qs = params.toString();
    if (qs) url += `?${qs}`;
    return this.http.post<SageImportResult>(url, formData);
  }

  /**
   * Retourne la liste hardcodée des primes non imposables (miroir de MapNiToContext dans le backend).
   * Données statiques — pas d'appel réseau.
   */
  static readonly NON_IMPOSABLE_LIST: NonImposableOption[] = [
    { code: 'TRANSPORT',      label: 'Prime de transport' },
    { code: 'KILOMETRIQUE',   label: 'Indemnité kilométrique' },
    { code: 'TOURNEE',        label: 'Indemnité de tournée' },
    { code: 'REPRESENTATION', label: 'Indemnité de représentation' },
    { code: 'PANIER',         label: 'Prime de panier' },
    { code: 'CAISSE',         label: 'Indemnité de caisse' },
    { code: 'SALISSURE',      label: 'Indemnité de salissure' },
    { code: 'LAIT',           label: 'Indemnité de lait' },
    { code: 'OUTILLAGE',      label: "Prime d'outillage" },
    { code: 'AIDE_MEDICALE',  label: 'Aide médicale' },
    { code: 'GRATIF_SOCIALE', label: 'Gratification sociale' },
  ];

  getNonImposableComponents(): NonImposableOption[] {
    return EmployeeService.NON_IMPOSABLE_LIST;
  }

  updateSalaryComponent(id: number, component: any): Observable<any> {
    return this.http.put(`${environment.apiUrl}/employee-salary-components/${id}`, component);
  }

  deleteSalaryComponent(id: number): Observable<void> {
    return this.http.delete<void>(`${environment.apiUrl}/employee-salary-components/${id}`);
  }

  searchCountries(query: string): Observable<CountryLookupOption[]> {
    const params = new HttpParams().set('search', query);
    return this.http.get<CountryResponseItem[]>(`${environment.apiUrl}/countries`, { params })
      .pipe(map(items => {
        const allItems = (items || []).map(item => ({
          id: item.id,
          label: item.countryName,
          phoneCode: item.countryPhoneCode
        }));
        if (!query) return allItems;
        const lowerQuery = query.toLowerCase();
        return allItems.filter(item => item.label.toLowerCase().includes(lowerQuery));
      }));
  }

  searchCities(query: string): Observable<CityLookupOption[]> {
    const params = new HttpParams().set('search', query);
    return this.http.get<CityResponseItem[]>(`${environment.apiUrl}/cities`, { params })
      .pipe(map(items => {
        const allItems = (items || []).map(item => ({
          id: item.id,
          label: item.cityName,
          countryId: item.countryId,
          countryName: item.countryName
        }));
        if (!query) return allItems;
        const lowerQuery = query.toLowerCase();
        return allItems.filter(item => item.label.toLowerCase().includes(lowerQuery));
      }));
  }

  createCountry(name: string): Observable<CountryLookupOption> {
    return this.http.post<CountryResponseItem>(`${environment.apiUrl}/countries`, { countryName: name })
      .pipe(map(item => ({
        id: item.id,
        label: item.countryName,
        phoneCode: item.countryPhoneCode
      })));
  }

  createCity(name: string, countryId?: number): Observable<CityLookupOption> {
    return this.http.post<CityResponseItem>(`${environment.apiUrl}/cities`, { cityName: name, countryId })
      .pipe(map(item => ({
        id: item.id,
        label: item.cityName,
        countryId: item.countryId,
        countryName: item.countryName
      })));
  }

  searchDepartments(query: string, companyId?: number): Observable<LookupOption[]> {
    if (!companyId) return this.http.get<DepartementResponseItem[]>(`${environment.apiUrl}/departements`)
      .pipe(map(items => {
        const allItems = (items || []).map((item: any) => ({
          id: item.id ?? item.Id,
          label: item.departementName ?? item.DepartementName ?? ''
        })).filter((x: LookupOption) => x.id != null && !Number.isNaN(Number(x.id)));
        // Dédupliquer par ID
        const uniqueItems = Array.from(new Map(allItems.map(item => [item.id, item])).values());
        if (!query) return uniqueItems;
        const lowerQuery = query.toLowerCase();
        return uniqueItems.filter(item => item.label.toLowerCase().includes(lowerQuery));
      }));
    
    return this.http.get<DepartementResponseItem[]>(`${environment.apiUrl}/departements/company/${companyId}`)
      .pipe(map(items => {
        const allItems = (items || []).map((item: any) => ({
          id: item.id ?? item.Id,
          label: item.departementName ?? item.DepartementName ?? ''
        })).filter((x: LookupOption) => x.id != null && !Number.isNaN(Number(x.id)));
        // Dédupliquer par ID
        const uniqueItems = Array.from(new Map(allItems.map(item => [item.id, item])).values());
        if (!query) return uniqueItems;
        const lowerQuery = query.toLowerCase();
        return uniqueItems.filter(item => item.label.toLowerCase().includes(lowerQuery));
      }));
  }

  searchJobPositions(query: string, companyId?: number): Observable<LookupOption[]> {
    if (!companyId) return this.http.get<JobPositionResponseItem[]>(`${environment.apiUrl}/job-positions`)
      .pipe(map(items => {
        const allItems = (items || []).map((item: any) => ({
          id: item.id ?? item.Id,
          label: item.name ?? item.Name ?? ''
        })).filter((x: LookupOption) => x.id != null && !Number.isNaN(Number(x.id)));
        // Dédupliquer par ID
        const uniqueItems = Array.from(new Map(allItems.map(item => [item.id, item])).values());
        if (!query) return uniqueItems;
        const lowerQuery = query.toLowerCase();
        return uniqueItems.filter(item => item.label.toLowerCase().includes(lowerQuery));
      }));
    
    return this.http.get<JobPositionResponseItem[]>(`${environment.apiUrl}/job-positions/by-company/${companyId}`)
      .pipe(map(items => {
        const allItems = (items || []).map((item: any) => ({
          id: item.id ?? item.Id,
          label: item.name ?? item.Name ?? ''
        })).filter((x: LookupOption) => x.id != null && !Number.isNaN(Number(x.id)));
        // Dédupliquer par ID
        const uniqueItems = Array.from(new Map(allItems.map(item => [item.id, item])).values());
        if (!query) return uniqueItems;
        const lowerQuery = query.toLowerCase();
        return uniqueItems.filter(item => item.label.toLowerCase().includes(lowerQuery));
      }));
  }

  createDepartment(name: string, companyId: number): Observable<LookupOption> {
    return this.http.post<DepartementResponseItem>(`${environment.apiUrl}/departements`, {
      departementName: name,
      companyId: companyId
    }).pipe(map((item: any) => ({
      id: item.id ?? item.Id,
      label: item.departementName ?? item.DepartementName ?? name
    })));
  }

  createJobPosition(name: string, companyId: number): Observable<LookupOption> {
    return this.http.post<JobPositionResponseItem>(`${environment.apiUrl}/job-positions`, {
      name: name,
      companyId: companyId
    }).pipe(map((item: any) => ({
      id: item.id ?? item.Id,
      label: item.name ?? item.Name ?? name
    })));
  }

  private mapEmployeeFormDataResponse(raw: EmployeeFormDataResponse | Record<string, unknown> = {} as EmployeeFormDataResponse): EmployeeFormData {
    const r = raw as Record<string, unknown>;
    // Monolithe ASP.NET : PropertyNamingPolicy = null → JSON en PascalCase
    const response: EmployeeFormDataResponse = {
      statuses: (r['statuses'] ?? r['Statuses']) as LookupResponseItem[] | undefined,
      genders: (r['genders'] ?? r['Genders']) as LookupResponseItem[] | undefined,
      educationLevels: (r['educationLevels'] ?? r['EducationLevels']) as LookupResponseItem[] | undefined,
      maritalStatuses: (r['maritalStatuses'] ?? r['MaritalStatuses']) as LookupResponseItem[] | undefined,
      nationalities: (r['nationalities'] ?? r['Nationalities']) as LookupResponseItem[] | undefined,
      countries: (r['countries'] ?? r['Countries']) as CountryResponseItem[] | undefined,
      cities: (r['cities'] ?? r['Cities']) as CityResponseItem[] | undefined,
      departements: (r['departements'] ?? r['Departements']) as DepartementResponseItem[] | undefined,
      jobPositions: (r['jobPositions'] ?? r['JobPositions']) as JobPositionResponseItem[] | undefined,
      contractTypes: (r['contractTypes'] ?? r['ContractTypes']) as ContractTypeResponseItem[] | undefined,
      potentialManagers: (r['potentialManagers'] ?? r['PotentialManagers']) as PotentialManagerResponseItem[] | undefined,
      employeeCategories: (r['employeeCategories'] ?? r['EmployeeCategories']) as EmployeeFormDataResponse['employeeCategories']
    };

    const lang = (this.translate?.currentLang || (this.translate?.getBrowserLang && this.translate.getBrowserLang()) || 'fr').toString().toLowerCase();
    const suffix = lang.startsWith('fr') ? 'Fr' : lang.startsWith('ar') ? 'Ar' : 'En';

    const getLocalizedLabel = (it: any) => {
      return (
        it[`Name${suffix}`] ??
        it[`name${suffix}`] ??
        it.Name ??
        it.name ??
        it.NameEn ??
        it.nameEn ??
        it.NameFr ??
        it.nameFr ??
        it.NameAr ??
        it.nameAr ??
        it.code ??
        it.Code ??
        String(it.id ?? it.Id ?? '')
      );
    };

    const toLookupOption = (items?: LookupResponseItem[]): LookupOption[] =>
      (items ?? []).map(item => {
        const it = item as any;
        const rawVal = it.Code ?? it.code ?? it.Id ?? it.id;
        return {
          id: it.Id ?? it.id,
          label: getLocalizedLabel(it),
          value: String(rawVal ?? '').toLowerCase()
        };
      });

    const toCountryOption = (items?: CountryResponseItem[]): CountryLookupOption[] =>
      (items ?? []).map(item => {
        const it = item as any;
        return {
          id: it.id ?? it.Id,
          label: it.countryName ?? it.CountryName ?? '',
          phoneCode: it.countryPhoneCode ?? it.CountryPhoneCode ?? ''
        };
      });

    const toCityOption = (items?: CityResponseItem[]): CityLookupOption[] =>
      (items ?? []).map(item => {
        const it = item as any;
        return {
          id: it.id ?? it.Id,
          label: it.cityName ?? it.CityName ?? '',
          countryId: it.countryId ?? it.CountryId,
          countryName: it.countryName ?? it.CountryName ?? ''
        };
      });

    const toDepartmentOption = (items?: DepartementResponseItem[]): LookupOption[] => {
      const allItems = (items ?? []).map(item => {
        const it = item as any;
        return { id: it.id ?? it.Id, label: it.departementName ?? it.DepartementName ?? '' };
      });
      return Array.from(new Map(allItems.map(item => [item.id, item])).values());
    };

    const toJobPositionOption = (items?: JobPositionResponseItem[]): LookupOption[] => {
      const allItems = (items ?? []).map(item => {
        const it = item as any;
        return { id: it.id ?? it.Id, label: it.name ?? it.Name ?? '' };
      });
      return Array.from(new Map(allItems.map(item => [item.id, item])).values());
    };

    const toContractTypeOption = (items?: ContractTypeResponseItem[]): LookupOption[] =>
      (items ?? []).map(item => {
        const it = item as any;
        return { id: it.id ?? it.Id, label: it.contractTypeName ?? it.ContractTypeName ?? '' };
      });

    const toManagerOption = (items?: PotentialManagerResponseItem[]): ManagerLookupOption[] =>
      (items ?? []).map(item => {
        const it = item as any;
        const fn = it.firstName ?? it.FirstName ?? '';
        const ln = it.lastName ?? it.LastName ?? '';
        const full = it.fullName ?? it.FullName ?? '';
        return {
          id: it.id ?? it.Id,
          label: (full || `${fn} ${ln}`).trim(),
          departmentName: it.departementName ?? it.DepartementName ?? ''
        };
      });

    const toCategoryOption = (items?: EmployeeFormDataResponse['employeeCategories']): LookupOption[] =>
      (items ?? []).map(c => {
        const it = c as any;
        return { id: it.id ?? it.Id, label: it.name ?? it.Name ?? '' };
      });

    return {
      statuses: toLookupOption(response.statuses),
      genders: toLookupOption(response.genders),
      educationLevels: toLookupOption(response.educationLevels),
      maritalStatuses: toLookupOption(response.maritalStatuses),
      nationalities: toLookupOption(response.nationalities),
      countries: toCountryOption(response.countries),
      cities: toCityOption(response.cities),
      departments: toDepartmentOption(response.departements),
      jobPositions: toJobPositionOption(response.jobPositions),
      contractTypes: toContractTypeOption(response.contractTypes),
      potentialManagers: toManagerOption(response.potentialManagers),
      employeeCategories: toCategoryOption(response.employeeCategories)
    };
  }

  private mapDashboardEmployeesResponse(response: DashboardEmployeesResponse): EmployeesResponse {
    const employees = (response?.employees ?? []).map(emp => this.mapDashboardEmployee(emp));
    const total = response?.totalEmployees ?? employees.length;
    const active = response?.activeEmployees ?? employees.filter(emp => emp.status === 'active').length;
    const departments = Array.from(new Set(response?.departments ?? employees
      .map(emp => emp.department)
      .filter(dep => !!dep))) as string[];
    // Use raw status codes or API-provided status objects when available so the UI can present all distinct statuses
    const statusCandidates = response?.statuses ?? employees.map(emp => emp.statusRaw ?? emp.status);
    const unique = Array.from(new Set((statusCandidates || []).filter(s => !!s)));
    const statuses = unique.map((s, idx) => {
      if (s && typeof s === 'object') {
        const it: any = s;
        let value: any = it.Code ?? it.code ?? it.Id ?? it.id ?? idx;
        value = String(value).toLowerCase();
        const lang = (this.translate?.currentLang || (this.translate?.getBrowserLang && this.translate.getBrowserLang()) || 'fr').toString().toLowerCase();
        const suffix = lang.startsWith('fr') ? 'Fr' : lang.startsWith('ar') ? 'Ar' : 'En';
        const label = it[`Name${suffix}`] ?? it[`name${suffix}`] ?? it.Name ?? it.name ?? it.NameEn ?? it.NameFr ?? it.NameAr ?? String(value);
        return { id: it.Id ?? it.id ?? idx, label, value: String(value).toLowerCase() } as LookupOption;
      }
      return { id: idx, label: String(s), value: String(s).toLowerCase() } as LookupOption;
    });

    return { employees, total, active, departments, statuses };
  }

  private mapDashboardEmployee(employee: any): Employee {
    // We treat the input as 'any' to handle both DashboardEmployee and EmployeeReadDto shapes
    // which might use PascalCase (Backend default) or camelCase (if auto-serialized)
    const rawStatus = this.extractStatusCode(employee);
    const localizedName =
      employee.NameFr ??
      employee.nameFr ??
      employee.NameEn ??
      employee.NameAr ??
      employee.name ??
      employee.StatusName ??
      employee.statusName ??
      '';

    return {
      id: this.toStringValue(employee.id || employee.Id),
      firstName: employee.firstName || employee.FirstName || '',
      lastName: employee.lastName || employee.LastName || '',
      position:
        employee.position ||
        employee.Position ||
        employee.JobPositionName ||
        employee.jobPositionName ||
        'Non assigné',
      department: employee.department || employee.Department || '',
      status: this.mapEmployeeStatus(rawStatus),
      statusRaw: rawStatus || undefined,
      statusName: localizedName || undefined,
      startDate:
        employee.startDate ||
        employee.StartDate ||
        employee.ContractStartDate ||
        employee.contractStartDate ||
        '',
      missingDocuments: this.toNumberValue(employee.missingDocuments || employee.MissingDocuments),
      contractType: this.mapContractType(
        employee.contractType || employee.ContractType || employee.ContractTypeName || employee.contractTypeName
      ),
      manager: employee.manager || employee.Manager || undefined,
        userId: employee.userId ?? employee.UserId ?? employee.user_id ?? employee.User_Id ?? undefined,
        // Preserve any role information the backend might include so callers can derive display roles
        roleName: employee.roleName ?? employee.RoleName ?? employee.RoleName ?? undefined,
        role: employee.role ?? employee.Role ?? undefined,
        roles: employee.roles ?? employee.Roles ?? undefined
    };
  }

  private extractStatusCode(employee: any): string {
    // Normalize the various shapes the backend may return.
    // Examples:
    // - { status: 'ACTIVE' }
    // - { statuses: 'ACTIVE' }
    // - { Status: { Code: 'ACTIVE' } }
    // - { StatusCode: 'ACTIVE' }
    if (!employee) return '';

    if (typeof employee === 'string') return employee;

    if (typeof employee.status === 'string') return employee.status;
    if (typeof employee.statuses === 'string') return employee.statuses;
    if (typeof employee.Status === 'string') return employee.Status;
    if (typeof employee.Statuses === 'string') return employee.Statuses;
    if (typeof employee.statusName === 'string') return employee.statusName;
    if (typeof employee.StatusName === 'string') return employee.StatusName;

    if (employee.StatusCode) return employee.StatusCode;
    if (employee.statusCode) return employee.statusCode;

    if (employee.Status && typeof employee.Status === 'object') {
      return (
        employee.Status.Code ??
        employee.Status.code ??
        employee.Status.StatusCode ??
        employee.Status.statusCode ??
        ''
      );
    }

    return '';
  }

  private mapEmployeeDetailsResponse(payload: EmployeeDetailsResponse): EmployeeProfileModel {
    const salaryComponents = (payload.salaryComponents ?? []).map(c => ({
      type: c.componentName,
      amount: c.amount,
      isTaxable: c.isTaxable !== undefined ? c.isTaxable : (c.IsTaxable ?? true)
    }));
    
    
    
    const addressPayload = payload.address || (payload as any).Address;
    
    const cityName = addressPayload?.cityName || addressPayload?.CityName || '';
    const countryName = addressPayload?.countryName || addressPayload?.CountryName || '';
    const cityIdFromAddr = addressPayload?.cityId ?? addressPayload?.CityId;
    const countryIdFromAddr = addressPayload?.countryId ?? addressPayload?.CountryId;
    const addressLine1 = addressPayload?.addressLine1 || addressPayload?.AddressLine1 || '';
    const addressLine2 = addressPayload?.addressLine2 || addressPayload?.AddressLine2 || '';
    const zipCode = addressPayload?.zipCode || addressPayload?.ZipCode || '';
    


    const cnssValue = this.toStringValue(payload.cnss ?? (payload as any).Cnss ?? (payload as any).CNSS);
    const amoValue = this.toStringValue(payload.amo ?? (payload as any).Amo ?? (payload as any).AMO);
    const cimrValue = this.toStringValue(payload.cimr ?? (payload as any).Cimr ?? (payload as any).CIMR);

    // Extract birthPlace from payload if it exists (separate from address city)
    //const birthPlace = (payload as any).birthPlace || (payload as any).BirthPlace || ''; Birthplace is not necessarily required

    const detail: EmployeeProfileModel = {
      id: this.toStringValue(payload.id),
      firstName: payload.firstName ?? '',
      lastName: payload.lastName ?? '',
      photo: undefined,
      cin: payload.cinNumber ?? '',
      maritalStatus: this.mapMaritalStatus(payload.maritalStatusName),
      //birthPlace: birthPlace,
      professionalEmail: payload.email ?? '',
      personalEmail: payload.email ?? '',
      phone: this.composePhone(payload.countryPhoneCode, payload.phone),
      address: this.formatAddress(addressPayload),
      countryId: countryIdFromAddr ?? undefined,
      countryName: countryName,
      city: cityName,
      cityId: cityIdFromAddr ?? undefined,
      addressLine1: addressLine1,
      addressLine2: addressLine2,
      zipCode: zipCode,
      position: payload.jobPositionName ?? 'Non assigné',
      jobPositionId: payload.jobPositionId ?? (payload as any).JobPositionId ?? undefined,
      department: payload.department ?? payload.departmentName ?? payload.departments ?? '',
      departementId: payload.departementId ?? (payload as any).DepartementId ?? undefined,
      manager: payload.managerName ?? '',
      contractType: this.mapContractType(payload.contractTypeName),
      contractTypeId: payload.contractTypeId ?? (payload as any).ContractTypeId ?? undefined,
      
      endDate: undefined,
      probationPeriod: payload.probationPeriod ?? '',
      exitReason: undefined,
      baseSalary: payload.baseSalary ?? 0,
      baseSalaryHourly: payload.baseSalaryHourly ?? (payload as any).BaseSalaryHourly ?? 0,
      salaryEffectiveDate: (() => {
        const raw =
          payload.salaryEffectiveDate ??
          (payload as any).SalaryEffectiveDate ??
          null;
        if (raw == null || raw === '') return null;
        const formatted = this.formatForDateInput(raw as string | Date);
        return formatted || null;
      })(),
      salaryComponents,
      activeSalaryId: undefined,
      paymentMethod: this.mapPaymentMethod(payload.salaryPaymentMethod),
      cnss: cnssValue || '',
      amo: amoValue || '',
      cimr: cimrValue || undefined,
      cimrEmployeeRate: payload.cimrEmployeeRate ?? (payload as any).CimrEmployeeRate ?? null,
      cimrCompanyRate: payload.cimrCompanyRate ?? (payload as any).CimrCompanyRate ?? null,
      hasPrivateInsurance: payload.hasPrivateInsurance ?? (payload as any).HasPrivateInsurance ?? false,
      privateInsuranceNumber: payload.privateInsuranceNumber ?? (payload as any).PrivateInsuranceNumber ?? null,
      privateInsuranceRate: payload.privateInsuranceRate ?? (payload as any).PrivateInsuranceRate ?? null,
      disableAmo: payload.disableAmo ?? (payload as any).DisableAmo ?? false,
      annualLeave: payload.annualLeave ?? (payload as any).AnnualLeave ?? 0,
      genderId: (payload as any).genderId ?? (payload as any).GenderId ?? null,
      genderName: (payload as any).genderName ?? (payload as any).GenderName ?? null,
      // Preserve raw status code (if present in payload) and localized label
      status: (this.mapEmployeeStatus(this.extractStatusCode(payload) || payload.statusName) as unknown) as string,
      statusRaw: this.extractStatusCode(payload) || payload.statusName || undefined,
      statusName: payload.statusName ?? undefined,
      missingDocuments: payload.missingDocuments ?? 0,
      companyId: this.toStringValue(payload.companyId) || undefined,
      userId: this.toStringValue(
        payload.userId ?? (payload as any).UserId ?? (payload as any).user_id ?? (payload as any).User_Id
      ) || undefined,
      employeeCategoryId: (payload as any).categoryId ?? (payload as any).CategoryId ?? (payload as any).employeeCategoryId ?? undefined,
      employeeCategoryName: (payload as any).categoryName ?? (payload as any).CategoryName ?? undefined,
      createdAt: payload.createdAt ? new Date(payload.createdAt) : undefined,
      updatedAt: payload.updatedAt ? new Date(payload.updatedAt) : undefined,
      dateOfBirth: this.formatForDateInput(payload.dateOfBirth),
      startDate: this.formatForDateInput(payload.contractStartDate),
      events: (payload.events || payload.Events || []).map((event: any) => {
        const details = event.details ?? event.Details;
        const modifiedBy = event.modifiedBy ?? event.ModifiedBy;
        const mappedEvent = {
          type: event.eventName || event.EventName || event.type || event.Type || event.Event || event.EventType || 'general_update',
          title: event.title ?? event.Title ?? event.EventTitle ?? '',
          date: event.date ?? event.Date ?? event.createdAt ?? event.CreatedAt ?? event.timestamp ?? event.Timestamp,
          description: event.description ?? event.Description ?? (event.newValue ? `→ ${event.newValue}` : ''),
          details: details ?? {
            oldValue: event.oldValue ?? event.OldValue ?? (event.OldValue === '' ? '(vide)' : event.OldValue),
            newValue: event.newValue ?? event.NewValue
          },
          modifiedBy: modifiedBy
            ? {
                name: modifiedBy.name ?? modifiedBy.Name ?? '',
                role: modifiedBy.role ?? modifiedBy.Role ?? ''
              }
            : (event.CreatorFullName
                ? { name: event.CreatorFullName, role: 'Admin' }
                : undefined),
          timestamp: event.timestamp ?? event.Timestamp ?? event.createdAt ?? event.CreatedAt
        };

        return mappedEvent;
      })
    };
      return detail;
  }

  private mapEmployeeStatus(status?: string): Employee['status'] {
    const raw = (status ?? '').toString().trim().toLowerCase();
    if (!raw) return 'inactive';

    const activeSet = new Set(['active', 'actif', 'enabled']);
    const onLeaveSet = new Set(['on_leave', 'on leave', 'leave', 'onleave', 'absent']);
    const inactiveSet = new Set(['inactive', 'resigned', 'resign', 'retired', 'suspended', 'terminated', 'left', 'departed']);

    if (activeSet.has(raw)) return 'active';
    if (onLeaveSet.has(raw)) return 'on_leave';
    if (inactiveSet.has(raw)) return 'inactive';

    // Handle common code patterns (safe exact matches already covered above).
    if (raw === 'on_leave' || raw === 'on-leave') return 'on_leave';
    if (raw === 'active') return 'active';

    // Fallback heuristics: prefer 'on_leave' if it clearly references leave/congé,
    // otherwise prefer 'inactive' as the safe default.
    if (raw.includes('leave') || raw.includes('cong') || raw.includes('abs')) return 'on_leave';
    if (raw.includes('active') && !raw.startsWith('in')) return 'active';

    return 'inactive';
  }

  private mapContractType(type?: string): Employee['contractType'] {
    const raw = (type ?? '').toString().trim();
    const normalized = raw.toLowerCase();
    if (!normalized) return '';
    // Keep common canonical short values
    if (normalized === 'cdd') return 'CDD';
    if (normalized === 'cdi') return 'CDI';
    if (normalized === 'stage' || normalized === 'intern' || normalized.includes('stage')) return 'Stage';
    // For company-specific contract type names (e.g. "Atlas Leader"), preserve the original label
    return raw;
  }

  private mapMaritalStatus(status?: string): EmployeeProfileModel['maritalStatus'] {
    const normalized = (status ?? '').toLowerCase();
    if (normalized.includes('mari')) return 'married';
    if (normalized.includes('divorc')) return 'divorced';
    if (normalized.includes('veuf') || normalized.includes('veuve')) return 'widowed';
    return 'single';
  }

  private mapPaymentMethod(method?: string): EmployeeProfileModel['paymentMethod'] {
    const normalized = (method ?? '').toLowerCase().trim();
    if (normalized === 'check' || normalized.includes('chèque') || normalized.includes('cheque')) return 'check';
    if (normalized === 'cash' || normalized.includes('esp')) return 'cash';
    return 'bank_transfer';
  }

  private composePhone(code?: string | null, phone?: string | number): string {
    const cleanCode = code ? String(code).trim() : '';
    let cleanPhone = phone ? String(phone).trim() : '';

    // Éviter de dupliquer l'indicatif (ex: +212+212600000000)
    if (cleanCode && cleanPhone && cleanPhone.startsWith(cleanCode)) {
      cleanPhone = cleanPhone.slice(cleanCode.length).trim();
    }

    return `${cleanCode} ${cleanPhone}`.trim();
  }

  private formatForDateInput(date?: string | Date | null): string {
    if (!date) return '';
    const d = typeof date === 'string' ? new Date(date) : date;
    if (!d || Number.isNaN(d.getTime())) return '';
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  private formatAddress(address?: any): string {
    if (!address) {
      return '';
    }
    const line1 = address.addressLine1 || address.AddressLine1;
    const line2 = address.addressLine2 || address.AddressLine2;
    const city = address.cityName || address.CityName;
    const zip = address.zipCode || address.ZipCode;
    const country = address.countryName || address.CountryName;

    const parts = [line1, line2, city, zip, country]
      .filter(part => !!part)
      .map(part => part?.trim());
    return parts.join(', ');
  }

  private toStringValue(value: string | number | undefined | null): string {
    return value !== undefined && value !== null ? String(value) : '';
  }

  private toNumberValue(value: number | string | undefined | null): number {
    const numeric = Number(value);
    return Number.isFinite(numeric) ? numeric : 0;
  }

  // Family Management
  createSpouse(employeeId: string | number, spouse: any): Observable<any> {
    return this.http.post(`${this.EMPLOYEES_URL}/${employeeId}/spouse`, spouse);
  }

  updateSpouse(employeeId: string | number, spouse: any): Observable<any> {
    return this.http.put(`${this.EMPLOYEES_URL}/${employeeId}/spouse`, spouse);
  }

  deleteSpouse(employeeId: string | number): Observable<void> {
    return this.http.delete<void>(`${this.EMPLOYEES_URL}/${employeeId}/spouse`);
  }

  createChild(employeeId: string | number, child: any): Observable<any> {
    return this.http.post(`${this.EMPLOYEES_URL}/${employeeId}/children`, child);
  }

  updateChild(employeeId: string | number, childId: number | string, child: any): Observable<any> {
    return this.http.put(`${this.EMPLOYEES_URL}/${employeeId}/children/${childId}`, child);
  }

  deleteChild(employeeId: string | number, childId: number | string): Observable<void> {
    return this.http.delete<void>(`${this.EMPLOYEES_URL}/${employeeId}/children/${childId}`);
  }

  // Salary Package Assignment
  getActivePackageAssignment(employeeId: string): Observable<EmployeeSalaryPackageAssignment | null> {
    const baseUrl = environment.apiUrl.replace('/api', '');
    return this.http.get<any[]>(`${baseUrl}/api/salary-package-assignments/employee/${employeeId}`)
      .pipe(
        map(assignments => {
          if (!assignments || assignments.length === 0) return null;

          // Filter for active assignment (effectiveDate <= today, endDate is null or > today)
          const today = new Date();
          const active = assignments.find(a => {
            const effectiveDate = new Date(a.EffectiveDate || a.effectiveDate);
            const endDate = a.EndDate || a.endDate;

            if (effectiveDate > today) return false; // Not yet effective
            if (!endDate) return true; // No end date = currently active
            return new Date(endDate) > today; // End date in future = still active
          });

          if (!active) return null;

          return this.transformAssignment(active);
        }),
        map(assignment => assignment),
        startWith(null as EmployeeSalaryPackageAssignment | null)
      );
  }

  private transformAssignment(data: any): EmployeeSalaryPackageAssignment {
    return {
      id: data.Id || data.id,
      salaryPackageId: data.SalaryPackageId || data.salaryPackageId,
      salaryPackageName: data.SalaryPackageName || data.salaryPackageName,
      effectiveDate: data.EffectiveDate || data.effectiveDate,
      endDate: data.EndDate || data.endDate,
      packageVersion: data.PackageVersion || data.packageVersion,
      contractId: data.ContractId || data.contractId,
      employeeSalaryId: data.EmployeeSalaryId || data.employeeSalaryId
    };
  }
}