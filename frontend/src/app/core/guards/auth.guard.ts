import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { CompanyContextService } from '../services/companyContext.service';

/**
 * Auth Guard - Protects routes requiring authentication
 */
export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (authService.isAuthenticated()) {
    return true;
  }

  // Redirect to login with return URL
  router.navigate(['/login'], {
    queryParams: { returnUrl: state.url }
  });
  return false;
};

/**
 * Context Guard - Ensures user has selected a company context
 * Should be used AFTER authGuard in route configuration
 */
export const contextGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const contextService = inject(CompanyContextService);
  const router = inject(Router);

  // First check authentication
  if (!authService.isAuthenticated()) {
    router.navigate(['/login'], {
      queryParams: { returnUrl: state.url }
    });
    return false;
  }

  // Check if user has selected a context
  if (!contextService.hasContext()) {
    // Check if there are memberships to choose from
    const memberships = contextService.memberships();
    
    if (memberships.length === 0) {
      // No memberships yet - might be right after login, allow routing
      // The login flow will handle setting memberships
      return true;
    }
    
    if (memberships.length === 1) {
      // Auto-select single membership
      contextService.autoSelectIfSingle();
      return true;
    }
    
    // Multiple memberships - redirect to selection
    router.navigate(['/select-context']);
    return false;
  }

  return true;
};

/**
 * Context Selection Guard - Prevents access to context selection if already selected
 * or if user has only one membership
 */
export const contextSelectionGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const contextService = inject(CompanyContextService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    router.navigate(['/login']);
    return false;
  }

  // If context is already selected, redirect to appropriate dashboard
  if (contextService.hasContext()) {
    const route = contextService.getDefaultRoute();
    router.navigate([route]);
    return false;
  }

  // If only one membership, auto-select and redirect
  if (contextService.autoSelectIfSingle()) {
    const route = contextService.getDefaultRoute();
    router.navigate([route]);
    return false;
  }

  return true;
};

/**
 * Expert Mode Guard - Only allows access in expert mode
 */
export const expertModeGuard: CanActivateFn = (route, state) => {
  const contextService = inject(CompanyContextService);
  const router = inject(Router);

  if (!contextService.hasContext()) {
    router.navigate(['/select-context']);
    return false;
  }

  if (!contextService.isExpertMode()) {
    // Not in expert mode, redirect to standard dashboard
    router.navigate(['/app/dashboard']);
    return false;
  }

  return true;
};

/**
 * Standard Mode Guard - Only allows access in standard mode
 */
export const standardModeGuard: CanActivateFn = (route, state) => {
  const contextService = inject(CompanyContextService);
  const router = inject(Router);

  if (!contextService.hasContext()) {
    router.navigate(['/select-context']);
    return false;
  }

  if (contextService.isExpertMode()) {
    // In expert mode, redirect to expert dashboard
    router.navigate(['/expert/dashboard']);
    return false;
  }

  return true;
};

/**
 * Guest Guard - Redirects authenticated users away from auth pages
 */
export const guestGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const contextService = inject(CompanyContextService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    return true;
  }

  // Check if context is selected
  if (!contextService.hasContext()) {
    const memberships = contextService.memberships();
    if (memberships.length > 1) {
      router.navigate(['/select-context']);
      return false;
    }
  }

  // Redirect to appropriate dashboard based on context
  const defaultRoute = contextService.getDefaultRoute();
  router.navigate([defaultRoute]);
  return false;
};

/**
 * Role Guard Factory - Creates guards for specific roles
 */
export const createRoleGuard = (allowedRoles: string[]): CanActivateFn => {
  return (route, state) => {
    const authService = inject(AuthService);
    const router = inject(Router);

    if (!authService.isAuthenticated()) {
      router.navigate(['/login'], {
        queryParams: { returnUrl: state.url }
      });
      return false;
    }

    const user = authService.getCurrentUser();
    if (user && allowedRoles.includes(user.role)) {
      return true;
    }

    // Redirect to access denied or appropriate page
    router.navigate(['/access-denied']);
    return false;
  };
};

/**
 * Admin Guard - Only Admin and Admin PayZen
 */
export const adminGuard: CanActivateFn = createRoleGuard(['admin', 'admin_payzen']);

/**
 * RH Guard - Admin, RH, and Admin PayZen
 */
export const rhGuard: CanActivateFn = createRoleGuard(['admin', 'rh', 'admin_payzen']);

/**
 * Manager Guard - Admin, RH, Manager, and Admin PayZen
 */
export const managerGuard: CanActivateFn = createRoleGuard(['admin', 'rh', 'manager', 'admin_payzen']);

/**
 * Cabinet Guard - Cabinet and Admin PayZen
 */
export const cabinetGuard: CanActivateFn = createRoleGuard(['cabinet', 'admin_payzen']);

/**
 * Permission Guard Factory - Checks current context or user permissions
 */
export const createPermissionGuard = (permission: string): CanActivateFn => {
  return (route, state) => {
    const authService = inject(AuthService);
    const contextService = inject(CompanyContextService);
    const router = inject(Router);

    if (!authService.isAuthenticated()) {
      router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
      return false;
    }

    // Check current context permissions first
    if (contextService.hasContext() && contextService.hasPermission(permission)) {
      return true;
    }

    // Fallback to user-level permissions
    const user = authService.getCurrentUser();
    if (user && Array.isArray(user.permissions) && user.permissions.includes(permission)) {
      return true;
    }

    // Redirect to access denied page
    router.navigate(['/access-denied']);
    return false;
  };
};

/**
 * View Presence Guard - Checks employee mode
 * Only allows access if mode is 'Presence' or 'Attendance'
 */
export const viewPresenceGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }

  const user = authService.getCurrentUser();
  
  // If mode is 'Absence', deny access to presence/attendance
  if (user?.mode && user.mode.toLowerCase() === 'absence') {
    router.navigate(['/access-denied']);
    return false;
  }

  return true;
};

/**
 * View Absence Guard - Checks employee mode
 * Only allows access if mode is 'Absence'
 */
export const viewAbsenceGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (!authService.isAuthenticated()) {
    router.navigate(['/login'], { queryParams: { returnUrl: state.url } });
    return false;
  }

  const user = authService.getCurrentUser();
  
  // If mode is 'Presence' or 'Attendance', deny access to absence
  const userMode = user?.mode?.toLowerCase();
  if (userMode && (userMode === 'presence' || userMode === 'attendance')) {
    router.navigate(['/access-denied']);
    return false;
  }

  return true;
};
