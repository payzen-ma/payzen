import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '@app/core/services/auth.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';

/**
 * HTTP Interceptor to attach authentication token and company context to requests
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  if (req.url.includes('/assets/') || req.url.startsWith('./assets/')) {
    return next(req);
  }

  const authService = inject(AuthService);
  const contextService = inject(CompanyContextService);

  const token = authService.getToken();
  const companyId = contextService.companyId();
  const isExpertMode = contextService.isExpertMode();

  if (req.url.includes('/auth/login') || req.url.includes('/auth/register') || req.url.includes('/auth/entra-login')) {
    return next(req);
  }

  const headers: Record<string, string> = {};

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  if (companyId) {
    headers['X-Company-Id'] = companyId;
  }

  if (isExpertMode) {
    headers['X-Role-Context'] = 'expert';
  } else if (companyId) {
    headers['X-Role-Context'] = 'standard';
  }

  if (Object.keys(headers).length > 0) {
    return next(req.clone({ setHeaders: headers }));
  }

  return next(req);
};
