/**
 * Payroll Referentiel Service
 * Main API service for Legal Parameters, Elements, and Rules
 * Follows existing patterns: PascalCase ↔ camelCase transformation, HttpParams for queries
 */

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import {
  LegalParameterDto,
  CreateLegalParameterDto,
  UpdateLegalParameterDto
} from '../../models/payroll-referentiel/legal-parameter.model';
import {
  ReferentielElementDto,
  ReferentielElementListDto,
  CreateReferentielElementDto,
  UpdateReferentielElementDto,
  ConvergenceResultDto,
  ElementStatus
} from '../../models/payroll-referentiel/referentiel-element.model';
import {
  ElementRuleDto,
  CreateElementRuleDto,
  UpdateElementRuleDto
} from '../../models/payroll-referentiel/element-rule.model';
import { PaymentFrequency, ExemptionType, CapUnit, BaseReference } from '../../models/payroll-referentiel/lookup.models';

@Injectable({
  providedIn: 'root'
})
export class PayrollReferentielService {
  private baseUrl = 'http://localhost:5119/api/payroll';

  constructor(private http: HttpClient) {}

  // ============================================================
  // Legal Parameters API
  // ============================================================

  /**
   * Get all legal parameters (SMIG, SMAG, CIMR rates, etc.)
   */
  getAllLegalParameters(includeInactive = false, asOfDate?: string): Observable<LegalParameterDto[]> {
    let params = new HttpParams();
    if (includeInactive) {
      params = params.set('includeInactive', 'true');
    }
    if (asOfDate) {
      params = params.set('asOfDate', asOfDate);
    }

    return this.http.get<any[]>(`${this.baseUrl}/legal-parameters`, { params }).pipe(
      map(list => list.map(item => this.transformLegalParameter(item)))
    );
  }

  /**
   * Get legal parameter by ID
   */
  getLegalParameterById(id: number): Observable<LegalParameterDto> {
    return this.http.get<any>(`${this.baseUrl}/legal-parameters/${id}`).pipe(
      map(item => this.transformLegalParameter(item))
    );
  }

  /**
   * Get legal parameter by name (latest active version)
   */
  getLegalParameterByName(name: string, asOfDate?: string): Observable<LegalParameterDto> {
    let params = new HttpParams();
    if (asOfDate) {
      params = params.set('asOfDate', asOfDate);
    }

    return this.http.get<any>(`${this.baseUrl}/legal-parameters/by-name/${encodeURIComponent(name)}`, { params }).pipe(
      map(item => this.transformLegalParameter(item))
    );
  }

  /**
   * Get legal parameter history (all versions by name)
   */
  getLegalParameterHistory(name: string): Observable<LegalParameterDto[]> {
    return this.http.get<any[]>(`${this.baseUrl}/legal-parameters/by-name/${encodeURIComponent(name)}/history`).pipe(
      map(list => list.map(item => this.transformLegalParameter(item)))
    );
  }

  /**
   * Create legal parameter
   */
  createLegalParameter(dto: CreateLegalParameterDto): Observable<LegalParameterDto> {
    const payload = {
      Name: dto.name,
      Description: dto.description,
      Value: dto.value,
      Unit: dto.unit,
      EffectiveFrom: dto.effectiveFrom,
      EffectiveTo: dto.effectiveTo
    };

    return this.http.post<any>(`${this.baseUrl}/legal-parameters`, payload).pipe(
      map(item => this.transformLegalParameter(item))
    );
  }

  /**
   * Update legal parameter
   */
  updateLegalParameter(id: number, dto: UpdateLegalParameterDto): Observable<LegalParameterDto> {
    const payload = {
      Name: dto.name,
      Description: dto.description,
      Value: dto.value,
      Unit: dto.unit,
      EffectiveFrom: dto.effectiveFrom,
      EffectiveTo: dto.effectiveTo
    };

    return this.http.put<any>(`${this.baseUrl}/legal-parameters/${id}`, payload).pipe(
      map(item => this.transformLegalParameter(item))
    );
  }

