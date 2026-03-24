import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ElementRuleWizardComponent } from './element-rule-wizard.component';
import { LookupCacheService } from '../../../../services/payroll-referentiel/lookup-cache.service';
import { PayrollReferentielService } from '../../../../services/payroll-referentiel/payroll-referentiel.service';

describe('ElementRuleWizardComponent', () => {
  it('falls back to CNSS/IR when authorities are empty', () => {
    const lookupStub = {
      getAuthorities: () => of([]),
      getEligibilityCriteria: () => of([])
    };
    const payrollStub = {
      getAllLegalParameters: () => of([])
    };

    TestBed.configureTestingModule({
      imports: [ElementRuleWizardComponent],
      providers: [
        { provide: LookupCacheService, useValue: lookupStub },
        { provide: PayrollReferentielService, useValue: payrollStub }
      ]
    });

    const fixture = TestBed.createComponent(ElementRuleWizardComponent);
    fixture.detectChanges();

    const component = fixture.componentInstance;
    expect(component.authorities.length).toBe(2);
    expect(component.authorities.map(a => a.code)).toEqual(['CNSS', 'IR']);
  });
});
