import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { initializeMsal, msalInstance } from '@app/core/config/msal.config';
import { AuthenticationResult, BrowserAuthError } from '@azure/msal-browser';

@Injectable({
  providedIn: 'root'
})
export class EntraRedirectService {
  private router = inject(Router);
  private static readonly INTERACTION_KEY = 'msal.interaction.status';
  /** À chaque appel : évite la navigation vers l’URL ayant lancé loginRedirect (ex. accept-invite). */
  private static readonly HANDLE_REDIRECT_OPTS = { navigateToLoginRequestUrl: false as const };

  private async ensureInitialized(): Promise<void> {
    await initializeMsal();
  }

  private isInteractionInProgress(): boolean {
    return sessionStorage.getItem(EntraRedirectService.INTERACTION_KEY) === 'interaction_in_progress';
  }

  private async safeLoginRedirect(request: Parameters<typeof msalInstance.loginRedirect>[0]): Promise<void> {
    await this.ensureInitialized();

    // Nettoie un éventuel state de redirect en attente avant de relancer une interaction.
    await msalInstance.handleRedirectPromise(EntraRedirectService.HANDLE_REDIRECT_OPTS);

    if (this.isInteractionInProgress()) {
      console.warn('MSAL interaction already in progress, skipping duplicate redirect.');
      return;
    }

    try {
      await msalInstance.loginRedirect(request);
    } catch (error: any) {
      // Évite de casser l'UI sur un double-clic ou un auto-trigger concurrent.
      if (error instanceof BrowserAuthError && error.errorCode === 'interaction_in_progress') {
        console.warn('MSAL interaction_in_progress ignored.');
        return;
      }
      throw error;
    }
  }

  /**
   * Redirect générique vers Entra (page de choix d'IdP).
   * Utilisé pour respecter PKCE sans construire l'URL authorize manuellement.
   */
  async loginWithEntra(): Promise<void> {
    await this.safeLoginRedirect({
      scopes: ['openid', 'profile', 'email'],
      prompt: 'select_account',
    });
  }

  /**
   * Inscription dans le tenant External ID (`prompt: 'create'`).
   * Utilisé pour les liens d'invitation et pour `/login?mode=signup` (landing).
   * Évite le message « This account does not exist in this organization » sur un simple sign-in.
   * @see https://learn.microsoft.com/en-us/entra/identity-platform/msal-js-prompt-behavior
   */
  async signupWithEntra(): Promise<void> {
    await this.safeLoginRedirect({
      scopes: ['openid', 'profile', 'email'],
      prompt: 'create',
    });
  }

  /** Invitation + compte Google (email invité @gmail). */
  async loginForInviteAcceptanceWithGoogle(): Promise<void> {
    await this.safeLoginRedirect({
      scopes: ['openid', 'profile', 'email'],
      prompt: 'create',
      extraQueryParameters: {
        identity_provider: 'Google',
      },
    });
  }

  /**
   * Redirect vers Microsoft Personal Accounts (@hotmail, @outlook, @live)
   * PKCE est automatiquement géré par MSAL v3+
   */
  async loginWithMicrosoft(): Promise<void> {
    await this.safeLoginRedirect({
      scopes: ['openid', 'profile', 'email'],
      prompt: 'select_account', // Force l'utilisateur à sélectionner un compte
      extraQueryParameters: { 
        domain_hint: 'consumers' // Force comptes personnels uniquement
      }
    });
  }

  /**
   * Redirect vers Google
   */
  async loginWithGoogle(): Promise<void> {
    await this.safeLoginRedirect({
      scopes: ['openid', 'profile', 'email'],
      prompt: 'select_account',
      extraQueryParameters: { 
        identity_provider: 'Google' 
      }
    });
  }

  /**
   * Gérer le retour du redirect Entra
   * IMPORTANT : handleRedirectPromise() gère automatiquement PKCE
   */
  async handleRedirectPromise(): Promise<AuthenticationResult | null> {
    try {
      await this.ensureInitialized();
      const result = await msalInstance.handleRedirectPromise(
        EntraRedirectService.HANDLE_REDIRECT_OPTS
      );
      
      if (result) {
        console.log('✅ Authentication successful:', result);
        const claims = (result as any)?.idTokenClaims as Record<string, unknown> | undefined;
        const keys = Object.keys(claims ?? {});
        console.log('[EntraRedirectService] idTokenClaims keys:', keys);
        console.log('[EntraRedirectService] idTokenClaims snapshot:', {
          email: claims?.['email'],
          name: claims?.['name'],
          preferred_username: claims?.['preferred_username'],
          oid: claims?.['oid'],
          companyName: (claims as any)?.['companyName'],
          company_name: (claims as any)?.['company_name'],
          organization: (claims as any)?.['organization'],
        });
        return result;
      }
      
      return null;
    } catch (error: unknown) {
      console.error('❌ Error handling redirect:', error);
      const err = error as { errorCode?: string; errorMessage?: string; message?: string };
      const msg = `${err?.errorMessage ?? ''} ${err?.message ?? ''}`;

      // AADSTS16000 : compte IdP (ex. Google) pas encore provisionné dans le tenant External ID — config portail, pas PKCE.
      if (msg.includes('AADSTS16000')) {
        console.warn(
          '[Entra] Ce compte Google n’existe pas encore dans le tenant Payzen HR. ' +
          'Dans le portail : External Identities → fournisseurs d’identité Google + user flow « Sign up and sign in » avec inscription, ' +
          'et lier l’app SPA. L’utilisateur doit compléter une première inscription dans ce tenant.'
        );
        await this.router.navigate(['/login'], {
          queryParams: { error: 'idp_user_not_in_tenant' },
        });
        return null;
      }

      if (err?.errorCode === 'invalid_request') {
        console.error(
          'invalid_request : lire le message AADSTS ci-dessus (souvent config tenant / user flow, pas PKCE).'
        );
      }

      await this.router.navigate(['/login'], {
        queryParams: { error: 'auth_failed' },
      });
      return null;
    }
  }

  /**
   * Logout
   */
  async logout(): Promise<void> {
    await this.ensureInitialized();
    const account = msalInstance.getAllAccounts()[0];
    if (account) {
      await msalInstance.logoutRedirect({
        account: account
      });
    } else {
      await msalInstance.logoutRedirect();
    }
  }

  /**
   * Obtenir les comptes authentifiés
   */
  getAllAccounts() {
    return msalInstance.getAllAccounts();
  }
}