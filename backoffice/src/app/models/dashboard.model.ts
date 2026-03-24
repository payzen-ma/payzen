// Models for Dashboard data used by the frontend
// Generated: 2025-12-24

export interface DashboardSummary {
  totalCompanies: number;
  totalEmployees: number;
  accountingFirmsCount: number; // nombre de "cabinet comptable"
  avgEmployeesPerCompany: number; // moyenne salariés par entreprise
  // repartition des salariés par tranche de taille d'entreprise
  employeeDistribution: EmployeeDistributionEntry[];
  // Optionnel : petites listes utiles pour le tableau / cards
  recentCompanies?: RecentCompany[];
  // ISO timestamp décrivant quand le résumé a été calculé
  asOf?: string;
}

export interface EmployeeDistributionEntry {
  // libellé de la tranche, ex: "1-10", "11-50", "51-200", ">200"
  bucket: string;
  companiesCount: number; // nombre d'entreprises dans cette tranche
  employeesCount: number; // nombre total d'employés dans cette tranche
  percentage?: number; // pourcentage d'employés total (0-100)
}

export interface RecentCompany {
  id: number | string;
  companyName: string;
  countryName?: string;
  cityName?: string;
  employeesCount?: number;
  createdAt?: string; // ISO date
  status?: string;
}

// Example JSON response for GET /api/dashboard/summary
/*
{
  "totalCompanies": 1240,
  "totalEmployees": 18350,
  "accountingFirmsCount": 230,
  "avgEmployeesPerCompany": 14.8,
  "employeeDistribution": [
    { "bucket": "1-10", "companiesCount": 800, "employeesCount": 3200, "percentage": 17.4 },
    { "bucket": "11-50", "companiesCount": 320, "employeesCount": 6400, "percentage": 34.9 },
    { "bucket": "51-200", "companiesCount": 100, "employeesCount": 7600, "percentage": 41.4 },
    { "bucket": ">200", "companiesCount": 20, "employeesCount": 1150, "percentage": 6.3 }
  ],
  "recentCompanies": [
    { "id": 9999, "companyName": "Acme SARL", "countryName": "Maroc", "cityName": "Casablanca", "employeesCount": 12, "createdAt": "2025-12-20T14:22:00Z" },
    { "id": 10000, "companyName": "Beta Solutions", "countryName": "Maroc", "cityName": "Rabat", "employeesCount": 45, "createdAt": "2025-12-19T09:10:00Z" }
  ],
  "asOf": "2025-12-24T12:00:00Z"
}
*/

// Recommendation: backend should return a paginated `recentCompanies` when large
// and expose separate endpoints for heavy metrics (time series) or events.
