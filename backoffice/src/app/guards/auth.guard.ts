import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const ok = authService.isAuthenticated();

  if (ok) {
    return true;
  }

  // Redirect to login page
  router.navigate(['/login']);
  return false;
};
