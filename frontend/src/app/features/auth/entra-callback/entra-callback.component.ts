import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '@app/core/services/auth.service';
import { EntraRedirectService } from '@app/core/services/entra-redirect.service';
import { ToastService } from '@app/core/services/toast.service';
import type { AccountInfo } from '@azure/msal-browser';

/**
 * External ID + Google : `account.username` est souvent l’UPN `...@tenant.onmicrosoft.com` ;
 * l’e-mail invité réel est dans `idTokenClaims.email`.
 */
function resolveEmailFromMsalAccount(account: AccountInfo): string | null {
  const c = account.idTokenClaims as Record<string, unknown> | undefined;
  const direct = c?.['email'];
  if (typeof direct === 'string' && direct.includes('@')) {
    return direct.trim();
  }
  const list = c?.['emails'];
  if (Array.isArray(list) && typeof list[0] === 'string' && list[0].includes('@')) {
    return list[0].trim();
  }
  const un = account.username;
  if (typeof un === 'string' && un.includes('@')) {
    return un.trim();
  }
  return null;
}

function decodeJwtPayload(token: string): Record<string, unknown> | null {
  try {
    const parts = token.split('.');
    if (parts.length < 2) return null;

    const b64url = parts[1];
    const b64 = b64url.replace(/-/g, '+').replace(/_/g, '/');
    const padded = b64 + '='.repeat((4 - (b64.length % 4)) % 4);
    const json = atob(padded);
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}

@Component({
  selector: 'app-entra-callback',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="callback-container">
      <div class="loading-wrapper">
        <div class="spinner">
          <div class="spinner-ring"></div>
          <div class="spinner-ring"></div>
          <div class="spinner-ring"></div>
          <div class="spinner-dot"></div>
        </div>
        <p class="loading-text">Authentification en cours...</p>
        <p class="loading-subtext">Veuillez patienter</p>
      </div>
    </div>
  `,
  styles: [`
    .callback-container {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      background: #FFFFFF;
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
      position: relative;
      overflow: hidden;
    }

    .callback-container::before {
      content: '';
      position: absolute;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background:
        radial-gradient(ellipse 55% 50% at 62% 35%, rgba(42, 44, 224, 0.05) 0%, transparent 70%),
        radial-gradient(ellipse 35% 35% at 18% 68%, rgba(245, 98, 28, 0.04) 0%, transparent 70%);
      pointer-events: none;
    }

    .loading-wrapper {
      text-align: center;
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 24px;
      position: relative;
      z-index: 1;
    }

    .spinner {
      position: relative;
      width: 80px;
      height: 80px;
    }

    .spinner-ring {
      position: absolute;
      width: 100%;
      height: 100%;
      border: 4px solid transparent;
      border-radius: 50%;
      border-top-color: rgba(42, 44, 224, 0.6);
      animation: spin 1.2s linear infinite;
    }

    .spinner-ring:nth-child(1) {
      animation-delay: 0s;
      border-top-color: #2A2CE0;
    }

    .spinner-ring:nth-child(2) {
      animation-delay: 0.4s;
      border-right-color: rgba(42, 44, 224, 0.4);
    }

    .spinner-ring:nth-child(3) {
      animation-delay: 0.8s;
      border-bottom-color: rgba(245, 98, 28, 0.3);
    }

    .spinner-dot {
      position: absolute;
      width: 12px;
      height: 12px;
      background: #2A2CE0;
      border-radius: 50%;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      animation: pulse 1.2s ease-in-out infinite;
    }

    .loading-text {
      color: #090A13;
      font-size: 18px;
      font-weight: 600;
      margin: 0;
      letter-spacing: 0.5px;
    }

    .loading-subtext {
      color: #7C7E96;
      font-size: 14px;
      margin: 0;
      font-weight: 400;
    }

    @keyframes spin {
      0% {
        transform: rotate(0deg);
      }
      100% {
        transform: rotate(360deg);
      }
    }

    @keyframes pulse {
      0%, 100% {
        opacity: 1;
        transform: translate(-50%, -50%) scale(1);
      }
      50% {
        opacity: 0.5;
        transform: translate(-50%, -50%) scale(0.8);
      }
    }

    @media (max-width: 480px) {
      .spinner {
        width: 60px;
        height: 60px;
      }

      .spinner-ring {
        border-width: 3px;
      }

      .loading-text {
        font-size: 16px;
      }

      .loading-subtext {
        font-size: 12px;
      }
    }
  `]
})
export class EntraCallbackComponent implements OnInit {
  private router = inject(Router);
  private authService = inject(AuthService);
  private entraService = inject(EntraRedirectService);
  private toastService = inject(ToastService);

  async ngOnInit() {
    const result = await this.entraService.handleRedirectPromise();

    if (!result) {
      this.router.navigate(['/login']);
      return;
    }

    const claims = (result as any)?.idTokenClaims as Record<string, unknown> | undefined;
    const claimsKeys = Object.keys(claims ?? {});

    const lowerKeys = claimsKeys.map(k => k.toLowerCase());
    const companyLikeKeys = claimsKeys.filter((k, idx) =>
      lowerKeys[idx].includes('company') ||
      lowerKeys[idx].includes('org') ||
      lowerKeys[idx].includes('organisation') ||
      lowerKeys[idx].includes('organization')
    );

    const idToken = (result as any)?.idToken as string | undefined;
    if (idToken) {
      const payload = decodeJwtPayload(idToken);
      const payloadKeys = Object.keys(payload ?? {});
    }

    const account = result.account;
    const email = resolveEmailFromMsalAccount(account);
    const oid = account.idTokenClaims?.['oid'] as string;

    if (!email || !oid) {
      this.router.navigate(['/login'], {
        queryParams: { error: 'missing_claims' }
      });
      return;
    }

    // Persist externalId for post-onboarding refresh of JWT (companyId, roles, etc.)
    localStorage.setItem('payzen_entra_oid', oid);

    this.handlePostEntraLogin(email, oid);
  }

  private handlePostEntraLogin(email: string, oid: string): void {
    this.authService.loginWithEntra(email, oid, { skipNavigation: true }).subscribe({
      next: () => {
        const user = this.authService.getCurrentUser();
        const hasCompany = !!user?.companyId && Number(user.companyId) > 0;
        if (!hasCompany) {
          this.router.navigate(['/signup/company']);
          return;
        }

        const defaultRoute = this.authService.getRoleDefaultRoute(user.role);
        this.router.navigate([defaultRoute]);
      },
      error: (error: any) => {
        const errorMessage = this.extractErrorMessage(error);
        this.toastService.error(errorMessage || 'Erreur d\'authentification. Veuillez réessayer.');

        setTimeout(() => {
          this.router.navigate(['/login'], {
            queryParams: { error: 'auth_failed', message: errorMessage }
          });
        }, 1500);
      }
    });
  }

  /**
   * Extract a human readable message from various backend error shapes
   */
  private extractErrorMessage(error: any): string {
    try {
      const candidates = [
        error?.error?.Message,
        error?.error?.message,
        error?.error?.Error,
        error?.error?.error,
        error?.error?.details,
        error?.Message,
        error?.message,
        typeof error === 'string' ? error : null,
        error?.statusText
      ];

      for (const candidate of candidates) {
        if (candidate !== null && candidate !== undefined && String(candidate).trim() !== '') {
          return String(candidate).trim();
        }
      }

      // Fallback based on status code
      if (error?.status) {
        const status = error.status;
        if (status === 401) return 'Identifiants invalides.';
        if (status === 403) return 'Accès refusé. Vérifiez vos permissions.';
        if (status === 404) return 'Ressource non trouvée.';
        if (status === 409) return 'Cet utilisateur existe déjà.';
        if (status === 429) return 'Trop de tentatives. Veuillez réessayer plus tard.';
        if (status === 500) return 'Erreur serveur. Veuillez réessayer.';
        if (status === 503) return 'Service temporairement indisponible.';
      }

      return 'Une erreur est survenue lors de l\'authentification.';
    } catch {
      return 'Une erreur est survenue lors de l\'authentification.';
    }
  }
}