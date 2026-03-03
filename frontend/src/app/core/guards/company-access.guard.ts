import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { CompanyContextService } from '../services/companyContext.service';
import { CompanyService } from '../services/company.service';
import { map, catchError, of } from 'rxjs';

/**
 * Company Access Guard
 * Verifies that the user has access to the company specified in the route parameters
 * Checks against memberships (Standard) or Managed Companies (Expert)
 */
export const companyAccessGuard: CanActivateFn = (route, state) => {
  const contextService = inject(CompanyContextService);
  const companyService = inject(CompanyService);
  const router = inject(Router);
  
  const targetCompanyId = route.paramMap.get('companyId') || route.queryParamMap.get('companyId');

  // If no company ID in URL, allow navigation (guard doesn't apply)
  if (!targetCompanyId) {
    return true;
  }

  // 1. Check against current context
  if (contextService.companyId() === targetCompanyId) {
    return true;
  }

  // 2. Check against memberships (Standard Mode)
  const membership = contextService.memberships().find(m => m.companyId === targetCompanyId);
  if (membership) {
    // Auto-switch context? Or just allow?
    // For safety, we might want to force a context switch if the URL implies a specific context
    // But usually guards just block/allow.
    return true;
  }

  // 3. Check against managed companies (Expert Mode)
  if (contextService.isExpertMode()) {
    // We need to check if this company is in the managed list
    // This might require an API call if not cached
    return companyService.getManagedCompanies().pipe(
      map(companies => {
        const hasAccess = companies.some(c => c.id === targetCompanyId);
        if (hasAccess) {
          return true;
        }
        // Redirect to dashboard if no access
        router.navigate(['/cabinet/dashboard']);
        return false;
      }),
      catchError(() => {
        router.navigate(['/cabinet/dashboard']);
        return of(false);
      })
    );
  }

  // Default: No access
  router.navigate(['/dashboard']);
  return false;
};
