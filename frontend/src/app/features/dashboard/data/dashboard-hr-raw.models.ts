export interface DashboardHrRawMeta {
  companyId: number;
  companyName: string;
  month: string;
  generatedAt: string;
}

export interface DashboardHrRawEmployee {
  id: number;
  firstName: string;
  lastName: string;
  department: string;
  statusCode: string;
  genderCode: string;
}

export interface DashboardHrRawContract {
  employeeId: number;
  startDate: string;
  endDate: string | null;
  position: string;
  contractType: string;
}

export interface DashboardHrRawSalary {
  employeeId: number;
  baseSalary: number;
  effectiveDate: string;
  endDate: string | null;
}

export interface DashboardHrRawData {
  meta: DashboardHrRawMeta;
  employees: DashboardHrRawEmployee[];
  contracts: DashboardHrRawContract[];
  salaries: DashboardHrRawSalary[];
}

export interface DashboardHrRawApiDto {
  Meta?: {
    CompanyId?: number;
    CompanyName?: string;
    Month?: string;
    GeneratedAt?: string;
  };
  meta?: {
    companyId?: number;
    companyName?: string;
    month?: string;
    generatedAt?: string;
  };
  Employees?: Array<{
    Id?: number;
    FirstName?: string;
    LastName?: string;
    Department?: string;
    StatusCode?: string;
    GenderCode?: string;
  }>;
  employees?: Array<{
    id?: number;
    firstName?: string;
    lastName?: string;
    department?: string;
    statusCode?: string;
    genderCode?: string;
  }>;
  Contracts?: Array<{
    EmployeeId?: number;
    StartDate?: string;
    EndDate?: string | null;
    Position?: string;
    ContractType?: string;
  }>;
  contracts?: Array<{
    employeeId?: number;
    startDate?: string;
    endDate?: string | null;
    position?: string;
    contractType?: string;
  }>;
  Salaries?: Array<{
    EmployeeId?: number;
    BaseSalary?: number;
    EffectiveDate?: string;
    EndDate?: string | null;
  }>;
  salaries?: Array<{
    employeeId?: number;
    baseSalary?: number;
    effectiveDate?: string;
    endDate?: string | null;
  }>;
}
