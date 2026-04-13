import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import {
  AutoRules,
  CimrConfig,
  CimrRegime,
  PayComponent,
  RegulationVersion,
  SalaryComponentType,
  SalaryPackage,
  SalaryPackageCloneRequest,
  SalaryPackageItem,
  SalaryPackageStatus,
  SalaryPackageWriteRequest,
  TemplateType
} from '../models/salary-package.model';

@Injectable({
  providedIn: 'root'
})
export class SalaryPackageService {
  private baseUrl = 'https://api-test.payzenhr.com';
  private packagesUrl = `${this.baseUrl}/api/salary-packages`;
  private componentsUrl = `${this.baseUrl}/api/pay-components`;

  constructor(private http: HttpClient) { }

  // ============ Salary Packages ============

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

  getGlobalTemplates(status?: SalaryPackageStatus, category?: string): Observable<SalaryPackage[]> {
    let params = new HttpParams();
    if (status) {
      params = params.set('status', status);
    }
    if (category) {
      params = params.set('category', category);
    }
    return this.http.get<any[]>(`${this.packagesUrl}/templates`, { params }).pipe(
      map(packages => packages.map(pkg => this.transformPackage(pkg)))
    );
  }

  getById(id: number): Observable<SalaryPackage> {
    return this.http.get<any>(`${this.packagesUrl}/${id}`).pipe(
      map(pkg => this.transformPackage(pkg))
    );
  }

  create(request: SalaryPackageWriteRequest): Observable<SalaryPackage> {
    const payload = this.toWritePayload(request, false);
    return this.http.post<any>(this.packagesUrl, payload).pipe(
      map(pkg => this.transformPackage(pkg))
    );
  }

  update(id: number, request: SalaryPackageWriteRequest): Observable<SalaryPackage> {
    const payload = this.toWritePayload(request, true);
    return this.http.put<any>(`${this.packagesUrl}/${id}`, payload).pipe(
      map(pkg => this.transformPackage(pkg))
    );
  }

  delete(id: number): Observable<void> {
    return this.http.delete<void>(`${this.packagesUrl}/${id}`);
  }

  clone(id: number, request: SalaryPackageCloneRequest): Observable<SalaryPackage> {
    const payload = {
      CompanyId: request.companyId,
      Name: request.name || null,
      ValidFrom: request.validFrom || null
    };
    return this.http.post<any>(`${this.packagesUrl}/${id}/clone`, payload).pipe(
      map(pkg => this.transformPackage(pkg))
    );
  }

  createNewVersion(id: number): Observable<SalaryPackage> {
    return this.http.post<any>(`${this.packagesUrl}/${id}/new-version`, {}).pipe(
      map(pkg => this.transformPackage(pkg))
    );
  }

  /**
   * Publish a draft template (transitions status from 'draft' to 'published')
   * Published templates become read-only and available to clients
   */
  publish(id: number): Observable<SalaryPackage> {
    return this.http.post<any>(`${this.packagesUrl}/${id}/publish`, {}).pipe(
      map(pkg => this.transformPackage(pkg))
    );
  }

  /**
   * Deprecate a published template (transitions status from 'published' to 'deprecated')
   * Deprecated templates are no longer available for new assignments
   */
  deprecate(id: number): Observable<SalaryPackage> {
    return this.http.post<any>(`${this.packagesUrl}/${id}/deprecate`, {}).pipe(
      map(pkg => this.transformPackage(pkg))
    );
  }

  /**
   * Duplicate an existing template (creates a new draft copy)
   */
  duplicate(id: number, name?: string): Observable<SalaryPackage> {
    const payload = { Name: name || null };
    return this.http.post<any>(`${this.packagesUrl}/${id}/duplicate`, payload).pipe(
      map(pkg => this.transformPackage(pkg))
    );
  }

  // ============ Pay Components ============

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

  getEffectivePayComponents(date?: Date): Observable<PayComponent[]> {
    let params = new HttpParams();
    if (date) {
      params = params.set('date', date.toISOString());
    }
    return this.http.get<any[]>(`${this.componentsUrl}/effective`, { params }).pipe(
      map(components => components.map(c => this.transformPayComponent(c)))
    );
  }

