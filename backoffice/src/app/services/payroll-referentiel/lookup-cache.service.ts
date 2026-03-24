/**
 * Lookup Cache Service
 * Caches read-only lookup tables with TTL expiration
 * Singleton service for Authorities, Categories, and Eligibility Criteria
 */

import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { BehaviorSubject, Observable, of } from 'rxjs';
import { map, tap, catchError } from 'rxjs/operators';
import { AuthorityDto, ElementCategoryDto, EligibilityCriteriaDto } from '../../models/payroll-referentiel/lookup.models';

interface CacheEntry<T> {
  data: T[];
  timestamp: number;
}

@Injectable({
  providedIn: 'root'
})
export class LookupCacheService {
  private baseUrl = 'http://localhost:5119/api/payroll';
  private cacheTTL = 3600000; // 1 hour in milliseconds

  // BehaviorSubject caches
  private authoritiesCache$ = new BehaviorSubject<CacheEntry<AuthorityDto> | null>(null);
  private categoriesCache$ = new BehaviorSubject<CacheEntry<ElementCategoryDto> | null>(null);
  private eligibilityCache$ = new BehaviorSubject<CacheEntry<EligibilityCriteriaDto> | null>(null);

  constructor(private http: HttpClient) {}

  /**
   * Get all authorities (CNSS, IR, AMO, CIMR)
   * @param includeInactive Include inactive authorities
   * @param forceRefresh Force refresh cache
   */
  getAuthorities(includeInactive = false, forceRefresh = false): Observable<AuthorityDto[]> {
    const cached = this.authoritiesCache$.value;

    // Check if cache is valid
    if (!forceRefresh && cached && this.isCacheValid(cached.timestamp)) {
      return of(cached.data);
    }

    // Fetch from API
    let params = new HttpParams();
    if (includeInactive) {
      params = params.set('includeInactive', 'true');
    }

    return this.http.get<any[]>(`${this.baseUrl}/authorities`, { params }).pipe(
      map(authorities => authorities.map(a => this.transformAuthority(a))),
      tap(data => {
        this.authoritiesCache$.next({
          data,
          timestamp: Date.now()
        });
      }),
      catchError(err => {
        console.error('[LookupCacheService] Failed to fetch authorities:', err);
        // Return cached data if available, even if expired
        return of(cached?.data || []);
      })
    );
  }

  /**
   * Get all element categories (IND_PRO, IND_SOCIAL, PRIME_SPEC, AVANTAGE)
   * @param includeInactive Include inactive categories
   * @param forceRefresh Force refresh cache
   */
  getCategories(includeInactive = false, forceRefresh = false): Observable<ElementCategoryDto[]> {
    const cached = this.categoriesCache$.value;

    if (!forceRefresh && cached && this.isCacheValid(cached.timestamp)) {
      return of(cached.data);
    }

    let params = new HttpParams();
    if (includeInactive) {
      params = params.set('includeInactive', 'true');
    }

    return this.http.get<any[]>(`${this.baseUrl}/element-categories`, { params }).pipe(
      map(categories => categories.map(c => this.transformCategory(c))),
      tap(data => {
        this.categoriesCache$.next({
          data,
          timestamp: Date.now()
        });
      }),
      catchError(err => {
        console.error('[LookupCacheService] Failed to fetch categories:', err);
        return of(cached?.data || []);
      })
    );
  }

  /**
   * Get all eligibility criteria (ALL, CADRES_SUP, PDG_DG, etc.)
   * @param includeInactive Include inactive criteria
   * @param forceRefresh Force refresh cache
   */
  getEligibilityCriteria(includeInactive = false, forceRefresh = false): Observable<EligibilityCriteriaDto[]> {
    const cached = this.eligibilityCache$.value;

    if (!forceRefresh && cached && this.isCacheValid(cached.timestamp)) {
      return of(cached.data);
    }

    let params = new HttpParams();
    if (includeInactive) {
      params = params.set('includeInactive', 'true');
    }

    return this.http.get<any[]>(`${this.baseUrl}/eligibility-criteria`, { params }).pipe(
      map(criteria => criteria.map(e => this.transformEligibility(e))),
      tap(data => {
        this.eligibilityCache$.next({
          data,
          timestamp: Date.now()
        });
      }),
      catchError(err => {
        console.error('[LookupCacheService] Failed to fetch eligibility criteria:', err);
        return of(cached?.data || []);
      })
    );
  }

  /**
   * Create a new element category
   */
  createCategory(name: string, description?: string): Observable<ElementCategoryDto> {
    const payload = { Name: name.trim(), Description: description?.trim() };
    return this.http.post<any>(`${this.baseUrl}/element-categories`, payload).pipe(
      map(data => this.transformCategory(data)),
      tap(() => {
        // Invalidate cache so next getCategories() fetches fresh data
        this.categoriesCache$.next(null);
      })
    );
  }

  /**
   * Clear all caches
   */
  clearCache(): void {
    this.authoritiesCache$.next(null);
    this.categoriesCache$.next(null);
    this.eligibilityCache$.next(null);
  }

  /**
   * Get authority by ID from cache
   */
  getAuthorityById(id: number): Observable<AuthorityDto | undefined> {
    return this.getAuthorities().pipe(
      map(authorities => authorities.find(a => a.id === id))
    );
  }

  /**
   * Get category by ID from cache
   */
  getCategoryById(id: number): Observable<ElementCategoryDto | undefined> {
    return this.getCategories().pipe(
      map(categories => categories.find(c => c.id === id))
    );
  }

  /**
   * Get eligibility criterion by ID from cache
   */
  getEligibilityById(id: number): Observable<EligibilityCriteriaDto | undefined> {
    return this.getEligibilityCriteria().pipe(
      map(criteria => criteria.find(e => e.id === id))
    );
  }

  // ============================================================
  // Private Helper Methods
  // ============================================================

  /**
   * Check if cache timestamp is still valid
   */
  private isCacheValid(timestamp: number): boolean {
    return (Date.now() - timestamp) < this.cacheTTL;
  }

  /**
   * Transform Authority from PascalCase to camelCase
   */
  private transformAuthority(data: any): AuthorityDto {
    return {
      id: data.Id || data.id,
      code: data.Code || data.code,
      name: data.Name || data.name,
      description: data.Description || data.description,
      isActive: data.IsActive !== undefined ? data.IsActive : data.isActive
    };
  }

  /**
   * Transform Category from PascalCase to camelCase
   */
  private transformCategory(data: any): ElementCategoryDto {
    return {
      id: data.Id || data.id,
      name: data.Name || data.name,
      description: data.Description || data.description,
      isActive: data.IsActive !== undefined ? data.IsActive : data.isActive
    };
  }

  /**
   * Transform Eligibility from PascalCase to camelCase
   */
  private transformEligibility(data: any): EligibilityCriteriaDto {
    return {
      id: data.Id || data.id,
      name: data.Name || data.name,
      description: data.Description || data.description,
      isActive: data.IsActive !== undefined ? data.IsActive : data.isActive
    };
  }
}
