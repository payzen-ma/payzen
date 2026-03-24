import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';

function isTokenExpired(token: string): boolean {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return true;
    const payload = JSON.parse(atob(parts[1]));
    const exp = payload.exp;
    if (!exp) return false;
    const now = Math.floor(Date.now() / 1000);
    return now >= exp;
  } catch {
    return true;
  }
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.getToken();

  // If token exists but expired, trigger logout (skip API call) and continue without header
  if (token && isTokenExpired(token)) {
    auth.logout(true);
    return next(req).pipe(catchError(err => throwError(() => err)));
  }

  const cloned = token ? req.clone({ headers: req.headers.set('Authorization', `Bearer ${token}`) }) : req;

  return next(cloned).pipe(catchError(err => {
    // If server responds 401, force logout
    if (err && err.status === 401) {
      try { auth.logout(); } catch (e) { /* ignore */ }
    }
    return throwError(() => err);
  }));
};
