import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { DashboardHrRepository } from './dashboard-hr.repository';
import { DashboardHrPayload, DashboardHrQuery, DashboardHrData } from '../state/dashboard-hr.models';
import { DashboardHrRawData } from './dashboard-hr-raw.models';

const MOCK_DASHBOARD_DATA: DashboardHrData = {
  appTitle: 'PayZen HR - Dashboards Demo',
  appSubtitle: 'Spec UI pour Ayoub - Donnees en dur - Entreprise test : TECHCO SARL - 87 employes - Casablanca',
  vueGlobale: {
    meta: {
      title: 'Vue Globale RH',
      badge: 'HOME',
      subtitle: 'KPIs instantanés - Snapshot du mois en cours - Janvier 2025',
      icon: 'pi pi-home'
    },
    kpis: [
      { label: 'Effectif total', value: '87', subLabel: '+3 ce mois', trend: { value: '+3', direction: 'up' } },
      { label: 'Masse salariale', value: '412 K', subLabel: 'MAD / mois', trend: { value: '+2.4%', direction: 'up' } },
      { label: 'Turnover (12M)', value: '8.3%', subLabel: 'vs N-1', trend: { value: '-1.1%', direction: 'down' } },
      { label: 'Parité F/H', value: '41 / 59', subLabel: '% Femmes / Hommes', trend: { value: 'stable', direction: 'flat' } }
    ],
    effectifEvolution: {
      labels: ['Aout', 'Sep', 'Oct', 'Nov', 'Dec', 'Jan'],
      values: [82, 83, 84, 84, 85, 87],
      datasetLabel: 'Effectif',
      color: '#2563eb',
      highlightLast: false,
      ySuggestedMax: 90,
      yTickStep: 10
    },
    repartitionDepartement: {
      centerLabel: '87',
      slices: [
        { label: 'Tech & Dev', value: 27, color: '#2563eb' },
        { label: 'Commercial', value: 20, color: '#22c55e' },
        { label: 'Finance', value: 17, color: '#f97316' },
        { label: 'Support', value: 12, color: '#6366f1' },
        { label: 'Autres', value: 11, color: '#d4d4d8' }
      ]
    },
    footerNav: { current: 2, total: 93 }
  },
  mouvementsRh: {
    meta: {
      title: 'Mouvements RH',
      badge: 'Entrées / sorties',
      subtitle: 'Historique des entrées et sorties — janvier 2025 — 5 entrées — 2 sorties',
      icon: 'pi pi-refresh'
    },
    summary: [
      { label: 'Entrees ce mois', value: '+5', subLabel: 'CDI: 4 - CDD: 1', accent: 'success' },
      { label: 'Sorties ce mois', value: '-2', subLabel: 'Demission: 1 - Fin CDD: 1', accent: 'danger' },
      { label: 'Solde net', value: '+3', subLabel: 'Taux de retention: 97.7%', accent: 'success' }
    ],
    history: [
      { employe: 'Salma Benali', departement: 'Tech & Dev', poste: 'Dev Frontend', type: 'CDI', date: '02/01/2025', motifNote: 'Recrutement externe', mouvement: { label: 'Entree', severity: 'success' } },
      { employe: 'Youssef Amrani', departement: 'Commercial', poste: 'Account Manager', type: 'CDI', date: '06/01/2025', motifNote: 'Recrutement externe', mouvement: { label: 'Entree', severity: 'success' } },
      { employe: 'Rim Tazi', departement: 'Finance', poste: 'Controleur de gestion', type: 'CDI', date: '08/01/2025', motifNote: 'Mobilite interne', mouvement: { label: 'Entree', severity: 'success' } },
      { employe: 'Hamza El Idrissi', departement: 'Support', poste: 'Tech Support N2', type: 'CDD', date: '13/01/2025', motifNote: 'Remplacement conge maternite', mouvement: { label: 'Entree', severity: 'success' } },
      { employe: 'Nadia Chraibi', departement: 'Tech & Dev', poste: 'QA Engineer', type: 'CDI', date: '20/01/2025', motifNote: 'Recrutement externe', mouvement: { label: 'Entree', severity: 'success' } },
      { employe: 'Karim Zoaoui', departement: 'Commercial', poste: 'Sales Rep', type: 'CDI', date: '15/01/2025', motifNote: 'Demission - depart volontaire', mouvement: { label: 'Sortie', severity: 'danger' } },
      { employe: 'Sara Hajji', departement: 'Support', poste: 'Customer Success', type: 'CDD', date: '31/01/2025', motifNote: 'Fin de contrat CDD', mouvement: { label: 'Sortie', severity: 'danger' } }
    ]
  },
  masseSalariale: {
    meta: {
      title: 'Masse salariale',
      badge: 'Paie',
      subtitle: 'Analyse des coûts salariaux — charges patronales incluses — janvier 2025',
      icon: 'pi pi-wallet'
    },
    kpis: [
      { label: 'Brut total', value: '412 K', subLabel: 'MAD' },
      { label: 'Net total versé', value: '318 K', subLabel: 'MAD après IR / CNSS' },
      { label: 'Charges patronales', value: '89 K', subLabel: 'CNSS + AMO employeur' },
      { label: 'Coût total employeur', value: '501 K', subLabel: 'MAD / mois' }
    ],
    masseBrute12Mois: {
      labels: ['Fev', 'Mar', 'Avr', 'Mai', 'Juin', 'Juil', 'Aou', 'Sep', 'Oct', 'Nov', 'Dec', 'Jan'],
      values: [368, 371, 370, 374, 380, 388, 383, 390, 397, 402, 408, 412],
      datasetLabel: 'Masse salariale brute',
      color: '#14b8a6',
      highlightLast: false,
      suffix: 'K MAD',
      ySuggestedMax: 420,
      yTickStep: 20
    },
    repartitionDepartement: [
      { label: 'Tech & Dev (27 pers.)', rightLabel: '148 K MAD - 36%', percent: 36, color: '#2563eb' },
      { label: 'Commercial (20 pers.)', rightLabel: '107 K MAD - 26%', percent: 26, color: '#22c55e' },
      { label: 'Finance (17 pers.)', rightLabel: '91 K MAD - 22%', percent: 22, color: '#f97316' },
      { label: 'Support (12 pers.)', rightLabel: '45 K MAD - 11%', percent: 11, color: '#6366f1' },
      { label: 'Autres (11 pers.)', rightLabel: '21 K MAD - 5%', percent: 5, color: '#93c5fd' }
    ]
  },
  pariteDiversite: {
    meta: {
      title: 'Parité & diversité',
      badge: 'Équité',
      subtitle: 'Indicateurs d’équilibre femmes / hommes — janvier 2025',
      icon: 'pi pi-balance-scale'
    },
    kpis: [
      { label: 'Effectif femmes', value: '36', subLabel: '41,4 % de l’effectif', accent: 'purple' },
      { label: 'Effectif hommes', value: '51', subLabel: '58,6 % de l’effectif', accent: 'blue' },
      { label: 'Écart salarial moyen', value: '-4,2 %', subLabel: 'Femmes vs hommes — même poste', accent: 'danger' }
    ],
    pariteDepartement: [
      { label: 'Tech & Dev', rightLabel: '7F / 20H', percent: 26, color: '#7c3aed' },
      { label: 'Commercial', rightLabel: '9F / 11H', percent: 45, color: '#7c3aed' },
      { label: 'Finance', rightLabel: '10F / 7H', percent: 58, color: '#7c3aed' },
      { label: 'Support', rightLabel: '8F / 4H', percent: 67, color: '#7c3aed' },
      { label: 'Autres', rightLabel: '2F / 9H', percent: 18, color: '#7c3aed' }
    ],
    pariteNiveauHierarchique: [
      { label: 'Direction (5)', rightLabel: '20% F', percent: 20, color: '#ef4444' },
      { label: 'Managers (12)', rightLabel: '33% F', percent: 33, color: '#f97316' },
      { label: 'Cadres (38)', rightLabel: '42% F', percent: 42, color: '#22c55e' },
      { label: 'Employés (32)', rightLabel: '53% F', percent: 53, color: '#14b8a6' }
    ]
  },
  conformiteSociale: {
    meta: {
      title: 'Conformité sociale',
      badge: 'CNSS — AMO — IR',
      subtitle: 'État des déclarations et cotisations — janvier 2025',
      icon: 'pi pi-check-circle'
    },
    kpis: [
      { label: 'CNSS salariale', value: '27,4 K', subLabel: 'MAD — taux 4,29 % — calculé' },
      { label: 'CNSS patronale', value: '58,1 K', subLabel: 'MAD — taux 21,09 % — calculé' },
      { label: 'AMO (salariale)', value: '8,2 K', subLabel: 'MAD — taux 2,26 % — calculé' },
      { label: 'IR retenu à la source', value: '66,4 K', subLabel: 'MAD — barème progressif — calculé' }
    ],
    declarations: [
      { declaration: 'Bordereau CNSS - Jan 2025', montantMad: '85 500', echeance: '28/02/2025', statut: { label: 'En attente', severity: 'warn' }, reference: 'BV-2025-001' },
      { declaration: 'AMO employeur - Jan 2025', montantMad: '12 340', echeance: '28/02/2025', statut: { label: 'En attente', severity: 'warn' }, reference: 'AMO-2025-001' },
      { declaration: 'Versement IR DGI - Jan 2025', montantMad: '66 400', echeance: '31/01/2025', statut: { label: 'Soumis', severity: 'success' }, reference: 'DGI-IR-J25-087' },
      { declaration: 'Bordereau CNSS - Dec 2024', montantMad: '83 200', echeance: '31/01/2025', statut: { label: 'Soumis', severity: 'success' }, reference: 'BV-2024-012' }
    ]
  }
};

