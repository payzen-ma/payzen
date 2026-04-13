import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, throwError } from 'rxjs';
import { map, tap, catchError } from 'rxjs/operators';
import { Router } from '@angular/router';
import { interval } from 'rxjs';
import { environment } from '@environments/environment';

export interface LoginResponse {
  token: string;
  user: {
    id: number;
    email: string;
    firstName: string;
    lastName: string;
    role: string;
    roles?: string[];
  };
}

export interface User {
  id: number;
  email: string;
  name: string;
  role: string;
  /** Tous les rôles JWT (utile pour le contrôle Admin Payzen). */
  roles?: string[];
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/auth`;
  private currentUserSubject = new BehaviorSubject<User | null>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(
    private http: HttpClient,
    private router: Router
  ) {
    const storedUser = localStorage.getItem('user');
    if (storedUser) {
      this.currentUserSubject.next(JSON.parse(storedUser));
    }

    interval(60 * 1000).subscribe(() => {
      const token = this.getToken();
      if (token && this.isTokenExpired(token)) {
        this.logout();
      }
    });
  }

  private static normalizeRoleList(userRaw: Record<string, unknown>): string[] {
    const roles = userRaw['roles'] ?? userRaw['Roles'];
    if (Array.isArray(roles)) {
      return roles.map((r) =>
        typeof r === 'string' ? r : String((r as { name?: string }).name ?? r)
      );
    }
    const single = userRaw['role'] ?? userRaw['Role'];
    return single ? [String(single)] : [];
  }

  private static hasAdminPayZenRole(roles: string[]): boolean {
    return roles.some((r) => r.toLowerCase().replace(/\s+/g, ' ') === 'admin payzen');
  }

  /**
   * Connexion backoffice : email + mot de passe → JWT PayZen (rôle Admin Payzen requis).
   */
  login(email: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<Record<string, unknown>>(`${this.apiUrl}/login`, { email, password })
      .pipe(
        map((response) => this.mapPasswordLoginResponse(response)),
        catchError((err) => {
          const msg = err?.error?.Message ?? err?.error?.message ?? err?.message;
          return throwError(() => new Error(msg || 'Connexion refusée'));
        })
      );
  }

  /**
   * Connexion via Microsoft Entra (MSAL) — conservé pour une évolution ultérieure du backoffice.
   */
  loginWithEntra(email: string, externalId: string): Observable<LoginResponse> {

    return this.http.post<Record<string, unknown>>(`${this.apiUrl}/entra-login`, { email, externalId }).pipe(
      map((response) => {
        return this.mapPayZenLoginResponse(response);
      }),
      catchError((err) => {
        const msg = err?.error?.Message ?? err?.error?.message ?? err?.message;
        return throwError(() => new Error(msg || 'Connexion refusée'));
      })
    );
  }

  private mapPasswordLoginResponse(response: Record<string, unknown>): LoginResponse {
    return this.mapPayZenLoginResponse(response);
  }

  private mapPayZenLoginResponse(response: Record<string, unknown>): LoginResponse {
    const userData = (response['user'] ?? response['User']) as Record<string, unknown> | undefined;
    const token = (response['token'] ?? response['Token']) as string | undefined;

    if (!userData || !token) {
      throw new Error('Réponse serveur invalide');
    }

    const roles = AuthService.normalizeRoleList(userData);
    if (!AuthService.hasAdminPayZenRole(roles)) {
      throw new Error('Accès refusé. Seuls les administrateurs PayZen peuvent accéder au backoffice.');
    }

    const userId = Number(userData['id'] ?? userData['Id']);
    const userEmail = String(userData['email'] ?? userData['Email'] ?? '');
    const firstName = String(userData['firstName'] ?? userData['FirstName'] ?? '');
    const lastName = String(userData['lastName'] ?? userData['LastName'] ?? '');
    const primaryRole = roles.find((r) => r.toLowerCase().includes('payzen')) ?? roles[0] ?? '';

    localStorage.setItem('auth_token', token);
    const user: User = {
      id: userId,
      email: userEmail,
      name: `${firstName} ${lastName}`.trim() || userEmail,
      role: primaryRole,
      roles,
    };
    localStorage.setItem('user', JSON.stringify(user));
    this.currentUserSubject.next(user);

    return {
      token,
      user: {
        id: userId,
        email: userEmail,
        firstName,
        lastName,
        role: primaryRole,
        roles,
      },
    };
  }

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

    this.http.post(`${this.apiUrl}/logout`, {}).subscribe({
      next: () => cleanup(),
      error: () => cleanup(),
    });
  }

  isAuthenticated(): boolean {
    const token = localStorage.getItem('auth_token');
    const user = this.getCurrentUser();
    if (!token || !user || this.isTokenExpired(token)) {
      return false;
    }
    const roles = user.roles?.length ? user.roles : [user.role];
    const isAdmin = AuthService.hasAdminPayZenRole(roles);
    return isAdmin;
  }

  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  getToken(): string | null {
    return localStorage.getItem('auth_token');
  }

  isTokenExpired(token?: string | null): boolean {
    const t = token ?? this.getToken();
    if (!t) return true;
    try {
      const parts = t.split('.');
      if (parts.length !== 3) return true;
      const payload = JSON.parse(atob(parts[1]));
      const exp = payload.exp;
      if (!exp) return false;
      const now = Math.floor(Date.now() / 1000);
      return now >= exp;
    } catch {
      return true;
    }
  }
}
