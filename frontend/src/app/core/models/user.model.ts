/**
 * User Roles - Based on PayZen Cahier des Charges
 */
export enum UserRole {
  ADMIN = 'admin',                    // 👑 Admin Société
  RH = 'rh',                          // 👩‍💼 RH / Payroll
  MANAGER = 'manager',                // 👔 Manager
  EMPLOYEE = 'employee',              // 👤 Salarié
  CABINET = 'cabinet',                // 📊 Cabinet Comptable
  ADMIN_PAYZEN = 'admin_payzen'       // ⚙️ Admin PayZen (Back-office)
}

/**
 * User Model
 */
export interface User {
  id: string;
  email: string;
  employee_id?: string;
  username: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  roles?: string[];
  companyId?: string;
  companyName?: string;           // Company name from backend (raison sociale)
  isCabinetExpert?: boolean;      // Expert mode flag from backend
  permissions?: string[];
  isActive?: boolean;             // Whether the user account is active
  employeeCategoryId?: number;    // Employee category (e.g., 1=presence, 2=absence)
  mode?: string;                  // Employee mode: 'Presence' or 'Absence'
}

/**
 * Login Request
 */
export interface LoginRequest {
  email: string;
  password: string;
  // rememberMe?: boolean;
}

/**
 * Login Response
 */
export interface LoginResponse {
  user: User;
  token: string;
  refreshToken?: string;
}

/**
 * Register Request
 */
export interface RegisterRequest {
  email: string;
  username: string;
  password: string;
  firstName?: string;
  lastName?: string;
  company?: string;
}

/**
 * Auth State
 */
export interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

/**
 * Role Permissions Matrix - V1 Simple
 */
export const ROLE_PERMISSIONS = {
  [UserRole.ADMIN]: {
    createEmployee: true,
    editEmployee: true,
    viewAllPayslips: true,
    viewOwnPayslips: true,
    runPayroll: true,
    approveLeave: false,
    requestLeave: false,
    editPersonalInfo: false,
    viewKPIs: true,
    importData: true
  },
  [UserRole.RH]: {
    createEmployee: true,
    editEmployee: true,
    viewAllPayslips: true,
    viewOwnPayslips: true,
    runPayroll: true,
    approveLeave: true,
    requestLeave: false,
    editPersonalInfo: false,
    viewKPIs: true,
    importData: true
  },
  [UserRole.MANAGER]: {
    createEmployee: false,
    editEmployee: false,
    viewAllPayslips: false,
    viewOwnPayslips: false,
    runPayroll: false,
    approveLeave: true,
    requestLeave: false,
    editPersonalInfo: false,
    viewKPIs: false,
    importData: false
  },
  [UserRole.EMPLOYEE]: {
    createEmployee: false,
    editEmployee: false,
    viewAllPayslips: false,
    viewOwnPayslips: true,
    runPayroll: false,
    approveLeave: false,
    requestLeave: true,
    editPersonalInfo: true,
    viewKPIs: false,
    importData: false
  },
  [UserRole.CABINET]: {
    createEmployee: true,
    editEmployee: true,
    viewAllPayslips: true,
    viewOwnPayslips: false,
    runPayroll: true,
    approveLeave: false,
    requestLeave: false,
    editPersonalInfo: false,
    viewKPIs: true,
    importData: true
  },
  [UserRole.ADMIN_PAYZEN]: {
    createEmployee: true,
    editEmployee: true,
    viewAllPayslips: true,
    viewOwnPayslips: true,
    runPayroll: true,
    approveLeave: true,
    requestLeave: false,
    editPersonalInfo: true,
    viewKPIs: true,
    importData: true
  }
};
