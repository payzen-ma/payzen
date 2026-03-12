// Employee model for PayZen SaaS

export interface SalaryComponent {
  id?: number;
  employeeSalaryId?: number;
  type: string;
  amount: number;
  isTaxable?: boolean;
  /** Flag transient (non sauvegardé en base) : true = saisie libre hors catalogue */
  _custom?: boolean;
}

export interface Spouse {
  id?: number;
  employeeId?: number | string;
  employeeFirstName?: string;
  employeeLastName?: string;
  employeeFullName?: string;
  firstName?: string;
  lastName?: string;
  fullName?: string;
  dateOfBirth?: string;
  age?: number;
  genderId?: number | null;
  genderName?: string | null;
  cinNumber?: string | null;
  marriageDate?: string | null;
  isDependent?: boolean;
  createdAt?: string;
  modifiedAt?: string | null;
}

export interface Child {
  id?: number;
  employeeId?: number | string;
  employeeFirstName?: string;
  employeeLastName?: string;
  employeeFullName?: string;
  firstName?: string;
  lastName?: string;
  fullName?: string;
  dateOfBirth?: string;
  age?: number;
  genderId?: number | null;
  genderName?: string | null;
  isDependent?: boolean;
  isStudent?: boolean;
  createdAt?: string;
  modifiedAt?: string | null;
}

export interface EmployeeSalaryPackageAssignment {
  id: number;
  salaryPackageId: number;
  salaryPackageName: string;
  effectiveDate: string;
  endDate?: string | null;
  packageVersion: number;
  contractId: number;
  employeeSalaryId: number;
}

export interface Employee {
  id: string;
  firstName: string;
  lastName: string;
  photo?: string;
  cin: string;
  maritalStatus: 'single' | 'married' | 'divorced' | 'widowed';
  dateOfBirth: string;
  //birthPlace: string;
  professionalEmail: string;
  personalEmail: string;
  phone: string;
  address: string;
  countryId?: number;
  countryName?: string;
  cityId?: number;
  city?: string;
  addressLine1?: string;
  addressLine2?: string;
  zipCode?: string;
  position: string;
  genderId?: number | null;
  genderName?: string | null;
  department: string;
  manager?: string;
  contractType: string;
  startDate: string;
  endDate?: string;
  probationPeriod: string;
  exitReason?: string;
  baseSalary: number;
  /** Date d'effet du nouveau salaire, renseignée lors d'une modification pour créer un historique. */
  salaryEffectiveDate?: string | null;
  salaryComponents: SalaryComponent[];
  activeSalaryId?: number;
  paymentMethod: 'bank_transfer' | 'check' | 'cash';
  cnss: string;
  amo: string;
  cimr?: string;
  cimrEmployeeRate?: number | null;
  cimrCompanyRate?: number | null;
  hasPrivateInsurance?: boolean;
  privateInsuranceNumber?: string | null;
  privateInsuranceRate?: number | null;
  disableAmo?: boolean;
  annualLeave: number;
  employeeCategoryId?: number;
  employeeCategoryName?: string;
  /**
   * Normalized status used by parts of the UI (may be derived),
   * but primary status code comes from backend in `statusRaw`.
   */
  status: string;
  /** Raw status code returned by backend API (e.g. "ACTIVE", "RESIGNED") */
  statusRaw?: string;
  /** Localized status label returned by backend (NameFr/NameEn/NameAr) */
  statusName?: string;
  missingDocuments: number;
  companyId?: string;
  userId?: string;
  createdAt?: Date;
  updatedAt?: Date;
  events?: EmployeeEvent[];
  spouses?: Spouse[];
  children?: Child[];
  assignedPackage?: EmployeeSalaryPackageAssignment | null;
}

export interface EmployeeEvent {
  type: string;
  title: string;
  date: string;
  description: string;
  details?: any;
  modifiedBy?: {
    name: string;
    role: string;
  };
  timestamp: string;
}

// Backend employee response format (PascalCase from C#)
export interface BackendEmployee {
  Id: string | number;
  FirstName: string;
  LastName: string;
  Photo?: string;
  Cin: string;
  MaritalStatus: string;
  DateOfBirth: string;
  BirthPlace: string;
  ProfessionalEmail: string;
  PersonalEmail: string;
  Phone: string;
  Address: string;
  Position: string;
  DepartmentName: string;
  Manager?: string;
  ContractType: string;
  StartDate: string;
  EndDate?: string;
  ProbationPeriod: string;
  ExitReason?: string;
  BaseSalary: number;
  TransportAllowance: number;
  MealAllowance: number;
  SeniorityBonus: number;
  BenefitsInKind?: string;
  PaymentMethod: string;
  cnss: string;
  Amo: string;
  Cimr?: string;
  AnnualLeave: number;
  Status: string;
  MissingDocuments: number;
  CompanyId?: string | number;
  UserId?: string | number;
  CreatedAt?: string;
  UpdatedAt?: string;
}

// Create employee request
export interface CreateEmployeeRequest {
  firstName: string;
  lastName: string;
  photo?: string;
  cin: string;
  maritalStatus: 'single' | 'married' | 'divorced' | 'widowed';
  dateOfBirth: string;
  birthPlace: string;
  professionalEmail: string;
  personalEmail: string;
  phone: string;
  address: string;
  position: string;
  department: string;
  manager?: string;
  contractType: string;
  startDate: string;
  endDate?: string;
  probationPeriod: string;
  baseSalary: number;
  transportAllowance?: number;
  mealAllowance?: number;
  seniorityBonus?: number;
  benefitsInKind?: string;
  paymentMethod: 'bank_transfer' | 'check' | 'cash';
  cnss: string;
  amo: string;
  cimr?: string;
  annualLeave?: number;
  status?: string;
  companyId?: string;
  userId?: string;
}

// Update employee request
export interface UpdateEmployeeRequest {
  firstName?: string;
  lastName?: string;
  photo?: string;
  cin?: string;
  maritalStatus?: 'single' | 'married' | 'divorced' | 'widowed';
  dateOfBirth?: string;
  birthPlace?: string;
  professionalEmail?: string;
  personalEmail?: string;
  phone?: string;
  address?: string;
  position?: string;
  department?: string;
  manager?: string;
  contractType?: string;
  startDate?: string;
  endDate?: string;
  probationPeriod?: string;
  exitReason?: string;
  baseSalary?: number;
  transportAllowance?: number;
  mealAllowance?: number;
  seniorityBonus?: number;
  benefitsInKind?: string;
  paymentMethod?: 'bank_transfer' | 'check' | 'cash';
  cnss?: string;
  amo?: string;
  cimr?: string;
  annualLeave?: number;
  status?: string;
}

// Employee filters
export interface EmployeeFilters {
  searchQuery?: string;
  department?: string;
  status?: string;
  contractType?: string;
  companyId?: string;
}

// Employee list response
export interface EmployeeListResponse {
  employees: Employee[];
  total: number;
  page?: number;
  pageSize?: number;
}

// Employee document
export interface EmployeeDocument {
  id: string;
  employeeId: string;
  type: 'cin' | 'contract' | 'rib' | 'job_description' | 'diploma' | 'other';
  name: string;
  url: string;
  uploadDate: string;
  status: 'uploaded' | 'missing' | 'pending';
}

// Employee history event
export interface EmployeeHistoryEvent {
  id: string;
  employeeId: string;
  date: string;
  type: 'salary_change' | 'position_change' | 'contract_change' | 'note';
  title: string;
  description: string;
  author: string;
  createdAt: string;
}

