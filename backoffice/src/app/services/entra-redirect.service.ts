import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { initializeMsal, msalInstance } from '../config/msal.config';
import { AuthenticationResult, BrowserAuthError } from '@azure/msal-browser';

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
    await msalInstance.handleRedirectPromise();

    if (this.isInteractionInProgress()) {
      return;
    }

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
      scopes: ['openid', 'profile', 'email'],
      prompt: 'select_account',
    });
  }

  async handleRedirectPromise(): Promise<AuthenticationResult | null> {
    try {
      await this.ensureInitialized();
      return await msalInstance.handleRedirectPromise();
    } catch {
      this.router.navigate(['/login'], { queryParams: { error: 'auth_failed' } });
      return null;
    }
  }
}
