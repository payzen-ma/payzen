/**
 * Company Membership Model
 * Represents a user's membership/role in a company context
 */

/**
 * Represents a user's membership in a company with their role
 */
export interface CompanyMembership {
  companyId: string;
  companyName: string;
  role: string;
  roleLabel?: string;
  isExpertMode: boolean; // true = Cabinet Expert / Expert mode, false = Standard company
  permissions?: string[];
}

/**
 * The current selected context for the application session
 */
export interface AppContext {
  companyId: string;
  companyName: string;
  role: string;
  isExpertMode: boolean;
  isClientView?: boolean; // True if expert is viewing a client company
  cabinetId?: string; // ID of the cabinet (if in expert mode)
  permissions: string[];
  selectedAt: Date;
}

/**
 * Storage keys for context persistence
 */
export const CONTEXT_STORAGE_KEYS = {
  CURRENT_CONTEXT: 'payzen_app_context',
  MEMBERSHIPS: 'payzen_user_memberships'
} as const;
