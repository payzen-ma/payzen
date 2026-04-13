import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { APP_INITIALIZER, ApplicationConfig, importProvidersFrom } from '@angular/core';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { routes } from './app.routes';
import { initializeMsal, msalInstance } from './core/config/msal.config';
import { authInterceptor } from './core/interceptors/auth.interceptor';
import { camelCaseInterceptor } from './core/interceptors/camelcase.interceptor';

import { MsalBroadcastService, MsalGuard, MsalModule, MsalService } from '@azure/msal-angular';
import { InteractionType } from '@azure/msal-browser';

import { environment } from '@environments/environment';
import { providePrimeNG } from 'primeng/config';
import { PayZenTheme } from '../assets/themes/payzen-theme';

export const appConfig: ApplicationConfig = {
  providers: [
    provideRouter(routes),

    provideHttpClient(
      withInterceptors([authInterceptor, camelCaseInterceptor])
    ),

    provideAnimationsAsync(),

    providePrimeNG({
      theme: {
        preset: PayZenTheme,
        options: {
          darkModeSelector: false,
          cssLayer: {
            name: 'primeng',
            order: 'tailwind-base, primeng, tailwind-utilities'
          }
        }
      }
    }),

    importProvidersFrom(
      TranslateModule.forRoot({
        fallbackLang: 'fr'
      }),

      // ✅ MSAL branché ici
      MsalModule.forRoot(
        msalInstance,
        // Guard config
        {
          interactionType: InteractionType.Redirect,
          authRequest: { scopes: environment.entra.scopes },
          loginFailedRoute: '/login'
        },
        // Interceptor config
        {
          interactionType: InteractionType.Redirect,
          protectedResourceMap: new Map([
            ['https://api-test.payzenhr.com/api/*', environment.entra.scopes],
            ['https://api-test.payzenhr.com/api', environment.entra.scopes]
          ])
        }
      )
    ),

    provideTranslateHttpLoader({
      prefix: '/assets/i18n/',
      suffix: '.json'
    }),

    // Services MSAL
    MsalGuard,
    MsalBroadcastService,
    MsalService,

    // ✅ Initialisation MSAL au démarrage
    {
      provide: APP_INITIALIZER,
      multi: true,
      useFactory: () => () => initializeMsal()
    }
  ],
};