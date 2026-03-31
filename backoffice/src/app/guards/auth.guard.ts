import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const ok = authService.isAuthenticated();
  console.log('[AUTH-FLOW][GUARD][authGuard]', {
    currentUrl: router.url,
    isAuthenticated: ok,
  });

  if (ok) {
    return true;
  }

  // Redirect to login page
  console.warn('[AUTH-FLOW][GUARD][authGuard] redirect -> /login');
  router.navigate(['/login']);
  return false;
};
