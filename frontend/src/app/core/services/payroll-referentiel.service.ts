/**
 * Payroll Referentiel Service
 * API service for fetching referentiel elements and their rules
 */

import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from '@environments/environment';

import {
  ReferentielElementListDto,
  ReferentielElementDto,
  ElementRuleDto,
  PaymentFrequency,
  ExemptionType,
  CapUnit,
  BaseReference
} from '@app/core/models/payroll-referentiel.model';

@Injectable({
  providedIn: 'root'
})
export class PayrollReferentielService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl.replace('/api', '');
  private readonly payrollUrl = `${this.baseUrl}/api/payroll`;

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

    return this.http.get<any[]>(`${this.payrollUrl}/referentiel-elements`, { params }).pipe(
      map(list => list.map(item => this.transformReferentielElementList(item)))
    );
  }

  /**
   * Get referentiel element by ID (full details with rules)
   */
  getReferentielElementById(id: number): Observable<ReferentielElementDto> {
    return this.http.get<any>(`${this.payrollUrl}/referentiel-elements/${id}`).pipe(
      map(item => this.transformReferentielElement(item))
    );
  }

  // ============================================================
  // Private Transformation Methods
  // ============================================================

  private transformReferentielElementList(data: any): ReferentielElementListDto {
    return {
      id: data.Id ?? data.id,
      name: data.Name ?? data.name,
      categoryName: data.CategoryName ?? data.categoryName,
      defaultFrequency: (data.DefaultFrequency ?? data.defaultFrequency) as PaymentFrequency,
      isActive: data.IsActive !== undefined ? data.IsActive : data.isActive,
      isConvergence: data.IsConvergence !== undefined ? data.IsConvergence : data.isConvergence,
      ruleCount: data.RuleCount ?? data.ruleCount ?? 0
    };
  }

  private transformReferentielElement(data: any): ReferentielElementDto {
    const rules = (data.Rules ?? data.rules ?? []).map((r: any) => this.transformElementRule(r));

    return {
      id: data.Id ?? data.id,
      name: data.Name ?? data.name,
      categoryId: data.CategoryId ?? data.categoryId,
      categoryName: data.CategoryName ?? data.categoryName,
      description: data.Description ?? data.description,
      defaultFrequency: (data.DefaultFrequency ?? data.defaultFrequency) as PaymentFrequency,
      isActive: data.IsActive !== undefined ? data.IsActive : data.isActive,
      isConvergence: data.IsConvergence !== undefined ? data.IsConvergence : data.isConvergence,
      rules
    };
  }

  private transformElementRule(data: any): ElementRuleDto {
    return {
      id: data.Id ?? data.id,
      elementId: data.ElementId ?? data.elementId,
      authorityId: data.AuthorityId ?? data.authorityId,
      authorityName: data.AuthorityName ?? data.authorityName,
      exemptionType: (data.ExemptionType ?? data.exemptionType) as ExemptionType,
      sourceRef: data.SourceRef ?? data.sourceRef,
      effectiveFrom: data.EffectiveFrom ?? data.effectiveFrom,
      effectiveTo: data.EffectiveTo ?? data.effectiveTo,
      isActive: data.IsActive !== undefined ? data.IsActive : data.isActive,
      cap: data.Cap ?? data.cap ? this.transformRuleCap(data.Cap ?? data.cap) : undefined,
      percentage: data.Percentage ?? data.percentage ? this.transformRulePercentage(data.Percentage ?? data.percentage) : undefined,
      formula: data.Formula ?? data.formula ? this.transformRuleFormula(data.Formula ?? data.formula) : undefined,
      tiers: (data.Tiers ?? data.tiers ?? []).map((t: any) => this.transformRuleTier(t)),
      variants: (data.Variants ?? data.variants ?? []).map((v: any) => this.transformRuleVariant(v))
    };
  }

  private transformRuleCap(data: any) {
    return {
      id: data.Id ?? data.id,
      capAmount: data.CapAmount ?? data.capAmount,
      capUnit: (data.CapUnit ?? data.capUnit) as CapUnit,
      minAmount: data.MinAmount ?? data.minAmount
    };
  }

  private transformRulePercentage(data: any) {
    return {
      id: data.Id ?? data.id,
      percentage: data.Percentage ?? data.percentage,
      baseReference: (data.BaseReference ?? data.baseReference) as BaseReference,
      eligibilityId: data.EligibilityId ?? data.eligibilityId,
      eligibilityName: data.EligibilityName ?? data.eligibilityName
    };
  }

  private transformRuleFormula(data: any) {
    return {
      id: data.Id ?? data.id,
      multiplier: data.Multiplier ?? data.multiplier,
      parameterId: data.ParameterId ?? data.parameterId,
      parameterName: data.ParameterName ?? data.parameterName,
      resultUnit: (data.ResultUnit ?? data.resultUnit) as CapUnit,
      currentCapValue: data.CurrentCapValue ?? data.currentCapValue
    };
  }

  private transformRuleTier(data: any) {
    return {
      id: data.Id ?? data.id,
      tierOrder: data.TierOrder ?? data.tierOrder,
      minAmount: data.MinAmount ?? data.minAmount,
      maxAmount: data.MaxAmount ?? data.maxAmount,
      exemptionRate: data.ExemptionRate ?? data.exemptionRate
    };
  }

  private transformRuleVariant(data: any) {
    return {
      id: data.Id ?? data.id,
      variantKey: data.VariantKey ?? data.variantKey,
      variantLabel: data.VariantLabel ?? data.variantLabel,
      overrideCap: data.OverrideCap ?? data.overrideCap,
      overrideEligibilityId: data.OverrideEligibilityId ?? data.overrideEligibilityId,
      overrideEligibilityName: data.OverrideEligibilityName ?? data.overrideEligibilityName
    };
  }
}
