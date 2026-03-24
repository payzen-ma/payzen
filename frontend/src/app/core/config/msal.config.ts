import { 
    BrowserCacheLocation, 
    IPublicClientApplication, 
    PublicClientApplication,
    LogLevel
  } from '@azure/msal-browser';
  import { environment } from '@environments/environment';
  
  export const msalConfig = {
    auth: {
      clientId: environment.entra.clientId,
      authority: environment.entra.authority,
      redirectUri: environment.entra.redirectUri,
      postLogoutRedirectUri: environment.entra.postLogoutRedirectUri,
      navigateToLoginRequestUrl: true,
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