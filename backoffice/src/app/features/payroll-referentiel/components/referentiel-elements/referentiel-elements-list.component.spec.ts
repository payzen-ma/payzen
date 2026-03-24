import { TestBed, ComponentFixture } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ReferentielElementsListComponent } from './referentiel-elements-list.component';
import { PayrollReferentielService } from '../../../../services/payroll-referentiel/payroll-referentiel.service';
import { LookupCacheService } from '../../../../services/payroll-referentiel/lookup-cache.service';
import { ReferentielElementListDto, ElementStatus } from '../../../../models/payroll-referentiel/referentiel-element.model';
import { PaymentFrequency } from '../../../../models/payroll-referentiel/lookup.models';

const mockCategories = [
  { id: 1, name: 'Indemnités professionnelles', isActive: true },
  { id: 2, name: 'Primes spéciales', isActive: true }
];

const mockElements: ReferentielElementListDto[] = [
  {
    id: 1, code: 'transport', name: 'Indemnité de transport',
    categoryName: 'Indemnités professionnelles',
    defaultFrequency: PaymentFrequency.MONTHLY,
    status: ElementStatus.ACTIVE, isActive: true,
    hasConvergence: true, ruleCount: 2,
    hasCnssRule: true, hasDgiRule: true
  },
  {
    id: 2, code: 'panier', name: 'Indemnité de panier',
    categoryName: 'Indemnités professionnelles',
    defaultFrequency: PaymentFrequency.MONTHLY,
    status: ElementStatus.ACTIVE, isActive: true,
    hasConvergence: false, ruleCount: 1,
    hasCnssRule: true, hasDgiRule: false
  },
  {
    id: 3, code: 'representation', name: 'Indemnité de représentation',
    categoryName: 'Primes spéciales',
    defaultFrequency: PaymentFrequency.MONTHLY,
    status: ElementStatus.DRAFT, isActive: true,
    hasConvergence: false, ruleCount: 0,
    hasCnssRule: false, hasDgiRule: false
  }
];

