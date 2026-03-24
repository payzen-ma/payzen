import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '@environments/environment';
import { CompanyContextService } from './companyContext.service';
import {
  SalaryPackage,
  SalaryPackageItem,
  SalaryPackageStatus,
  SalaryPackageWriteRequest,
  SalaryPackageCloneRequest,
  SalaryComponentType,
  PayComponent,
  TemplateType,
  CimrConfig,
  CimrRegime,
  SalaryPackageAssignment,
  SalaryPackageAssignmentCreateRequest,
  SalaryPackageAssignmentUpdateRequest
} from '@app/core/models/salary-package.model';

@Injectable({
  providedIn: 'root'
})
export class SalaryPackageService {
  private readonly http = inject(HttpClient);
  private readonly contextService = inject(CompanyContextService);
  
  private readonly baseUrl = environment.apiUrl.replace('/api', '');
  private readonly packagesUrl = `${this.baseUrl}/api/salary-packages`;
  private readonly componentsUrl = `${this.baseUrl}/api/pay-components`;

  // ============ Salary Packages ============

  /**
   * Get all salary packages, optionally filtered by company and status
   */
  getAll(companyId?: number, status?: string): Observable<SalaryPackage[]> {
    let params = new HttpParams();
    if (companyId) {
      params = params.set('companyId', String(companyId));
    }
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<any[]>(this.packagesUrl, { params }).pipe(
      map(packages => packages.map(pkg => this.transformPackage(pkg)))
    );
  }

  /**
   * Get company salary packages (company-owned templates)
   */
  getCompanyPackages(companyId?: number, status?: SalaryPackageStatus): Observable<SalaryPackage[]> {
    const cid = companyId ?? this.contextService.companyId();
    if (!cid) {
      return new Observable(subscriber => subscriber.next([]));
    }
    let params = new HttpParams().set('companyId', String(cid));
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<any[]>(this.packagesUrl, { params }).pipe(
      map(packages => packages.map(pkg => this.transformPackage(pkg)))
    );
  }

  /**
   * Get official templates (global templates from backoffice)
   * Only published templates are visible to clients
   */
  getOfficialTemplates(status?: SalaryPackageStatus, category?: string): Observable<SalaryPackage[]> {
    let params = new HttpParams();
    // Default to published for client view
    params = params.set('status', status || 'published');
    if (category) {
      params = params.set('category', category);
    }
    return this.http.get<any[]>(`${this.packagesUrl}/templates`, { params }).pipe(
      map(packages => packages.map(pkg => this.transformPackage(pkg)))
    );
  }

  /**
   * Get a single salary package by ID
   */
  getById(id: number): Observable<SalaryPackage> {
    return this.http.get<any>(`${this.packagesUrl}/${id}`).pipe(
      map(pkg => this.transformPackage(pkg))
    );
  }

  /**
   * Create a new company salary package
   */
  create(request: SalaryPackageWriteRequest): Observable<SalaryPackage> {
    // Ensure company ID is set from context if not provided
    if (!request.companyId) {
      const contextCompanyId = this.contextService.companyId();
      request.companyId = contextCompanyId ? Number(contextCompanyId) : undefined;
    }
    // Company templates are always of type COMPANY
    request.templateType = 'COMPANY';
    const payload = this.toWritePayload(request, false);
    return this.http.post<any>(this.packagesUrl, payload).pipe(
      map(pkg => this.transformPackage(pkg))
    );
  }

  /**
   * Update an existing salary package
   */
  update(id: number, request: SalaryPackageWriteRequest): Observable<SalaryPackage> {
    const payload = this.toWritePayload(request, true);
    return this.http.put<any>(`${this.packagesUrl}/${id}`, payload).pipe(
      map(pkg => this.transformPackage(pkg))
    );
  }

