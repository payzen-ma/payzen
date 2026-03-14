import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '@app/core/services/auth.service';
import { CompanyContextService } from '@app/core/services/companyContext.service';

/**
 * HTTP Interceptor to attach authentication token and company context to requests
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  console.log('🔍 [AUTH INTERCEPTOR] Interception de la requête:', req.url);
  
  // Skip adding headers for static assets (translations, etc.)
  if (req.url.includes('/assets/') || req.url.startsWith('./assets/')) {
    console.log('🔍 [AUTH INTERCEPTOR] Fichier statique détecté, skip');
    return next(req);
  }
  
  const authService = inject(AuthService);
  const contextService = inject(CompanyContextService);
  
  const token = authService.getToken();
  const companyId = contextService.companyId();
  const isExpertMode = contextService.isExpertMode();

  console.log('🔍 [AUTH INTERCEPTOR] Token récupéré:', token ? token.substring(0, 50) + '...' : 'AUCUN');

  // Skip adding headers for auth endpoints
  if (req.url.includes('/auth/login') || req.url.includes('/auth/register')) {
    console.log('🔍 [AUTH INTERCEPTOR] Endpoint auth détecté, skip');
    return next(req);
  }

  // Build headers object
  const headers: Record<string, string> = {};

  // Add Authorization header if token exists
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
    console.log('✅ [AUTH INTERCEPTOR] Header Authorization ajouté');
  } else {
    console.warn('⚠️ [AUTH INTERCEPTOR] Aucun token disponible !');
  }

  // Add X-Company-Id header if context is selected
  if (companyId) {
    headers['X-Company-Id'] = companyId;
    console.log('✅ [AUTH INTERCEPTOR] Header X-Company-Id ajouté:', companyId);
  }

  // Add X-Role-Context header
  if (isExpertMode) {
    headers['X-Role-Context'] = 'expert';
  } else if (companyId) {
    headers['X-Role-Context'] = 'standard';
  }

  console.log('🔍 [AUTH INTERCEPTOR] Headers finaux à ajouter:', headers);

  // Clone request with new headers if we have any
  if (Object.keys(headers).length > 0) {
    const clonedReq = req.clone({
      setHeaders: headers
    });
    console.log('✅ [AUTH INTERCEPTOR] Requête clonée avec nouveaux headers');
    return next(clonedReq);
  }

  console.log('⚠️ [AUTH INTERCEPTOR] Aucun header à ajouter, requête originale envoyée');
  return next(req);
};
