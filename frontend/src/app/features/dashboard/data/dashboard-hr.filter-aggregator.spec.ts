import { aggregateDashboardFromRaw } from './dashboard-hr.filter-aggregator';
import { DashboardFilterState } from '../state/dashboard-hr.models';
import { DashboardHrRawData } from './dashboard-hr-raw.models';

describe('aggregateDashboardFromRaw month filtering', () => {
  const rawData: DashboardHrRawData = {
    meta: {
      companyId: 1,
      companyName: 'Atlach Tech SARL',
      month: '2026-02',
      generatedAt: '2026-02-27T10:00:00Z'
    },
    employees: [
      {
        id: 1,
        firstName: 'Aya',
        lastName: 'Rahimi',
        department: 'Finance',
        statusCode: 'Active',
        genderCode: 'F'
      },
      {
        id: 2,
        firstName: 'Yassine',
        lastName: 'Karim',
        department: 'Finance',
        statusCode: 'Active',
        genderCode: 'M'
      }
    ],
    contracts: [
      {
        employeeId: 1,
        startDate: '2026-01-05',
        endDate: null,
        position: 'Analyst',
        contractType: 'CDI'
      },
      {
        employeeId: 2,
        startDate: '2026-02-10',
        endDate: null,
        position: 'Senior Analyst',
        contractType: 'CDI'
      }
    ],
    salaries: [
      {
        employeeId: 1,
        baseSalary: 10000,
        effectiveDate: '2026-01-01',
        endDate: null
      },
      {
        employeeId: 2,
        baseSalary: 20000,
        effectiveDate: '2026-02-01',
        endDate: null
      }
    ]
  };

  const baseFilters: Omit<DashboardFilterState, 'month'> = {
    departments: [],
    contractTypes: [],
    parity: ['F', 'H'],
    compareMonth: null
  };

  it('returns January-only numbers when month is 2026-01', () => {
    const jan = aggregateDashboardFromRaw(rawData, {
      ...baseFilters,
      month: '2026-01'
    });

    expect(jan.vueGlobale.kpis[0].value).toBe('1');
    expect(jan.masseSalariale.kpis[0].value).toBe('10 K');
    expect(jan.mouvementsRh.summary[0].value).toBe('+1');
    expect(jan.mouvementsRh.history.length).toBe(1);
    expect(jan.mouvementsRh.history[0].employe).toContain('Aya');
  });

  it('returns February numbers when month is 2026-02', () => {
    const feb = aggregateDashboardFromRaw(rawData, {
      ...baseFilters,
      month: '2026-02'
    });

    expect(feb.vueGlobale.kpis[0].value).toBe('2');
    expect(feb.masseSalariale.kpis[0].value).toBe('30 K');
    expect(feb.mouvementsRh.summary[0].value).toBe('+1');
    expect(feb.mouvementsRh.history.length).toBe(1);
    expect(feb.mouvementsRh.history[0].employe).toContain('Yassine');
  });
});

