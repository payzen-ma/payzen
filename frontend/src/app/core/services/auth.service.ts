import { Injectable, signal, computed, inject, Injector } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, BehaviorSubject, throwError } from 'rxjs';
import { tap, catchError, map } from 'rxjs/operators';
import { environment } from '@environments/environment';
import { 
  User, 
  UserRole,
  LoginRequest, 
  LoginResponse, 
  RegisterRequest,
  AuthState,
  ROLE_PERMISSIONS 
} from '@app/core/models/user.model';
import { CompanyMembership } from '@app/core/models/membership.model';
import { CompanyContextService } from './companyContext.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  // API endpoint
  private readonly API_URL = `${environment.apiUrl}/auth`;
  
  // Storage keys
  private readonly TOKEN_KEY = 'payzen_auth_token';
  private readonly USER_KEY = 'payzen_user';
  private readonly REFRESH_TOKEN_KEY = 'payzen_refresh_token';

  // Auth state signals
  private authStateSubject = new BehaviorSubject<AuthState>({
    user: this.getStoredUser(),
    token: this.getStoredToken(),
    isAuthenticated: !!this.getStoredToken(),
    isLoading: false,
    error: null
  });

  // Public observables
  authState$ = this.authStateSubject.asObservable();
  
  // Signals for reactive components
  currentUser = signal<User | null>(this.getStoredUser());
  isAuthenticated = signal<boolean>(!!this.getStoredToken());
  isLoading = signal<boolean>(false);
  hasSubordinates = signal<boolean>(false);

  // Computed signals
  userRole = computed(() => this.currentUser()?.role);
  isAdmin = computed(() => this.currentUser()?.role === UserRole.ADMIN || this.currentUser()?.role === UserRole.ADMIN_PAYZEN);
  isRH = computed(() => this.currentUser()?.role === UserRole.RH);
  isManager = computed(() => this.currentUser()?.role === UserRole.MANAGER);
  isEmployee = computed(() => this.currentUser()?.role === UserRole.EMPLOYEE);
  isCabinet = computed(() => this.currentUser()?.role === UserRole.CABINET);
  isAdminPayZen = computed(() => this.currentUser()?.role === UserRole.ADMIN_PAYZEN);
  isManagerWithTeam = computed(() => this.hasSubordinates());

  constructor(
    private http: HttpClient,
    private router: Router,
    private contextService: CompanyContextService
  ) {
    // Initialize auth state from storage
    this.initializeAuth();
  }

  /**
   * Initialize authentication state from storage
   */
  private initializeAuth(): void {
    const token = this.getStoredToken();
    const user = this.getStoredUser();
    
    // Validate token before restoring auth state
    if (token && user && this.isTokenValid(token)) {
      this.currentUser.set(user);
      this.isAuthenticated.set(true);
      this.updateAuthState({
        user,
        token,
        isAuthenticated: true,
        isLoading: false,
        error: null
      });
      // Check if user has subordinates
      this.checkSubordinates();
    } else {
      // Clear invalid/expired session
      this.clearStorage();
      this.currentUser.set(null);
      this.isAuthenticated.set(false);
    }
  }

  /**
   * Check if current user has subordinates (is a manager with team)
   */
  checkSubordinates(): void {
    const user = this.currentUser();
    if (!user || !user.employee_id) {
      this.hasSubordinates.set(false);
      return;
    }

    // Import EmployeeService dynamically to avoid circular dependency
    import('./employee.service').then(module => {
      const injector = inject(Injector);
      const employeeService = injector.get(module.EmployeeService);
      
      employeeService.getSubordinates(user.employee_id!).subscribe({
        next: (subordinates) => {
          this.hasSubordinates.set(subordinates.length > 0);
        },
        error: () => {
          this.hasSubordinates.set(false);
        }
      });
    }).catch(() => {
      this.hasSubordinates.set(false);
    });
  }

  /**
   * Check if JWT token is valid and not expired
   */
  private isTokenValid(token: string): boolean {
    try {
      const payload = this.decodeToken(token);
      if (!payload) return false;
      
      // Check expiration
      const exp = payload.exp;
      if (!exp) return true; // No expiration claim, assume valid
      
      const expirationDate = new Date(exp * 1000);
      const now = new Date();
      
      // Add 30 second buffer to account for clock skew
      return expirationDate.getTime() > now.getTime() + 30000;
    } catch {
      return false;
    }
  }

  /**
   * Decode JWT token payload
   */
  private decodeToken(token: string): any {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      
      const payload = parts[1];
      const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
      return JSON.parse(decoded);
    } catch {
      return null;
    }
  }

  /**
   * Login user
   */
  login(credentials: LoginRequest): Observable<LoginResponse> {
    this.isLoading.set(true);
    this.updateAuthState({ ...this.authStateSubject.value, isLoading: true, error: null });

    return this.http.post<any>(`${this.API_URL}/login`, credentials).pipe(
      map(response => this.normalizeLoginResponse(response)),
      tap(response => {
        this.handleLoginSuccess(response);
      }),
      catchError(error => {
        this.handleAuthError(error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Handle successful login
   */
  private handleLoginSuccess(response: LoginResponse): void {
    // Store tokens and user
    this.storeToken(response.token);
    this.storeUser(response.user);
    if (response.refreshToken) {
      this.storeRefreshToken(response.refreshToken);
    }

    // Update state
    this.currentUser.set(response.user);
    this.isAuthenticated.set(true);
    this.isLoading.set(false);
    
    this.updateAuthState({
      user: response.user,
      token: response.token,
      isAuthenticated: true,
      isLoading: false,
      error: null
    });

    // Helper to set memberships and navigate
    const proceedWithMemberships = (userWithName: User) => {
      const memberships = this.buildMembershipsFromUser(userWithName as User);
      this.contextService.setMemberships(memberships);

      if (memberships.length > 1) {
        this.router.navigate(['/select-context']);
      } else if (memberships.length === 1) {
        this.contextService.selectContext(memberships[0], true);
      } else {
        const defaultRoute = this.getRoleDefaultRoute(userWithName.role);
        this.router.navigate([defaultRoute]);
      }
    };

    // If backend did not provide companyName, try to fetch company details
    if (!response.user.companyName && response.user.companyId) {
      this.http.get<any>(`${environment.apiUrl}/companies/${response.user.companyId}`).subscribe({
        next: (companyDto) => {
          const name = companyDto?.companyName || companyDto?.legalName || companyDto?.name || `Company #${response.user.companyId}`;
          response.user.companyName = name;
          console.log('Fetched company name for user:', name);
          this.storeUser(response.user);
          this.currentUser.set(response.user);
          proceedWithMemberships(response.user as User);
        },
        error: () => {
          // Fallback if company fetch fails
          proceedWithMemberships(response.user as User);
        }
      });
    } else {
      proceedWithMemberships(response.user as User);
    }

    // Check for subordinates after successful login
    this.checkSubordinates();
  }

  /**
   * Build memberships array from user data
   * Uses isCabinetExpert flag from backend to determine expert mode
   */
  private buildMembershipsFromUser(user: User): CompanyMembership[] {
    const memberships: CompanyMembership[] = [];

    // If user has a companyId, create a membership
    if (user.companyId) {
      // Use isCabinetExpert from backend, fallback to role-based detection
      const isExpertCapable = user.isCabinetExpert ?? 
        (user.role === UserRole.CABINET || user.role === UserRole.ADMIN_PAYZEN);
      
      // If expert capable, add expert membership
      if (isExpertCapable) {
        memberships.push({
          companyId: user.companyId,
          companyName: user.companyName || this.getCompanyNameFromUser(user),
          role: UserRole.CABINET, // Use 'cabinet' role for expert mode context
          roleLabel: this.getRoleLabel(UserRole.CABINET),
          isExpertMode: true,
          permissions: user.permissions
        });
      }

      // Always add standard membership (employee view)
      memberships.push({
        companyId: user.companyId,
        companyName: user.companyName || this.getCompanyNameFromUser(user),
        role: user.role,
        roleLabel: this.getRoleLabel(user.role),
        isExpertMode: false,
        permissions: user.permissions
      });
    }

    // TODO: In the future, if backend supports multiple memberships,
    // parse them from a memberships array in the response
    // Example:
    // if (response.memberships?.length) {
    //   return response.memberships.map(m => ({
    //     companyId: m.companyId,
    //     companyName: m.companyName,
    //     role: m.role,
    //     isExpertMode: m.isCabinetExpert,
    //     permissions: m.permissions
    //   }));
    // }

    return memberships;
  }

  /**
   * Get company name from user or fallback to companyId
   */
  private getCompanyNameFromUser(user: User): string {
    // Prefer companyName from backend, fallback to Company #ID if not available
    if (user.companyName) {
      return user.companyName;
    }
    return user.companyId ? `Company #${user.companyId}` : 'Unknown Company';
  }

  /**
   * Get human-readable role label
   */
  private getRoleLabel(role: UserRole | string): string {
    const labels: Record<string, string> = {
      [UserRole.ADMIN]: 'Administrator',
      [UserRole.RH]: 'HR Manager',
      [UserRole.MANAGER]: 'Manager',
      [UserRole.EMPLOYEE]: 'Employee',
      [UserRole.CABINET]: 'Cabinet Expert',
      [UserRole.ADMIN_PAYZEN]: 'PayZen Admin'
    };
    return labels[role] || role;
  }

  /**
   * Register new user
   */
  register(data: RegisterRequest): Observable<LoginResponse> {
    this.isLoading.set(true);
    
    return this.http.post<any>(`${this.API_URL}/register`, data).pipe(
      map(response => this.normalizeLoginResponse(response)),
      tap(response => {
        this.handleLoginSuccess(response);
      }),
      catchError(error => {
        this.handleAuthError(error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Logout user
   */
  logout(): void {
    // Clear storage
    this.clearStorage();
    
    // Clear company context
    this.contextService.clearAll();
    
    // Reset state
    this.currentUser.set(null);
    this.isAuthenticated.set(false);
    this.isLoading.set(false);
    
    this.updateAuthState({
      user: null,
      token: null,
      isAuthenticated: false,
      isLoading: false,
      error: null
    });

    // Navigate to login
    this.router.navigate(['/login']);
  }

  /**
   * Refresh authentication token
   */
  refreshToken(): Observable<LoginResponse> {
    const refreshToken = this.getStoredRefreshToken();
    
    if (!refreshToken) {
      return throwError(() => new Error('No refresh token available'));
    }

    const cachedUser = this.currentUser();

    return this.http.post<any>(`${this.API_URL}/refresh`, { refreshToken }).pipe(
      map(response => this.normalizeLoginResponse(response, cachedUser)),
      tap(response => {
        this.storeToken(response.token);
        if (response.refreshToken) {
          this.storeRefreshToken(response.refreshToken);
        }
        this.storeUser(response.user);
        this.currentUser.set(response.user);
        this.updateAuthState({
          ...this.authStateSubject.value,
          user: response.user,
          token: response.token,
          isAuthenticated: true
        });
      }),
      catchError(error => {
        this.logout();
        return throwError(() => error);
      })
    );
  }

  /**
   * Check if user is authenticated
   */
  isUserAuthenticated(): boolean {
    return this.isAuthenticated();
  }

  /**
   * Get current user
   */
  getCurrentUser(): User | null {
    return this.currentUser();
  }

  /**
   * Get auth token
   */
  getToken(): string | null {
    return this.getStoredToken();
  }

  /**
   * Get auth headers
   */
  getAuthHeaders(): HttpHeaders {
    const token = this.getToken();
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    });
  }

  /**
   * Handle authentication errors
   */
  private handleAuthError(error: any): void {
    const errorMessage = error.error?.message || error.message || 'Authentication failed';
    
    this.isLoading.set(false);
    this.updateAuthState({
      ...this.authStateSubject.value,
      isLoading: false,
      error: errorMessage
    });
  }

  /**
   * Update auth state
   */
  private updateAuthState(state: AuthState): void {
    this.authStateSubject.next(state);
  }

  // Storage methods
  private storeToken(token: string): void {
    localStorage.setItem(this.TOKEN_KEY, token);
  }

  private storeUser(user: User): void {
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
  }

  private storeRefreshToken(token: string): void {
    localStorage.setItem(this.REFRESH_TOKEN_KEY, token);
  }

  private getStoredToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private getStoredUser(): User | null {
    const userJson = localStorage.getItem(this.USER_KEY);
    return userJson ? JSON.parse(userJson) : null;
  }

  private getStoredRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  private clearStorage(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
  }

  private normalizeLoginResponse(payload: any, fallbackUser: User | null = null): LoginResponse {
    const token = this.extractToken(payload);
    const refreshToken = this.extractRefreshToken(payload);
    const backendUser = payload?.user ?? payload?.User ?? null;

    let user: User | null = null;
    if (backendUser) {
      user = this.normalizeUserPayload(backendUser, fallbackUser);
    } else if (fallbackUser) {
      user = fallbackUser;
    }

    if (!user) {
      throw new Error('Invalid auth response: missing user payload');
    }

    return {
      user,
      token,
      refreshToken: refreshToken ?? undefined
    };
  }

  private extractToken(payload: any): string {
    const token = payload?.token ?? payload?.Token ?? null;
    if (!token) {
      throw new Error('Invalid auth response: missing token');
    }
    return String(token);
  }

  private extractRefreshToken(payload: any): string | null {
    const refreshToken = payload?.refreshToken ?? payload?.RefreshToken ?? null;
    return refreshToken ? String(refreshToken) : null;
  }

  private normalizeUserPayload(userRaw: any, fallbackUser: User | null = null): User {
    const permissions = Array.isArray(userRaw?.permissions ?? userRaw?.Permissions) ? (userRaw?.permissions ?? userRaw?.Permissions) : [];
    const rolesArray = Array.isArray(userRaw?.roles ?? userRaw?.Roles) ? (userRaw?.roles ?? userRaw?.Roles) : [];
    // Prefer 'admin' role if present in the roles array (regardless of order)
    let resolvedRole: any = null;
    if (rolesArray && rolesArray.length) {
      const foundAdmin = rolesArray.find((r: any) => {
        const s = (typeof r === 'string' ? r : (r?.name ?? r?.role ?? r?.code ?? r?.id ?? '')).toString().toLowerCase();
        return s === 'admin' || s === 'administrator';
      });
      if (foundAdmin) {
        resolvedRole = typeof foundAdmin === 'string' ? foundAdmin : (foundAdmin?.name ?? foundAdmin?.role ?? foundAdmin?.code ?? foundAdmin?.id);
      }
    }
    // Fallback to explicit role field or first role in array
    resolvedRole = resolvedRole ?? (userRaw?.role ?? userRaw?.Role ?? rolesArray[0]);
    
    // Try to get companyId from payload, fallback to existing user if missing
    const rawCompanyId = userRaw?.companyId ?? userRaw?.CompanyId;
    const companyId = this.normalizeString(rawCompanyId) ?? fallbackUser?.companyId;

    // Get companyName from backend response
    const companyName = this.normalizeString(userRaw?.companyName ?? userRaw?.CompanyName) ?? fallbackUser?.companyName;

    // Get isCabinetExpert flag from backend response
    const isCabinetExpert = userRaw?.isCabinetExpert ?? userRaw?.IsCabinetExpert ?? false;

    // Get employeeCategoryId from backend response
    const employeeCategoryId = userRaw?.employeeCategoryId ?? userRaw?.EmployeeCategoryId;

    // Get mode from backend response
    const mode = this.normalizeString(userRaw?.mode ?? userRaw?.Mode);

    return {
      id: this.normalizeString(userRaw?.id ?? userRaw?.Id) ?? '',
      email: this.normalizeString(userRaw?.email ?? userRaw?.Email) ?? '',
      username: this.normalizeString(userRaw?.username ?? userRaw?.Username) ?? '',
      firstName: this.normalizeString(userRaw?.firstName ?? userRaw?.FirstName) ?? '',
      lastName: this.normalizeString(userRaw?.lastName ?? userRaw?.LastName) ?? '',
      role: this.mapBackendRole(resolvedRole),
      roles: rolesArray,
      employee_id: this.normalizeString(userRaw?.employeeId ?? userRaw?.EmployeeId),
      companyId,
      companyName,
      isCabinetExpert,
      employeeCategoryId,
      mode,
      permissions
    };
  }

  private normalizeString(value: any): string | undefined {
    if (value === null || value === undefined) {
      return undefined;
    }
    return String(value);
  }

  private mapBackendRole(role?: string): UserRole {
    const normalized = (role ?? '').toLowerCase();
    const roleMap: Record<string, UserRole> = {
      admin: UserRole.ADMIN,
      rh: UserRole.RH,
      manager: UserRole.MANAGER,
      employee: UserRole.EMPLOYEE,
      cabinet: UserRole.CABINET,
      admin_payzen: UserRole.ADMIN_PAYZEN
    };
    return roleMap[normalized] ?? UserRole.EMPLOYEE;
  }

  /**
   * Check if user has specific permission
   */
  hasPermission(permission: keyof typeof ROLE_PERMISSIONS[UserRole]): boolean {
    const user = this.getCurrentUser();
    if (!user) return false;
    
    const rolePermissions = ROLE_PERMISSIONS[user.role as UserRole];
    return rolePermissions ? rolePermissions[permission] : false;
  }

  /**
   * Check if user has one of the specified roles
   */
  hasRole(roles: UserRole[]): boolean {
    const user = this.getCurrentUser();
    if (!user) return false;
    return roles.includes(user.role as UserRole);
  }

  /**
   * Resolve default route for a given role
   */
  getRoleDefaultRoute(role: string): string {
    const roleRoutes: Record<string, string> = {
      [UserRole.ADMIN]: '/dashboard',
      [UserRole.RH]: '/dashboard',
      [UserRole.MANAGER]: '/employees',
      [UserRole.EMPLOYEE]: '/my-profile',
      [UserRole.CABINET]: '/companies',
      [UserRole.ADMIN_PAYZEN]: '/admin/dashboard'
    };

    return roleRoutes[role] || '/dashboard';
  }
}

