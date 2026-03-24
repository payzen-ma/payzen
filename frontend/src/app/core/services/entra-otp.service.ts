import { Injectable } from '@angular/core';
import { PublicClientApplication, RedirectRequest, AuthenticationResult } from '@azure/msal-browser';
import { environment } from '@environments/environment';

@Injectable({
  providedIn: 'root'
})
export class EntraOtpService {
  private msalInstance: PublicClientApplication;
  // Une seule Promise qui couvre initialize() + handleRedirectPromise().
  // Toutes les méthodes l'attendent — garantit l'ordre quelles que soient
  // les instanciations lazy ou le timing du APP_INITIALIZER.
  private readonly ready: Promise<AuthenticationResult | null>;

  constructor() {
    const entra = environment.entra;
    console.debug('[EntraOtpService] ctor - authority:', entra.authority);

    this.msalInstance = new PublicClientApplication({
      auth: {
        clientId: entra.clientId,
        authority: entra.authority,
        knownAuthorities: entra.knownAuthorities
      },
      cache: {
        cacheLocation: 'sessionStorage',
        storeAuthStateInCookie: false
      }
    });

    this.ready = this.msalInstance
      .initialize()
      .then(() => {
        console.debug('[EntraOtpService] initialized, calling handleRedirectPromise');
        return this.msalInstance.handleRedirectPromise();
      })
      .then((result) => {
        console.debug('[EntraOtpService] handleRedirectPromise result:', result);
        return result;
      });
  }

  /**
   * Appelé par APP_INITIALIZER — attend simplement que la Promise ready se résolve.
   */
  async initializeAndConsumeRedirect(): Promise<void> {
    await this.ready;
  }

  /**
   * Lance la redirection vers le user flow Entra External ID.
   * @param idpHint 'google' | 'microsoft' | undefined — force l'IdP sans écran de sélection Entra.
   */
  async redirectToSignIn(idpHint?: 'google' | 'microsoft'): Promise<void> {
    await this.ready;
    console.debug('[EntraOtpService] redirectToSignIn idpHint:', idpHint);

    const extraQueryParameters: Record<string, string> = {};
    if (idpHint === 'google') {
      extraQueryParameters['domain_hint'] = 'google.com';
    } else if (idpHint === 'microsoft') {
      extraQueryParameters['domain_hint'] = 'organizations';
    }

    const request: RedirectRequest = {
      scopes: environment.entra.scopes,
      redirectUri: `${window.location.origin}/auth/callback`,
      ...(Object.keys(extraQueryParameters).length > 0 && { extraQueryParameters })
    };
    console.debug('[EntraOtpService] loginRedirect request:', request);

    await this.msalInstance.loginRedirect(request);
  }

  /**
   * Appelé par EntraCallbackComponent sur /auth/callback.
   * Attend la même Promise ready — retourne le résultat MSAL du redirect.
   */
  async handleRedirect(): Promise<AuthenticationResult | null> {
    return this.ready;
  }
}

