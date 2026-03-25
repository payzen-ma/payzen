import { 
    BrowserCacheLocation, 
    IPublicClientApplication, 
    PublicClientApplication,
    LogLevel
  } from '@azure/msal-browser';
  import { environment } from '@environments/environment';

  /**
   * Entra External ID (*.ciamlogin.com) : authority = https://{sous-domaine}.ciamlogin.com
   * (sans /tenant.onmicrosoft.com ni nom de user flow). Le flux « Sign up and sign in » est
   * lié à l’app dans le portail (External Identities → User flows → ajouter l’application).
   *
   * Option signUpSignInUserFlow : réservé à un locataire Azure AD B2C *classique* (*.b2clogin.com),
   * pas à External ID — sinon MSAL appelle .../b2c_1_.../v2.0/.well-known/... qui peut échouer (CORS / 404).
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
        : ['payzenhr.ciamlogin.com'],
      redirectUri: environment.entra.redirectUri,
      postLogoutRedirectUri: environment.entra.postLogoutRedirectUri,
      // false : le redirect OAuth doit rester sur redirectUri (/auth/callback), pas l’URL
      // courante (ex. /auth/accept-invite). Sinon après sign-up MS renvoie sur accept-invite,
      // le composant relance signupWithEntra() → boucle infinie sur l’écran de création.
      navigateToLoginRequestUrl: false,
    },
    cache: {
      cacheLocation: BrowserCacheLocation.LocalStorage,
      storeAuthStateInCookie: false, // Mettre à true si vous supportez IE11
    },
    system: {
      allowRedirectInIframe: false,
      loggerOptions: {
        logLevel: LogLevel.Info,
        loggerCallback: (level: LogLevel, message: string, containsPii: boolean) => {
          if (containsPii) return;
          switch (level) {
            case LogLevel.Error:
              console.error(message);
              break;
            case LogLevel.Info:
              console.info(message);
              break;
            case LogLevel.Verbose:
              console.debug(message);
              break;
            case LogLevel.Warning:
              console.warn(message);
              break;
          }
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