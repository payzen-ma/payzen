import { Injectable, signal, computed, effect, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Subject } from 'rxjs';
import { 
  CompanyMembership, 
  AppContext, 
  CONTEXT_STORAGE_KEYS 
} from '@app/core/models/membership.model';

@Injectable({
  providedIn: 'root'
})
export class CompanyContextService {
  // Use inject() to avoid circular dependency issues
  private readonly router = inject(Router);
  
  // ============================================
  // SIGNALS - Reactive State Management
  // ============================================
  
  /** Current selected context (company + role) */
  private readonly _currentContext = signal<AppContext | null>(this.loadStoredContext());
  
  /** All available memberships for the logged-in user */
  private readonly _memberships = signal<CompanyMembership[]>(this.loadStoredMemberships());
  
  /** Loading state for context operations */
  private readonly _isLoading = signal<boolean>(false);

  // ============================================
  // CONTEXT CHANGE NOTIFICATION
  // ============================================
  
  /** Subject to emit when context changes - components can subscribe to refresh their data */
  private readonly _contextChanged$ = new Subject<{ companyId: string | null; isExpertMode: boolean }>();
  
  /** Public observable for components to subscribe to context changes */
  readonly contextChanged$ = this._contextChanged$.asObservable();

  // ============================================
  // PUBLIC COMPUTED SIGNALS
  // ============================================
  
  /** Read-only access to current context */
  readonly currentContext = this._currentContext.asReadonly();
  
  /** Read-only access to memberships */
  readonly memberships = this._memberships.asReadonly();
  
  /** Loading state */
  readonly isLoading = this._isLoading.asReadonly();
  
  /** Check if a context has been selected */
  readonly hasContext = computed(() => this._currentContext() !== null);
  
  /** Check if user has multiple memberships requiring selection */
  readonly requiresContextSelection = computed(() => this._memberships().length > 1);
  
  /** Current company ID from context */
  readonly companyId = computed(() => this._currentContext()?.companyId ?? null);
  
  /** Current role from context */
  readonly role = computed(() => this._currentContext()?.role ?? null);
  
  /** Check if current context is in Expert Mode */
  readonly isExpertMode = computed(() => this._currentContext()?.isExpertMode ?? false);

  /** Check if expert is viewing a client company */
  readonly isClientView = computed(() => this._currentContext()?.isClientView ?? false);
  
  /** Current company name */
  readonly companyName = computed(() => this._currentContext()?.companyName ?? null);
  
  /** Current permissions */
  readonly permissions = computed(() => this._currentContext()?.permissions ?? []);

  constructor() {
    // Effect to persist context changes to localStorage
    effect(() => {
      const context = this._currentContext();
      if (context) {
        localStorage.setItem(
          CONTEXT_STORAGE_KEYS.CURRENT_CONTEXT, 
          JSON.stringify(context)
        );
      } else {
        localStorage.removeItem(CONTEXT_STORAGE_KEYS.CURRENT_CONTEXT);
      }
    });
    
    // Effect to persist memberships
    effect(() => {
      const memberships = this._memberships();
      if (memberships.length > 0) {
        localStorage.setItem(
          CONTEXT_STORAGE_KEYS.MEMBERSHIPS,
          JSON.stringify(memberships)
        );
      } else {
        localStorage.removeItem(CONTEXT_STORAGE_KEYS.MEMBERSHIPS);
      }
    });
  }

  // ============================================
  // PUBLIC METHODS
  // ============================================

  /**
   * Switch context to a specific company ID
   * Wrapper that handles both Expert Client switching and standard context switching
   */
  switchContext(companyId: string): void {
    const current = this._currentContext();
    
    // If in expert mode, we are switching client view
    if (current?.isExpertMode) {
      // If switching to the cabinet itself (portfolio view)
      if (companyId === current.cabinetId) {
        this.resetToPortfolioContext();
        return;
      }
      
      // Otherwise switching to a client
      // We need the company name. If we don't have it, we might need to fetch it or pass it.
      // For now, we'll try to find it in the loaded companies if possible, or use a placeholder.
      // Ideally, this method should take a Company object or we fetch it.
      // Since we are calling this from the dashboard where we have the company object, 
      // we should probably update the signature or find a way to get the name.
      // For this implementation, we'll assume the caller might have passed the name or we use a placeholder.
      
      this.switchToClientContext({ id: companyId, legalName: 'Loading...' }, true);
    } else {
      // Standard mode switching (between memberships)
      const membership = this._memberships().find(m => m.companyId === companyId);
      if (membership) {
        this.selectContext(membership, true);
      }
    }
  }

  /**
   * Set available memberships for the user (called after login)
   * @param memberships - Array of company memberships
   */
  setMemberships(memberships: CompanyMembership[]): void {
    this._memberships.set(memberships);
  }

  /**
   * Select a context (company + role) from available memberships
   * Persists to localStorage and redirects to appropriate dashboard
   * @param membership - The selected membership
   * @param navigate - Whether to navigate after selection (default: true)
   */
  selectContext(membership: CompanyMembership, navigate: boolean = true): void {
    this._isLoading.set(true);

    const context: AppContext = {
      companyId: membership.companyId,
      companyName: membership.companyName,
      role: membership.role,
      isExpertMode: membership.isExpertMode,
      isClientView: false,
      cabinetId: membership.isExpertMode ? membership.companyId : undefined,
      permissions: membership.permissions ?? [],
      selectedAt: new Date()
    };

    this._currentContext.set(context);
    this._isLoading.set(false);

    // Notify subscribers about context change
    this._contextChanged$.next({
      companyId: context.companyId,
      isExpertMode: context.isExpertMode
    });

    if (navigate) {
      this.navigateToDashboard(membership.isExpertMode);
    }
  }

