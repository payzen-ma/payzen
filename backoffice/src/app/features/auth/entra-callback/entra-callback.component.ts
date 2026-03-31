import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../../services/auth.service';
import { EntraRedirectService } from '../../../services/entra-redirect.service';
import { msalInstance } from '../../../config/msal.config';

@Component({
  selector: 'app-entra-callback',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="flex min-h-screen flex-col items-center justify-center gap-4 bg-slate-50">
      <div
        class="h-10 w-10 animate-spin rounded-full border-4 border-slate-200 border-t-blue-600"></div>
      <p class="text-sm font-medium text-slate-600">Authentification en cours...</p>
    </div>
  `,
})
export class EntraCallbackComponent implements OnInit {
  private router = inject(Router);
  private authService = inject(AuthService);
  private entraService = inject(EntraRedirectService);

  async ngOnInit(): Promise<void> {
    console.log('[AUTH-FLOW][CALLBACK] ngOnInit');
    // Debug : vérifier que MSAL reçoit bien la réponse OAuth (code/state) dans l'URL.
    // Si `hash` est vide ici, `handleRedirectPromise()` renverra souvent `null`.
    console.log('[AUTH-FLOW][CALLBACK] href=', window.location.href);
    console.log('[AUTH-FLOW][CALLBACK] hash=', window.location.hash);
    console.log('[AUTH-FLOW][CALLBACK] search=', window.location.search);
    const accountsBefore = msalInstance.getAllAccounts();
    console.log('[AUTH-FLOW][MSAL] callback accounts before handleRedirectPromise', {
      count: accountsBefore.length,
      usernames: accountsBefore.map((a) => a.username),
      homeAccountIds: accountsBefore.map((a) => a.homeAccountId),
    });

    const result = await this.entraService.handleRedirectPromise();
    const account = result?.account ?? this.entraService.getCachedAccount();
    const accountsAfter = msalInstance.getAllAccounts();
    console.log('[AUTH-FLOW][MSAL] callback accounts after handleRedirectPromise', {
      count: accountsAfter.length,
      usernames: accountsAfter.map((a) => a.username),
      homeAccountIds: accountsAfter.map((a) => a.homeAccountId),
      activeAccount: msalInstance.getActiveAccount()?.username ?? null,
    });

    console.log('[AUTH-FLOW][CALLBACK] handleRedirectPromise result=', result);
    console.log('[AUTH-FLOW][CALLBACK] resolved account=', account);

    if (!account) {
      console.warn('[AUTH-FLOW][CALLBACK] no account from result/cache -> redirect login(no_result)');
      this.router.navigate(['/login'], { queryParams: { error: 'no_result' } });
      return;
    }
    // Workforce peut retourner l'email et l'identifiant stable sous des clés différentes selon la config.
    // On fait donc un fallback sur plusieurs claims courants.
    const claims = account.idTokenClaims ?? {};
    const email =
      (account.username as string) ??
      (claims['email'] as string) ??
      (claims['preferred_username'] as string) ??
      (claims['upn'] as string);

    // "oid" (court) ou "http://schemas.microsoft.com/identity/claims/objectidentifier" (long).
    // En dernier recours, on utilise "sub" pour garantir un externalId stable.
    const externalId =
      (claims['oid'] as string) ??
      (claims['http://schemas.microsoft.com/identity/claims/objectidentifier'] as string) ??
      (claims['sub'] as string) ??
      account.localAccountId ??
      account.homeAccountId;

    if (!email || !externalId) {
      console.warn('[AUTH-FLOW][CALLBACK] missing claims for backend login', {
        email,
        externalId,
        username: account.username,
        localAccountId: account.localAccountId,
        homeAccountId: account.homeAccountId,
      });
      this.router.navigate(['/login'], { queryParams: { error: 'missing_claims' } });
      return;
    }

    console.log('[AUTH-FLOW][CALLBACK] call backend /auth/entra-login', {
      email,
      externalIdPreview: `${externalId}`.slice(0, 12),
    });
    this.authService.loginWithEntra(email, externalId).subscribe({
      next: () => {
        console.log('[AUTH-FLOW][CALLBACK] backend login success -> /dashboard');
        this.router.navigate(['/dashboard']);
      },
      error: (error) => {
        console.error('[AUTH-FLOW][CALLBACK] backend login failed -> /login(auth_failed)', error);
        this.router.navigate(['/login'], { queryParams: { error: 'auth_failed' } });
      },
    });
  }
}
