import {
  BrowserCacheLocation,
  IPublicClientApplication,
  LogLevel,
  PublicClientApplication,
} from '@azure/msal-browser';
import { environment } from '@environments/environment';

/** Voir frontend msal.config : External ID = authority ciamlogin sans user flow dans l’URL. */
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

const entra = environment.entra as { knownAuthorities?: string[] };

export const msalConfig = {
  auth: {
    clientId: environment.entra.clientId,
    authority: buildEntraAuthority(),
    knownAuthorities: entra.knownAuthorities?.length
      ? entra.knownAuthorities
      : ['payzenhr.ciamlogin.com'],
    redirectUri: environment.entra.redirectUri,
    postLogoutRedirectUri: environment.entra.postLogoutRedirectUri,
    navigateToLoginRequestUrl: true,
  },
  cache: {
    cacheLocation: BrowserCacheLocation.LocalStorage,
    storeAuthStateInCookie: false,
  },
  system: {
    allowRedirectInIframe: false,
    loggerOptions: {
      logLevel: LogLevel.Warning,
      piiLoggingEnabled: false,
    },
  },
};

export const msalInstance: IPublicClientApplication = new PublicClientApplication(msalConfig);

let initPromise: Promise<void> | null = null;

export function initializeMsal(): Promise<void> {
  if (!initPromise) {
    initPromise = msalInstance.initialize();
  }
  return initPromise;
}