  /**
   * Switch context to a specific client company (for Expert mode)
   * Keeps the expert role but changes the active company
   * @param company - The client company to switch to
   * @param navigate - Whether to navigate after switching (default: false)
   */
  switchToClientContext(company: { id: string, legalName: string }, navigate: boolean = false): void {
    const current = this._currentContext();
    if (!current || !current.isExpertMode) {
      console.warn('Cannot switch to client context: Not in expert mode');
      return;
    }

    const newContext: AppContext = {
      ...current,
      companyId: company.id,
      companyName: company.legalName,
      isClientView: true,
      cabinetId: current.cabinetId || current.companyId, // Ensure cabinetId is preserved or set
      // Keep existing role and permissions
      selectedAt: new Date()
    };

    this._currentContext.set(newContext);
    
    // Notify subscribers about context change - this will trigger data refresh
    this._contextChanged$.next({
      companyId: newContext.companyId,
      isExpertMode: newContext.isExpertMode
    });
    
    // Optionally navigate to client view
    if (navigate) {
      this.router.navigate(['/expert/client-view']);
    }
  }

  /**
   * Reset context to the portfolio view (Cabinet context)
   */
  resetToPortfolioContext(): void {
    const current = this._currentContext();
    if (!current || !current.isExpertMode || !current.cabinetId) {
      return;
    }

    // Find the original membership to restore correct name and details if needed
    // For now, just resetting ID and flags is enough
    const newContext: AppContext = {
      ...current,
      companyId: current.cabinetId,
      // We might need to fetch the cabinet name if we don't have it stored separately
      // But usually we can just keep the current name or look it up
      // Let's try to find the membership
      isClientView: false,
      selectedAt: new Date()
    };

    // Try to restore company name from memberships
    const membership = this._memberships().find(m => m.companyId === current.cabinetId);
    if (membership) {
      newContext.companyName = membership.companyName;
    }

    this._currentContext.set(newContext);
    
    // Notify subscribers about context change
    this._contextChanged$.next({
      companyId: newContext.companyId,
      isExpertMode: newContext.isExpertMode
    });
  }

  /**
   * Navigate to the appropriate dashboard based on mode
   * @param isExpertMode - Whether to navigate to expert dashboard
   */
  navigateToDashboard(isExpertMode: boolean): void {
    const route = isExpertMode ? '/expert/dashboard' : '/app/dashboard';
    this.router.navigate([route]);
  }

  /**
   * Clear current context (but keep memberships)
   * Used when switching context
   */
  clearContext(): void {
    this._currentContext.set(null);
    localStorage.removeItem(CONTEXT_STORAGE_KEYS.CURRENT_CONTEXT);
  }

  /**
   * Full logout - clear all context and memberships
   * Called during logout flow
   */
  clearAll(): void {
    this._currentContext.set(null);
    this._memberships.set([]);
    localStorage.removeItem(CONTEXT_STORAGE_KEYS.CURRENT_CONTEXT);
    localStorage.removeItem(CONTEXT_STORAGE_KEYS.MEMBERSHIPS);
  }

  /**
   * Check if user has a specific permission in current context
   * @param permission - Permission to check
   */
  hasPermission(permission: string): boolean {
    return this.permissions().includes(permission);
  }

  /**
   * Get the default route based on current context
   */
  getDefaultRoute(): string {
    if (!this.hasContext()) {
      return '/select-context';
    }
    return this.isExpertMode() ? '/expert/dashboard' : '/app/dashboard';
  }

  /**
   * Auto-select context if user has only one membership
   * Returns true if auto-selected, false if manual selection required
   */
  autoSelectIfSingle(): boolean {
    const memberships = this._memberships();
    if (memberships.length === 1) {
      this.selectContext(memberships[0], false);
      return true;
    }
    return false;
  }

  // ============================================
  // PRIVATE METHODS
  // ============================================

  /**
   * Load stored context from localStorage
   */
  private loadStoredContext(): AppContext | null {
    try {
      const stored = localStorage.getItem(CONTEXT_STORAGE_KEYS.CURRENT_CONTEXT);
      if (stored) {
        const context = JSON.parse(stored) as AppContext;
        // Convert date string back to Date object
        context.selectedAt = new Date(context.selectedAt);
        return context;
      }
    } catch (error) {
      console.warn('Failed to load stored context:', error);
      localStorage.removeItem(CONTEXT_STORAGE_KEYS.CURRENT_CONTEXT);
    }
    return null;
  }

  /**
   * Load stored memberships from localStorage
   */
  private loadStoredMemberships(): CompanyMembership[] {
    try {
      const stored = localStorage.getItem(CONTEXT_STORAGE_KEYS.MEMBERSHIPS);
      if (stored) {
        return JSON.parse(stored) as CompanyMembership[];
      }
    } catch (error) {
      console.warn('Failed to load stored memberships:', error);
      localStorage.removeItem(CONTEXT_STORAGE_KEYS.MEMBERSHIPS);
    }
    return [];
  }

  // ============================================
  // LEGACY COMPATIBILITY (deprecated)
  // ============================================

  /** @deprecated Use selectContext() instead */
  setCompany(id: string, role: string): void {
    const membership: CompanyMembership = {
      companyId: id,
      companyName: 'Unknown',
      role: role,
      isExpertMode: false
    };
    this.selectContext(membership, false);
  }
}