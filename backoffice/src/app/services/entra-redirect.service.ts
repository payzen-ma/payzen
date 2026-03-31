import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { initializeMsal, loginRequest, msalInstance } from '../config/msal.config';
import { AccountInfo, AuthenticationResult, BrowserAuthError } from '@azure/msal-browser';

@Injectable({ providedIn: 'root' })
export class EntraRedirectService {
  private router = inject(Router);
  private static readonly INTERACTION_KEY = 'msal.interaction.status';

  private async ensureInitialized(): Promise<void> {
    console.log('[AUTH-FLOW][MSAL] initializeMsal start');
    await initializeMsal();
    console.log('[AUTH-FLOW][MSAL] initializeMsal done');
  }

  private isInteractionInProgress(): boolean {
    return sessionStorage.getItem(EntraRedirectService.INTERACTION_KEY) === 'interaction_in_progress';
  }

  private async safeLoginRedirect(
    request: Parameters<typeof msalInstance.loginRedirect>[0]
  ): Promise<void> {
    await this.ensureInitialized();

    try {
      console.log('[AUTH-FLOW][MSAL] loginRedirect request', request);
      await msalInstance.loginRedirect(request);
    } catch (error: unknown) {
      if (error instanceof BrowserAuthError && error.errorCode === 'interaction_in_progress') {
        console.warn('[AUTH-FLOW][MSAL] interaction already in progress');
        return;
      }
      console.error('[AUTH-FLOW][MSAL] loginRedirect fatal error', error);
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
      const rawHash = window.location.hash ?? '';
      const rawSearch = window.location.search ?? '';
      console.log('[AUTH-FLOW][MSAL] handleRedirectPromise input', {
        hashLength: rawHash.length,
        searchLength: rawSearch.length,
        hasCodeInHash: rawHash.includes('code='),
        hasStateInHash: rawHash.includes('state='),
        hasCodeInSearch: rawSearch.includes('code='),
        hasStateInSearch: rawSearch.includes('state='),
      });

      const result = await msalInstance.handleRedirectPromise();

      if (result?.account) {
        msalInstance.setActiveAccount(result.account);
        console.log('[AUTH-FLOW][MSAL] setActiveAccount', {
          username: result.account.username,
          localAccountId: result.account.localAccountId,
          homeAccountId: result.account.homeAccountId,
        });
      }
      console.log('[AUTH-FLOW][MSAL] handleRedirectPromise result', {
        hasResult: !!result,
        accountUsername: result?.account?.username,
        hasAccessToken: !!result?.accessToken,
        accessTokenPreview: result?.accessToken ? `${result.accessToken.slice(0, 20)}...` : null,
        hasIdToken: !!result?.idToken,
        idTokenPreview: result?.idToken ? `${result.idToken.slice(0, 20)}...` : null,
        expiresOn: result?.expiresOn?.toISOString?.() ?? null,
        tenantId: result?.tenantId ?? null,
        uniqueId: result?.uniqueId ?? null,
        scopes: result?.scopes ?? [],
      });
      return result;
    } catch (error: unknown) {
      const e = error as { errorCode?: string; errorMessage?: string };
      console.error('[AUTH-FLOW][MSAL] handleRedirectPromise fatal', {
        errorCode: e?.errorCode,
        errorMessage: e?.errorMessage,
        raw: error,
      });
      return null;
    }
  }

  getCachedAccount(): AccountInfo | null {
    const active = msalInstance.getActiveAccount();
    if (active) {
      console.log('[AUTH-FLOW][MSAL] getCachedAccount active', active.username);
      return active;
    }

    const accounts = msalInstance.getAllAccounts();
    console.log('[AUTH-FLOW][MSAL] getAllAccounts count', accounts.length);
    if (!accounts.length) return null;

    msalInstance.setActiveAccount(accounts[0]);
    console.log('[AUTH-FLOW][MSAL] setActiveAccount from cache', accounts[0].username);
    return accounts[0];
  }
}
