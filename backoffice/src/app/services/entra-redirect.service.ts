import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AccountInfo, AuthenticationResult, BrowserAuthError } from '@azure/msal-browser';
import { initializeMsal, loginRequest, msalInstance } from '../config/msal.config';

@Injectable({ providedIn: 'root' })
export class EntraRedirectService {
  private router = inject(Router);
  private static readonly INTERACTION_KEY = 'msal.interaction.status';

  private async ensureInitialized(): Promise<void> {
    await initializeMsal();
  }

  private isInteractionInProgress(): boolean {
    return sessionStorage.getItem(EntraRedirectService.INTERACTION_KEY) === 'interaction_in_progress';
  }

  private async safeLoginRedirect(
    request: Parameters<typeof msalInstance.loginRedirect>[0]
  ): Promise<void> {
    await this.ensureInitialized();

    try {
      await msalInstance.loginRedirect(request);
    } catch (error: unknown) {
      if (error instanceof BrowserAuthError && error.errorCode === 'interaction_in_progress') {
        return;
      }
      throw error;
    }
  }

  async loginWithEntra(): Promise<void> {
    await this.safeLoginRedirect({
      scopes: loginRequest.scopes,
      prompt: 'select_account',
      // Garde la reprise explicitement sur la page callback après le retour Entra.
      redirectStartPage: `${window.location.origin}/auth/callback`,
    });
  }

  async handleRedirectPromise(): Promise<AuthenticationResult | null> {
    try {
      await this.ensureInitialized();

      const result = await msalInstance.handleRedirectPromise();

      if (result?.account) {
        msalInstance.setActiveAccount(result.account);
      }
      return result;
    } catch (error: unknown) {
      const e = error as { errorCode?: string; errorMessage?: string };
      return null;
    }
  }

  getCachedAccount(): AccountInfo | null {
    const active = msalInstance.getActiveAccount();
    if (active) {
      return active;
    }

    const accounts = msalInstance.getAllAccounts();
    if (!accounts.length) return null;

    msalInstance.setActiveAccount(accounts[0]);
    return accounts[0];
  }
}
