import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { map, tap } from 'rxjs/operators';
import { Router } from '@angular/router';
import { interval } from 'rxjs';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  user: {
    id: number;
    email: string;
    firstName: string;
    lastName: string;
    role: string;
  };
}

export interface User {
  id: number;
  email: string;
  name: string;
  role: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private baseUrl = 'http://localhost:5119';
  private apiUrl = `${this.baseUrl}/api/auth`;
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    // Load user from localStorage on init
    const storedUser = localStorage.getItem('user');
    if (storedUser) {
      this.currentUserSubject.next(JSON.parse(storedUser));
    }

    // Periodically check token expiry (every 60s)
    interval(60 * 1000).subscribe(() => {
      const token = this.getToken();
      if (token && this.isTokenExpired(token)) {
        this.logout();
      }
    });
  }

  /**
   * Login with email and password
   * Only allows "admin payzen" role
   */
  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<any>(`${this.apiUrl}/login`, { email, password }).pipe(
      map(response => {
        // Log response for debugging
        console.log('API Login Response:', response);
        
        // Handle different response structures
        const userData = response.user || response.User || response;
        const token = response.token || response.Token || response.accessToken || response.AccessToken;
        
        if (!userData) {
          throw new Error('Format de réponse invalide: utilisateur manquant');
        }

        // Extract user properties (handle both camelCase and PascalCase)
        const userId = userData.id || userData.Id;
        const userEmail = userData.email || userData.Email;
        const firstName = userData.firstName || userData.FirstName || '';
        const lastName = userData.lastName || userData.LastName || '';
        const userRoles = userData.roles || userData.Roles || [];
        const userRole = userData.role || userData.Role || '';

        // Check if user has "admin payzen" role
        // Handle both array format (Roles: ['admin payzen']) and string format (role: 'admin payzen')
        const hasAdminRole = Array.isArray(userRoles) 
          ? userRoles.some((r: string) => r.toLowerCase() === 'admin payzen')
          : userRole.toLowerCase() === 'admin payzen';

        if (!hasAdminRole) {
          throw new Error('Accès refusé. Seuls les administrateurs PayZen peuvent se connecter.');
        }

        // Store token and user info
        if (token) {
          localStorage.setItem('auth_token', token);
        }
        
        const user: User = {
          id: userId,
          email: userEmail,
          name: `${firstName} ${lastName}`.trim() || userEmail,
          role: Array.isArray(userRoles) && userRoles.length > 0 ? userRoles[0] : userRole
        };
        
        localStorage.setItem('user', JSON.stringify(user));
        this.currentUserSubject.next(user);

        return { token, user: userData } as LoginResponse;
      })
    );
  }

  /**
   * Logout user
   */
  logout(skipApi = false, reason?: 'expired' | 'unauthorized'): void {
    const cleanup = () => {
      localStorage.removeItem('auth_token');
      localStorage.removeItem('user');
      this.currentUserSubject.next(null);
      if (reason) {
        this.router.navigate(['/login'], { queryParams: { reason } });
        return;
      }
      this.router.navigate(['/login']);
    };

    if (skipApi) {
      cleanup();
      return;
    }

    // Try to notify backend, but always cleanup afterwards
    this.http.post(`${this.apiUrl}/logout`, {}).subscribe({
      next: () => cleanup(),
      error: () => cleanup()
    });
  }

  /**
   * Check if user is authenticated
   */
  isAuthenticated(): boolean {
    const token = localStorage.getItem('auth_token');
    const user = this.getCurrentUser();
    return !!token && !!user && user.role.toLowerCase() === 'admin payzen';
  }

  /**
   * Get current user
   */
  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  /**
   * Get auth token
   */
  getToken(): string | null {
    return localStorage.getItem('auth_token');
  }

  /**
   * Check whether a JWT token is expired
   */
  isTokenExpired(token?: string | null): boolean {
    const t = token ?? this.getToken();
    if (!t) return true;
    try {
      const parts = t.split('.');
      if (parts.length !== 3) return true;
      const payload = JSON.parse(atob(parts[1]));
      const exp = payload.exp;
      if (!exp) return false; // no exp claim -> treat as non-expiring
      const now = Math.floor(Date.now() / 1000);
      return now >= exp;
    } catch (e) {
      console.warn('Failed to parse token for expiry check', e);
      return true;
    }
  }
}