  // ============ Transformations ============

  private transformPackage(data: any): SalaryPackage {
    const items: SalaryPackageItem[] = (data.Items ?? data.items ?? [])
      .map((item: any) => this.transformItem(item));
    const sortedItems = [...items].sort((a, b) => (a.sortOrder ?? 0) - (b.sortOrder ?? 0));

    // Determine template type based on companyId
    const companyId = data.CompanyId ?? data.companyId ?? null;
    const templateType: TemplateType = companyId === null ? 'OFFICIAL' : 'COMPANY';

    // Parse auto rules or use defaults
    const autoRules: AutoRules = this.transformAutoRules(data.AutoRules ?? data.autoRules);

    return {
      id: data.Id ?? data.id,
      name: data.Name ?? data.name ?? '',
      category: data.Category ?? data.category ?? '',
      description: data.Description ?? data.description ?? null,
      baseSalary: Number(data.BaseSalary ?? data.baseSalary ?? 0),
      status: this.normalizeStatus(data.Status ?? data.status),
      companyId,
      companyName: data.CompanyName ?? data.companyName ?? null,

      // Template classification
      templateType: this.normalizeTemplateType(data.TemplateType ?? data.templateType) ?? templateType,
      regulationVersion: this.normalizeRegulationVersion(data.RegulationVersion ?? data.regulationVersion),
      autoRules,

      // CIMR configuration
      cimrConfig: this.transformCimrConfig(data.CimrConfig ?? data.cimrConfig),
      cimrRate: data.CimrRate ?? data.cimrRate ?? null, // Legacy field
      hasPrivateInsurance: data.HasPrivateInsurance ?? data.hasPrivateInsurance ?? false,

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
      referentielElementId: data.ReferentielElementId ?? data.referentielElementId ?? null,
      referentielElementName: data.ReferentielElementName ?? data.referentielElementName ?? null,
      label: data.Label ?? data.label ?? '',
      defaultValue: Number(data.DefaultValue ?? data.defaultValue ?? 0),
      sortOrder: data.SortOrder ?? data.sortOrder,
      type: this.normalizeComponentType(data.Type ?? data.type),
      isTaxable: data.IsTaxable ?? data.isTaxable ?? true,
      isSocial: data.IsSocial ?? data.isSocial ?? true,
      isCIMR: data.IsCIMR ?? data.isCIMR ?? false,
      isVariable: data.IsVariable ?? data.isVariable ?? false,
      exemptionLimit: data.ExemptionLimit ?? data.exemptionLimit ?? null
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
      isTaxable: data.IsTaxable ?? data.isTaxable ?? true,
      isSocial: data.IsSocial ?? data.isSocial ?? true,
      isCIMR: data.IsCIMR ?? data.isCIMR ?? false,
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

  private normalizeRegulationVersion(value: string | null | undefined): RegulationVersion {
    // Currently only MA_2025 is supported
    return 'MA_2025';
  }

  private transformAutoRules(data: any): AutoRules {
    if (!data) {
      return {
        seniorityBonusEnabled: true, // Default enabled for Morocco
        ruleVersion: 'MA_2025'
      };
    }
    return {
      seniorityBonusEnabled: data.SeniorityBonusEnabled ?? data.seniorityBonusEnabled ?? true,
      ruleVersion: this.normalizeRegulationVersion(data.RuleVersion ?? data.ruleVersion)
    };
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
        ReferentielElementId: item.referentielElementId ?? null,
        Label: item.label,
        DefaultValue: item.defaultValue,
        SortOrder: item.sortOrder ?? index + 1,
        Type: item.type,
        IsTaxable: item.isTaxable,
        IsSocial: item.isSocial,
        IsCIMR: item.isCIMR,
        IsVariable: item.isVariable,
        ExemptionLimit: item.exemptionLimit ?? null
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
      TemplateType: request.templateType ?? 'OFFICIAL',
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
      CimrRate: request.cimrRate ?? 0, // Legacy field; DB does not allow NULL
      HasPrivateInsurance: request.hasPrivateInsurance ?? false,
      ValidFrom: request.validFrom ?? null,
      ValidTo: request.validTo ?? null,
      Items: items
    };
  }
}
