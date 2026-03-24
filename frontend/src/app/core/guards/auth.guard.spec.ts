import { TestBed } from '@angular/core/testing';
import { Router, RouterStateSnapshot, ActivatedRouteSnapshot } from '@angular/router';
import { contextGuard, expertModeGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';
import { CompanyContextService } from '../services/companyContext.service';

describe('Auth Guards', () => {
  let routerSpy: jasmine.SpyObj<Router>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let contextServiceSpy: jasmine.SpyObj<CompanyContextService>;

  beforeEach(() => {
    const rSpy = jasmine.createSpyObj('Router', ['navigate']);
    const aSpy = jasmine.createSpyObj('AuthService', ['isAuthenticated']);
    const cSpy = jasmine.createSpyObj('CompanyContextService', [
      'hasContext', 
      'memberships', 
      'autoSelectIfSingle', 
      'isExpertMode'
    ]);

    TestBed.configureTestingModule({
      providers: [
        { provide: Router, useValue: rSpy },
        { provide: AuthService, useValue: aSpy },
        { provide: CompanyContextService, useValue: cSpy }
      ]
    });

    routerSpy = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    authServiceSpy = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    contextServiceSpy = TestBed.inject(CompanyContextService) as jasmine.SpyObj<CompanyContextService>;
  });

  const mockRoute = {} as ActivatedRouteSnapshot;
  const mockState = { url: '/test' } as RouterStateSnapshot;

  describe('contextGuard', () => {
    it('should allow access if authenticated and context selected', () => {
      authServiceSpy.isAuthenticated.and.returnValue(true);
      contextServiceSpy.hasContext.and.returnValue(true);

      const result = TestBed.runInInjectionContext(() => contextGuard(mockRoute, mockState));
      expect(result).toBeTrue();
    });

    it('should redirect to login if not authenticated', () => {
      authServiceSpy.isAuthenticated.and.returnValue(false);

      const result = TestBed.runInInjectionContext(() => contextGuard(mockRoute, mockState));
      
      expect(result).toBeFalse();
      expect(routerSpy.navigate).toHaveBeenCalledWith(['/login'], jasmine.any(Object));
    });

    it('should redirect to select-context if authenticated but no context and multiple memberships', () => {
      authServiceSpy.isAuthenticated.and.returnValue(true);
      contextServiceSpy.hasContext.and.returnValue(false);
      contextServiceSpy.memberships.and.returnValue([{} as any, {} as any]); // Multiple

      const result = TestBed.runInInjectionContext(() => contextGuard(mockRoute, mockState));

      expect(result).toBeFalse();
      expect(routerSpy.navigate).toHaveBeenCalledWith(['/select-context']);
    });
  });

  describe('expertModeGuard', () => {
    it('should allow access if context is expert mode', () => {
      contextServiceSpy.hasContext.and.returnValue(true);
      contextServiceSpy.isExpertMode.and.returnValue(true);

      const result = TestBed.runInInjectionContext(() => expertModeGuard(mockRoute, mockState));
      expect(result).toBeTrue();
    });

    it('should redirect to standard dashboard if context is NOT expert mode', () => {
      contextServiceSpy.hasContext.and.returnValue(true);
      contextServiceSpy.isExpertMode.and.returnValue(false);

      const result = TestBed.runInInjectionContext(() => expertModeGuard(mockRoute, mockState));

      expect(result).toBeFalse();
      expect(routerSpy.navigate).toHaveBeenCalledWith(['/app/dashboard']);
    });
  });
});
