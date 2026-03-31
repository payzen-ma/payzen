import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const isAuth = auth.isAuthenticated();
  console.log('[AUTH-FLOW][GUARD][guestGuard]', {
    currentUrl: router.url,
    isAuthenticated: isAuth,
  });

  if (isAuth) {
    console.warn('[AUTH-FLOW][GUARD][guestGuard] redirect auth user -> /dashboard');
    router.navigate(['/dashboard']);
    return false;
  }
  return true;
};
