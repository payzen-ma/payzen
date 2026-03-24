import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Dashboard } from './dashboard';
import { DashboardHrRepository } from './data/dashboard-hr.repository';
import { DashboardHrMockRepository } from './data/dashboard-hr.mock.repository';
import { DashboardHrStore } from './state/dashboard-hr.store';

describe('Dashboard', () => {
  let component: Dashboard;
  let fixture: ComponentFixture<Dashboard>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Dashboard]
    })
    .overrideComponent(Dashboard, {
      set: {
        providers: [
          DashboardHrStore,
          DashboardHrMockRepository,
          { provide: DashboardHrRepository, useClass: DashboardHrMockRepository }
        ]
      }
    })
    .compileComponents();

    fixture = TestBed.createComponent(Dashboard);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