describe('ReferentielElementsListComponent', () => {
  let component: ReferentielElementsListComponent;
  let fixture: ComponentFixture<ReferentielElementsListComponent>;
  let payrollStub: jasmine.SpyObj<PayrollReferentielService>;
  let lookupStub: jasmine.SpyObj<LookupCacheService>;

  beforeEach(() => {
    payrollStub = jasmine.createSpyObj('PayrollReferentielService', ['getAllReferentielElements']);
    lookupStub = jasmine.createSpyObj('LookupCacheService', ['getCategories']);

    payrollStub.getAllReferentielElements.and.returnValue(of(mockElements));
    lookupStub.getCategories.and.returnValue(of(mockCategories));

    TestBed.configureTestingModule({
      imports: [ReferentielElementsListComponent],
      providers: [
        { provide: PayrollReferentielService, useValue: payrollStub },
        { provide: LookupCacheService, useValue: lookupStub }
      ]
    });

    fixture = TestBed.createComponent(ReferentielElementsListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ================================================================
  // Initialization
  // ================================================================

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load elements on init', () => {
    expect(payrollStub.getAllReferentielElements).toHaveBeenCalled();
    expect(component.allElements.length).toBe(3);
    expect(component.filteredElements.length).toBe(3);
  });

  it('should load categories on init', () => {
    expect(lookupStub.getCategories).toHaveBeenCalled();
    expect(component.categories.length).toBe(2);
  });

  it('should emit loaded event with count', () => {
    spyOn(component.loaded, 'emit');
    component.loadElements();
    expect(component.loaded.emit).toHaveBeenCalledWith(3);
  });

  it('should set loading to false after load', () => {
    expect(component.loading).toBeFalse();
  });

  it('should handle load error gracefully', () => {
    payrollStub.getAllReferentielElements.and.returnValue(throwError(() => new Error('Network error')));
    component.loadElements();
    expect(component.loading).toBeFalse();
  });

  // ================================================================
  // Filtering
  // ================================================================

  it('should filter by search term on name', () => {
    component.searchTerm = 'transport';
    component.applyFilters();
    expect(component.filteredElements.length).toBe(1);
    expect(component.filteredElements[0].name).toBe('Indemnité de transport');
  });

  it('should filter by search term on code', () => {
    component.searchTerm = 'panier';
    component.applyFilters();
    expect(component.filteredElements.length).toBe(1);
    expect(component.filteredElements[0].code).toBe('panier');
  });

  it('should filter by convergence status', () => {
    component.filterConvergence = 'convergence';
    component.applyFilters();
    expect(component.filteredElements.length).toBe(1);
    expect(component.filteredElements[0].hasConvergence).toBeTrue();
  });

  it('should filter by divergence status', () => {
    component.filterConvergence = 'divergence';
    component.applyFilters();
    expect(component.filteredElements.length).toBe(2);
    expect(component.filteredElements.every(e => !e.hasConvergence)).toBeTrue();
  });

  it('should show all elements when filters are empty', () => {
    component.searchTerm = '';
    component.filterConvergence = '';
    component.applyFilters();
    expect(component.filteredElements.length).toBe(3);
  });

  it('should combine search and convergence filters', () => {
    component.searchTerm = 'indemnité';
    component.filterConvergence = 'divergence';
    component.applyFilters();
    expect(component.filteredElements.length).toBe(2);
  });

  it('should sort elements alphabetically by name', () => {
    component.applyFilters();
    const names = component.filteredElements.map(e => e.name);
    const sorted = [...names].sort((a, b) => a.localeCompare(b));
    expect(names).toEqual(sorted);
  });

  // ================================================================
  // Stats
  // ================================================================

  it('should compute convergence count', () => {
    expect(component.convergenceCount).toBe(1);
  });

  it('should compute divergence count', () => {
    expect(component.divergenceCount).toBe(2);
  });

  // ================================================================
  // Refresh trigger
  // ================================================================

  it('should reload elements when refreshTrigger changes', () => {
    payrollStub.getAllReferentielElements.calls.reset();
    component.refreshTrigger = 1;
    component.ngOnChanges({
      refreshTrigger: {
        currentValue: 1, previousValue: 0,
        firstChange: false, isFirstChange: () => false
      }
    });
    expect(payrollStub.getAllReferentielElements).toHaveBeenCalled();
  });

  it('should not reload when refreshTrigger is 0', () => {
    payrollStub.getAllReferentielElements.calls.reset();
    component.refreshTrigger = 0;
    component.ngOnChanges({
      refreshTrigger: {
        currentValue: 0, previousValue: 0,
        firstChange: false, isFirstChange: () => false
      }
    });
    expect(payrollStub.getAllReferentielElements).not.toHaveBeenCalled();
  });

  // ================================================================
  // Event emissions
  // ================================================================

  it('should emit add event', () => {
    spyOn(component.add, 'emit');
    component.onAdd();
    expect(component.add.emit).toHaveBeenCalled();
  });

  it('should emit edit event with element', () => {
    spyOn(component.edit, 'emit');
    component.onEdit(mockElements[0]);
    expect(component.edit.emit).toHaveBeenCalledWith(mockElements[0]);
  });

  it('should emit delete event with element', () => {
    spyOn(component.delete, 'emit');
    component.onDelete(mockElements[0]);
    expect(component.delete.emit).toHaveBeenCalledWith(mockElements[0]);
  });

  it('should emit viewRules event with element', () => {
    spyOn(component.viewRules, 'emit');
    component.onViewRules(mockElements[0]);
    expect(component.viewRules.emit).toHaveBeenCalledWith(mockElements[0]);
  });

  it('should emit addRule event with element', () => {
    spyOn(component.addRule, 'emit');
    component.onAddRule(mockElements[0]);
    expect(component.addRule.emit).toHaveBeenCalledWith(mockElements[0]);
  });

  // ================================================================
  // Helper methods
  // ================================================================

  it('should return correct frequency label', () => {
    expect(component.getFrequencyLabel(PaymentFrequency.MONTHLY)).toBe('Mensuel');
    expect(component.getFrequencyLabel(PaymentFrequency.ANNUAL)).toBe('Annuel');
  });

  it('should return correct convergence text', () => {
    expect(component.getConvergenceText(true)).toBe('Convergence');
    expect(component.getConvergenceText(false)).toBe('Divergence');
  });

  it('should return status badge for ACTIVE', () => {
    const badge = component.getStatusBadge(ElementStatus.ACTIVE);
    expect(badge.text).toBe('Actif');
  });

  it('should return status badge for DRAFT', () => {
    const badge = component.getStatusBadge(ElementStatus.DRAFT);
    expect(badge.text).toBe('Brouillon');
  });

  it('should track elements by id', () => {
    expect(component.trackById(0, mockElements[0])).toBe(1);
    expect(component.trackById(1, mockElements[1])).toBe(2);
  });
});
