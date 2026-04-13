import {
  BrowserCacheLocation,
  IPublicClientApplication,
  LogLevel,
  PublicClientApplication
} from '@azure/msal-browser';
import { environment } from '@environments/environment';

/**
 * Entra External ID (*.ciamlogin.com) : authority = https://{sous-domaine}.ciamlogin.com/{tenantId}
 * Pas de signUpSignInUserFlow — réservé à Azure AD B2C classique (*.b2clogin.com).
 * Le flux Sign-in est lié à l'app directement dans le portail External ID.
 */
function buildEntraAuthority(): string {
  const e = environment.entra as {
    authority: string;
    signUpSignInUserFlow?: string;
  };
  const base = e.authority.replace(/\/$/, '');
  const flow = e.signUpSignInUserFlow?.replace(/^\//, '');
  if (!flow) return base;
  return base.toLowerCase().endsWith(`/${flow.toLowerCase()}`) ? base : `${base}/${flow}`;
}

const entra = environment.entra as {
  knownAuthorities?: string[];
};

export const msalConfig = {
  auth: {
    clientId: environment.entra.clientId,
    authority: buildEntraAuthority(),
    knownAuthorities: entra.knownAuthorities?.length
      ? entra.knownAuthorities
      : ['payzenhrtest.ciamlogin.com'],
    redirectUri: environment.entra.redirectUri,
    postLogoutRedirectUri: environment.entra.postLogoutRedirectUri,
    navigateToLoginRequestUrl: false,
  },
  cache: {
    cacheLocation: BrowserCacheLocation.LocalStorage,
    storeAuthStateInCookie: false,
  },
  system: {
    allowRedirectInIframe: false,
    loggerOptions: {
      logLevel: LogLevel.Warning,
      loggerCallback: (level: LogLevel, message: string, containsPii: boolean) => {
        if (containsPii) return;
      },
      piiLoggingEnabled: false
    }
  }
};

export const msalInstance: IPublicClientApplication = new PublicClientApplication(msalConfig);

let msalInitializationPromise: Promise<void> | null = null;

export function initializeMsal(): Promise<void> {
  if (!msalInitializationPromise) {
    msalInitializationPromise = msalInstance.initialize();
  }
  return msalInitializationPromise;
}