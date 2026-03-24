import { TestBed, ComponentFixture } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { ReferentielElementModalComponent } from './referentiel-element-modal.component';
import { LookupCacheService } from '../../../../services/payroll-referentiel/lookup-cache.service';
import { PaymentFrequency } from '../../../../models/payroll-referentiel/lookup.models';
import { ElementStatus } from '../../../../models/payroll-referentiel/referentiel-element.model';

const mockCategories = [
  { id: 1, name: 'Indemnités professionnelles', isActive: true },
  { id: 2, name: 'Primes spéciales', isActive: true }
];

describe('ReferentielElementModalComponent', () => {
  let component: ReferentielElementModalComponent;
  let fixture: ComponentFixture<ReferentielElementModalComponent>;
  let lookupStub: jasmine.SpyObj<LookupCacheService>;

  beforeEach(() => {
    lookupStub = jasmine.createSpyObj('LookupCacheService', ['getCategories', 'createCategory']);
    lookupStub.getCategories.and.returnValue(of(mockCategories));

    TestBed.configureTestingModule({
      imports: [ReferentielElementModalComponent],
      providers: [
        { provide: LookupCacheService, useValue: lookupStub }
      ]
    });

    fixture = TestBed.createComponent(ReferentielElementModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  // ================================================================
  // Initialization
  // ================================================================

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load categories on init', () => {
    expect(lookupStub.getCategories).toHaveBeenCalled();
    expect(component.categories.length).toBe(2);
  });

  it('should default to create mode', () => {
    expect(component.mode).toBe('create');
  });

  it('should default frequency to MONTHLY', () => {
    expect(component.form.defaultFrequency).toBe(PaymentFrequency.MONTHLY);
  });

  // ================================================================
  // Modal title
  // ================================================================

  it('should show create title in create mode', () => {
    component.mode = 'create';
    expect(component.modalTitle).toBe('Nouvel élément de référentiel');
  });

  it('should show edit title in edit mode', () => {
    component.mode = 'edit';
    expect(component.modalTitle).toContain('Modifier');
  });

  // ================================================================
  // Form initialization
  // ================================================================

  it('should reset form when opening in create mode', () => {
    component.mode = 'create';
    component.visible = true;
    component.ngOnChanges({
      visible: {
        currentValue: true, previousValue: false,
        firstChange: false, isFirstChange: () => false
      }
    });

    expect(component.form.name).toBe('');
    expect(component.form.categoryId).toBeNull();
    expect(component.form.code).toBe('');
    expect(component.form.description).toBe('');
    expect(component.form.isActive).toBeTrue();
  });

  it('should populate form when opening in edit mode', () => {
    component.mode = 'edit';
    component.element = {
      id: 1, code: 'transport', name: 'Transport',
      categoryId: 1, categoryName: 'Indemnités professionnelles',
      description: 'Test description',
      defaultFrequency: PaymentFrequency.MONTHLY,
      status: ElementStatus.ACTIVE, isActive: true,
      hasConvergence: true, rules: []
    };
    component.visible = true;
    component.ngOnChanges({
      visible: {
        currentValue: true, previousValue: false,
        firstChange: false, isFirstChange: () => false
      }
    });

    expect(component.form.name).toBe('Transport');
    expect(component.form.code).toBe('transport');
    expect(component.form.categoryId).toBe(1);
    expect(component.form.description).toBe('Test description');
    expect(component.form.isActive).toBeTrue();
  });

  it('should find category by name when categoryId is missing', () => {
    component.mode = 'edit';
    component.element = {
      id: 1, name: 'Transport',
      categoryName: 'Indemnités professionnelles',
      defaultFrequency: PaymentFrequency.MONTHLY,
      status: ElementStatus.ACTIVE, isActive: true,
      hasConvergence: true, ruleCount: 2,
      hasCnssRule: true, hasDgiRule: true
    };
    component.visible = true;
    component.ngOnChanges({
      visible: {
        currentValue: true, previousValue: false,
        firstChange: false, isFirstChange: () => false
      }
    });

    expect(component.form.categoryId).toBe(1);
  });

  // ================================================================
  // Validation
  // ================================================================

  it('should be invalid when name is empty', () => {
    component.form.name = '';
    component.form.categoryId = 1;
    expect(component.isValid()).toBeFalse();
  });

  it('should be invalid when name is whitespace', () => {
    component.form.name = '   ';
    component.form.categoryId = 1;
    expect(component.isValid()).toBeFalse();
  });

  it('should be invalid when category is not selected', () => {
    component.form.name = 'Test';
    component.form.categoryId = null;
    expect(component.isValid()).toBeFalse();
  });

  it('should be valid with name and category', () => {
    component.form.name = 'Test Element';
    component.form.categoryId = 1;
    expect(component.isValid()).toBeTrue();
  });

  // ================================================================
  // Submit
  // ================================================================

  it('should emit CreateDto in create mode', () => {
    spyOn(component.save, 'emit');
    component.mode = 'create';
    component.form.name = 'New Element';
    component.form.categoryId = 1;
    component.form.code = 'new_element';
    component.form.defaultFrequency = PaymentFrequency.MONTHLY;

    component.onSubmit();

    expect(component.save.emit).toHaveBeenCalledWith(jasmine.objectContaining({
      name: 'New Element',
      categoryId: 1,
      code: 'new_element',
      defaultFrequency: PaymentFrequency.MONTHLY
    }));
  });

  it('should emit UpdateDto with id in edit mode', () => {
    spyOn(component.save, 'emit');
    component.mode = 'edit';
    component.element = {
      id: 5, name: 'Old', categoryName: 'Cat',
      defaultFrequency: PaymentFrequency.MONTHLY,
      status: ElementStatus.ACTIVE, isActive: true,
      hasConvergence: false, ruleCount: 0,
      hasCnssRule: false, hasDgiRule: false
    };
    component.form.name = 'Updated Name';
    component.form.categoryId = 2;
    component.form.isActive = false;
    component.form.defaultFrequency = PaymentFrequency.ANNUAL;

    component.onSubmit();

    const emitted = (component.save.emit as jasmine.Spy).calls.mostRecent().args[0] as any;
    expect(emitted.id).toBe(5);
    expect(emitted.dto.name).toBe('Updated Name');
    expect(emitted.dto.categoryId).toBe(2);
    expect(emitted.dto.isActive).toBeFalse();
  });

  it('should not submit when invalid', () => {
    spyOn(component.save, 'emit');
    component.form.name = '';
    component.form.categoryId = null;
    component.onSubmit();
    expect(component.save.emit).not.toHaveBeenCalled();
  });

  it('should trim code to undefined when empty', () => {
    spyOn(component.save, 'emit');
    component.mode = 'create';
    component.form.name = 'Test';
    component.form.categoryId = 1;
    component.form.code = '   ';

    component.onSubmit();

    const emitted = (component.save.emit as jasmine.Spy).calls.mostRecent().args[0] as any;
    expect(emitted.code).toBeUndefined();
  });

  // ================================================================
  // Cancel / visibility
  // ================================================================

  it('should emit visibleChange false on cancel', () => {
    spyOn(component.visibleChange, 'emit');
    component.onCancel();
    expect(component.visibleChange.emit).toHaveBeenCalledWith(false);
  });

  it('should forward visibility changes', () => {
    spyOn(component.visibleChange, 'emit');
    component.onVisibleChange(false);
    expect(component.visibleChange.emit).toHaveBeenCalledWith(false);
  });

  // ================================================================
  // Category creation
  // ================================================================

  it('should show new category input when -1 is selected', () => {
    component.onCategoryChange(-1);
    expect(component.showNewCategoryInput).toBeTrue();
    expect(component.form.categoryId).toBeNull();
  });

  it('should not show new category input for valid category', () => {
    component.showNewCategoryInput = false;
    component.onCategoryChange(1);
    expect(component.showNewCategoryInput).toBeFalse();
  });

  it('should create category and select it', () => {
    const newCat = { id: 99, name: 'New Category', isActive: true };
    lookupStub.createCategory.and.returnValue(of(newCat));

    component.showNewCategoryInput = true;
    component.newCategoryName = 'New Category';
    component.onCreateCategory();

    expect(lookupStub.createCategory).toHaveBeenCalledWith('New Category');
    expect(component.form.categoryId).toBe(99);
    expect(component.showNewCategoryInput).toBeFalse();
    expect(component.categories.length).toBe(3);
  });

  it('should handle category creation error', () => {
    lookupStub.createCategory.and.returnValue(throwError(() => ({ error: { Message: 'Duplicate' } })));

    component.showNewCategoryInput = true;
    component.newCategoryName = 'Existing Category';
    component.onCreateCategory();

    expect(component.newCategoryError).toBe('Duplicate');
    expect(component.creatingCategory).toBeFalse();
  });

  it('should not create category with empty name', () => {
    component.newCategoryName = '   ';
    component.onCreateCategory();
    expect(lookupStub.createCategory).not.toHaveBeenCalled();
  });

  it('should cancel new category input', () => {
    component.showNewCategoryInput = true;
    component.newCategoryName = 'Something';
    component.onCancelNewCategory();

    expect(component.showNewCategoryInput).toBeFalse();
    expect(component.newCategoryName).toBe('');
    expect(component.form.categoryId).toBeNull();
  });

  // ================================================================
  // Error handling
  // ================================================================

  it('should set and clear error', () => {
    component.setError('Test error');
    expect(component.error).toBe('Test error');

    // Error clears when form is re-initialized
    component.mode = 'create';
    component.visible = true;
    component.ngOnChanges({
      visible: {
        currentValue: true, previousValue: false,
        firstChange: false, isFirstChange: () => false
      }
    });
    expect(component.error).toBe('');
  });
});
