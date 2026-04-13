import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../services/auth.service';
import { EntraRedirectService } from '../../../services/entra-redirect.service';

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
    const result = await this.entraService.handleRedirectPromise();
    const account = result?.account ?? this.entraService.getCachedAccount();

    if (!account) {
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
      this.router.navigate(['/login'], { queryParams: { error: 'missing_claims' } });
      return;
    }

    this.authService.loginWithEntra(email, externalId).subscribe({
      next: () => {
        this.router.navigate(['/dashboard']);
      },
      error: (error) => {
        this.router.navigate(['/login'], { queryParams: { error: 'auth_failed' } });
      },
    });
  }
}