  /**
   * Delete a salary package (only drafts can be deleted)
   */
  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.packagesUrl}/${id}`);
  }

  /**
   * Clone an official template to create a company-specific version
   */
  cloneTemplate(templateId: number, request: SalaryPackageCloneRequest): Observable<SalaryPackage> {
    // Ensure company ID is set from context if not provided
    if (!request.companyId) {
      request.companyId = Number(this.contextService.companyId()) ?? 0;
    }
    const payload = {
      CompanyId: request.companyId,
      Name: request.name || null,
      ValidFrom: request.validFrom || null
    };
    return this.http.post<any>(`${this.packagesUrl}/${templateId}/clone`, payload).pipe(
      map(pkg => this.transformPackage(pkg))
    );
  }

  /**
   * Create a new version of an existing package
   */
  createNewVersion(id: number): Observable<SalaryPackage> {
    return this.http.post<any>(`${this.packagesUrl}/${id}/new-version`, {}).pipe(
      map(pkg => this.transformPackage(pkg))
    );
  }

  /**
   * Publish a draft package (transitions from 'draft' to 'published')
   */
  publish(id: number): Observable<SalaryPackage> {
    return this.http.post<any>(`${this.packagesUrl}/${id}/publish`, {}).pipe(
      map(pkg => this.transformPackage(pkg))
    );
  }

  /**
   * Deprecate a published package (transitions from 'published' to 'deprecated')
   */
  deprecate(id: number): Observable<SalaryPackage> {
    return this.http.post<any>(`${this.packagesUrl}/${id}/deprecate`, {}).pipe(
      map(pkg => this.transformPackage(pkg))
    );
  }

  /**
   * Duplicate a package (creates a new draft copy)
   */
  duplicate(id: number, name?: string): Observable<SalaryPackage> {
    const payload = { Name: name || null };
    return this.http.post<any>(`${this.packagesUrl}/${id}/duplicate`, payload).pipe(
      map(pkg => this.transformPackage(pkg))
    );
  }

  // ============ Pay Components ============

  /**
   * Get pay components from the global catalog
   */
  getPayComponents(type?: string, isActive?: boolean): Observable<PayComponent[]> {
    let params = new HttpParams();
    if (type) {
      params = params.set('type', type);
    }
    if (isActive !== undefined) {
      params = params.set('isActive', String(isActive));
    }
    return this.http.get<any[]>(this.componentsUrl, { params }).pipe(
      map(components => components.map(c => this.transformPayComponent(c)))
    );
  }

  /**
   * Get effective pay components for a specific date
   */
  getEffectivePayComponents(date?: Date): Observable<PayComponent[]> {
    let params = new HttpParams();
    if (date) {
      params = params.set('date', date.toISOString());
    }
    return this.http.get<any[]>(`${this.componentsUrl}/effective`, { params }).pipe(
      map(components => components.map(c => this.transformPayComponent(c)))
    );
  }

  // ============ Salary Package Assignments ============

  private readonly assignmentsUrl = `${this.baseUrl}/api/salary-package-assignments`;

  getAssignments(packageId: number): Observable<SalaryPackageAssignment[]> {
    return this.http.get<any[]>(this.assignmentsUrl, {
      params: new HttpParams().set('salaryPackageId', String(packageId))
    }).pipe(
      map(assignments => assignments.map(a => this.transformAssignment(a)))
    );
  }

  getAssignmentsByEmployee(employeeId: number): Observable<SalaryPackageAssignment[]> {
    return this.http.get<any[]>(`${this.assignmentsUrl}/employee/${employeeId}`).pipe(
      map(assignments => assignments.map(a => this.transformAssignment(a)))
    );
  }

  createAssignment(request: SalaryPackageAssignmentCreateRequest): Observable<SalaryPackageAssignment> {
    const payload = {
      SalaryPackageId: request.salaryPackageId,
      EmployeeId: request.employeeId,
      ContractId: request.contractId,
      EffectiveDate: request.effectiveDate
    };
    return this.http.post<any>(this.assignmentsUrl, payload).pipe(
      map(a => this.transformAssignment(a))
    );
  }

  endAssignment(assignmentId: number, request: SalaryPackageAssignmentUpdateRequest): Observable<void> {
    return this.http.put<void>(`${this.assignmentsUrl}/${assignmentId}`, {
      EndDate: request.endDate
    });
  }

  private transformAssignment(data: any): SalaryPackageAssignment {
    return {
      id: data.Id ?? data.id,
      salaryPackageId: data.SalaryPackageId ?? data.salaryPackageId,
      salaryPackageName: data.SalaryPackageName ?? data.salaryPackageName ?? '',
      employeeId: data.EmployeeId ?? data.employeeId,
      employeeFullName: data.EmployeeFullName ?? data.employeeFullName ?? '',
      contractId: data.ContractId ?? data.contractId,
      employeeSalaryId: data.EmployeeSalaryId ?? data.employeeSalaryId,
      effectiveDate: data.EffectiveDate ?? data.effectiveDate,
      endDate: data.EndDate ?? data.endDate ?? null,
      packageVersion: data.PackageVersion ?? data.packageVersion ?? 1,
      createdAt: data.CreatedAt ?? data.createdAt
    };
  }

  // ============ Transformations ============

  private transformPackage(data: any): SalaryPackage {
    const items: SalaryPackageItem[] = (data.Items ?? data.items ?? [])
      .map((item: any) => this.transformItem(item));
    const sortedItems = [...items].sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0));

    // Determine template type based on companyId
    const companyId = data.CompanyId ?? data.companyId ?? null;
    const templateType: TemplateType = companyId === null ? 'OFFICIAL' : 'COMPANY';

    return {
      id: data.Id ?? data.id,
      name: data.Name ?? data.name ?? '',
      code: data.Code ?? data.code ?? null,
      category: data.Category ?? data.category ?? '',
      description: data.Description ?? data.description ?? null,
      baseSalary: Number(data.BaseSalary ?? data.baseSalary ?? 0),
      status: this.normalizeStatus(data.Status ?? data.status),
      companyId,
      companyName: data.CompanyName ?? data.companyName ?? null,

      // Business sector
      businessSectorId: data.BusinessSectorId ?? data.businessSectorId ?? 0,
      businessSectorName: data.BusinessSectorName ?? data.businessSectorName ?? null,

      // Template classification
      templateType: this.normalizeTemplateType(data.TemplateType ?? data.templateType) ?? templateType,

      // CIMR configuration
      cimrConfig: this.transformCimrConfig(data.CimrConfig ?? data.cimrConfig),
      cimrRate: data.CimrRate ?? data.cimrRate ?? null, // Legacy field

      // Versioning
      version: data.Version ?? data.version ?? 1,
      sourceTemplateId: data.SourceTemplateId ?? data.sourceTemplateId ?? null,
      sourceTemplateName: data.SourceTemplateName ?? data.sourceTemplateName ?? null,
      sourceTemplateVersion: data.SourceTemplateVersion ?? data.sourceTemplateVersion ?? null,
      validFrom: data.ValidFrom ?? data.validFrom ?? null,
      validTo: data.ValidTo ?? data.validTo ?? null,
      isLocked: data.IsLocked ?? data.isLocked ?? false,
      isGlobalTemplate: data.IsGlobalTemplate ?? data.isGlobalTemplate ?? companyId === null,

      items: sortedItems,
      updatedAt: data.UpdatedAt ?? data.updatedAt ?? null,
      createdAt: data.CreatedAt ?? data.createdAt ?? null
    };
  }

  private transformItem(data: any): SalaryPackageItem {
    return {
      id: data.Id ?? data.id,
      payComponentId: data.PayComponentId ?? data.payComponentId ?? null,
      payComponentCode: data.PayComponentCode ?? data.payComponentCode ?? null,
      label: data.Label ?? data.label ?? '',
      defaultValue: Number(data.DefaultValue ?? data.defaultValue ?? 0),
      sortOrder: data.SortOrder ?? data.sortOrder,
      type: this.normalizeComponentType(data.Type ?? data.type),
      isTaxable: data.IsTaxable ?? data.isTaxable ?? data.isIR ?? data.isIr ?? true,
      isSocial: data.IsSocial ?? data.isSocial ?? data.isCNSS ?? data.isCnss ?? true,
      isCIMR: data.IsCIMR ?? data.isCIMR ?? data.isCimr ?? false,
      isVariable: data.IsVariable ?? data.isVariable ?? false,
      exemptionLimit: data.ExemptionLimit ?? data.exemptionLimit ?? null,
      // Referentiel element link
      referentielElementId: data.ReferentielElementId ?? data.referentielElementId ?? null,
      referentielElementCode: data.ReferentielElementCode ?? data.referentielElementCode ?? null,
      isConvergence: data.IsConvergence ?? data.isConvergence
    };
  }

  private transformPayComponent(data: any): PayComponent {
    return {
      id: data.Id ?? data.id,
      code: data.Code ?? data.code ?? '',
      nameFr: data.NameFr ?? data.nameFr ?? '',
      nameAr: data.NameAr ?? data.nameAr ?? null,
      nameEn: data.NameEn ?? data.nameEn ?? null,
      type: this.normalizeComponentType(data.Type ?? data.type),
      isTaxable: data.IsTaxable ?? data.isTaxable ?? data.isIR ?? data.isIr ?? true,
      isSocial: data.IsSocial ?? data.isSocial ?? data.isCNSS ?? data.isCnss ?? true,
      isCIMR: data.IsCIMR ?? data.isCIMR ?? data.isCimr ?? false,
      exemptionLimit: data.ExemptionLimit ?? data.exemptionLimit ?? null,
      exemptionRule: data.ExemptionRule ?? data.exemptionRule ?? null,
      defaultAmount: data.DefaultAmount ?? data.defaultAmount ?? null,
      version: data.Version ?? data.version ?? 1,
      validFrom: data.ValidFrom ?? data.validFrom ?? '',
      validTo: data.ValidTo ?? data.validTo ?? null,
      isRegulated: data.IsRegulated ?? data.isRegulated ?? false,
      isActive: data.IsActive ?? data.isActive ?? true,
      sortOrder: data.SortOrder ?? data.sortOrder ?? 0,
      createdAt: data.CreatedAt ?? data.createdAt ?? '',
      updatedAt: data.UpdatedAt ?? data.updatedAt ?? ''
    };
  }

  private normalizeStatus(value: string | null | undefined): SalaryPackageStatus {
    const normalized = (value ?? 'draft').toString().trim().toLowerCase();
    if (normalized === 'published' || normalized === 'deprecated') return normalized;
    return 'draft';
  }

  private normalizeComponentType(value: string | null | undefined): SalaryComponentType {
    const normalized = (value ?? 'allowance').toString().trim().toLowerCase();
    const validTypes: SalaryComponentType[] = ['base_salary', 'allowance', 'bonus', 'benefit_in_kind', 'social_charge'];
    if (validTypes.includes(normalized as SalaryComponentType)) {
      return normalized as SalaryComponentType;
    }
    return 'allowance';
  }

  private normalizeTemplateType(value: string | null | undefined): TemplateType {
    const normalized = (value ?? '').toString().trim().toUpperCase();
    if (normalized === 'OFFICIAL' || normalized === 'COMPANY') {
      return normalized;
    }
    return 'OFFICIAL';
  }

  private transformCimrConfig(data: any): CimrConfig | null {
    if (!data) {
      return null;
    }

    const regime = this.normalizeCimrRegime(data.Regime ?? data.regime);
    const employeeRate = Number(data.EmployeeRate ?? data.employeeRate ?? 0);
    const employerRate = Number(data.EmployerRate ?? data.employerRate ?? 0);
    const customEmployerRate = data.CustomEmployerRate ?? data.customEmployerRate ?? null;

    return {
      regime,
      employeeRate,
      employerRate,
      customEmployerRate
    };
  }

  private normalizeCimrRegime(value: string | null | undefined): CimrRegime {
    const normalized = (value ?? 'NONE').toString().trim().toUpperCase();
    if (normalized === 'AL_KAMIL' || normalized === 'AL_MOUNASSIB') {
      return normalized;
    }
    return 'NONE';
  }

  private toWritePayload(request: SalaryPackageWriteRequest, includeIds: boolean): any {
    const items = (request.items || []).map((item, index) => {
      const payload: any = {
        PayComponentId: item.payComponentId ?? null,
        Label: item.label,
        DefaultValue: item.defaultValue,
        SortOrder: item.sortOrder ?? index + 1,
        Type: item.type,
        IsTaxable: item.isTaxable,
        IsSocial: item.isSocial,
        IsCIMR: item.isCIMR,
        IsVariable: item.isVariable,
        ExemptionLimit: item.exemptionLimit ?? null,
        ReferentielElementId: item.referentielElementId ?? null
      };
      if (includeIds && item.id) {
        payload.Id = item.id;
      }
      return payload;
    });

    return {
      Name: request.name,
      Category: request.category,
      Description: request.description ?? null,
      BaseSalary: request.baseSalary,
      Status: request.status,
      CompanyId: request.companyId ?? null,
      TemplateType: request.templateType ?? 'COMPANY',
      RegulationVersion: request.regulationVersion ?? 'MA_2025',
      AutoRules: request.autoRules ? {
        SeniorityBonusEnabled: request.autoRules.seniorityBonusEnabled,
        RuleVersion: request.autoRules.ruleVersion
      } : null,
      CimrConfig: request.cimrConfig ? {
        Regime: request.cimrConfig.regime,
        EmployeeRate: request.cimrConfig.employeeRate,
        EmployerRate: request.cimrConfig.employerRate,
        CustomEmployerRate: request.cimrConfig.customEmployerRate ?? null
      } : null,
      CimrRate: request.cimrRate ?? null, // Legacy field
      HasPrivateInsurance: request.hasPrivateInsurance ?? false,
      ValidFrom: request.validFrom ?? null,
      ValidTo: request.validTo ?? null,
      Items: items
    };
  }
}
