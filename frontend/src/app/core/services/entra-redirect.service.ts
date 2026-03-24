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

  private async ensureInitialized(): Promise<void> {
    await initializeMsal();
  }

  private isInteractionInProgress(): boolean {
    return sessionStorage.getItem(EntraRedirectService.INTERACTION_KEY) === 'interaction_in_progress';
  }

  private async safeLoginRedirect(request: Parameters<typeof msalInstance.loginRedirect>[0]): Promise<void> {
    await this.ensureInitialized();

    // Nettoie un éventuel state de redirect en attente avant de relancer une interaction.
    await msalInstance.handleRedirectPromise();

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
      prompt: 'select_account'
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
      const result = await msalInstance.handleRedirectPromise();
      
      if (result) {
        console.log('✅ Authentication successful:', result);
        return result;
      }
      
      return null;
    } catch (error: any) {
      console.error('❌ Error handling redirect:', error);
      
      // Gérer les erreurs spécifiques
      if (error?.errorCode === 'invalid_request') {
        console.error('Invalid request - PKCE may be misconfigured');
      }
      
      this.router.navigate(['/login'], { 
        queryParams: { error: 'auth_failed' } 
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