@Injectable()
export class DashboardHrMockRepository implements DashboardHrRepository {
  getDashboardData(_query: DashboardHrQuery): Observable<DashboardHrPayload> {
    return of({
      data: MOCK_DASHBOARD_DATA,
      meta: {
        source: 'mock',
        loadedAtIso: new Date().toISOString(),
        warnings: []
      }
    });
  }

  getDashboardRawData(_query: DashboardHrQuery): Observable<DashboardHrRawData> {
    return of({
      meta: {
        companyId: 1,
        companyName: 'TECHCO SARL',
        month: '2025-01',
        generatedAt: new Date().toISOString()
      },
      employees: [
        { id: 1, firstName: 'Salma', lastName: 'Benali', department: 'Tech & Dev', statusCode: 'Active', genderCode: 'F' },
        { id: 2, firstName: 'Youssef', lastName: 'Amrani', department: 'Commercial', statusCode: 'Active', genderCode: 'M' },
        { id: 3, firstName: 'Rim', lastName: 'Tazi', department: 'Finance', statusCode: 'Active', genderCode: 'F' },
        { id: 4, firstName: 'Hamza', lastName: 'El Idrissi', department: 'Support', statusCode: 'Active', genderCode: 'M' },
        { id: 5, firstName: 'Nadia', lastName: 'Chraibi', department: 'Tech & Dev', statusCode: 'Active', genderCode: 'F' },
        { id: 6, firstName: 'Karim', lastName: 'Zoaoui', department: 'Commercial', statusCode: 'Inactive', genderCode: 'M' },
        { id: 7, firstName: 'Sara', lastName: 'Hajji', department: 'Support', statusCode: 'Inactive', genderCode: 'F' }
      ],
      contracts: [
        { employeeId: 1, startDate: '2025-01-02', endDate: null, position: 'Dev Frontend', contractType: 'CDI' },
        { employeeId: 2, startDate: '2025-01-06', endDate: null, position: 'Account Manager', contractType: 'CDI' },
        { employeeId: 3, startDate: '2025-01-08', endDate: null, position: 'Controleur de gestion', contractType: 'CDI' },
        { employeeId: 4, startDate: '2025-01-13', endDate: null, position: 'Tech Support N2', contractType: 'CDD' },
        { employeeId: 5, startDate: '2025-01-20', endDate: null, position: 'QA Engineer', contractType: 'CDI' },
        { employeeId: 6, startDate: '2024-06-01', endDate: '2025-01-15', position: 'Sales Rep', contractType: 'CDI' },
        { employeeId: 7, startDate: '2024-07-01', endDate: '2025-01-31', position: 'Customer Success', contractType: 'CDD' }
      ],
      salaries: [
        { employeeId: 1, baseSalary: 42000, effectiveDate: '2025-01-01', endDate: null },
        { employeeId: 2, baseSalary: 38000, effectiveDate: '2025-01-01', endDate: null },
        { employeeId: 3, baseSalary: 36000, effectiveDate: '2025-01-01', endDate: null },
        { employeeId: 4, baseSalary: 22000, effectiveDate: '2025-01-01', endDate: null },
        { employeeId: 5, baseSalary: 34000, effectiveDate: '2025-01-01', endDate: null },
        { employeeId: 6, baseSalary: 32000, effectiveDate: '2024-06-01', endDate: '2025-01-15' },
        { employeeId: 7, baseSalary: 21000, effectiveDate: '2024-07-01', endDate: '2025-01-31' }
      ]
    });
  }
}