  /**
   * Check if a legal parameter is used in active rule formulas (and by which elements).
   */
  getLegalParameterUsage(id: number): Observable<{ used: boolean; usedByElements: { elementId: number; elementName: string }[] }> {
    return this.http.get<{ Used?: boolean; UsedByElements?: { ElementId: number; ElementName: string }[] }>(
      `${this.baseUrl}/legal-parameters/${id}/usage`
    ).pipe(
      map(res => ({
        used: res.Used ?? false,
        usedByElements: (res.UsedByElements ?? []).map(e => ({ elementId: e.ElementId, elementName: e.ElementName }))
      }))
    );
  }

  /**
   * Check freshness of legal parameters (identify parameters not updated in 6+ months).
   * Critical parameters like SMIG should be updated when legal values change.
   */
  checkParameterFreshness(): Observable<{
    hasStaleParameters: boolean;
    hasCriticalStale: boolean;
    staleParameters: { id: number; name: string; value: number; unit: string; lastUpdated: string; effectiveFrom: string }[];
    criticalStale: { id: number; name: string; value: number; unit: string; lastUpdated: string; effectiveFrom: string }[];
  }> {
    return this.http.get<any>(`${this.baseUrl}/legal-parameters/freshness-check`).pipe(
      map(res => ({
        hasStaleParameters: res.HasStaleParameters ?? false,
        hasCriticalStale: res.HasCriticalStale ?? false,
        staleParameters: (res.StaleParameters ?? []).map((p: any) => ({
          id: p.Id,
          name: p.Name,
          value: p.Value,
          unit: p.Unit,
          lastUpdated: p.LastUpdated,
          effectiveFrom: p.EffectiveFrom
        })),
        criticalStale: (res.CriticalStale ?? []).map((p: any) => ({
          id: p.Id,
          name: p.Name,
          value: p.Value,
          unit: p.Unit,
          lastUpdated: p.LastUpdated,
          effectiveFrom: p.EffectiveFrom
        }))
      }))
    );
  }

