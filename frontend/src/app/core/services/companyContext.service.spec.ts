import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideZoneChangeDetection } from '@angular/core';
import { CompanyContextService } from './companyContext.service';
import { AppContext, CompanyMembership, CONTEXT_STORAGE_KEYS } from '@app/core/models/membership.model';

describe('CompanyContextService', () => {
  let service: CompanyContextService;
  let routerSpy: jasmine.SpyObj<Router>;

  const mockMembership1: CompanyMembership = {
    companyId: '1',
    companyName: 'Company 1',
    role: 'admin',
    isExpertMode: true,
    permissions: ['READ_ALL']
  };

  const mockMembership2: CompanyMembership = {
    companyId: '2',
    companyName: 'Company 2',
    role: 'employee',
    isExpertMode: false,
    permissions: ['READ_OWN']
  };

  beforeEach(() => {
    const spy = jasmine.createSpyObj('Router', ['navigate']);
    
    // Clear storage before each test
    sessionStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        CompanyContextService,
        { provide: Router, useValue: spy }
      ]
    });

    service = TestBed.inject(CompanyContextService);
    routerSpy = TestBed.inject(Router) as jasmine.SpyObj<Router>;
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('Context Selection', () => {
    it('should select context and navigate to expert dashboard for expert mode', () => {
      service.selectContext(mockMembership1);

      const currentContext = service.currentContext();
      expect(currentContext).toBeTruthy();
      expect(currentContext?.companyId).toBe('1');
      expect(currentContext?.isExpertMode).toBeTrue();
      expect(routerSpy.navigate).toHaveBeenCalledWith(['/expert/dashboard']);
    });

    it('should select context and navigate to app dashboard for standard mode', () => {
      service.selectContext(mockMembership2);

      const currentContext = service.currentContext();
      expect(currentContext).toBeTruthy();
      expect(currentContext?.companyId).toBe('2');
      expect(currentContext?.isExpertMode).toBeFalse();
      expect(routerSpy.navigate).toHaveBeenCalledWith(['/app/dashboard']);
    });

    it('should persist context to sessionStorage', () => {
      service.selectContext(mockMembership1, false);
      
      // Allow the effect to run
      TestBed.flushEffects();

      const stored = sessionStorage.getItem(CONTEXT_STORAGE_KEYS.CURRENT_CONTEXT);
      expect(stored).toBeTruthy();
      const parsed = JSON.parse(stored!);
      expect(parsed.companyId).toBe('1');
    });
  });

  describe('Auto Selection', () => {
    it('should auto-select if single membership exists', () => {
      service.setMemberships([mockMembership1]);
      
      const result = service.autoSelectIfSingle();
      
      expect(result).toBeTrue();
      expect(service.companyId()).toBe('1');
    });

    it('should NOT auto-select if multiple memberships exist', () => {
      service.setMemberships([mockMembership1, mockMembership2]);
      
      const result = service.autoSelectIfSingle();
      
      expect(result).toBeFalse();
      expect(service.companyId()).toBeNull();
    });
  });

  describe('Context Switching (Hybrid Workflow)', () => {
    it('should update signals correctly when switching from Expert to Employee context', () => {
      // 1. Select Expert Context
      service.selectContext(mockMembership1, false);
      expect(service.isExpertMode()).toBeTrue();
      expect(service.role()).toBe('admin');

      // 2. Switch to Employee Context
      service.selectContext(mockMembership2, false);
      expect(service.isExpertMode()).toBeFalse();
      expect(service.role()).toBe('employee');
    });
  });

  describe('Clear Context', () => {
    it('should clear current context but keep memberships', () => {
      service.setMemberships([mockMembership1]);
      service.selectContext(mockMembership1, false);
      
      service.clearContext();
      
      expect(service.currentContext()).toBeNull();
      expect(service.memberships().length).toBe(1);
      expect(sessionStorage.getItem(CONTEXT_STORAGE_KEYS.CURRENT_CONTEXT)).toBeNull();
    });

    it('should clear all data on clearAll', () => {
      service.setMemberships([mockMembership1]);
      service.selectContext(mockMembership1, false);
      
      service.clearAll();
      
      expect(service.currentContext()).toBeNull();
      expect(service.memberships().length).toBe(0);
      expect(sessionStorage.getItem(CONTEXT_STORAGE_KEYS.MEMBERSHIPS)).toBeNull();
    });
  });
});
