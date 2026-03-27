export interface EmployeeDashboardData {
  employeeId: number;
  employeeName: string;
  initials: string;
  role: string;
  department: string;
  contractType: string;
  matricule: string;
  manager: string;
  seniority: string;

  salaryNet: number;
  paidDate: string;

  leavesRemaining: number;
  leavesTotal: number;

  presenceDays: number;
  presenceTotal: number;

  extraHours: number;

  leavesDetails: LeaveDetail[];
  contractInfo: ContractInfo[];
  payslipDetails: PayslipDetail[];
  documents: EmployeeDocument[];
}

export interface LeaveDetail {
  label: string;
  remaining?: number;
  total?: number;
  colorClass: string;
  isText?: boolean;
  text?: string;
}

export interface ContractInfo {
  label: string;
  value: string;
  isTag?: boolean;
  tagColor?: string;
}

export interface PayslipDetail {
  label: string;
  value: string;
  type: 'normal' | 'deduction' | 'net';
}

export interface EmployeeDocument {
  title: string;
  subtitle: string;
  status: 'À venir' | 'Télécharger' | string;
}