  /**
   * Delete legal parameter
   */
  deleteLegalParameter(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/legal-parameters/${id}`);
  }

  // ============================================================
  // Referentiel Elements API
  // ============================================================

  /**
   * Get all referentiel elements (summary list)
   */
  getAllReferentielElements(includeInactive = false, categoryId?: number): Observable<ReferentielElementListDto[]> {
    let params = new HttpParams();
    if (includeInactive) {
      params = params.set('includeInactive', 'true');
    }
    if (categoryId) {
      params = params.set('categoryId', categoryId.toString());
    }

    return this.http.get<any[]>(`${this.baseUrl}/referentiel-elements`, { params }).pipe(
      map(list => list.map(item => this.transformReferentielElementList(item)))
    );
  }

  /**
   * Get referentiel element by ID (full details with rules)
   */
  getReferentielElementById(id: number): Observable<ReferentielElementDto> {
    return this.http.get<any>(`${this.baseUrl}/referentiel-elements/${id}`).pipe(
      map(item => this.transformReferentielElement(item))
    );
  }

  /**
   * Get element rules
   */
  getElementRules(elementId: number): Observable<ElementRuleDto[]> {
    const params = new HttpParams().set('elementId', elementId.toString());
    return this.http.get<any[]>(`${this.baseUrl}/element-rules`, { params }).pipe(
      map(list => list.map(rule => this.transformElementRule(rule)))
    );
  }

  /**
   * Check convergence between CNSS and IR rules
   */
  checkConvergence(elementId: number, asOfDate?: string): Observable<ConvergenceResultDto> {
    let params = new HttpParams();
    if (asOfDate) {
      params = params.set('asOfDate', asOfDate);
    }

    return this.http.get<any>(`${this.baseUrl}/referentiel-elements/${elementId}/convergence`, { params }).pipe(
      map(item => this.transformConvergenceResult(item))
    );
  }

  /**
   * Create referentiel element
   */
  createReferentielElement(dto: CreateReferentielElementDto): Observable<ReferentielElementDto> {
    const payload = {
      Name: dto.name,
      CategoryId: dto.categoryId,
      Description: dto.description,
      DefaultFrequency: dto.defaultFrequency
    };

    return this.http.post<any>(`${this.baseUrl}/referentiel-elements`, payload).pipe(
      map(item => this.transformReferentielElement(item))
    );
  }

  /**
   * Update referentiel element
   */
  updateReferentielElement(id: number, dto: UpdateReferentielElementDto): Observable<ReferentielElementDto> {
    const payload = {
      Name: dto.name,
      CategoryId: dto.categoryId,
      Description: dto.description,
      DefaultFrequency: dto.defaultFrequency,
      IsActive: dto.isActive
    };

    return this.http.put<any>(`${this.baseUrl}/referentiel-elements/${id}`, payload).pipe(
      map(item => this.transformReferentielElement(item))
    );
  }

  /**
   * Delete referentiel element
   */
  deleteReferentielElement(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/referentiel-elements/${id}`);
  }

  // ============================================================
  // Element Rules API
  // ============================================================

  /**
   * Create element rule
   */
  createElementRule(dto: CreateElementRuleDto): Observable<ElementRuleDto> {
    const payload = this.buildElementRulePayload(dto);
    return this.http.post<any>(`${this.baseUrl}/element-rules`, payload).pipe(
      map(item => this.transformElementRule(item))
    );
  }

  /**
   * Update element rule
   */
  updateElementRule(ruleId: number, dto: UpdateElementRuleDto): Observable<ElementRuleDto> {
    const payload = this.buildElementRulePayload(dto);
    return this.http.put<any>(`${this.baseUrl}/element-rules/${ruleId}`, payload).pipe(
      map(item => this.transformElementRule(item))
    );
  }

  /**
   * Delete element rule
   */
  deleteElementRule(ruleId: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/element-rules/${ruleId}`);
  }

  // ============================================================
  // Private Transformation Methods
  // ============================================================

  private transformLegalParameter(data: any): LegalParameterDto {
    return {
      id: data.Id || data.id,
      name: data.Name || data.name,
      description: data.Description || data.description,
      value: data.Value || data.value,
      unit: data.Unit || data.unit,
      effectiveFrom: data.EffectiveFrom || data.effectiveFrom,
      effectiveTo: data.EffectiveTo || data.effectiveTo,
      isActive: data.IsActive !== undefined ? data.IsActive : data.isActive
    };
  }

  private transformReferentielElementList(data: any): ReferentielElementListDto {
    return {
      id: data.Id || data.id,
      name: data.Name || data.name,
      categoryName: data.CategoryName || data.categoryName,
      defaultFrequency: (data.DefaultFrequency || data.defaultFrequency) as PaymentFrequency,
      isActive: data.IsActive !== undefined ? data.IsActive : data.isActive,
      hasConvergence: data.HasConvergence !== undefined ? data.HasConvergence : (data.IsConvergence !== undefined ? data.IsConvergence : (data.hasConvergence ?? data.isConvergence ?? false)),
      ruleCount: data.RuleCount || data.ruleCount || 0,
      hasCnssRule: data.HasCnssRule !== undefined ? data.HasCnssRule : (data.hasCnssRule ?? false),
      hasDgiRule: data.HasDgiRule !== undefined ? data.HasDgiRule : (data.hasDgiRule ?? false),
      status: (data.Status || data.status || 'ACTIVE') as ElementStatus,
      code: data.Code || data.code
    };
  }

  private transformReferentielElement(data: any): ReferentielElementDto {
    const rules = (data.Rules || data.rules || []).map((r: any) => this.transformElementRule(r));

    return {
      id: data.Id || data.id,
      name: data.Name || data.name,
      categoryId: data.CategoryId || data.categoryId,
      categoryName: data.CategoryName || data.categoryName,
      description: data.Description || data.description,
      defaultFrequency: (data.DefaultFrequency || data.defaultFrequency) as PaymentFrequency,
      isActive: data.IsActive !== undefined ? data.IsActive : data.isActive,
      hasConvergence: data.HasConvergence !== undefined ? data.HasConvergence : (data.IsConvergence !== undefined ? data.IsConvergence : (data.hasConvergence ?? data.isConvergence ?? false)),
      status: (data.Status || data.status || 'ACTIVE') as ElementStatus,
      code: data.Code || data.code,
      rules
    };
  }

  private transformElementRule(data: any): ElementRuleDto {
    return {
      id: data.Id || data.id,
      elementId: data.ElementId || data.elementId,
      authorityId: data.AuthorityId || data.authorityId,
      authorityName: data.AuthorityName || data.authorityName,
      exemptionType: (data.ExemptionType || data.exemptionType) as ExemptionType,
      sourceRef: data.SourceRef || data.sourceRef,
      effectiveFrom: data.EffectiveFrom || data.effectiveFrom,
      effectiveTo: data.EffectiveTo || data.effectiveTo,
      isActive: data.IsActive !== undefined ? data.IsActive : data.isActive,
      status: (data.Status || data.status || 'ACTIVE') as ElementStatus,
      ruleDetails: data.RuleDetails || data.ruleDetails || '{}',
      cap: data.Cap || data.cap ? this.transformRuleCap(data.Cap || data.cap) : undefined,
      percentage: data.Percentage || data.percentage ? this.transformRulePercentage(data.Percentage || data.percentage) : undefined,
      formula: data.Formula || data.formula ? this.transformRuleFormula(data.Formula || data.formula) : undefined,
      tiers: (data.Tiers || data.tiers || []).map((t: any) => this.transformRuleTier(t)),
      variants: (data.Variants || data.variants || []).map((v: any) => this.transformRuleVariant(v))
    };
  }

  private transformRuleCap(data: any) {
    return {
      id: data.Id || data.id,
      capAmount: data.CapAmount || data.capAmount,
      capUnit: (data.CapUnit || data.capUnit) as CapUnit
    };
  }

  private transformRulePercentage(data: any) {
    return {
      id: data.Id || data.id,
      percentage: data.Percentage || data.percentage,
      baseReference: (data.BaseReference || data.baseReference) as BaseReference,
      eligibilityId: data.EligibilityId || data.eligibilityId,
      eligibilityName: data.EligibilityName || data.eligibilityName
    };
  }

  private transformRuleFormula(data: any) {
    return {
      id: data.Id || data.id,
      multiplier: data.Multiplier || data.multiplier,
      parameterId: data.ParameterId || data.parameterId,
      parameterName: data.ParameterName || data.parameterName,
      resultUnit: (data.ResultUnit || data.resultUnit) as CapUnit,
      currentCapValue: data.CurrentCapValue || data.currentCapValue
    };
  }

  private transformRuleTier(data: any) {
    return {
      id: data.Id || data.id,
      tierOrder: data.TierOrder || data.tierOrder,
      minAmount: data.MinAmount || data.minAmount,
      maxAmount: data.MaxAmount || data.maxAmount,
      exemptionRate: data.ExemptionRate || data.exemptionRate
    };
  }

  private transformRuleVariant(data: any) {
    return {
      id: data.Id || data.id,
      variantKey: data.VariantKey || data.variantKey,
      variantLabel: data.VariantLabel || data.variantLabel,
      overrideCap: data.OverrideCap || data.overrideCap,
      overrideEligibilityId: data.OverrideEligibilityId || data.overrideEligibilityId,
      overrideEligibilityName: data.OverrideEligibilityName || data.overrideEligibilityName
    };
  }

  private transformConvergenceResult(data: any): ConvergenceResultDto {
    return {
      elementId: data.ElementId || data.elementId,
      elementName: data.ElementName || data.elementName,
      isConvergence: data.IsConvergence !== undefined ? data.IsConvergence : data.isConvergence,
      checkDate: data.CheckDate || data.checkDate,
      cnssRule: data.CnssRule || data.cnssRule ? {
        exemptionType: data.CnssRule.ExemptionType || data.cnssRule.exemptionType,
        effectiveFrom: data.CnssRule.EffectiveFrom || data.cnssRule.effectiveFrom,
        effectiveTo: data.CnssRule.EffectiveTo || data.cnssRule.effectiveTo
      } : undefined,
      irRule: data.IrRule || data.irRule ? {
        exemptionType: data.IrRule.ExemptionType || data.irRule.exemptionType,
        effectiveFrom: data.IrRule.EffectiveFrom || data.irRule.effectiveFrom,
        effectiveTo: data.IrRule.EffectiveTo || data.irRule.effectiveTo
      } : undefined
    };
  }

  /**
   * Normalize date to yyyy-MM-dd (backend may reject ISO datetime)
   */
  private toDateOnly(value: string | undefined): string | undefined {
    if (value == null || value === '') return undefined;
    const s = value.trim();
    if (s.length >= 10) return s.substring(0, 10);
    return s;
  }

  /**
   * Build element rule payload for create/update.
   * Backend uses PropertyNamingPolicy = null, so JSON must use PascalCase.
   * UpdateElementRuleDto: ExemptionType?, SourceRef?, EffectiveFrom?, EffectiveTo?, Cap?, Percentage?, Formula?, Tiers?, Variants?
   * CreateElementRuleDto: + ElementId, AuthorityId (required for create).
   */
  private buildElementRulePayload(dto: CreateElementRuleDto | UpdateElementRuleDto): any {
    const effectiveFrom = this.toDateOnly(dto.effectiveFrom);
    const effectiveTo = this.toDateOnly(dto.effectiveTo);

    const payload: any = {
      ExemptionType: dto.exemptionType,
      EffectiveFrom: effectiveFrom ?? dto.effectiveFrom
    };

    if (dto.sourceRef != null && dto.sourceRef !== '') {
      payload.SourceRef = dto.sourceRef;
    }
    if (effectiveTo != null && effectiveTo !== '') {
      payload.EffectiveTo = effectiveTo;
    }

    // Always send AuthorityId (required for both create and update)
    if ('authorityId' in dto && dto.authorityId != null) {
      payload.AuthorityId = dto.authorityId;
    }

    // Only for create: backend CreateElementRuleDto has ElementId
    if ('elementId' in dto) {
      payload.ElementId = dto.elementId;
      if ('authorityCode' in dto && dto.authorityCode) {
        payload.AuthorityCode = dto.authorityCode;
      }
    }

    // Add type-specific data (only one is typically set per exemption type)
    if (dto.cap) {
      payload.Cap = {
        CapAmount: dto.cap.capAmount,
        CapUnit: dto.cap.capUnit,
        ...(dto.cap.minAmount != null && { MinAmount: dto.cap.minAmount })
      };
    }

    if (dto.percentage) {
      payload.Percentage = {
        Percentage: dto.percentage.percentage,
        BaseReference: dto.percentage.baseReference,
        ...(dto.percentage.eligibilityId != null && { EligibilityId: dto.percentage.eligibilityId })
      };
    }

    if (dto.formula) {
      payload.Formula = {
        Multiplier: dto.formula.multiplier,
        ParameterId: dto.formula.parameterId,
        ResultUnit: dto.formula.resultUnit
      };
    }

    if (dto.dualCap) {
      payload.DualCap = {
        FixedCapAmount: dto.dualCap.fixedCapAmount,
        FixedCapUnit: dto.dualCap.fixedCapUnit,
        PercentageCap: dto.dualCap.percentageCap,
        BaseReference: dto.dualCap.baseReference,
        Logic: dto.dualCap.logic
      };
    }

    if (dto.tiers && dto.tiers.length > 0) {
      payload.Tiers = dto.tiers.map(t => ({
        TierOrder: t.tierOrder,
        ...(t.minAmount != null && { MinAmount: t.minAmount }),
        ...(t.maxAmount != null && { MaxAmount: t.maxAmount }),
        ExemptionRate: t.exemptionRate
      }));
    }

    if (dto.variants && dto.variants.length > 0) {
      payload.Variants = dto.variants.map(v => ({
        VariantKey: v.variantKey,
        VariantLabel: v.variantLabel,
        ...(v.overrideCap != null && { OverrideCap: v.overrideCap }),
        ...(v.overrideEligibilityId != null && { OverrideEligibilityId: v.overrideEligibilityId })
      }));
    }

    return payload;
  }
}
