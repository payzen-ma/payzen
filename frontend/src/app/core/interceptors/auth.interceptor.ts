import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '@app/core/services/auth.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';

/**
 * HTTP Interceptor to attach authentication token and company context to requests
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const contextService = inject(CompanyContextService);
  
  const token = authService.getToken();
  const companyId = contextService.companyId();
  const isExpertMode = contextService.isExpertMode();

  // Skip adding headers for auth endpoints
  if (req.url.includes('/auth/login') || req.url.includes('/auth/register')) {
    return next(req);
  }

  // Build headers object
  const headers: Record<string, string> = {};

  // Add Authorization header if token exists
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  // Add X-Company-Id header if context is selected
  if (companyId) {
    headers['X-Company-Id'] = companyId;
  }

  // Add X-Role-Context header
  if (isExpertMode) {
    headers['X-Role-Context'] = 'expert';
  } else if (companyId) {
    headers['X-Role-Context'] = 'standard';
  }

  // Clone request with new headers if we have any
  if (Object.keys(headers).length > 0) {
    const clonedReq = req.clone({
      setHeaders: headers
    });
    return next(clonedReq);
  }

  return next(req);
};
