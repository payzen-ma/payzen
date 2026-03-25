import { Component, OnInit, inject } from '@angular/core';
import { Router } from '@angular/router';
import type { AccountInfo } from '@azure/msal-browser';
import { AuthService } from '@app/core/services/auth.service';
import { EntraRedirectService } from '@app/core/services/entra-redirect.service';
import { InvitationService } from '@app/core/services/invitation.service';
import { switchMap } from 'rxjs/operators';
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
  private invitationService = inject(InvitationService);

  async ngOnInit() {
    const result = await this.entraService.handleRedirectPromise();

    if (!result) {
      this.router.navigate(['/login']);
      return;
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

    // Cas invitation : token présent dans sessionStorage
    const invitationToken = sessionStorage.getItem('pending_invitation_token');

    if (invitationToken) {
      sessionStorage.removeItem('pending_invitation_token');
      this.handleInvitationFlow(invitationToken, email, oid);
      return;
    }

    // Cas connexion normale
    this.handleNormalLogin(email, oid);
  }

  private handleNormalLogin(email: string, oid: string): void {
    this.authService.loginWithEntra(email, oid).subscribe({
      next: () => {},
      error: () => {
        this.router.navigate(['/login'], {
          queryParams: { error: 'auth_failed' }
        });
      }
    });
  }

  private handleInvitationFlow(token: string, email: string, oid: string): void {
    this.authService
      .loginWithEntra(email, oid, { skipNavigation: true })
      .pipe(
        switchMap(() => this.invitationService.acceptViaIdp(token)),
        switchMap(() => this.authService.loginWithEntra(email, oid))
      )
      .subscribe({
      next: () => {},
      error: (err) => {
        if (err?.error?.Code === 'EMAIL_MISMATCH') {
          this.router.navigate(['/auth/accept-invite'], {
            queryParams: {
              token,
              error: 'email_mismatch',
              message: err.error.Message
            }
          });
        } else {
          this.router.navigate(['/login'], {
            queryParams: { error: 'invite_failed' }
          });
        }
      }
    });
  }
}