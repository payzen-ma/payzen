import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import type { AccountInfo } from '@azure/msal-browser';
import { AuthService } from '@app/core/services/auth.service';
import { EntraRedirectService } from '@app/core/services/entra-redirect.service';
import { CommonModule } from '@angular/common';

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
      <div class="spinner"></div>
      <p>Authentification en cours...</p>
    </div>
  `
})
export class EntraCallbackComponent implements OnInit {
  private router = inject(Router);
  private authService = inject(AuthService);
  private entraService = inject(EntraRedirectService);

  async ngOnInit() {
    const result = await this.entraService.handleRedirectPromise();

    if (!result) {
      this.router.navigate(['/login']);
      return;
    }

    const claims = (result as any)?.idTokenClaims as Record<string, unknown> | undefined;
    const claimsKeys = Object.keys(claims ?? {});
    console.log('[EntraCallback] idTokenClaims keys:', claimsKeys);

    const lowerKeys = claimsKeys.map(k => k.toLowerCase());
    const companyLikeKeys = claimsKeys.filter((k, idx) =>
      lowerKeys[idx].includes('company') ||
      lowerKeys[idx].includes('org') ||
      lowerKeys[idx].includes('organisation') ||
      lowerKeys[idx].includes('organization')
    );

    console.log('[EntraCallback] company-like claim keys:', companyLikeKeys);
    console.log('[EntraCallback] idTokenClaims snapshot:', {
      email: claims?.['email'],
      name: claims?.['name'],
      preferred_username: claims?.['preferred_username'],
      oid: claims?.['oid'],
      // keep potential custom claims explicit (may be undefined)
      companyName: (claims as any)?.['companyName'],
      company_name: (claims as any)?.['company_name'],
      organization: (claims as any)?.['organization'],
      org: (claims as any)?.['org'],
    });

    const idToken = (result as any)?.idToken as string | undefined;
    if (idToken) {
      const payload = decodeJwtPayload(idToken);
      const payloadKeys = Object.keys(payload ?? {});
      console.log('[EntraCallback] idToken payload keys:', payloadKeys);
      console.log('[EntraCallback] idToken payload snapshot:', {
        name: payload?.['name'],
        companyName: (payload as any)?.['companyName'],
        company_name: (payload as any)?.['company_name'],
        organization: (payload as any)?.['organization'],
        email: payload?.['email'],
      });
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
      error: () => {
        this.router.navigate(['/login'], {
          queryParams: { error: 'auth_failed' }
        });
      }
    });
  }
}